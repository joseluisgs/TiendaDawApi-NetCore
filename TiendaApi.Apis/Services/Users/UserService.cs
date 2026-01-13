using System.ComponentModel.DataAnnotations;
using CSharpFunctionalExtensions;
using TiendaApi.Apis.Dtos.Usuarios;
using TiendaApi.Apis.Errors;
using TiendaApi.Apis.Mappers;
using TiendaApi.Apis.Models;
using TiendaApi.Apis.Repositories.Usuarios;

namespace TiendaApi.Apis.Services.Users;

/// <summary>
/// Servicio de usuarios usando Patrón Result.
/// Maneja las operaciones CRUD de usuarios con Programación Orientada al Resultado.
/// </summary>
public class UserService(
    IUserRepository userRepository,
    ILogger<UserService> logger
) : IUserService
{

    /// <summary>
    /// Obtiene todos los usuarios (excluyendo eliminados).
    /// Returns: Result.Success(List) | Result.Failure nunca
    /// </summary>
    public async Task<Result<IEnumerable<UserDto>, DomainError>> FindAllAsync()
    {
        logger.LogInformation("Obteniendo todos los usuarios");

        var users = await userRepository.FindAllAsync();

        var activeUsers = users.Where(u => !u.IsDeleted);

        var dtos = activeUsers.ToDtoList();

        return Result.Success<IEnumerable<UserDto>, DomainError>(dtos);
    }

    /// <summary>
    /// Obtiene un usuario por su ID.
    /// Returns: Result.Success(UserDto) | Result.Failure(NotFound)
    /// </summary>
    public async Task<Result<UserDto, DomainError>> FindByIdAsync(long id)
    {
        logger.LogInformation("Buscando usuario con id: {Id}", id);

        var user = await userRepository.FindByIdAsync(id);

        if (user == null || user.IsDeleted)
        {
            logger.LogWarning("Usuario con id {Id} no encontrado", id);
            return Result.Failure<UserDto, DomainError>(
                DomainError.NotFound($"Usuario con ID {id} no encontrado")
            );
        }

        var dto = user.ToDto();

        return Result.Success<UserDto, DomainError>(dto);
    }

    /// <summary>
    /// Crea un nuevo usuario.
    /// Returns: Result.Success(UserDto) | Result.Failure(Validation/Conflict)
    /// </summary>
    public async Task<Result<UserDto, DomainError>> CreateAsync(RegisterDto dto)
    {
        logger.LogInformation("Creando usuario: {Username}", dto.Username);

        var validationResult = ValidateRegistration(dto);
        if (validationResult.IsFailure)
        {
            return CSharpFunctionalExtensions.Result.Failure<UserDto, DomainError>(validationResult.Error);
        }

        var duplicateCheck = await CheckDuplicatesAsync(dto.Username, dto.Email, excludeUserId: null);
        if (duplicateCheck.IsFailure)
        {
            return CSharpFunctionalExtensions.Result.Failure<UserDto, DomainError>(duplicateCheck.Error);
        }

        var passwordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password, workFactor: 11);

        var user = new User
        {
            Username = dto.Username,
            Email = dto.Email,
            PasswordHash = passwordHash,
            Role = UserRoles.USER,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var savedUser = await userRepository.SaveAsync(user);

        logger.LogInformation("Usuario creado con id: {Id}", savedUser.Id);

        var resultDto = savedUser.ToDto();

        return Result.Success<UserDto, DomainError>(resultDto);
    }

    /// <summary>
    /// Actualiza un usuario existente.
    /// Returns: Result.Success(UserDto) | Result.Failure(NotFound/Validation/Conflict)
    /// </summary>
    public async Task<Result<UserDto, DomainError>> UpdateAsync(long id, UserUpdateDto dto)
    {
        logger.LogInformation("Actualizando usuario con id: {Id}", id);

        var user = await userRepository.FindByIdAsync(id);

        if (user == null || user.IsDeleted)
        {
            logger.LogWarning("Usuario con id {Id} no encontrado para actualizar", id);
            return Result.Failure<UserDto, DomainError>(
                DomainError.NotFound($"Usuario con ID {id} no encontrado")
            );
        }

        var validationResult = ValidateUpdate(dto);
        if (validationResult.IsFailure)
        {
            return CSharpFunctionalExtensions.Result.Failure<UserDto, DomainError>(validationResult.Error);
        }

        if (!string.IsNullOrWhiteSpace(dto.Email) && dto.Email != user.Email)
        {
            var duplicateCheck = await CheckDuplicatesAsync(null, dto.Email, excludeUserId: id);
            if (duplicateCheck.IsFailure)
            {
                return CSharpFunctionalExtensions.Result.Failure<UserDto, DomainError>(duplicateCheck.Error);
            }

            user.Email = dto.Email;
        }

        if (!string.IsNullOrWhiteSpace(dto.Password))
        {
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password, workFactor: 11);
        }

        user.UpdatedAt = DateTime.UtcNow;

        var updated = await userRepository.UpdateAsync(user);

        logger.LogInformation("Usuario actualizado con id: {Id}", id);

        var resultDto = updated.ToDto();

        return Result.Success<UserDto, DomainError>(resultDto);
    }

    /// <summary>
    /// Actualiza el avatar de un usuario.
    /// Returns: Result.Success(UserDto) | Result.Failure(NotFound/Validation)
    /// </summary>
    public async Task<Result<UserDto, DomainError>> UpdateAvatarAsync(long id, string avatarUrl)
    {
        logger.LogInformation("Actualizando avatar de usuario con id: {Id}", id);

        var user = await userRepository.FindByIdAsync(id);

        if (user == null || user.IsDeleted)
        {
            logger.LogWarning("Usuario con id {Id} no encontrado para actualizar avatar", id);
            return Result.Failure<UserDto, DomainError>(
                DomainError.NotFound($"Usuario con ID {id} no encontrado")
            );
        }

        if (string.IsNullOrWhiteSpace(avatarUrl))
        {
            user.Avatar = User.AVATAR_DEFAULT;
        }
        else
        {
            user.Avatar = avatarUrl;
        }

        user.UpdatedAt = DateTime.UtcNow;

        var updated = await userRepository.UpdateAsync(user);

        logger.LogInformation("Avatar actualizado para usuario con id: {Id}", id);

        var resultDto = updated.ToDto();

        return Result.Success<UserDto, DomainError>(resultDto);
    }

    /// <summary>
    /// Elimina un usuario (soft delete).
    /// Returns: UnitResult.Success | UnitResult.Failure(NotFound)
    /// </summary>
    public async Task<UnitResult<DomainError>> DeleteAsync(long id)
    {
        logger.LogInformation("Eliminando usuario con id: {Id}", id);

        var user = await userRepository.FindByIdAsync(id);

        if (user == null || user.IsDeleted)
        {
            logger.LogWarning("Usuario con id {Id} no encontrado para eliminar", id);
            return UnitResult.Failure<DomainError>(
                DomainError.NotFound($"Usuario con ID {id} no encontrado")
            );
        }

        user.IsDeleted = true;
        user.UpdatedAt = DateTime.UtcNow;

        await userRepository.UpdateAsync(user);

        logger.LogInformation("Usuario eliminado lógicamente con id: {Id}", id);

        return UnitResult.Success<DomainError>();
    }

    /// <summary>
    /// Valida los datos de registro de un usuario.
    /// Returns: UnitResult.Success | UnitResult.Failure(Validation)
    /// </summary>
    private UnitResult<DomainError> ValidateRegistration(RegisterDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Username))
        {
            return UnitResult.Failure<DomainError>(
                DomainError.Validation("El nombre de usuario es requerido")
            );
        }

        if (dto.Username.Length < 3)
        {
            return UnitResult.Failure<DomainError>(
                DomainError.Validation("El nombre de usuario debe tener al menos 3 caracteres")
            );
        }

        if (string.IsNullOrWhiteSpace(dto.Email))
        {
            return UnitResult.Failure<DomainError>(
                DomainError.Validation("El email es requerido")
            );
        }

        if (!new EmailAddressAttribute().IsValid(dto.Email))
        {
            return UnitResult.Failure<DomainError>(
                DomainError.Validation("El email no es válido")
            );
        }

        if (string.IsNullOrWhiteSpace(dto.Password))
        {
            return UnitResult.Failure<DomainError>(
                DomainError.Validation("La contraseña es requerida")
            );
        }

        if (dto.Password.Length < 6)
        {
            return UnitResult.Failure<DomainError>(
                DomainError.Validation("La contraseña debe tener al menos 6 caracteres")
            );
        }

        return UnitResult.Success<DomainError>();
    }

    /// <summary>
    /// Valida los datos de actualización de un usuario.
    /// Returns: UnitResult.Success | UnitResult.Failure(Validation)
    /// </summary>
    private UnitResult<DomainError> ValidateUpdate(UserUpdateDto dto)
    {
        if (!string.IsNullOrWhiteSpace(dto.Email))
        {
            if (!new EmailAddressAttribute().IsValid(dto.Email))
            {
                return UnitResult.Failure<DomainError>(
                    DomainError.Validation("El email no es válido")
                );
            }
        }

        if (!string.IsNullOrWhiteSpace(dto.Password))
        {
            if (dto.Password.Length < 6)
            {
                return UnitResult.Failure<DomainError>(
                    DomainError.Validation("La contraseña debe tener al menos 6 caracteres")
                );
            }
        }

        return UnitResult.Success<DomainError>();
    }

    /// <summary>
    /// Verifica duplicados de username y email.
    /// Returns: UnitResult.Success | UnitResult.Failure(Conflict)
    /// </summary>
    private async Task<UnitResult<DomainError>> CheckDuplicatesAsync(
        string? username,
        string? email,
        long? excludeUserId)
    {
        if (!string.IsNullOrWhiteSpace(username))
        {
            var existingUser = await userRepository.FindByUsernameAsync(username);
            if (existingUser != null && existingUser.Id != excludeUserId)
            {
                return UnitResult.Failure<DomainError>(
                    DomainError.Conflict("El nombre de usuario ya existe")
                );
            }
        }

        if (!string.IsNullOrWhiteSpace(email))
        {
            var existingEmail = await userRepository.FindByEmailAsync(email);
            if (existingEmail != null && existingEmail.Id != excludeUserId)
            {
                return UnitResult.Failure<DomainError>(
                    DomainError.Conflict("El email ya existe")
                );
            }
        }

        return UnitResult.Success<DomainError>();
    }
}

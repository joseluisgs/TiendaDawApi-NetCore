using System.ComponentModel.DataAnnotations;
using CSharpFunctionalExtensions;
using FluentValidation;
using TiendaApi.Api.Dtos.Common;
using TiendaApi.Api.Dtos.Usuarios;
using TiendaApi.Api.Errors;
using TiendaApi.Api.Errors.Usuarios;
using TiendaApi.Api.Mappers;
using TiendaApi.Api.Models;
using TiendaApi.Api.Repositories.Usuarios;
using TiendaApi.Api.Services.Cache;

namespace TiendaApi.Api.Services.Users;

/// <summary>
/// Servicio de usuarios usando Patrón Result.
/// Maneja las operaciones CRUD de usuarios con Programación Orientada al Resultado.
/// Las operaciones de caché se ejecutan en Task.Run (fire & forget)
/// para no bloquear el hilo principal. Esto es especialmente importante si:
/// - La caché está en Redis (latencia de red)
/// Si la operación de caché falla, se registra un warning pero no afecta a la respuesta.
/// </summary>
public class UserService(
    IUserRepository userRepository,
    ILogger<UserService> logger,
    IValidator<RegisterDto> registerValidator,
    IValidator<UserUpdateDto> userUpdateValidator,
    ICacheService cacheService,
    IConfiguration configuration
) : IUserService
{
    private readonly TimeSpan _cacheTTL = TimeSpan.FromMinutes(
        int.Parse(configuration["Cache:UsuarioCacheTTLMinutes"] ?? "10"));

    /// <summary>
    /// Obtiene todos los usuarios (excluyendo eliminados).
    /// Devuelve: Result.Success(List) | Result.Failure nunca
    /// </summary>
    public async Task<Result<IEnumerable<UserDto>, DomainError>> FindAllAsync()
    {
        logger.LogInformation("Obteniendo todos los usuarios");

        const string cacheKey = "usuarios:all";
        var cachedUsuarios = await cacheService.GetAsync<IEnumerable<UserDto>>(cacheKey);

        if (cachedUsuarios is not null)
        {
            logger.LogInformation("Devolviendo usuarios desde caché");
            return Result.Success<IEnumerable<UserDto>, DomainError>(cachedUsuarios);
        }

        var users = await userRepository.FindAllAsync();

        var activeUsers = users.Where(u => !u.IsDeleted);

        var dtos = activeUsers.ToDtoList();

        return Result.Success<IEnumerable<UserDto>, DomainError>(dtos)
            .Tap(_ => AñadirCacheUsuario(cacheKey, dtos));
    }

    /// <summary>
    /// Obtiene usuarios paginados con filtros opcionales.
    /// Devuelve: Result.Success(PagedResult) | Result.Failure nunca
    /// </summary>
    public async Task<Result<PagedResult<UserDto>, DomainError>> FindAllPagedAsync(UserFilterDto filter)
    {
        logger.LogInformation("Obteniendo usuarios paginados - Página: {Page}, Tamaño: {Size}", filter.Page, filter.Size);

        var (users, totalCount) = await userRepository.FindAllPagedAsync(filter);
        var dtos = users.ToDtoList();

        var pagedResult = new PagedResult<UserDto>
        {
            Items = dtos,
            TotalCount = totalCount,
            Page = filter.Page + 1,
            PageSize = filter.Size
        };

        return Result.Success<PagedResult<UserDto>, DomainError>(pagedResult);
    }

    /// <summary>
    /// Obtiene un usuario por su ID.
    /// Devuelve: Result.Success(UserDto) | Result.Failure(NotFound)
    /// </summary>
    public async Task<Result<UserDto, DomainError>> FindByIdAsync(long id)
    {
        logger.LogInformation("Buscando usuario con id: {Id}", id);

        var cacheKey = $"usuarios:{id}";
        var cachedUsuario = await cacheService.GetAsync<UserDto>(cacheKey);

        if (cachedUsuario is not null)
        {
            logger.LogInformation("Devolviendo usuario desde caché: {Id}", id);
            return Result.Success<UserDto, DomainError>(cachedUsuario);
        }

        var user = await userRepository.FindByIdAsync(id);

        if (user is null or { IsDeleted: true })
        {
            logger.LogWarning("Usuario con id {Id} no encontrado", id);
            return Result.Failure<UserDto, DomainError>(
                UsuarioError.NotFound(id)
            );
        }

        var dto = user.ToDto();

        return Result.Success<UserDto, DomainError>(dto)
            .Tap(_ => AñadirCacheUsuario(cacheKey, dto));
    }

    /// <summary>
    /// Crea un nuevo usuario.
    /// Devuelve: Result.Success(UserDto) | Result.Failure(Validation/Conflict)
    /// </summary>
    public async Task<Result<UserDto, DomainError>> CreateAsync(RegisterDto dto)
    {
        logger.LogInformation("Creando usuario: {Username}", dto.Username);

        var validationResult = await registerValidator.ValidateAsync(dto);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.ErrorMessage).ToArray()
                );
            return Result.Failure<UserDto, DomainError>(
                UsuarioError.ValidacionConCampos(errors)
            );
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
            IsDeleted = false
        };

        var savedUser = await userRepository.SaveAsync(user);
        var resultDto = savedUser.ToDto();

        return Result.Success<UserDto, DomainError>(resultDto)
            .Tap(_ =>
            {
                logger.LogInformation("Usuario creado con id: {Id}", savedUser.Id);
                InvalidarCacheUsuario("usuarios:all", $"usuarios:{savedUser.Id}");
            });
    }

    /// <summary>
    /// Actualiza un usuario existente.
    /// Devuelve: Result.Success(UserDto) | Result.Failure(NotFound/Validation/Conflict)
    /// </summary>
    public async Task<Result<UserDto, DomainError>> UpdateAsync(long id, UserUpdateDto dto)
    {
        logger.LogInformation("Actualizando usuario con id: {Id}", id);

        var user = await userRepository.FindByIdAsync(id);

        if (user is null or { IsDeleted: true })
        {
            logger.LogWarning("Usuario con id {Id} no encontrado para actualizar", id);
            return Result.Failure<UserDto, DomainError>(
                UsuarioError.NotFound(id)
            );
        }

        var validationResult = await userUpdateValidator.ValidateAsync(dto);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.ErrorMessage).ToArray()
                );
            return Result.Failure<UserDto, DomainError>(
                UsuarioError.ValidacionConCampos(errors)
            );
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

        var updated = await userRepository.UpdateAsync(user);
        var resultDto = updated.ToDto();

        return Result.Success<UserDto, DomainError>(resultDto)
            .Tap(_ =>
            {
                logger.LogInformation("Usuario actualizado con id: {Id}", id);
                InvalidarCacheUsuario("usuarios:all", $"usuarios:{id}");
            });
    }

    /// <summary>
    /// Actualiza el avatar de un usuario.
    /// Devuelve: Result.Success(UserDto) | Result.Failure(NotFound/Validation)
    /// </summary>
    public async Task<Result<UserDto, DomainError>> UpdateAvatarAsync(long id, string avatarUrl)
    {
        logger.LogInformation("Actualizando avatar de usuario con id: {Id}", id);

        var user = await userRepository.FindByIdAsync(id);

        if (user is null or { IsDeleted: true })
        {
            logger.LogWarning("Usuario con id {Id} no encontrado para actualizar avatar", id);
            return Result.Failure<UserDto, DomainError>(
                UsuarioError.NotFound(id)
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

        var updated = await userRepository.UpdateAsync(user);
        var resultDto = updated.ToDto();

        return Result.Success<UserDto, DomainError>(resultDto)
            .Tap(_ =>
            {
                logger.LogInformation("Avatar actualizado para usuario con id: {Id}", id);
                InvalidarCacheUsuario("usuarios:all", $"usuarios:{id}");
            });
    }

    /// <summary>
    /// Elimina un usuario (soft delete).
    /// Devuelve: UnitResult.Success | UnitResult.Failure(NotFound)
    /// </summary>
    public async Task<UnitResult<DomainError>> DeleteAsync(long id)
    {
        logger.LogInformation("Eliminando usuario con id: {Id}", id);

        var user = await userRepository.FindByIdAsync(id);

        if (user is null or { IsDeleted: true })
        {
            logger.LogWarning("Usuario con id {Id} no encontrado para eliminar", id);
            return UnitResult.Failure<DomainError>(
                UsuarioError.NotFound(id)
            );
        }

        user.IsDeleted = true;

        await userRepository.UpdateAsync(user);

        logger.LogInformation("Usuario eliminado lógicamente con id: {Id}", id);

        _ = Task.Run(() => InvalidarCacheUsuario("usuarios:all", $"usuarios:{id}"));

        return UnitResult.Success<DomainError>();
    }

    // ========== MÉTODOS PRIVADOS - CACHE ==========

    /// <summary>
    /// Añade un elemento a la caché de forma asíncrona (fire & forget).
    /// </summary>
    private void AñadirCacheUsuario<T>(string key, T value)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await cacheService.SetAsync(key, value, _cacheTTL);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Error adding to cache: Key={Key}", key);
            }
        });
    }

    /// <summary>
    /// Invalida las claves de caché especificadas de forma asíncrona (fire & forget).
    /// </summary>
    private void InvalidarCacheUsuario(params string[] keys)
    {
        _ = Task.Run(async () =>
        {
            foreach (var key in keys)
            {
                try
                {
                    await cacheService.RemoveAsync(key);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Cache invalidation error: Key={Key}", key);
                }
            }
        });
    }

    // ========== VALIDACIÓN ==========

    /// <summary>
    /// Verifica duplicados de username y email.
    /// Devuelve: UnitResult.Success | UnitResult.Failure(Conflict)
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
                    UsuarioError.UsernameExistente(username)
                );
            }
        }

        if (!string.IsNullOrWhiteSpace(email))
        {
            var existingEmail = await userRepository.FindByEmailAsync(email);
            if (existingEmail != null && existingEmail.Id != excludeUserId)
            {
                return UnitResult.Failure<DomainError>(
                    UsuarioError.EmailExistente(email)
                );
            }
        }

        return UnitResult.Success<DomainError>();
    }
}

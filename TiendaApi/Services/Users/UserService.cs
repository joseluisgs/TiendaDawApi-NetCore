using System.ComponentModel.DataAnnotations;
using CSharpFunctionalExtensions;
using TiendaApi.Dtos.Usuarios;
using TiendaApi.Errors;
using TiendaApi.Mappers;
using TiendaApi.Models;
using TiendaApi.Repositories.Usuarios;

namespace TiendaApi.Services.Users;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<UserService> _logger;

    public UserService(
        IUserRepository userRepository,
        ILogger<UserService> logger)
    {
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<Result<IEnumerable<UserDto>, DomainError>> FindAllAsync()
    {
        _logger.LogInformation("Finding all users");
        
        var users = await _userRepository.FindAllAsync();
        
        var activeUsers = users.Where(u => !u.IsDeleted);
        
        var dtos = activeUsers.ToDtoList();
        
        return Result.Success<IEnumerable<UserDto>, DomainError>(dtos);
    }

    public async Task<Result<UserDto, DomainError>> FindByIdAsync(long id)
    {
        _logger.LogInformation("Finding user with id: {Id}", id);
        
        var user = await _userRepository.FindByIdAsync(id);
        
        if (user == null || user.IsDeleted)
        {
            _logger.LogWarning("User with id {Id} not found", id);
            return Result.Failure<UserDto, DomainError>(
                DomainError.NotFound($"Usuario con ID {id} no encontrado")
            );
        }
        
        var dto = user.ToDto();
        
        return Result.Success<UserDto, DomainError>(dto);
    }

    public async Task<Result<UserDto, DomainError>> CreateAsync(RegisterDto dto)
    {
        _logger.LogInformation("Creating user: {Username}", dto.Username);
        
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
        
        var savedUser = await _userRepository.SaveAsync(user);
        
        _logger.LogInformation("User created with id: {Id}", savedUser.Id);
        
        var resultDto = savedUser.ToDto();
        
        return Result.Success<UserDto, DomainError>(resultDto);
    }

    public async Task<Result<UserDto, DomainError>> UpdateAsync(long id, UserUpdateDto dto)
    {
        _logger.LogInformation("Updating user with id: {Id}", id);
        
        var user = await _userRepository.FindByIdAsync(id);
        
        if (user == null || user.IsDeleted)
        {
            _logger.LogWarning("User with id {Id} not found for update", id);
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
        
        var updated = await _userRepository.UpdateAsync(user);
        
        _logger.LogInformation("User updated with id: {Id}", id);
        
        var resultDto = updated.ToDto();
        
        return Result.Success<UserDto, DomainError>(resultDto);
    }

    public async Task<UnitResult<DomainError>> DeleteAsync(long id)
    {
        _logger.LogInformation("Deleting user with id: {Id}", id);
        
        var user = await _userRepository.FindByIdAsync(id);
        
        if (user == null || user.IsDeleted)
        {
            _logger.LogWarning("User with id {Id} not found for delete", id);
            return UnitResult.Failure<DomainError>(
                DomainError.NotFound($"Usuario con ID {id} no encontrado")
            );
        }
        
        user.IsDeleted = true;
        user.UpdatedAt = DateTime.UtcNow;
        
        await _userRepository.UpdateAsync(user);
        
        _logger.LogInformation("User soft deleted with id: {Id}", id);
        
        return UnitResult.Success<DomainError>();
    }

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

    private async Task<UnitResult<DomainError>> CheckDuplicatesAsync(
        string? username, 
        string? email, 
        long? excludeUserId)
    {
        if (!string.IsNullOrWhiteSpace(username))
        {
            var existingUser = await _userRepository.FindByUsernameAsync(username);
            if (existingUser != null && existingUser.Id != excludeUserId)
            {
                return UnitResult.Failure<DomainError>(
                    DomainError.Conflict("El nombre de usuario ya existe")
                );
            }
        }

        if (!string.IsNullOrWhiteSpace(email))
        {
            var existingEmail = await _userRepository.FindByEmailAsync(email);
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

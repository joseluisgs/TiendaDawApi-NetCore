/*
 * EJEMPLO EDUCATIVO: CRUD CON RESULT PATTERN
 * 
 * Este servicio demuestra cómo implementar operaciones CRUD completas
 * usando Railway Oriented Programming:
 * 
 * Cada método retorna Result<T, AppError>:
 * - FindAll: Nunca falla, retorna lista vacía si no hay datos
 * - FindById: Retorna NotFound si no existe
 * - Create: Valida → Verifica duplicados → Guarda → Retorna DTO
 * - Update: Busca → Valida → Actualiza → Guarda
 * - Delete: Busca → Soft delete → Retorna Unit
 * 
 * Comparación con Java/Spring Boot:
 * - Similar a @Service con métodos que retornan Either<Error, Value>
 * - Evita @ExceptionHandler en controllers
 * - Más explícito y testeable que try/catch
 * 
 * Ventajas:
 * ✅ Errores como valores (no excepciones)
 * ✅ Type-safe (el compilador ayuda)
 * ✅ Fácil de testear (sin mockear excepciones)
 * ✅ Performance (no stack unwinding)
 */

using System.ComponentModel.DataAnnotations;
using AutoMapper;
using TiendaApi.Common;
using TiendaApi.Models.DTOs;
using TiendaApi.Models.Entities;
using TiendaApi.Repositories;

namespace TiendaApi.Services.Users;

/// <summary>
/// User Service implementation using Result Pattern
/// Handles all user CRUD operations with Railway Oriented Programming
/// </summary>
public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<UserService> _logger;

    public UserService(
        IUserRepository userRepository,
        IMapper mapper,
        ILogger<UserService> logger)
    {
        _userRepository = userRepository;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>
    /// Get all users (excluding deleted)
    /// Este método NUNCA falla - retorna lista vacía si no hay usuarios
    /// </summary>
    public async Task<Result<IEnumerable<UserDto>, AppError>> FindAllAsync()
    {
        _logger.LogInformation("Finding all users");
        
        var users = await _userRepository.FindAllAsync();
        
        // Filter out deleted users
        var activeUsers = users.Where(u => !u.IsDeleted);
        
        var dtos = _mapper.Map<IEnumerable<UserDto>>(activeUsers);
        
        return Result<IEnumerable<UserDto>, AppError>.Success(dtos);
    }

    /// <summary>
    /// Find user by ID
    /// Railway Pattern: Buscar → Verificar existencia → Mapear → Retornar
    /// </summary>
    public async Task<Result<UserDto, AppError>> FindByIdAsync(long id)
    {
        _logger.LogInformation("Finding user with id: {Id}", id);
        
        var user = await _userRepository.FindByIdAsync(id);
        
        if (user == null || user.IsDeleted)
        {
            _logger.LogWarning("User with id {Id} not found", id);
            return Result<UserDto, AppError>.Failure(
                AppError.NotFound($"Usuario con ID {id} no encontrado")
            );
        }
        
        var dto = _mapper.Map<UserDto>(user);
        
        return Result<UserDto, AppError>.Success(dto);
    }

    /// <summary>
    /// Create a new user
    /// 
    /// Railway Pattern flow:
    /// ValidateRegistration → CheckDuplicates → HashPassword → CreateUser → SaveUser
    /// 
    /// Si cualquier paso falla, el tren se desvía a la vía del fracaso
    /// </summary>
    public async Task<Result<UserDto, AppError>> CreateAsync(RegisterDto dto)
    {
        _logger.LogInformation("Creating user: {Username}", dto.Username);
        
        // Step 1: Validate input
        var validationResult = ValidateRegistration(dto);
        if (validationResult.IsFailure)
        {
            return Result<UserDto, AppError>.Failure(validationResult.Error);
        }
        
        // Step 2: Check for duplicates
        var duplicateCheck = await CheckDuplicatesAsync(dto.Username, dto.Email, excludeUserId: null);
        if (duplicateCheck.IsFailure)
        {
            return Result<UserDto, AppError>.Failure(duplicateCheck.Error);
        }
        
        // Step 3: Hash password with BCrypt (workFactor 11)
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password, workFactor: 11);
        
        // Step 4: Create user entity
        var user = new User
        {
            Username = dto.Username,
            Email = dto.Email,
            PasswordHash = passwordHash,
            Role = UserRoles.USER, // Default role for new users
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        // Step 5: Save user
        var savedUser = await _userRepository.SaveAsync(user);
        
        _logger.LogInformation("User created with id: {Id}", savedUser.Id);
        
        var resultDto = _mapper.Map<UserDto>(savedUser);
        
        return Result<UserDto, AppError>.Success(resultDto);
    }

    /// <summary>
    /// Update existing user
    /// 
    /// Railway Pattern flow:
    /// FindUser → Validate → CheckDuplicates → UpdateFields → SaveUser
    /// </summary>
    public async Task<Result<UserDto, AppError>> UpdateAsync(long id, UserUpdateDto dto)
    {
        _logger.LogInformation("Updating user with id: {Id}", id);
        
        // Step 1: Find user
        var user = await _userRepository.FindByIdAsync(id);
        
        if (user == null || user.IsDeleted)
        {
            _logger.LogWarning("User with id {Id} not found for update", id);
            return Result<UserDto, AppError>.Failure(
                AppError.NotFound($"Usuario con ID {id} no encontrado")
            );
        }
        
        // Step 2: Validate input
        var validationResult = ValidateUpdate(dto);
        if (validationResult.IsFailure)
        {
            return Result<UserDto, AppError>.Failure(validationResult.Error);
        }
        
        // Step 3: Check for duplicate email (if email is being changed)
        if (!string.IsNullOrWhiteSpace(dto.Email) && dto.Email != user.Email)
        {
            var duplicateCheck = await CheckDuplicatesAsync(null, dto.Email, excludeUserId: id);
            if (duplicateCheck.IsFailure)
            {
                return Result<UserDto, AppError>.Failure(duplicateCheck.Error);
            }
            
            user.Email = dto.Email;
        }
        
        // Step 4: Update password if provided
        if (!string.IsNullOrWhiteSpace(dto.Password))
        {
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password, workFactor: 11);
        }
        
        user.UpdatedAt = DateTime.UtcNow;
        
        // Step 5: Save changes
        var updated = await _userRepository.UpdateAsync(user);
        
        _logger.LogInformation("User updated with id: {Id}", id);
        
        var resultDto = _mapper.Map<UserDto>(updated);
        
        return Result<UserDto, AppError>.Success(resultDto);
    }

    /// <summary>
    /// Delete user (soft delete - sets IsDeleted = true)
    /// 
    /// Railway Pattern flow:
    /// FindUser → SoftDelete → SaveUser → ReturnUnit
    /// </summary>
    public async Task<Result<Unit, AppError>> DeleteAsync(long id)
    {
        _logger.LogInformation("Deleting user with id: {Id}", id);
        
        // Step 1: Find user
        var user = await _userRepository.FindByIdAsync(id);
        
        if (user == null || user.IsDeleted)
        {
            _logger.LogWarning("User with id {Id} not found for delete", id);
            return Result<Unit, AppError>.Failure(
                AppError.NotFound($"Usuario con ID {id} no encontrado")
            );
        }
        
        // Step 2: Soft delete
        user.IsDeleted = true;
        user.UpdatedAt = DateTime.UtcNow;
        
        await _userRepository.UpdateAsync(user);
        
        _logger.LogInformation("User soft deleted with id: {Id}", id);
        
        // Return Unit (void-like success)
        return Result<Unit, AppError>.Success(Unit.Value);
    }

    #region Private Validation Methods

    /// <summary>
    /// Validate registration input
    /// Retorna Result en lugar de lanzar excepciones
    /// </summary>
    private Result<Unit, AppError> ValidateRegistration(RegisterDto dto)
    {
        // Validate username
        if (string.IsNullOrWhiteSpace(dto.Username))
        {
            return Result<Unit, AppError>.Failure(
                AppError.Validation("El nombre de usuario es requerido")
            );
        }

        if (dto.Username.Length < 3)
        {
            return Result<Unit, AppError>.Failure(
                AppError.Validation("El nombre de usuario debe tener al menos 3 caracteres")
            );
        }

        // Validate email
        if (string.IsNullOrWhiteSpace(dto.Email))
        {
            return Result<Unit, AppError>.Failure(
                AppError.Validation("El email es requerido")
            );
        }

        // Use proper email validation
        if (!new EmailAddressAttribute().IsValid(dto.Email))
        {
            return Result<Unit, AppError>.Failure(
                AppError.Validation("El email no es válido")
            );
        }

        // Validate password
        if (string.IsNullOrWhiteSpace(dto.Password))
        {
            return Result<Unit, AppError>.Failure(
                AppError.Validation("La contraseña es requerida")
            );
        }

        if (dto.Password.Length < 6)
        {
            return Result<Unit, AppError>.Failure(
                AppError.Validation("La contraseña debe tener al menos 6 caracteres")
            );
        }

        return Result<Unit, AppError>.Success(Unit.Value);
    }

    /// <summary>
    /// Validate update input
    /// </summary>
    private Result<Unit, AppError> ValidateUpdate(UserUpdateDto dto)
    {
        // Validate email if provided
        if (!string.IsNullOrWhiteSpace(dto.Email))
        {
            if (!new EmailAddressAttribute().IsValid(dto.Email))
            {
                return Result<Unit, AppError>.Failure(
                    AppError.Validation("El email no es válido")
                );
            }
        }

        // Validate password if provided
        if (!string.IsNullOrWhiteSpace(dto.Password))
        {
            if (dto.Password.Length < 6)
            {
                return Result<Unit, AppError>.Failure(
                    AppError.Validation("La contraseña debe tener al menos 6 caracteres")
                );
            }
        }

        return Result<Unit, AppError>.Success(Unit.Value);
    }

    /// <summary>
    /// Check for duplicate username or email
    /// Permite excluir un usuario por ID (útil para updates)
    /// </summary>
    private async Task<Result<Unit, AppError>> CheckDuplicatesAsync(
        string? username, 
        string? email, 
        long? excludeUserId)
    {
        // Check username if provided
        if (!string.IsNullOrWhiteSpace(username))
        {
            var existingUser = await _userRepository.FindByUsernameAsync(username);
            if (existingUser != null && existingUser.Id != excludeUserId)
            {
                return Result<Unit, AppError>.Failure(
                    AppError.Conflict("El nombre de usuario ya existe")
                );
            }
        }

        // Check email if provided
        if (!string.IsNullOrWhiteSpace(email))
        {
            var existingEmail = await _userRepository.FindByEmailAsync(email);
            if (existingEmail != null && existingEmail.Id != excludeUserId)
            {
                return Result<Unit, AppError>.Failure(
                    AppError.Conflict("El email ya existe")
                );
            }
        }

        return Result<Unit, AppError>.Success(Unit.Value);
    }

    #endregion
}

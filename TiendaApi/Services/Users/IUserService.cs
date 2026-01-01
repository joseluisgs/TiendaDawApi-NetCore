using TiendaApi.Common;
using TiendaApi.Models.DTOs;

namespace TiendaApi.Services.Users;

/// <summary>
/// User Service interface using Result Pattern
/// 
/// Este servicio maneja las operaciones CRUD de usuarios
/// con Railway Oriented Programming (Result Pattern)
/// 
/// Comparación con Java/Spring Boot:
/// - Java: UserService con métodos que retornan Optional<User> o lanzan excepciones
/// - Este servicio: Retorna Result<T, AppError> - sin excepciones
/// - Más explícito y type-safe que Optional
/// 
/// Patrón de retorno:
/// - Result<IEnumerable<UserDto>, AppError>: Lista de usuarios (nunca falla)
/// - Result<UserDto, AppError>: Usuario encontrado o error NotFound
/// - Result<Unit, AppError>: Operación sin retorno (ej: Delete)
/// </summary>
public interface IUserService
{
    /// <summary>
    /// Get all users (excluding deleted)
    /// Never fails - returns empty list if no users
    /// </summary>
    Task<Result<IEnumerable<UserDto>, AppError>> FindAllAsync();

    /// <summary>
    /// Find user by ID
    /// Returns NotFound error if user doesn't exist or is deleted
    /// </summary>
    Task<Result<UserDto, AppError>> FindByIdAsync(long id);

    /// <summary>
    /// Create a new user
    /// 
    /// Railway Pattern flow:
    /// 1. Validate input (username, email, password)
    /// 2. Check for duplicate username/email
    /// 3. Hash password with BCrypt
    /// 4. Save user to database
    /// 5. Return UserDto
    /// 
    /// Possible errors:
    /// - Validation: Invalid input data
    /// - Conflict: Username or email already exists
    /// </summary>
    Task<Result<UserDto, AppError>> CreateAsync(RegisterDto dto);

    /// <summary>
    /// Update existing user
    /// 
    /// Railway Pattern flow:
    /// 1. Find user by ID
    /// 2. Validate input
    /// 3. Check for duplicate email (excluding current user)
    /// 4. Update fields
    /// 5. Hash password if provided
    /// 6. Save changes
    /// 
    /// Possible errors:
    /// - NotFound: User doesn't exist
    /// - Validation: Invalid input data
    /// - Conflict: Email already exists
    /// </summary>
    Task<Result<UserDto, AppError>> UpdateAsync(long id, UserUpdateDto dto);

    /// <summary>
    /// Delete user (soft delete - sets IsDeleted = true)
    /// 
    /// Railway Pattern flow:
    /// 1. Find user by ID
    /// 2. Set IsDeleted = true
    /// 3. Save changes
    /// 4. Return Unit (void-like)
    /// 
    /// Possible errors:
    /// - NotFound: User doesn't exist
    /// </summary>
    Task<Result<Unit, AppError>> DeleteAsync(long id);
}

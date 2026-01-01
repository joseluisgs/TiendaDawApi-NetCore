using TiendaApi.Common;
using TiendaApi.Models.DTOs;

namespace TiendaApi.Services.Auth;

/// <summary>
/// Authentication Service interface using Result Pattern
/// 
/// Java Spring Security equivalent: UserDetailsService + AuthenticationManager
/// 
/// This service encapsulates authentication logic with Railway Oriented Programming,
/// making error handling explicit and composable.
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Register a new user
    /// 
    /// Railway Pattern flow:
    /// 1. Validate input (username, email, password format)
    /// 2. Check for duplicate username/email
    /// 3. Hash password with BCrypt
    /// 4. Save user to database
    /// 5. Generate JWT token
    /// 6. Return authentication response
    /// 
    /// Returns Result with AuthResponseDto on success or AppError on failure
    /// </summary>
    Task<Result<AuthResponseDto, AppError>> SignUpAsync(RegisterDto dto);

    /// <summary>
    /// Authenticate an existing user
    /// 
    /// Railway Pattern flow:
    /// 1. Validate input (username, password not empty)
    /// 2. Find user by username
    /// 3. Verify password with BCrypt
    /// 4. Generate JWT token
    /// 5. Return authentication response
    /// 
    /// Returns Result with AuthResponseDto on success or AppError on failure
    /// </summary>
    Task<Result<AuthResponseDto, AppError>> SignInAsync(LoginDto dto);
}

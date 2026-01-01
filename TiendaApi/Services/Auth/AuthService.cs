using System.ComponentModel.DataAnnotations;
using TiendaApi.Common;
using TiendaApi.Models.DTOs;
using TiendaApi.Models.Entities;
using TiendaApi.Repositories;

namespace TiendaApi.Services.Auth;

/// <summary>
/// Authentication Service using RESULT PATTERN (Railway Oriented Programming)
/// 
/// ANTES (Excepciones en Controller): throw new ConflictException()
/// AHORA (Result Pattern): return Result.Failure(AppError.Conflict(...))
/// 
/// Ventajas del Result Pattern:
/// 1. Errores explícitos en la firma del método (Task<Result<T, AppError>>)
/// 2. Sin overhead de excepciones (no stack unwinding)
/// 3. Más fácil de testear (sin try/catch)
/// 4. Encadenamiento funcional con .Bind(), .Map(), .Tap()
/// 5. Type-safe: el compilador garantiza que se manejen los errores
/// 
/// Comparación con Java/Spring Boot:
/// - Java: Optional<T> + custom Either<Error, Value>
/// - Spring: @ControllerAdvice maneja excepciones → aquí el controller hace Match
/// - Spring Security: AuthenticationManager.authenticate() puede lanzar excepciones
/// - Este servicio: Retorna Result sin lanzar excepciones
/// </summary>
public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IJwtService _jwtService;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IUserRepository userRepository,
        IJwtService jwtService,
        ILogger<AuthService> logger)
    {
        _userRepository = userRepository;
        _jwtService = jwtService;
        _logger = logger;
    }

    /// <summary>
    /// Register a new user using Railway Oriented Programming
    /// 
    /// Railway Pattern flow:
    /// ValidateRegistration → CheckDuplicates → HashPassword → CreateUser → GenerateAuthResponse
    /// 
    /// Si cualquier paso falla, el tren se desvía a la vía del fracaso y 
    /// los pasos subsiguientes se omiten automáticamente.
    /// </summary>
    public async Task<Result<AuthResponseDto, AppError>> SignUpAsync(RegisterDto dto)
    {
        // Sanitize username for logging to prevent log forging
        var sanitizedUsername = dto.Username?.Replace("\n", "").Replace("\r", "");
        _logger.LogInformation("SignUp request for username: {Username}", sanitizedUsername);

        // Step 1: Validate input
        var validationResult = ValidateRegistration(dto);
        if (validationResult.IsFailure)
        {
            return Result<AuthResponseDto, AppError>.Failure(validationResult.Error);
        }

        // Step 2: Check for duplicates
        var duplicateCheck = await CheckDuplicatesAsync(dto);
        if (duplicateCheck.IsFailure)
        {
            return Result<AuthResponseDto, AppError>.Failure(duplicateCheck.Error);
        }

        // Step 3: Hash password with BCrypt
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password, workFactor: 11);

        // Step 4: Create and save user
        // Note: dto.Username and dto.Email are guaranteed non-null by ValidateRegistration
        var user = new User
        {
            Username = dto.Username!,
            Email = dto.Email!,
            PasswordHash = passwordHash,
            Role = UserRoles.USER, // Default role for new users
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var savedUser = await _userRepository.SaveAsync(user);

        // Step 5: Generate JWT token and create response
        var authResponse = GenerateAuthResponse(savedUser);

        _logger.LogInformation("User registered successfully: {Username}", sanitizedUsername);

        return Result<AuthResponseDto, AppError>.Success(authResponse);
    }

    /// <summary>
    /// Authenticate an existing user using Railway Oriented Programming
    /// 
    /// Railway Pattern flow:
    /// ValidateLogin → FindUser → VerifyPassword → GenerateAuthResponse
    /// 
    /// Si cualquier paso falla, retornamos un error sin ejecutar los pasos siguientes.
    /// </summary>
    public async Task<Result<AuthResponseDto, AppError>> SignInAsync(LoginDto dto)
    {
        // Sanitize username for logging to prevent log forging
        var sanitizedUsername = dto.Username?.Replace("\n", "").Replace("\r", "");
        _logger.LogInformation("SignIn request for username: {Username}", sanitizedUsername);

        // Step 1: Validate input
        var validationResult = ValidateLogin(dto);
        if (validationResult.IsFailure)
        {
            return Result<AuthResponseDto, AppError>.Failure(validationResult.Error);
        }

        // Step 2: Find user
        // Note: dto.Username is guaranteed non-null by ValidateLogin
        var user = await _userRepository.FindByUsernameAsync(dto.Username!);
        if (user == null)
        {
            _logger.LogWarning("SignIn failed: User not found - {Username}", sanitizedUsername);
            // Don't reveal whether username exists - generic error message
            return Result<AuthResponseDto, AppError>.Failure(
                AppError.Unauthorized("Invalid username or password")
            );
        }

        // Step 3: Verify password
        var passwordValid = BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash);
        if (!passwordValid)
        {
            _logger.LogWarning("SignIn failed: Invalid password - {Username}", sanitizedUsername);
            // Don't reveal whether password is wrong - generic error message
            return Result<AuthResponseDto, AppError>.Failure(
                AppError.Unauthorized("Invalid username or password")
            );
        }

        // Step 4: Generate authentication response
        var authResponse = GenerateAuthResponse(user);

        _logger.LogInformation("User signed in successfully: {Username}", sanitizedUsername);

        return Result<AuthResponseDto, AppError>.Success(authResponse);
    }

    #region Private Helper Methods

    /// <summary>
    /// Validate registration input
    /// </summary>
    private Result<Unit, AppError> ValidateRegistration(RegisterDto dto)
    {
        // Validate username
        if (string.IsNullOrWhiteSpace(dto.Username))
        {
            return Result<Unit, AppError>.Failure(
                AppError.Validation("Username is required")
            );
        }

        if (dto.Username.Length < 3)
        {
            return Result<Unit, AppError>.Failure(
                AppError.Validation("Username must be at least 3 characters")
            );
        }

        // Validate email
        if (string.IsNullOrWhiteSpace(dto.Email))
        {
            return Result<Unit, AppError>.Failure(
                AppError.Validation("Email is required")
            );
        }

        // Use proper email validation
        if (!new EmailAddressAttribute().IsValid(dto.Email))
        {
            return Result<Unit, AppError>.Failure(
                AppError.Validation("Valid email is required")
            );
        }

        // Validate password
        if (string.IsNullOrWhiteSpace(dto.Password))
        {
            return Result<Unit, AppError>.Failure(
                AppError.Validation("Password is required")
            );
        }

        if (dto.Password.Length < 6)
        {
            return Result<Unit, AppError>.Failure(
                AppError.Validation("Password must be at least 6 characters")
            );
        }

        return Result<Unit, AppError>.Success(Unit.Value);
    }

    /// <summary>
    /// Validate login input
    /// </summary>
    private Result<Unit, AppError> ValidateLogin(LoginDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Username))
        {
            return Result<Unit, AppError>.Failure(
                AppError.Validation("Username is required")
            );
        }

        if (string.IsNullOrWhiteSpace(dto.Password))
        {
            return Result<Unit, AppError>.Failure(
                AppError.Validation("Password is required")
            );
        }

        return Result<Unit, AppError>.Success(Unit.Value);
    }

    /// <summary>
    /// Check for duplicate username or email concurrently for better performance
    /// </summary>
    private async Task<Result<Unit, AppError>> CheckDuplicatesAsync(RegisterDto dto)
    {
        // Run both checks concurrently for better performance
        var usernameCheckTask = _userRepository.FindByUsernameAsync(dto.Username!);
        var emailCheckTask = _userRepository.FindByEmailAsync(dto.Email!);

        await Task.WhenAll(usernameCheckTask, emailCheckTask);

        var existingUser = await usernameCheckTask;
        if (existingUser != null)
        {
            return Result<Unit, AppError>.Failure(
                AppError.Conflict("Username already exists")
            );
        }

        var existingEmail = await emailCheckTask;
        if (existingEmail != null)
        {
            return Result<Unit, AppError>.Failure(
                AppError.Conflict("Email already exists")
            );
        }

        return Result<Unit, AppError>.Success(Unit.Value);
    }

    /// <summary>
    /// Generate authentication response with JWT token
    /// </summary>
    private AuthResponseDto GenerateAuthResponse(User user)
    {
        var token = _jwtService.GenerateToken(user);

        var userDto = new UserDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            Role = user.Role,
            CreatedAt = user.CreatedAt
        };

        return new AuthResponseDto
        {
            Token = token,
            User = userDto
        };
    }

    #endregion
}

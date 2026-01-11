using System.ComponentModel.DataAnnotations;
using CSharpFunctionalExtensions;
using TiendaApi.Dtos.Usuarios;
using TiendaApi.Errors;
using TiendaApi.Models;
using TiendaApi.Repositories.Usuarios;

namespace TiendaApi.Services.Auth;

/// <summary>
/// Authentication Service using RESULT PATTERN (Railway Oriented Programming)
/// 
/// ANTES (Excepciones en Controller): throw new ConflictException()
/// AHORA (Result Pattern): return Result.Failure(DomainError.Conflict(...))
/// 
/// Ventajas del Result Pattern:
/// 1. Errores explícitos en la firma del método (Task<Result<T, DomainError>>)
/// 2. Sin overhead de excepciones (no stack unwinding)
/// 3. Más fácil de testear (sin try/catch)
/// 4. Encadenamiento funcional con .Bind(), .Map(), .Tap()
/// 5. Type-safe: el compilador garantiza que se manejen los errores
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

    public async Task<Result<AuthResponseDto, DomainError>> SignUpAsync(RegisterDto dto)
    {
        var sanitizedUsername = dto.Username?.Replace("\n", "").Replace("\r", "");
        _logger.LogInformation("SignUp request for username: {Username}", sanitizedUsername);

        var validationResult = ValidateRegistration(dto);
        if (validationResult.IsFailure)
        {
            return Result.Failure<AuthResponseDto, DomainError>(validationResult.Error);
        }

        var duplicateCheck = await CheckDuplicatesAsync(dto);
        if (duplicateCheck.IsFailure)
        {
            return Result.Failure<AuthResponseDto, DomainError>(duplicateCheck.Error);
        }

        var passwordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password, workFactor: 11);

        var user = new User
        {
            Username = dto.Username!,
            Email = dto.Email!,
            PasswordHash = passwordHash,
            Role = UserRoles.USER,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var savedUser = await _userRepository.SaveAsync(user);
        var authResponse = GenerateAuthResponse(savedUser);

        _logger.LogInformation("User registered successfully: {Username}", sanitizedUsername);

        return Result.Success<AuthResponseDto, DomainError>(authResponse);
    }

    public async Task<Result<AuthResponseDto, DomainError>> SignInAsync(LoginDto dto)
    {
        var sanitizedUsername = dto.Username?.Replace("\n", "").Replace("\r", "");
        _logger.LogInformation("SignIn request for username: {Username}", sanitizedUsername);

        var validationResult = ValidateLogin(dto);
        if (validationResult.IsFailure)
        {
            return Result.Failure<AuthResponseDto, DomainError>(validationResult.Error);
        }

        var user = await _userRepository.FindByUsernameAsync(dto.Username!);
        if (user == null)
        {
            _logger.LogWarning("SignIn failed: User not found - {Username}", sanitizedUsername);
            return Result.Failure<AuthResponseDto, DomainError>(
                DomainError.Unauthorized("Invalid username or password")
            );
        }

        var passwordValid = BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash);
        if (!passwordValid)
        {
            _logger.LogWarning("SignIn failed: Invalid password - {Username}", sanitizedUsername);
            return Result.Failure<AuthResponseDto, DomainError>(
                DomainError.Unauthorized("Invalid username or password")
            );
        }

        var authResponse = GenerateAuthResponse(user);
        _logger.LogInformation("User signed in successfully: {Username}", sanitizedUsername);

        return Result.Success<AuthResponseDto, DomainError>(authResponse);
    }

    private UnitResult<DomainError> ValidateRegistration(RegisterDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Username))
        {
            return UnitResult.Failure(DomainError.Validation("Username is required"));
        }

        if (dto.Username.Length < 3)
        {
            return UnitResult.Failure(DomainError.Validation("Username must be at least 3 characters"));
        }

        if (string.IsNullOrWhiteSpace(dto.Email))
        {
            return UnitResult.Failure(DomainError.Validation("Email is required"));
        }

        if (!new EmailAddressAttribute().IsValid(dto.Email))
        {
            return UnitResult.Failure(DomainError.Validation("Valid email is required"));
        }

        if (string.IsNullOrWhiteSpace(dto.Password))
        {
            return UnitResult.Failure(DomainError.Validation("Password is required"));
        }

        if (dto.Password.Length < 6)
        {
            return UnitResult.Failure(DomainError.Validation("Password must be at least 6 characters"));
        }

        return UnitResult.Success<DomainError>();
    }

    private UnitResult<DomainError> ValidateLogin(LoginDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Username))
        {
            return UnitResult.Failure(DomainError.Validation("Username is required"));
        }

        if (string.IsNullOrWhiteSpace(dto.Password))
        {
            return UnitResult.Failure(DomainError.Validation("Password is required"));
        }

        return UnitResult.Success<DomainError>();
    }

    private async Task<UnitResult<DomainError>> CheckDuplicatesAsync(RegisterDto dto)
    {
        var usernameCheckTask = _userRepository.FindByUsernameAsync(dto.Username!);
        var emailCheckTask = _userRepository.FindByEmailAsync(dto.Email!);

        await Task.WhenAll(usernameCheckTask, emailCheckTask);

        var existingUser = await usernameCheckTask;
        if (existingUser != null)
        {
            return UnitResult.Failure(DomainError.Conflict("Username already exists"));
        }

        var existingEmail = await emailCheckTask;
        if (existingEmail != null)
        {
            return UnitResult.Failure(DomainError.Conflict("Email already exists"));
        }

        return UnitResult.Success<DomainError>();
    }

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
}

using System.ComponentModel.DataAnnotations;
using CSharpFunctionalExtensions;
using TiendaApi.Dtos.Usuarios;
using TiendaApi.Errors;
using TiendaApi.Models;
using TiendaApi.Repositories.Usuarios;

namespace TiendaApi.Services.Auth;

/// <summary>
/// Servicio de autenticación usando Patrón Result.
/// Encapsula la lógica de autenticación con Programación Orientada al Resultado.
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
    /// Registra un nuevo usuario.
    /// Returns: Result.Success(AuthResponseDto) | Result.Failure(Validation/Conflict)
    /// </summary>
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

    /// <summary>
    /// Autentica un usuario existente.
    /// Returns: Result.Success(AuthResponseDto) | Result.Failure(Validation/Unauthorized/NotFound)
    /// </summary>
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
            _logger.LogWarning("SignIn fallido: Usuario no encontrado - {Username}", sanitizedUsername);
            return Result.Failure<AuthResponseDto, DomainError>(
                DomainError.Unauthorized("Credenciales inválidas")
            );
        }

        var passwordValid = BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash);
        if (!passwordValid)
        {
            _logger.LogWarning("SignIn fallido: Password inválido - {Username}", sanitizedUsername);
            return Result.Failure<AuthResponseDto, DomainError>(
                DomainError.Unauthorized("Credenciales inválidas")
            );
        }

        var authResponse = GenerateAuthResponse(user);
        _logger.LogInformation("Usuario inició sesión correctamente: {Username}", sanitizedUsername);

        return Result.Success<AuthResponseDto, DomainError>(authResponse);
    }

    private UnitResult<DomainError> ValidateRegistration(RegisterDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Username))
        {
            return UnitResult.Failure(DomainError.Validation("El nombre de usuario es requerido"));
        }

        if (dto.Username.Length < 3)
        {
            return UnitResult.Failure(DomainError.Validation("El nombre de usuario debe tener al menos 3 caracteres"));
        }

        if (string.IsNullOrWhiteSpace(dto.Email))
        {
            return UnitResult.Failure(DomainError.Validation("El email es requerido"));
        }

        if (!new EmailAddressAttribute().IsValid(dto.Email))
        {
            return UnitResult.Failure(DomainError.Validation("El email no es válido"));
        }

        if (string.IsNullOrWhiteSpace(dto.Password))
        {
            return UnitResult.Failure(DomainError.Validation("La contraseña es requerida"));
        }

        if (dto.Password.Length < 6)
        {
            return UnitResult.Failure(DomainError.Validation("La contraseña debe tener al menos 6 caracteres"));
        }

        return UnitResult.Success<DomainError>();
    }

    /// <summary>
    /// Valida los datos de inicio de sesión.
    /// Returns: UnitResult.Success | UnitResult.Failure(Validation)
    /// </summary>
    private UnitResult<DomainError> ValidateLogin(LoginDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Username))
        {
            return UnitResult.Failure(DomainError.Validation("El nombre de usuario es requerido"));
        }

        if (string.IsNullOrWhiteSpace(dto.Password))
        {
            return UnitResult.Failure(DomainError.Validation("La contraseña es requerida"));
        }

        return UnitResult.Success<DomainError>();
    }

    /// <summary>
    /// Verifica duplicados de username y email.
    /// Returns: UnitResult.Success | UnitResult.Failure(Conflict)
    /// </summary>
    private async Task<UnitResult<DomainError>> CheckDuplicatesAsync(RegisterDto dto)
    {
        var usernameCheckTask = _userRepository.FindByUsernameAsync(dto.Username!);
        var emailCheckTask = _userRepository.FindByEmailAsync(dto.Email!);

        await Task.WhenAll(usernameCheckTask, emailCheckTask);

        var existingUser = await usernameCheckTask;
        if (existingUser != null)
        {
            return UnitResult.Failure(DomainError.Conflict("El nombre de usuario ya existe"));
        }

        var existingEmail = await emailCheckTask;
        if (existingEmail != null)
        {
            return UnitResult.Failure(DomainError.Conflict("El email ya existe"));
        }

        return UnitResult.Success<DomainError>();
    }

    /// <summary>
    /// Genera la respuesta de autenticación con token JWT.
    /// Returns: AuthResponseDto
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
}

using CSharpFunctionalExtensions;
using FluentValidation;
using TiendaApi.Api.Dtos.Usuarios;
using TiendaApi.Api.Errors;
using AuthErrors = TiendaApi.Api.Errors.Auth.AuthError;
using TiendaApi.Api.Models;
using TiendaApi.Api.Repositories.Usuarios;
using TiendaApi.Api.Validators.Usuarios;

namespace TiendaApi.Api.Services.Auth;

/// <summary>
/// Servicio de autenticación usando Patrón Result.
/// Encapsula la lógica de autenticación con Programación Orientada al Resultado.
/// </summary>
public class AuthService(
    IUserRepository userRepository,
    IJwtService jwtService,
    ILogger<AuthService> logger,
    IValidator<RegisterDto> registerValidator,
    IValidator<LoginDto> loginValidator
) : IAuthService
{

    /// <summary>
    /// Registra un nuevo usuario.
    /// Devuelve: Result.Success(AuthResponseDto) | Result.Failure(Validation/Conflict)
    /// </summary>
    public async Task<Result<AuthResponseDto, DomainError>> SignUpAsync(RegisterDto dto)
    {
        var sanitizedUsername = dto.Username?.Replace("\n", "").Replace("\r", "");
        logger.LogInformation("SignUp request for username: {Username}", sanitizedUsername);

        var validationResult = await ValidateRegistrationAsync(dto);
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
            IsDeleted = false
        };

        var savedUser = await userRepository.SaveAsync(user);
        var authResponse = GenerateAuthResponse(savedUser);

        logger.LogInformation("User registered successfully: {Username}", sanitizedUsername);

        return Result.Success<AuthResponseDto, DomainError>(authResponse);
    }

    /// <summary>
    /// Autentica un usuario existente.
    /// Devuelve: Result.Success(AuthResponseDto) | Result.Failure(Validation/Unauthorized/NotFound)
    /// </summary>
    public async Task<Result<AuthResponseDto, DomainError>> SignInAsync(LoginDto dto)
    {
        var sanitizedUsername = dto.Username?.Replace("\n", "").Replace("\r", "");
        logger.LogInformation("SignIn request for username: {Username}", sanitizedUsername);

        var validationResult = await ValidateLoginAsync(dto);
        if (validationResult.IsFailure)
        {
            return Result.Failure<AuthResponseDto, DomainError>(validationResult.Error);
        }

        var user = await userRepository.FindByUsernameAsync(dto.Username!);
        if (user is null)
        {
            logger.LogWarning("SignIn fallido: Usuario no encontrado - {Username}", sanitizedUsername);
            return Result.Failure<AuthResponseDto, DomainError>(
                AuthErrors.CredencialesInvalidas()
            );
        }

        var passwordValid = BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash);
        if (!passwordValid)
        {
            logger.LogWarning("SignIn fallido: Password inválido - {Username}", sanitizedUsername);
            return Result.Failure<AuthResponseDto, DomainError>(
                AuthErrors.CredencialesInvalidas()
            );
        }

        var authResponse = GenerateAuthResponse(user);
        logger.LogInformation("Usuario inició sesión correctamente: {Username}", sanitizedUsername);

        return Result.Success<AuthResponseDto, DomainError>(authResponse);
    }

    /// <summary>
    /// Valida el registro usando FluentValidation.
    /// Devuelve: UnitResult.Success | UnitResult.Failure(Validation)
    /// </summary>
    private async Task<UnitResult<DomainError>> ValidateRegistrationAsync(RegisterDto dto)
    {
        var validationResult = await registerValidator.ValidateAsync(dto);

        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.ErrorMessage).ToArray()
                );

            return UnitResult.Failure<DomainError>(
                AuthErrors.ValidacionConCampos(errors)
            );
        }

        return UnitResult.Success<DomainError>();
    }

    /// <summary>
    /// Valida el login usando FluentValidation.
    /// Devuelve: UnitResult.Success | UnitResult.Failure(Validation)
    /// </summary>
    private async Task<UnitResult<DomainError>> ValidateLoginAsync(LoginDto dto)
    {
        var validationResult = await loginValidator.ValidateAsync(dto);

        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.ErrorMessage).ToArray()
                );

            return UnitResult.Failure<DomainError>(
                AuthErrors.ValidacionConCampos(errors)
            );
        }

        return UnitResult.Success<DomainError>();
    }

    /// <summary>
    /// Verifica duplicados de username y email.
    /// Devuelve: UnitResult.Success | UnitResult.Failure(Conflict)
    /// </summary>
    private async Task<UnitResult<DomainError>> CheckDuplicatesAsync(RegisterDto dto)
    {
        var existingUser = await userRepository.FindByUsernameAsync(dto.Username!);
        if (existingUser is not null)
        {
            return UnitResult.Failure<DomainError>(AuthErrors.UsernameExistente(dto.Username!));
        }

        var existingEmail = await userRepository.FindByEmailAsync(dto.Email!);
        if (existingEmail is not null)
        {
            return UnitResult.Failure<DomainError>(AuthErrors.EmailExistente(dto.Email!));
        }

        return UnitResult.Success<DomainError>();
    }

    /// <summary>
    /// Genera la respuesta de autenticación con token JWT.
    /// Devuelve: AuthResponseDto
    /// </summary>
    private AuthResponseDto GenerateAuthResponse(User user)
    {
        var token = jwtService.GenerateToken(user);

        var userDto = new UserDto(
            user.Id,
            user.Username,
            user.Email,
            user.Avatar ?? string.Empty,
            user.Role,
            user.CreatedAt
        );

        return new AuthResponseDto(token, userDto);
    }
}

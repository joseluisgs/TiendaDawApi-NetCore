using CSharpFunctionalExtensions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TiendaApi.Api.Dtos.Auth;
using TiendaApi.Api.Dtos.Usuarios;
using TiendaApi.Api.Errors;
using TiendaApi.Api.Models;
using AuthErrors = TiendaApi.Api.Errors.Auth.AuthError;

namespace TiendaApi.Api.Services.Auth;

/// <summary>
/// Servicio de autenticación con ASP.NET Core Identity (Auth:Mode = "Identity").
///
/// A diferencia de <see cref="AuthService"/> (BCrypt manual + JWT), este servicio
/// delega en UserManager/SignInManager de Identity para:
/// - Hash de contraseñas (PBKDF2 en vez de BCrypt)
/// - Validación de credenciales
/// - Gestión de usuarios (CRUD con validaciones integradas)
///
/// El JWT se genera igual que en modo Custom: el middleware solo valida el token,
/// no sabe cómo se creó la sesión.
/// </summary>
public class IdentityAuthService(
    UserManager<User> userManager,
    SignInManager<User> signInManager,
    IJwtService jwtService,
    ILogger<IdentityAuthService> logger
) : IAuthService
{
    /// <summary>
    /// Registra un nuevo usuario con Identity.
    /// </summary>
    public async Task<Result<AuthResponseDto, DomainError>> SignUpAsync(RegisterDto dto)
    {
        logger.LogInformation("SignUp (Identity) para: {Username}", dto.Username);

        var user = new User
        {
            UserName = dto.Username,
            Email = dto.Email,
            Nombre = "",
            Apellidos = "",
            Role = UserRoles.USER
        };

        var result = await userManager.CreateAsync(user, dto.Password!);

        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            logger.LogWarning("SignUp falló: {Errors}", errors);

            if (result.Errors.Any(e => e.Code.Contains("Duplicate")))
                return Result.Failure<AuthResponseDto, DomainError>(AuthErrors.EmailExistente(dto.Email!));

            return Result.Failure<AuthResponseDto, DomainError>(
                new BusinessRuleError(errors));
        }

        logger.LogInformation("Usuario Identity creado: {Email}", dto.Email);
        return Result.Success<AuthResponseDto, DomainError>(GenerateAuthResponse(user));
    }

    /// <summary>
    /// Inicia sesión con Identity (verifica contraseña con PBKDF2).
    /// </summary>
    public async Task<Result<AuthResponseDto, DomainError>> SignInAsync(LoginDto dto)
    {
        logger.LogInformation("SignIn (Identity) para: {Username}", dto.Username);

        var user = await userManager.FindByNameAsync(dto.Username!)
            ?? await userManager.FindByEmailAsync(dto.Username!);

        if (user is null)
        {
            logger.LogWarning("SignIn falló: usuario no encontrado");
            return Result.Failure<AuthResponseDto, DomainError>(AuthErrors.CredencialesInvalidas());
        }

        var result = await signInManager.CheckPasswordSignInAsync(user, dto.Password!, lockoutOnFailure: false);

        if (!result.Succeeded)
        {
            logger.LogWarning("SignIn falló: contraseña inválida");
            return Result.Failure<AuthResponseDto, DomainError>(AuthErrors.CredencialesInvalidas());
        }

        logger.LogInformation("SignIn exitoso: {Email}", user.Email);
        return Result.Success<AuthResponseDto, DomainError>(GenerateAuthResponse(user));
    }

    /// <summary>
    /// Inicia sesión con proveedor externo (OAuth 2.0) usando Identity.
    /// </summary>
    public async Task<Result<AuthResponseDto, DomainError>> LoginExternalAsync(
        Dtos.Auth.ExternalLoginDto info)
    {
        logger.LogInformation("Login externo (Identity) con {Provider}", info.LoginProvider);

        if (string.IsNullOrWhiteSpace(info.Email))
            return Result.Failure<AuthResponseDto, DomainError>(
                new BusinessRuleError("El proveedor no devolvió email"));

        var user = await userManager.FindByEmailAsync(info.Email);

        if (user is null)
        {
            user = new User
            {
                UserName = info.Email,
                Email = info.Email,
                Nombre = info.Name ?? "",
                Role = UserRoles.USER
            };
            await userManager.CreateAsync(user);
            logger.LogInformation("Usuario OAuth Identity creado: {Email}", info.Email);
        }

        // Vincular login externo
        var loginInfo = new UserLoginInfo(info.LoginProvider, info.ProviderKey, info.LoginProvider);
        var existingLogins = await userManager.GetLoginsAsync(user);
        if (!existingLogins.Any(l => l.LoginProvider == info.LoginProvider))
        {
            await userManager.AddLoginAsync(user, loginInfo);
        }

        return Result.Success<AuthResponseDto, DomainError>(GenerateAuthResponse(user));
    }

    /// <summary>
    /// Genera la respuesta con JWT (mismo flujo que modo Custom).
    /// </summary>
    private AuthResponseDto GenerateAuthResponse(User user)
    {
        var token = jwtService.GenerateToken(user);

        var userDto = new UserDto(
            user.Id,
            user.UserName ?? string.Empty,
            user.Email ?? string.Empty,
            user.Avatar ?? string.Empty,
            user.Role,
            user.CreatedAt
        );

        return new AuthResponseDto(token, userDto);
    }
}

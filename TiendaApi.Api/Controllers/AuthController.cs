using Asp.Versioning;
using CSharpFunctionalExtensions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using TiendaApi.Api.Dtos.Auth;
using TiendaApi.Api.Dtos.Usuarios;
using TiendaApi.Api.Errors;
using TiendaApi.Api.Extensions;
using TiendaApi.Api.Services.Auth;
using TiendaApi.Api.Services.Cache;

namespace TiendaApi.Api.Controllers;

/// <summary>
/// Controlador de API para autenticación de usuarios.
/// Endpoints: SignUp (registro) y SignIn (login) con JWT.
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
[Produces("application/json")]
public class AuthController(
    IAuthService authService,
    IAuthenticationSchemeProvider schemeProvider,
    IAuthenticationService authenticationService,
    ILogger<AuthController> logger
) : ControllerBase
{
    /// <summary>
    /// Registra un nuevo usuario en el sistema.
    /// </summary>
    /// <param name="dto">Datos de registro (username, email, password).</param>
    /// <returns>201 Created con la respuesta de autenticación, o 400/409 si hay errores.</returns>
    [HttpPost("signup")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AuthResponseDto>> SignUp([FromBody] RegisterDto dto)
    {
        logger.LogInformation("Signup request received for user: {Username}", dto.Username);

        var resultado = await authService.SignUpAsync(dto);

        return resultado.Match(
            response => CreatedAtAction(nameof(SignUp), response),
            error => error.ToHttpResult<AuthResponseDto>()
        );
    }

    /// <summary>
    /// Inicia sesión y devuelve un token JWT.
    /// </summary>
    /// <param name="dto">Credenciales de acceso (username, password).</param>
    /// <returns>200 OK con el token JWT, o 401 si las credenciales son inválidas.</returns>
    [HttpPost("signin")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponseDto>> SignIn([FromBody] LoginDto dto)
    {
        logger.LogInformation("Petición de inicio de sesión recibida para usuario: {Username}", dto.Username);

        var resultado = await authService.SignInAsync(dto);

        return resultado.Match(
            response => response,
            error => error.ToHttpResult<AuthResponseDto>()
        );
    }

    /// <summary>
    /// Inicia login externo con un proveedor OAuth 2.0 (Google, GitHub, Microsoft).
    /// Redirige al usuario al proveedor para que autentique.
    /// </summary>
    /// <param name="provider">Nombre del proveedor: "Google", "GitHub" o "Microsoft".</param>
    /// <returns>302 Redirect al proveedor OAuth, o 404 si el proveedor no está configurado.</returns>
    [HttpGet("external/{provider}")]
    [ProducesResponseType(StatusCodes.Status302Found)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ExternalLogin([FromRoute] string provider)
    {
        var scheme = await schemeProvider.GetSchemeAsync(provider);
        if (scheme is null)
        {
            logger.LogWarning("Proveedor OAuth no configurado: {Provider}", provider);
            return NotFound($"Proveedor '{provider}' no está configurado");
        }

        var redirectUrl = Url.Action(nameof(ExternalLoginCallback), "Auth");
        var properties = new AuthenticationProperties { RedirectUri = redirectUrl };

        logger.LogInformation("Iniciando login externo con {Provider}", provider);
        return Challenge(properties, provider);
    }

    /// <summary>
    /// Callback del proveedor OAuth 2.0. Procesa la respuesta del proveedor,
    /// busca/crea el usuario local y devuelve un JWT propio.
    /// </summary>
    /// <returns>200 OK con token JWT, o 400 si hay errores.</returns>
    [HttpGet("external-callback")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AuthResponseDto>> ExternalLoginCallback()
    {
        var authenticateResult = await authenticationService.AuthenticateAsync(
            HttpContext, "ExternalCookie");
        // Fallback: algunos proveedores usan el esquema directamente
        if (!authenticateResult.Succeeded)
        {
            // Intentar con cada esquema disponible
            foreach (var scheme in await schemeProvider.GetAllSchemesAsync())
            {
                if (scheme.Name is "Bearer" or "Cookies" or "ExternalCookie") continue;
                authenticateResult = await authenticationService.AuthenticateAsync(
                    HttpContext, scheme.Name);
                if (authenticateResult.Succeeded) break;
            }
        }

        if (!authenticateResult.Succeeded || authenticateResult.Principal is null)
        {
            logger.LogWarning("Login externo falló: sin principal autenticado");
            return BadRequest("No se pudo autenticar con el proveedor externo");
        }

        var claims = authenticateResult.Principal.Claims;
        var externalLogin = new ExternalLoginDto(
            LoginProvider: authenticateResult.Properties?.Items[".AuthScheme"] ?? "Unknown",
            ProviderKey: claims.FirstOrDefault(c => c.Type.EndsWith("nameidentifier"))?.Value
                ?? claims.FirstOrDefault(c => c.Type.EndsWith("sub"))?.Value
                ?? string.Empty,
            Email: claims.FirstOrDefault(c => c.Type.EndsWith("email"))?.Value,
            Name: claims.FirstOrDefault(c => c.Type.EndsWith("name"))?.Value,
            Avatar: claims.FirstOrDefault(c => c.Type.EndsWith("picture"))?.Value
        );

        var resultado = await authService.LoginExternalAsync(externalLogin);

        return resultado.Match(
            response => response,
            error => error.ToHttpResult<AuthResponseDto>()
        );
    }
}

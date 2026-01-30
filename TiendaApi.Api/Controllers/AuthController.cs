using CSharpFunctionalExtensions;
using Microsoft.AspNetCore.Mvc;
using TiendaApi.Api.Dtos.Usuarios;
using TiendaApi.Api.Errors;
using TiendaApi.Api.Services.Auth;

namespace TiendaApi.Api.Controllers;

/// <summary>
/// Controlador de API para autenticación de usuarios.
/// Proporciona endpoints para registro (SignUp) e inicio de sesión (SignIn) emitiendo tokens JWT.
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
[Produces("application/json")]
public class AuthController(
    IAuthService authService,
    ILogger<AuthController> logger
) : ControllerBase
{
    /// <summary>
    /// Registra un nuevo usuario en el sistema.
    /// </summary>
    /// <param name="dto">Objeto con los datos de registro (username, email, password).</param>
    /// <returns>
    /// 201 Created con la respuesta de autenticación (token y datos de usuario), 
    /// o 400 BadRequest si hay fallos de validación, 
    /// o 409 Conflict si el usuario o email ya existen.
    /// </returns>
    [HttpPost("signup")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> SignUp([FromBody] RegisterDto dto)
    {
        logger.LogInformation("Signup request received for user: {Username}", dto.Username);

        var resultado = await authService.SignUpAsync(dto);

        return resultado.Match(
            response => CreatedAtAction(nameof(SignUp), response),
            error => error switch
            {
                ValidationError validationError => BadRequest(new { message = validationError.Message }),
                ConflictError conflictError => Conflict(new { message = conflictError.Message }),
                _ => StatusCode(500, new { message = error.Message })
            }
        );
    }

    /// <summary>
    /// Inicia sesión y devuelve un token JWT válido.
    /// </summary>
    /// <param name="dto">Credenciales de acceso (username y password).</param>
    /// <returns>
    /// 200 OK con el token JWT y datos de perfil, 
    /// o 401 Unauthorized si las credenciales son inválidas.
    /// </returns>
    [HttpPost("signin")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> SignIn([FromBody] LoginDto dto)
    {
        logger.LogInformation("Petición de inicio de sesión recibida para usuario: {Username}", dto.Username);

        var resultado = await authService.SignInAsync(dto);

        return resultado.Match(
            response => Ok(response),
            error => error switch
            {
                UnauthorizedError unauthorizedError => Unauthorized(new { message = unauthorizedError.Message }),
                ValidationError validationError => BadRequest(new { message = validationError.Message }),
                _ => StatusCode(500, new { message = error.Message })
            }
        );
    }
}
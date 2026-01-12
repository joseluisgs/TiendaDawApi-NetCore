using CSharpFunctionalExtensions;
using Microsoft.AspNetCore.Mvc;
using TiendaApi.Dtos.Usuarios;
using TiendaApi.Errors;
using TiendaApi.Services.Auth;

namespace TiendaApi.Controllers;

/// <summary>
///     Controlador de autenticación para registro e inicio de sesión.
/// </summary>
[ApiController]
[Route("v1/[controller]")]
[Produces("application/json")]
public class AuthController(
    IAuthService authService,
    ILogger<AuthController> logger
) : ControllerBase {

    /// <summary>
    ///     Registrar un nuevo usuario.
    ///     POST /v1/auth/signup
    ///     Returns: 201 Created | 400 Bad Request | 409 Conflict
    /// </summary>
    [HttpPost("signup")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> SignUp([FromBody] RegisterDto dto) {
        logger.LogInformation("Signup request received for user: {Username}", dto.Username);
        
        var resultado = await authService.SignUpAsync(dto);

        return resultado.Match(
            response => CreatedAtAction(nameof(SignUp), response),
            error => error.Type switch {
                ErrorType.Validation => BadRequest(new { message = error.Message }),
                ErrorType.Conflict => Conflict(new { message = error.Message }),
                _ => StatusCode(500, new { message = error.Message })
            }
        );
    }

    /// <summary>
    ///     Iniciar sesión y obtener token JWT.
    ///     POST /v1/auth/signin
    ///     Returns: 200 OK | 401 Unauthorized
    /// </summary>
    [HttpPost("signin")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> SignIn([FromBody] LoginDto dto) {
        logger.LogInformation("Petición de inicio de sesión recibida para usuario: {Username}", dto.Username);
        
        var resultado = await authService.SignInAsync(dto);

        return resultado.Match(
            response => Ok(response),
            error => error.Type switch {
                ErrorType.Unauthorized => Unauthorized(new { message = error.Message }),
                ErrorType.Validation => BadRequest(new { message = error.Message }),
                _ => StatusCode(500, new { message = error.Message })
            }
        );
    }
}

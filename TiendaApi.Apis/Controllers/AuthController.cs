using CSharpFunctionalExtensions;
using Microsoft.AspNetCore.Mvc;
using TiendaApi.Apis.Dtos.Usuarios;
using TiendaApi.Apis.Errors;
using TiendaApi.Apis.Services.Auth;

namespace TiendaApi.Apis.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
[Produces("application/json")]
public class AuthController(
    IAuthService authService,
    ILogger<AuthController> logger
) : ControllerBase
{
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

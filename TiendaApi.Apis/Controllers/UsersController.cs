using System.Security.Claims;
using CSharpFunctionalExtensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TiendaApi.Apis.Dtos.Usuarios;
using TiendaApi.Apis.Errors;
using TiendaApi.Apis.Models;
using TiendaApi.Apis.Services.Storage;
using TiendaApi.Apis.Services.Users;

namespace TiendaApi.Apis.Controllers;

/// <summary>
/// Controlador de usuarios para administradores.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class UsersController(
    IUserService service,
    ILogger<UsersController> logger
) : ControllerBase
{

    /// <summary>
    /// Obtener todos los usuarios (solo administradores).
    /// GET /api/users
    /// Returns: 200 OK | 401 Unauthorized | 403 Forbidden
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "ADMIN")]
    [ProducesResponseType(typeof(IEnumerable<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAll()
    {
        logger.LogInformation("Obteniendo todos los usuarios");

        var resultado = await service.FindAllAsync();

        return resultado.Match(
            onSuccess: usuarios => Ok(usuarios),
            onFailure: error => StatusCode(500, new { message = error.Message })
        );
    }

    /// <summary>
    /// Obtener un usuario por ID (solo administradores).
    /// GET /api/users/{id}
    /// Returns: 200 OK | 401 Unauthorized | 403 Forbidden | 404 Not Found
    /// </summary>
    [HttpGet("{id}")]
    [Authorize(Roles = "ADMIN")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(long id)
    {
        logger.LogInformation("Obteniendo usuario con ID: {Id}", id);

        var resultado = await service.FindByIdAsync(id);

        return resultado.Match(
            onSuccess: usuario => Ok(usuario),
            onFailure: error => error.Type switch
            {
                ErrorType.NotFound => NotFound(new { message = error.Message }),
                _ => StatusCode(500, new { message = error.Message })
            }
        );
    }

    /// <summary>
    /// Crear un nuevo usuario (solo administradores).
    /// POST /api/users
    /// Returns: 201 Created | 400 Bad Request | 401 Unauthorized | 403 Forbidden | 409 Conflict
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "ADMIN")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] RegisterDto dto)
    {
        logger.LogInformation("Creando nuevo usuario: {Username}", dto.Username);

        var resultado = await service.CreateAsync(dto);

        return resultado.Match(
            onSuccess: usuario => CreatedAtAction(nameof(GetById), new { id = usuario.Id }, usuario),
            onFailure: error => error.Type switch
            {
                ErrorType.Validation => BadRequest(new { message = error.Message, errors = error.ValidationErrors }),
                ErrorType.Conflict => Conflict(new { message = error.Message }),
                _ => StatusCode(500, new { message = error.Message })
            }
        );
    }

    /// <summary>
    /// Actualizar un usuario existente (solo administradores).
    /// PUT /api/users/{id}
    /// Returns: 200 OK | 400 Bad Request | 401 Unauthorized | 403 Forbidden | 404 Not Found | 409 Conflict
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "ADMIN")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(long id, [FromBody] UserUpdateDto dto)
    {
        logger.LogInformation("Actualizando usuario con ID: {Id}", id);

        var resultado = await service.UpdateAsync(id, dto);

        return resultado.Match(
            onSuccess: usuario => Ok(usuario),
            onFailure: error => error.Type switch
            {
                ErrorType.NotFound => NotFound(new { message = error.Message }),
                ErrorType.Validation => BadRequest(new { message = error.Message, errors = error.ValidationErrors }),
                ErrorType.Conflict => Conflict(new { message = error.Message }),
                _ => StatusCode(500, new { message = error.Message })
            }
        );
    }

    /// <summary>
    /// Actualizar avatar de un usuario (ADMIN o el propio usuario).
    /// PATCH /api/users/{id}/avatar
    /// Returns: 200 OK | 400 Bad Request | 401 Unauthorized | 403 Forbidden | 404 Not Found
    /// </summary>
    [HttpPatch("{id}/avatar")]
    [Authorize]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateAvatar(long id, [FromBody] AvatarUpdateDto dto)
    {
        logger.LogInformation("Actualizando avatar de usuario con ID: {Id}", id);

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !long.TryParse(userIdClaim, out var currentUserId))
            return Unauthorized(new { message = "Usuario no autenticado correctamente" });

        var resultado = await service.UpdateAvatarAsync(id, dto.AvatarUrl);

        return resultado.Match(
            onSuccess: usuario => Ok(usuario),
            onFailure: error => error.Type switch
            {
                ErrorType.NotFound => NotFound(new { message = error.Message }),
                ErrorType.Validation => BadRequest(new { message = error.Message }),
                _ => StatusCode(500, new { message = error.Message })
            }
        );
    }

    /// <summary>
    /// Eliminar un usuario (solo administradores).
    /// DELETE /api/users/{id}
    /// Returns: 204 No Content | 401 Unauthorized | 403 Forbidden | 404 Not Found
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "ADMIN")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(long id)
    {
        logger.LogInformation("Eliminando usuario con ID: {Id}", id);

        var resultado = await service.DeleteAsync(id);

        if (resultado.IsSuccess)
            return NoContent();

        var error = resultado.Error;
        return error.Type switch
        {
            ErrorType.NotFound => NotFound(new { message = error.Message }),
            _ => StatusCode(500, new { message = error.Message })
        };
    }

    /// <summary>
    /// Obtener el perfil del usuario autenticado.
    /// GET /api/users/me/profile
    /// Returns: 200 OK | 401 Unauthorized
    /// </summary>
    [HttpGet("me/profile")]
    [Authorize]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyProfile()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !long.TryParse(userIdClaim, out var userId))
            return Unauthorized(new { message = "Usuario no autenticado correctamente" });

        var resultado = await service.FindByIdAsync(userId);

        return resultado.Match(
            onSuccess: usuario => Ok(usuario),
            onFailure: error => error.Type switch
            {
                ErrorType.NotFound => NotFound(new { message = error.Message }),
                _ => StatusCode(500, new { message = error.Message })
            }
        );
    }

    /// <summary>
    /// Actualizar el perfil del usuario autenticado.
    /// PUT /api/users/me/profile
    /// Returns: 200 OK | 400 Bad Request | 401 Unauthorized | 404 Not Found
    /// </summary>
    [HttpPut("me/profile")]
    [Authorize]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateMyProfile([FromBody] UserUpdateDto dto)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !long.TryParse(userIdClaim, out var userId))
            return Unauthorized(new { message = "Usuario no autenticado correctamente" });

        logger.LogInformation("Usuario {UserId} actualizando su perfil", userId);

        var resultado = await service.UpdateAsync(userId, dto);

        return resultado.Match(
            onSuccess: usuario => Ok(usuario),
            onFailure: error => error.Type switch
            {
                ErrorType.NotFound => NotFound(new { message = error.Message }),
                ErrorType.Validation => BadRequest(new { message = error.Message, errors = error.ValidationErrors }),
                ErrorType.Conflict => Conflict(new { message = error.Message }),
                _ => StatusCode(500, new { message = error.Message })
            }
        );
    }

    /// <summary>
    /// Eliminar la cuenta del usuario autenticado.
    /// DELETE /api/users/me/profile
    /// Returns: 204 No Content | 401 Unauthorized
    /// </summary>
    [HttpDelete("me/profile")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DeleteMyProfile()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !long.TryParse(userIdClaim, out var userId))
            return Unauthorized(new { message = "Usuario no autenticado correctamente" });

        logger.LogInformation("Usuario {UserId} eliminando su cuenta", userId);

        var resultado = await service.DeleteAsync(userId);

        if (resultado.IsSuccess)
            return NoContent();

        var error = resultado.Error;
        return error.Type switch
        {
            ErrorType.NotFound => NotFound(new { message = error.Message }),
            _ => StatusCode(500, new { message = error.Message })
        };
    }
}

/// <summary>
/// DTO para actualizar el avatar de un usuario.
/// </summary>
public record AvatarUpdateDto
{
    /// <summary>
    /// URL del nuevo avatar.
    /// </summary>
    public string AvatarUrl { get; init; } = string.Empty;
}

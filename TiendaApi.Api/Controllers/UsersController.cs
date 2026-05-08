using System.Security.Claims;
using CSharpFunctionalExtensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TiendaApi.Api.Dtos.Common;
using TiendaApi.Api.Dtos.Usuarios;
using TiendaApi.Api.Errors;
using TiendaApi.Api.Models;
using TiendaApi.Api.Services.Users;
using TiendaApi.Api.Helpers.Pagination;

namespace TiendaApi.Api.Controllers;

/// <summary>
/// Controlador de API para gestión de usuarios.
/// Endpoints: CRUD paginado, solo accesibles por administradores.
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
    /// Obtiene todos los usuarios paginados con filtros opcionales.
    /// </summary>
    /// <param name="username">Filtrar por nombre de usuario (contiene).</param>
    /// <param name="email">Filtrar por email (contiene).</param>
    /// <param name="isDeleted">Filtrar por estado de eliminación.</param>
    /// <param name="page">Número de página (0-indexed).</param>
    /// <param name="size">Elementos por página.</param>
    /// <param name="sortBy">Campo de ordenación.</param>
    /// <param name="direction">Dirección (asc, desc).</param>
    /// <returns>200 OK con lista paginada de usuarios.</returns>
    [HttpGet]
    [Authorize(Roles = UserRoles.ADMIN)]
    [ProducesResponseType(typeof(PagedResult<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? username = null,
        [FromQuery] string? email = null,
        [FromQuery] bool? isDeleted = null,
        [FromQuery] int page = 0,
        [FromQuery] int size = 10,
        [FromQuery] string sortBy = "id",
        [FromQuery] string direction = "asc")
    {
        logger.LogInformation("Obteniendo todos los usuarios - Página: {Page}, Tamaño: {Size}", page, size);

        var filter = new UserFilterDto(
            username,
            email,
            isDeleted,
            page,
            size,
            sortBy,
            direction
        );

        var resultado = await service.FindAllPagedAsync(filter);

        return resultado.Match(
            onSuccess: pagedResult =>
            {
                var linkHeader = PaginationLinksHelper.CreateLinkHeader(pagedResult, Request, sortBy, direction);
                if (!string.IsNullOrEmpty(linkHeader))
                    Response.Headers.Append("Link", linkHeader);
                return Ok(pagedResult);
            },
            onFailure: error => StatusCode(500, new { message = error.Message })
        );
    }

    /// <summary>
    /// Obtiene un usuario por su ID.
    /// </summary>
    /// <param name="id">ID del usuario.</param>
    /// <returns>200 OK con el usuario, o 404 si no existe.</returns>
    [HttpGet("{id}")]
    [Authorize(Roles = UserRoles.ADMIN)]
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
            onFailure: error => error switch
            {
                NotFoundError => NotFound(new { message = error.Message }),
                _ => StatusCode(500, new { message = error.Message })
            }
        );
    }

    /// <summary>
    /// Crea un nuevo usuario en el sistema.
    /// </summary>
    /// <param name="dto">Datos del usuario a crear.</param>
    /// <returns>201 Created con el usuario creado, o 400/409 si hay errores.</returns>
    [HttpPost]
    [Authorize(Roles = UserRoles.ADMIN)]
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
            onFailure: error => error switch
            {
                ValidationError ve => BadRequest(new { message = ve.Message, errors = ve.ValidationErrors }),
                ConflictError => Conflict(new { message = error.Message }),
                _ => StatusCode(500, new { message = error.Message })
            }
        );
    }

    /// <summary>
    /// Actualiza un usuario existente.
    /// </summary>
    /// <param name="id">ID del usuario.</param>
    /// <param name="dto">Nuevos datos del usuario.</param>
    /// <returns>200 OK con el usuario actualizado, o 400/404/409 si hay errores.</returns>
    [HttpPut("{id}")]
    [Authorize(Roles = UserRoles.ADMIN)]
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
            onFailure: error => error switch
            {
                NotFoundError => NotFound(new { message = error.Message }),
                ValidationError ve => BadRequest(new { message = ve.Message, errors = ve.ValidationErrors }),
                ConflictError => Conflict(new { message = error.Message }),
                _ => StatusCode(500, new { message = error.Message })
            }
        );
    }

    /// <summary>
    /// Actualiza el avatar de un usuario.
    /// </summary>
    /// <param name="id">ID del usuario.</param>
    /// <param name="dto">URL del nuevo avatar.</param>
    /// <returns>200 OK con el usuario actualizado, o 400/404 si hay errores.</returns>
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

        if (id != currentUserId && userRole != UserRoles.ADMIN)
            return Forbid();

        var resultado = await service.UpdateAvatarAsync(id, dto.AvatarUrl);

        return resultado.Match(
            onSuccess: usuario => Ok(usuario),
            onFailure: error => error switch
            {
                NotFoundError => NotFound(new { message = error.Message }),
                ValidationError => BadRequest(new { message = error.Message }),
                _ => StatusCode(500, new { message = error.Message })
            }
        );
    }

    /// <summary>
    /// Elimina un usuario (soft-delete).
    /// </summary>
    /// <param name="id">ID del usuario a eliminar.</param>
    /// <returns>204 No Content si tiene éxito, o 404 si no existe.</returns>
    [HttpDelete("{id}")]
    [Authorize(Roles = UserRoles.ADMIN)]
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
        return error switch
        {
            NotFoundError => NotFound(new { message = error.Message }),
            _ => StatusCode(500, new { message = error.Message })
        };
    }

    /// <summary>
    /// Obtiene el perfil del usuario autenticado.
    /// </summary>
    /// <returns>200 OK con los datos del usuario.</returns>
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
            onFailure: error => error switch
            {
                NotFoundError => NotFound(new { message = error.Message }),
                _ => StatusCode(500, new { message = error.Message })
            }
        );
    }

    /// <summary>
    /// Actualiza el perfil del usuario autenticado.
    /// </summary>
    /// <param name="dto">Nuevos datos del perfil.</param>
    /// <returns>200 OK con el usuario actualizado, o 400/404 si hay errores.</returns>
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
            onFailure: error => error switch
            {
                NotFoundError => NotFound(new { message = error.Message }),
                ValidationError ve => BadRequest(new { message = ve.Message, errors = ve.ValidationErrors }),
                ConflictError => Conflict(new { message = error.Message }),
                _ => StatusCode(500, new { message = error.Message })
            }
        );
    }

    /// <summary>
    /// Elimina la cuenta del usuario autenticado (soft-delete).
    /// </summary>
    /// <returns>204 No Content si tiene éxito.</returns>
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
        return error switch
        {
            NotFoundError => NotFound(new { message = error.Message }),
            _ => StatusCode(500, new { message = error.Message })
        };
    }

    /// <summary>
    /// Actualiza el avatar del usuario autenticado.
    /// </summary>
    /// <param name="dto">URL del nuevo avatar.</param>
    /// <returns>200 OK con el usuario actualizado, o 400/404 si hay errores.</returns>
    [HttpPatch("me/profile/avatar")]
    [Authorize]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateMyAvatar([FromBody] AvatarUpdateDto dto)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !long.TryParse(userIdClaim, out var userId))
            return Unauthorized(new { message = "Usuario no autenticado correctamente" });

        logger.LogInformation("Usuario {UserId} actualizando su avatar", userId);

        var resultado = await service.UpdateAvatarAsync(userId, dto.AvatarUrl);

        return resultado.Match(
            onSuccess: usuario => Ok(usuario),
            onFailure: error => error switch
            {
                NotFoundError => NotFound(new { message = error.Message }),
                ValidationError => BadRequest(new { message = error.Message }),
                _ => StatusCode(500, new { message = error.Message })
            }
        );
    }
}

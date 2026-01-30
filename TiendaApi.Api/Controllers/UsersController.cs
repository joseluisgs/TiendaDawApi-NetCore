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
/// Controlador de API para la gestión de usuarios y perfiles.
/// Proporciona endpoints para administración de usuarios (solo ADMIN) y gestión del perfil propio.
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
    /// Obtiene un listado paginado de todos los usuarios registrados.
    /// Requiere rol de administrador.
    /// </summary>
    /// <param name="username">Filtro opcional por nombre de usuario.</param>
    /// <param name="email">Filtro opcional por correo electrónico.</param>
    /// <param name="isDeleted">Filtrar por estado de eliminación lógica.</param>
    /// <param name="page">Número de página (0-indexed).</param>
    /// <param name="size">Elementos por página.</param>
    /// <param name="sortBy">Campo de ordenación.</param>
    /// <param name="direction">Dirección (asc/desc).</param>
    /// <returns>Resultado paginado de usuarios.</returns>
    [HttpGet]
    [Authorize(Roles = UserRoles.ADMIN)]
    [ProducesResponseType(typeof(PagedResult<UserDto>), StatusCodes.Status200OK)]
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

        var filter = new UserFilterDto(username, email, isDeleted, page, size, sortBy, direction);
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
    /// Recupera el perfil del usuario actualmente autenticado basado en su token JWT.
    /// </summary>
    /// <returns>Datos del perfil del usuario.</returns>
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
    /// Permite al usuario autenticado actualizar sus propios datos de perfil.
    /// </summary>
    /// <param name="dto">Nuevos datos del usuario.</param>
    /// <returns>Perfil actualizado.</returns>
    [HttpPut("me/profile")]
    [Authorize]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateMyProfile([FromBody] UserUpdateDto dto)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !long.TryParse(userIdClaim, out var userId))
            return Unauthorized(new { message = "Usuario no autenticado correctamente" });

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
}
using System.Security.Claims;
using CSharpFunctionalExtensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TiendaApi.Apis.Dtos.Common;
using TiendaApi.Apis.Dtos.Usuarios;
using TiendaApi.Apis.Errors;
using TiendaApi.Apis.Models;
using TiendaApi.Apis.Services.Pedidos;
using TiendaApi.Apis.Services.Storage;
using TiendaApi.Apis.Services.Users;
using TiendaApi.Apis.Helpers.Pagination;

namespace TiendaApi.Apis.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class UsersController(
    IUserService service,
    ILogger<UsersController> logger
) : ControllerBase
{
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
            onFailure: error => error switch
            {
                ValidationError ve => BadRequest(new { message = ve.Message, errors = ve.ValidationErrors }),
                ConflictError => Conflict(new { message = error.Message }),
                _ => StatusCode(500, new { message = error.Message })
            }
        );
    }

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
            onFailure: error => error switch
            {
                NotFoundError => NotFound(new { message = error.Message }),
                ValidationError => BadRequest(new { message = error.Message }),
                _ => StatusCode(500, new { message = error.Message })
            }
        );
    }

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
}

public record AvatarUpdateDto
{
    public string AvatarUrl { get; init; } = string.Empty;
}

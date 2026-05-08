using System.Security.Claims;
using CSharpFunctionalExtensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TiendaApi.Api.Dtos.Common;
using TiendaApi.Api.Dtos.Pedidos;
using TiendaApi.Api.Errors;
using TiendaApi.Api.Models;
using TiendaApi.Api.Services.Pedidos;
using TiendaApi.Api.Helpers.Pagination;

namespace TiendaApi.Api.Controllers;

/// <summary>
/// Controlador REST para la gestión de pedidos.
/// Separa endpoints para administradores (todos los pedidos) y usuarios (sus pedidos).
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class PedidosController(IPedidosService service, ILogger<PedidosController> logger) : ControllerBase
{
    #region ========== ENDPOINTS DE ADMINISTRADORES ==========

    /// <summary>
    /// Obtiene todos los pedidos del sistema (solo administradores).
    /// </summary>
    [HttpGet]
    [Authorize(Roles = UserRoles.ADMIN)]
    [ProducesResponseType(typeof(IEnumerable<PedidoDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAllPedidos()
    {
        var resultado = await service.FindAllAsync();

        return resultado.Match(
            onSuccess: pedidos => Ok(pedidos),
            onFailure: error => StatusCode(500, new { message = error.Message })
        );
    }

    /// <summary>
    /// Obtiene los pedidos del sistema de forma paginada (solo administradores).
    /// </summary>
    /// <param name="page">Número de página (1-indexed).</param>
    /// <param name="size">Elementos por página.</param>
    /// <param name="sortBy">Campo de ordenación.</param>
    /// <param name="direction">Dirección (asc, desc).</param>
    /// <returns>200 OK con lista paginada de pedidos.</returns>
    [HttpGet("paged")]
    [Authorize(Roles = UserRoles.ADMIN)]
    [ProducesResponseType(typeof(PagedResult<PedidoDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAllPedidosPaged(
        [FromQuery] int page = 1,
        [FromQuery] int size = 10,
        [FromQuery] string? sortBy = null,
        [FromQuery] string? direction = null)
    {
        var resultado = await service.FindAllPagedAsync(page - 1, size);

        return resultado.Match(
            onSuccess: pedidos =>
            {
                var linkHeader = PaginationLinksHelper.CreateLinkHeader(pedidos, Request, sortBy, direction);
                if (!string.IsNullOrEmpty(linkHeader))
                    Response.Headers.Append("Link", linkHeader);
                return Ok(pedidos);
            },
            onFailure: error => StatusCode(500, new { message = error.Message })
        );
    }

    /// <summary>
    /// Obtiene un pedido específico por su ID (solo administradores).
    /// </summary>
    /// <param name="id">ID del pedido.</param>
    /// <returns>200 OK con el pedido, o 404 si no existe.</returns>
    [HttpGet("{id}")]
    [Authorize(Roles = UserRoles.ADMIN)]
    [ProducesResponseType(typeof(PedidoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPedidoById(string id)
    {
        var resultado = await service.FindByIdAsync(id);

        return resultado.Match(
            onSuccess: pedido => Ok(pedido),
            onFailure: error => error switch
            {
                NotFoundError => NotFound(new { message = error.Message }),
                _ => StatusCode(500, new { message = error.Message })
            }
        );
    }

    /// <summary>
    /// Actualiza un pedido (solo administradores).
    /// Los administradores pueden actualizar cualquier pedido.
    /// </summary>
    /// <param name="id">ID del pedido.</param>
    /// <param name="dto">Nuevos datos del pedido.</param>
    /// <returns>200 OK con el pedido actualizado, o 400/404 si hay errores.</returns>
    [HttpPut("{id}")]
    [Authorize(Roles = UserRoles.ADMIN)]
    [ProducesResponseType(typeof(PedidoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdatePedidoAdmin(string id, [FromBody] UpdatePedidoDto dto)
    {
        var resultado = await service.UpdateAdminAsync(id, dto);

        return resultado.Match(
            onSuccess: pedido => Ok(pedido),
            onFailure: error => error switch
            {
                NotFoundError => NotFound(new { message = error.Message }),
                ValidationError => BadRequest(new { message = error.Message }),
                ForbiddenError => StatusCode(403, new { message = error.Message }),
                _ => StatusCode(500, new { message = error.Message })
            }
        );
    }

    /// <summary>
    /// Elimina un pedido (solo administradores).
    /// </summary>
    /// <param name="id">ID del pedido a eliminar.</param>
    /// <returns>204 No Content si tiene éxito, o 404 si no existe.</returns>
    [HttpDelete("{id}")]
    [Authorize(Roles = UserRoles.ADMIN)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeletePedidoAdmin(string id)
    {
        var resultado = await service.DeleteAdminAsync(id);

        if (resultado.IsSuccess)
            return NoContent();

        var error = resultado.Error;
        return error switch
        {
            NotFoundError => NotFound(new { message = error.Message }),
            ForbiddenError => StatusCode(403, new { message = error.Message }),
            _ => StatusCode(500, new { message = error.Message })
        };
    }

    /// <summary>
    /// Actualiza el estado de un pedido (solo administradores).
    /// </summary>
    /// <param name="id">ID del pedido.</param>
    /// <param name="dto">Nuevo estado del pedido.</param>
    /// <returns>200 OK con el pedido actualizado, o 400/404 si hay errores.</returns>
    [HttpPut("{id}/estado")]
    [Authorize(Roles = UserRoles.ADMIN)]
    [ProducesResponseType(typeof(PedidoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdatePedidoEstado(string id, [FromBody] UpdateEstadoDto dto)
    {
        var resultado = await service.UpdateEstadoAsync(id, dto.Estado);

        return resultado.Match(
            onSuccess: pedido => Ok(pedido),
            onFailure: error => error switch
            {
                NotFoundError => NotFound(new { message = error.Message }),
                ValidationError => BadRequest(new { message = error.Message }),
                BusinessRuleError => BadRequest(new { message = error.Message }),
                ForbiddenError => StatusCode(403, new { message = error.Message }),
                _ => StatusCode(500, new { message = error.Message })
            }
        );
    }

    #endregion

    #region ========== ENDPOINTS DE USUARIOS (MIS PEDIDOS) ==========

    /// <summary>
    /// Obtiene todos los pedidos del usuario autenticado (sin paginación).
    /// </summary>
    /// <returns>200 OK con lista de pedidos del usuario.</returns>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(IEnumerable<PedidoDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyPedidos()
    {
        logger.LogInformation("GetMyPedidos - User: {User}", User?.Identity?.Name);
        logger.LogInformation("GetMyPedidos - IsAuthenticated: {IsAuth}", User?.Identity?.IsAuthenticated);
        
        if (User?.Identity == null || !User.Identity.IsAuthenticated)
            return Unauthorized(new { message = "Usuario no autenticado correctamente" });

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        logger.LogInformation("GetMyPedidos - NameIdentifier claim: {Claim}", userIdClaim);

        if (string.IsNullOrEmpty(userIdClaim) || !long.TryParse(userIdClaim, out var userId))
            return Unauthorized(new { message = "Usuario no autenticado correctamente" });

        var resultado = await service.FindByUserIdAsync(userId);

        return resultado.Match(
            onSuccess: pedidos => Ok(pedidos),
            onFailure: error => StatusCode(500, new { message = error.Message })
        );
    }

    /// <summary>
    /// Obtiene los pedidos del usuario autenticado de forma paginada.
    /// </summary>
    /// <param name="page">Número de página (1-indexed).</param>
    /// <param name="size">Elementos por página.</param>
    /// <param name="sortBy">Campo de ordenación.</param>
    /// <param name="direction">Dirección (asc, desc).</param>
    /// <returns>200 OK con lista paginada de pedidos.</returns>
    [HttpGet("me/paged")]
    [Authorize]
    [ProducesResponseType(typeof(PagedResult<PedidoDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyPedidosPaged(
        [FromQuery] int page = 1,
        [FromQuery] int size = 10,
        [FromQuery] string? sortBy = null,
        [FromQuery] string? direction = null)
    {
        if (User?.Identity == null || !User.Identity.IsAuthenticated)
            return Unauthorized(new { message = "Usuario no autenticado correctamente" });

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !long.TryParse(userIdClaim, out var userId))
            return Unauthorized(new { message = "Usuario no autenticado correctamente" });

        var resultado = await service.FindMyPedidosAsync(userId, page - 1, size);

        return resultado.Match(
            onSuccess: pedidos =>
            {
                var linkHeader = PaginationLinksHelper.CreateLinkHeader(pedidos, Request, sortBy, direction);
                if (!string.IsNullOrEmpty(linkHeader))
                    Response.Headers.Append("Link", linkHeader);
                return Ok(pedidos);
            },
            onFailure: error => StatusCode(500, new { message = error.Message })
        );
    }

    /// <summary>
    /// Crea un nuevo pedido para el usuario autenticado.
    /// </summary>
    /// <param name="dto">Datos del pedido a crear.</param>
    /// <returns>201 Created con el pedido creado, o 400/404 si hay errores.</returns>
    [HttpPost("me")]
    [Authorize]
    [ProducesResponseType(typeof(PedidoDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateMyPedido([FromBody] PedidoRequestDto dto)
    {
        if (User?.Identity == null || !User.Identity.IsAuthenticated)
            return Unauthorized(new { message = "Usuario no autenticado correctamente" });

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !long.TryParse(userIdClaim, out var userId))
            return Unauthorized(new { message = "Usuario no autenticado correctamente" });

        var resultado = await service.CreateAsync(userId, dto);

        if (resultado.IsSuccess)
        {
            var pedido = resultado.Value;
            return CreatedAtAction(nameof(GetMyPedidoById), new { id = pedido.Id }, pedido);
        }

        var error = resultado.Error;
        return error switch
        {
            NotFoundError => NotFound(new { message = error.Message }),
            ValidationError ve => BadRequest(new { message = ve.Message, errors = ve.ValidationErrors }),
            BusinessRuleError => BadRequest(new { message = error.Message }),
            ForbiddenError => StatusCode(403, new { message = error.Message }),
            ConflictError => Conflict(new { message = error.Message }),
            _ => StatusCode(500, new { message = error.Message })
        };
    }

    /// <summary>
    /// Obtiene un pedido propio por su ID.
    /// </summary>
    /// <param name="id">ID del pedido.</param>
    /// <returns>200 OK con el pedido, o 404 si no existe o no es suyo.</returns>
    [HttpGet("me/{id}")]
    [Authorize]
    [ProducesResponseType(typeof(PedidoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMyPedidoById(string id)
    {
        if (User?.Identity == null || !User.Identity.IsAuthenticated)
            return Unauthorized(new { message = "Usuario no autenticado correctamente" });

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !long.TryParse(userIdClaim, out var userId))
            return Unauthorized(new { message = "Usuario no autenticado correctamente" });

        var resultado = await service.FindMyPedidoAsync(id, userId);

        return resultado.Match(
            onSuccess: pedido => Ok(pedido),
            onFailure: error => error switch
            {
                NotFoundError => NotFound(new { message = error.Message }),
                ForbiddenError => StatusCode(403, new { message = error.Message }),
                _ => StatusCode(500, new { message = error.Message })
            }
        );
    }

    /// <summary>
    /// Actualiza un pedido propio.
    /// Solo permite modificar pedidos en estado PENDIENTE.
    /// </summary>
    /// <param name="id">ID del pedido.</param>
    /// <param name="dto">Nuevos datos del pedido.</param>
    /// <returns>200 OK con el pedido actualizado, o 400/404 si hay errores.</returns>
    [HttpPut("me/{id}")]
    [Authorize]
    [ProducesResponseType(typeof(PedidoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateMyPedido(string id, [FromBody] UpdatePedidoDto dto)
    {
        if (User?.Identity == null || !User.Identity.IsAuthenticated)
            return Unauthorized(new { message = "Usuario no autenticado correctamente" });

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !long.TryParse(userIdClaim, out var userId))
            return Unauthorized(new { message = "Usuario no autenticado correctamente" });

        var resultado = await service.UpdateMyPedidoAsync(id, userId, dto);

        return resultado.Match(
            onSuccess: pedido => Ok(pedido),
            onFailure: error => error switch
            {
                NotFoundError => NotFound(new { message = error.Message }),
                ValidationError => BadRequest(new { message = error.Message }),
                BusinessRuleError => BadRequest(new { message = error.Message }),
                ForbiddenError => StatusCode(403, new { message = error.Message }),
                _ => StatusCode(500, new { message = error.Message })
            }
        );
    }

    /// <summary>
    /// Cancela y elimina un pedido propio.
    /// Solo permite eliminar pedidos en estado PENDIENTE.
    /// </summary>
    /// <param name="id">ID del pedido a eliminar.</param>
    /// <returns>204 No Content si tiene éxito, o 400/404 si hay errores.</returns>
    [HttpDelete("me/{id}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteMyPedido(string id)
    {
        if (User?.Identity == null || !User.Identity.IsAuthenticated)
            return Unauthorized(new { message = "Usuario no autenticado correctamente" });

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !long.TryParse(userIdClaim, out var userId))
            return Unauthorized(new { message = "Usuario no autenticado correctamente" });

        var resultado = await service.DeleteMyPedidoAsync(id, userId);

        if (resultado.IsSuccess)
            return NoContent();

        var error = resultado.Error;
        return error switch
        {
            NotFoundError => NotFound(new { message = error.Message }),
            ValidationError => BadRequest(new { message = error.Message }),
            ForbiddenError => StatusCode(403, new { message = error.Message }),
            _ => StatusCode(500, new { message = error.Message })
        };
    }

    #endregion
}

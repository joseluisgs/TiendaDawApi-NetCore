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
/// Controlador de API para la gestión de pedidos de compra.
/// Ofrece funcionalidades diferenciadas para administradores y usuarios finales.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class PedidosController(IPedidosService service) : ControllerBase
{
    #region ========== ENDPOINTS DE ADMINISTRADORES ==========

    /// <summary>
    /// Recupera la totalidad de los pedidos registrados en el sistema.
    /// Solo accesible para administradores.
    /// </summary>
    /// <returns>Lista completa de pedidos.</returns>
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
    /// Obtiene un listado paginado de todos los pedidos del sistema.
    /// </summary>
    /// <param name="page">Página a recuperar (1-indexed para compatibilidad).</param>
    /// <param name="size">Número de elementos por página.</param>
    /// <param name="sortBy">Campo de ordenación.</param>
    /// <param name="direction">Sentido de la ordenación.</param>
    /// <returns>Resultado paginado de pedidos.</returns>
    [HttpGet("paged")]
    [Authorize(Roles = UserRoles.ADMIN)]
    [ProducesResponseType(typeof(PagedResult<PedidoDto>), StatusCodes.Status200OK)]
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
    /// Obtiene el detalle de un pedido específico por su ID único (GUID).
    /// </summary>
    /// <param name="id">Identificador del pedido.</param>
    /// <returns>Los datos del pedido o 404.</returns>
    [HttpGet("{id}")]
    [Authorize(Roles = UserRoles.ADMIN)]
    [ProducesResponseType(typeof(PedidoDto), StatusCodes.Status200OK)]
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
    /// Actualiza la información de un pedido (Administración).
    /// </summary>
    /// <param name="id">ID del pedido.</param>
    /// <param name="dto">Nuevos datos a aplicar.</param>
    /// <returns>Pedido actualizado.</returns>
    [HttpPut("{id}")]
    [Authorize(Roles = UserRoles.ADMIN)]
    [ProducesResponseType(typeof(PedidoDto), StatusCodes.Status200OK)]
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
    /// Elimina físicamente un pedido de la base de datos (Administración).
    /// </summary>
    /// <param name="id">ID del pedido.</param>
    /// <returns>204 No Content.</returns>
    [HttpDelete("{id}")]
    [Authorize(Roles = UserRoles.ADMIN)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
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
    /// Cambia el estado actual de un pedido (PENDIENTE, ENVIADO, etc).
    /// </summary>
    /// <param name="id">ID del pedido.</param>
    /// <param name="dto">Objeto con el nuevo estado.</param>
    /// <returns>Pedido con el nuevo estado aplicado.</returns>
    [HttpPut("{id}/estado")]
    [Authorize(Roles = UserRoles.ADMIN)]
    [ProducesResponseType(typeof(PedidoDto), StatusCodes.Status200OK)]
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
    /// Obtiene todos los pedidos realizados por el usuario autenticado.
    /// </summary>
    /// <returns>Lista de pedidos del usuario.</returns>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(IEnumerable<PedidoDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyPedidos()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !long.TryParse(userIdClaim, out var userId))
            return Unauthorized(new { message = "Usuario no autenticado correctamente" });

        var resultado = await service.FindByUserIdAsync(userId);

        return resultado.Match(
            onSuccess: pedidos => Ok(pedidos),
            onFailure: error => StatusCode(500, new { message = error.Message })
        );
    }

    /// <summary>
    /// Crea un nuevo pedido a nombre del usuario autenticado.
    /// </summary>
    /// <param name="dto">Datos del pedido (líneas, dirección, etc).</param>
    /// <returns>201 Created con el pedido generado.</returns>
    [HttpPost("me")]
    [Authorize]
    [ProducesResponseType(typeof(PedidoDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateMyPedido([FromBody] PedidoRequestDto dto)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !long.TryParse(userIdClaim, out var userId))
            return Unauthorized(new { message = "Usuario no autenticado correctamente" });

        var resultado = await service.CreateAsync(userId, dto);

        return resultado.Match(
            onSuccess: pedido => CreatedAtAction(nameof(GetMyPedidoById), new { id = pedido.Id }, pedido),
            onFailure: error => error switch
            {
                NotFoundError => NotFound(new { message = error.Message }),
                ValidationError ve => BadRequest(new { message = ve.Message, errors = ve.ValidationErrors }),
                BusinessRuleError => BadRequest(new { message = error.Message }),
                ConflictError => Conflict(new { message = error.Message }),
                _ => StatusCode(500, new { message = error.Message })
            }
        );
    }

    /// <summary>
    /// Obtiene el detalle de un pedido propio por su ID.
    /// Verifica que el pedido pertenezca al usuario que lo solicita.
    /// </summary>
    /// <param name="id">ID del pedido.</param>
    /// <returns>El pedido o 404/403.</returns>
    [HttpGet("me/{id}")]
    [Authorize]
    [ProducesResponseType(typeof(PedidoDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyPedidoById(string id)
    {
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
    /// Permite cancelar o modificar un pedido propio, siempre que esté en estado PENDIENTE.
    /// </summary>
    /// <param name="id">ID del pedido.</param>
    /// <param name="dto">Nuevos datos.</param>
    /// <returns>Pedido actualizado.</returns>
    [HttpPut("me/{id}")]
    [Authorize]
    [ProducesResponseType(typeof(PedidoDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateMyPedido(string id, [FromBody] UpdatePedidoDto dto)
    {
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
    /// Cancela y elimina un pedido propio si todavía está PENDIENTE.
    /// </summary>
    /// <param name="id">ID del pedido.</param>
    /// <returns>204 No Content.</returns>
    [HttpDelete("me/{id}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteMyPedido(string id)
    {
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
using System.Security.Claims;
using CSharpFunctionalExtensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TiendaApi.Apis.Dtos.Pedidos;
using TiendaApi.Apis.Errors;
using TiendaApi.Apis.Services.Pedidos;

namespace TiendaApi.Apis.Controllers;

/// <summary>
/// Controlador de pedidos usando Patrón Result.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class PedidosController(
    IPedidosService service
) : ControllerBase
{

    /// <summary>
    /// Obtener todos los pedidos (solo administradores).
    /// GET /api/pedidos
    /// Devuelve: 200 OK | 401 Unauthorized | 403 Forbidden
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "ADMIN")]
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
    /// Crear un nuevo pedido.
    /// POST /api/pedidos
    /// Devuelve: 201 Created | 400 Bad Request | 401 Unauthorized | 404 Not Found
    /// </summary>
    [HttpPost]
    [Authorize]
    [ProducesResponseType(typeof(PedidoDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreatePedido([FromBody] PedidoRequestDto dto)
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
            return CreatedAtAction(nameof(GetPedidoById), new { id = pedido.Id }, pedido);
        }

        var error = resultado.Error;
        return error.Type switch
        {
            ErrorType.NotFound => NotFound(new { message = error.Message }),
            ErrorType.Validation => BadRequest(new { message = error.Message, errors = error.ValidationErrors }),
            ErrorType.BusinessRule => BadRequest(new { message = error.Message }),
            ErrorType.Unauthorized => Unauthorized(new { message = error.Message }),
            ErrorType.Forbidden => StatusCode(403, new { message = error.Message }),
            ErrorType.Conflict => Conflict(new { message = error.Message }),
            _ => StatusCode(500, new { message = error.Message })
        };
    }

    /// <summary>
    /// Obtener pedidos del usuario autenticado.
    /// GET /api/pedidos/me
    /// Devuelve: 200 OK | 401 Unauthorized
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(IEnumerable<PedidoDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyPedidos()
    {
        if (User?.Identity == null || !User.Identity.IsAuthenticated)
            return Unauthorized(new { message = "Usuario no autenticado correctamente" });

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
    /// Obtener un pedido por ID.
    /// GET /api/pedidos/{id}
    /// Devuelve: 200 OK | 401 Unauthorized | 403 Forbidden | 404 Not Found
    /// </summary>
    [HttpGet("{id}")]
    [Authorize]
    [ProducesResponseType(typeof(PedidoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPedidoById(string id)
    {
        if (User?.Identity == null || !User.Identity.IsAuthenticated)
            return Unauthorized(new { message = "Usuario no autenticado correctamente" });

        var resultado = await service.FindByIdAsync(id);

        if (resultado.IsFailure)
        {
            var error = resultado.Error;
            return error.Type switch
            {
                ErrorType.NotFound => NotFound(new { message = error.Message }),
                _ => StatusCode(500, new { message = error.Message })
            };
        }

        var pedido = resultado.Value;

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !long.TryParse(userIdClaim, out var userId))
            return Unauthorized(new { message = "Usuario no autenticado correctamente" });

        if (pedido.UserId != userId && userRole != "ADMIN")
            return Forbid();

        return Ok(pedido);
    }

    /// <summary>
    /// Actualizar estado de un pedido (solo administradores).
    /// PUT /api/pedidos/{id}/estado
    /// Devuelve: 200 OK | 400 Bad Request | 401 Unauthorized | 403 Forbidden | 404 Not Found
    /// </summary>
    [HttpPut("{id}/estado")]
    [Authorize(Roles = "ADMIN")]
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
            onFailure: error => error.Type switch
            {
                ErrorType.NotFound => NotFound(new { message = error.Message }),
                ErrorType.Validation => BadRequest(new { message = error.Message }),
                ErrorType.BusinessRule => BadRequest(new { message = error.Message }),
                ErrorType.Unauthorized => Unauthorized(new { message = error.Message }),
                ErrorType.Forbidden => StatusCode(403, new { message = error.Message }),
                _ => StatusCode(500, new { message = error.Message })
            }
        );
    }

    /// <summary>
    /// Actualizar un pedido (el usuario puede actualizar sus propios pedidos).
    /// PUT /api/pedidos/{id}
    /// Devuelve: 200 OK | 400 Bad Request | 401 Unauthorized | 403 Forbidden | 404 Not Found
    /// </summary>
    [HttpPut("{id}")]
    [Authorize]
    [ProducesResponseType(typeof(PedidoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdatePedido(string id, [FromBody] UpdatePedidoDto dto)
    {
        if (User?.Identity == null || !User.Identity.IsAuthenticated)
            return Unauthorized(new { message = "Usuario no autenticado correctamente" });

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !long.TryParse(userIdClaim, out var userId))
            return Unauthorized(new { message = "Usuario no autenticado correctamente" });

        var resultado = await service.UpdateAsync(id, userId, dto);

        return resultado.Match(
            onSuccess: pedido => Ok(pedido),
            onFailure: error => error.Type switch
            {
                ErrorType.NotFound => NotFound(new { message = error.Message }),
                ErrorType.Validation => BadRequest(new { message = error.Message }),
                ErrorType.BusinessRule => BadRequest(new { message = error.Message }),
                ErrorType.Unauthorized => Unauthorized(new { message = error.Message }),
                ErrorType.Forbidden => StatusCode(403, new { message = error.Message }),
                _ => StatusCode(500, new { message = error.Message })
            }
        );
    }

    /// <summary>
    /// Eliminar un pedido (el usuario puede eliminar sus propios pedidos).
    /// DELETE /api/pedidos/{id}
    /// Devuelve: 204 No Content | 401 Unauthorized | 403 Forbidden | 404 Not Found
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeletePedido(string id)
    {
        if (User?.Identity == null || !User.Identity.IsAuthenticated)
            return Unauthorized(new { message = "Usuario no autenticado correctamente" });

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !long.TryParse(userIdClaim, out var userId))
            return Unauthorized(new { message = "Usuario no autenticado correctamente" });

        var resultado = await service.DeleteAsync(id, userId);

        if (resultado.IsSuccess)
            return NoContent();

        var error = resultado.Error;
        return error.Type switch
        {
            ErrorType.NotFound => NotFound(new { message = error.Message }),
            ErrorType.Forbidden => StatusCode(403, new { message = error.Message }),
            _ => StatusCode(500, new { message = error.Message })
        };
    }
}

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TiendaApi.Common;
using TiendaApi.Models.DTOs;
using TiendaApi.Services.Pedidos;
using TiendaApi.WebSockets;

namespace TiendaApi.Controllers;

/// <summary>
/// Controller for Pedidos using Result Pattern
/// Handles order creation, retrieval, and status updates
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class PedidosController : ControllerBase
{
    private readonly IPedidosService _service;
    private readonly ILogger<PedidosController> _logger;
    private readonly PedidoWebSocketHandler _webSocketHandler;

    public PedidosController(
        IPedidosService service, 
        ILogger<PedidosController> logger,
        PedidoWebSocketHandler webSocketHandler)
    {
        _service = service;
        _logger = logger;
        _webSocketHandler = webSocketHandler;
    }

    /// <summary>
    /// Create a new pedido for authenticated user
    /// POST /api/pedidos
    /// </summary>
    [HttpPost]
    [Authorize]
    [ProducesResponseType(typeof(PedidoDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreatePedido([FromBody] PedidoRequestDto dto)
    {
        // Get user ID from JWT token
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(userIdClaim) || !long.TryParse(userIdClaim, out var userId))
        {
            _logger.LogWarning("Invalid user ID in token");
            return Unauthorized(new { message = "Usuario no autenticado correctamente" });
        }

        var resultado = await _service.CreateAsync(userId, dto);

        if (resultado.IsSuccess)
        {
            var pedido = resultado.Value;
            
            // Send WebSocket notification (fire-and-forget)
            _ = Task.Run(async () =>
            {
                try
                {
                    await _webSocketHandler.NotifyPedidoCreatedAsync(pedido.Id, pedido.UserId, pedido);
                    _logger.LogDebug("WebSocket notification sent for pedido creation: {PedidoId}", pedido.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to send WebSocket notification for pedido creation");
                }
            });
            
            return CreatedAtAction(nameof(GetPedidoById), new { id = pedido.Id }, pedido);
        }

        // Handle failure case
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
    /// Get pedidos for authenticated user
    /// GET /api/pedidos/me
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(IEnumerable<PedidoDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyPedidos()
    {
        // Get user ID from JWT token
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(userIdClaim) || !long.TryParse(userIdClaim, out var userId))
        {
            _logger.LogWarning("Invalid user ID in token");
            return Unauthorized(new { message = "Usuario no autenticado correctamente" });
        }

        var resultado = await _service.FindByUserIdAsync(userId);

        return resultado.Match(
            onSuccess: pedidos => Ok(pedidos),
            onFailure: error => StatusCode(500, new { message = error.Message })
        );
    }

    /// <summary>
    /// Get pedido by ID (user can only see their own pedidos, admins can see all)
    /// GET /api/pedidos/{id}
    /// </summary>
    [HttpGet("{id}")]
    [Authorize]
    [ProducesResponseType(typeof(PedidoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPedidoById(string id)
    {
        var resultado = await _service.FindByIdAsync(id);

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
        
        // Get user ID from JWT token
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
        
        if (string.IsNullOrEmpty(userIdClaim) || !long.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new { message = "Usuario no autenticado correctamente" });
        }

        // Check authorization: user can only see their own pedidos, admins can see all
        if (pedido.UserId != userId && userRole != "ADMIN")
        {
            _logger.LogWarning("User {UserId} attempted to access pedido {PedidoId} that belongs to user {OwnerId}", 
                userId, id, pedido.UserId);
            return Forbid();
        }

        return Ok(pedido);
    }

    /// <summary>
    /// Update pedido estado (admin only)
    /// PUT /api/pedidos/{id}/estado
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
        var resultado = await _service.UpdateEstadoAsync(id, dto.Estado);

        if (resultado.IsSuccess)
        {
            var pedido = resultado.Value;
            
            // Send WebSocket notification (fire-and-forget)
            _ = Task.Run(async () =>
            {
                try
                {
                    await _webSocketHandler.NotifyPedidoEstadoUpdatedAsync(pedido.Id, pedido.UserId, pedido.Estado, pedido);
                    _logger.LogDebug("WebSocket notification sent for pedido estado update: {PedidoId}", pedido.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to send WebSocket notification for pedido estado update");
                }
            });
        }

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
}

/// <summary>
/// DTO for updating pedido estado
/// </summary>
public record UpdateEstadoDto
{
    public string Estado { get; init; } = string.Empty;
}

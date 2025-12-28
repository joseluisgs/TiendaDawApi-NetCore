using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace TiendaApi.WebSockets;

/// <summary>
/// WebSocket notification DTOs for pedidos
/// </summary>
public class PedidoNotificationDto
{
    public string Type { get; set; } = string.Empty;
    public string PedidoId { get; set; } = string.Empty;
    public long UserId { get; set; }
    public string Estado { get; set; } = string.Empty;
    public object? Data { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public static class PedidoNotificationType
{
    public const string CREATED = "PEDIDO_CREATED";
    public const string ESTADO_UPDATED = "PEDIDO_ESTADO_UPDATED";
}

/// <summary>
/// WebSocket handler for pedidos notifications
/// Manages connected clients and broadcasts pedido events
/// </summary>
public class PedidoWebSocketHandler
{
    private readonly ConcurrentDictionary<string, WebSocket> _connections;
    private readonly ILogger<PedidoWebSocketHandler> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public PedidoWebSocketHandler(ILogger<PedidoWebSocketHandler> logger)
    {
        _connections = new ConcurrentDictionary<string, WebSocket>();
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    /// <summary>
    /// Handle new WebSocket connection
    /// </summary>
    public async Task HandleConnectionAsync(HttpContext context, WebSocket webSocket)
    {
        var connectionId = Guid.NewGuid().ToString();
        _connections.TryAdd(connectionId, webSocket);
        
        _logger.LogInformation("WebSocket connection established for pedidos: {ConnectionId}", connectionId);

        try
        {
            // Keep connection alive and listen for messages (ping/pong)
            var buffer = new byte[1024 * 4];
            var result = await webSocket.ReceiveAsync(
                new ArraySegment<byte>(buffer), 
                CancellationToken.None);

            while (!result.CloseStatus.HasValue)
            {
                // Echo back any received messages (optional)
                result = await webSocket.ReceiveAsync(
                    new ArraySegment<byte>(buffer), 
                    CancellationToken.None);
            }

            await webSocket.CloseAsync(
                result.CloseStatus.Value, 
                result.CloseStatusDescription, 
                CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "WebSocket connection error for pedidos: {ConnectionId}", connectionId);
        }
        finally
        {
            _connections.TryRemove(connectionId, out _);
            _logger.LogInformation("WebSocket connection closed for pedidos: {ConnectionId}", connectionId);
        }
    }

    /// <summary>
    /// Notify all connected clients about pedido creation
    /// </summary>
    public async Task NotifyPedidoCreatedAsync(string pedidoId, long userId, object? data = null)
    {
        var notification = new PedidoNotificationDto
        {
            Type = PedidoNotificationType.CREATED,
            PedidoId = pedidoId,
            UserId = userId,
            Estado = "PENDIENTE",
            Data = data,
            Timestamp = DateTime.UtcNow
        };
        
        await BroadcastAsync(notification);
    }

    /// <summary>
    /// Notify all connected clients about pedido estado update
    /// </summary>
    public async Task NotifyPedidoEstadoUpdatedAsync(string pedidoId, long userId, string nuevoEstado, object? data = null)
    {
        var notification = new PedidoNotificationDto
        {
            Type = PedidoNotificationType.ESTADO_UPDATED,
            PedidoId = pedidoId,
            UserId = userId,
            Estado = nuevoEstado,
            Data = data,
            Timestamp = DateTime.UtcNow
        };
        
        await BroadcastAsync(notification);
    }

    /// <summary>
    /// Broadcast notification to all connected clients
    /// </summary>
    private async Task BroadcastAsync(PedidoNotificationDto notification)
    {
        var json = JsonSerializer.Serialize(notification, _jsonOptions);
        var bytes = Encoding.UTF8.GetBytes(json);
        var arraySegment = new ArraySegment<byte>(bytes);
        
        _logger.LogInformation("Broadcasting pedido notification: {Type}, PedidoId: {PedidoId} to {Count} clients", 
            notification.Type, notification.PedidoId, _connections.Count);

        var disconnectedConnections = new List<string>();

        foreach (var (connectionId, webSocket) in _connections)
        {
            if (webSocket.State == WebSocketState.Open)
            {
                try
                {
                    await webSocket.SendAsync(
                        arraySegment,
                        WebSocketMessageType.Text,
                        endOfMessage: true,
                        cancellationToken: CancellationToken.None);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to send notification to connection: {ConnectionId}", connectionId);
                    disconnectedConnections.Add(connectionId);
                }
            }
            else
            {
                disconnectedConnections.Add(connectionId);
            }
        }

        // Clean up disconnected connections
        foreach (var connectionId in disconnectedConnections)
        {
            _connections.TryRemove(connectionId, out _);
            _logger.LogDebug("Removed disconnected pedido WebSocket connection: {ConnectionId}", connectionId);
        }
    }
}

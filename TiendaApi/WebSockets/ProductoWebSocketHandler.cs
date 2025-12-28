using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace TiendaApi.WebSockets;

/// <summary>
/// WebSocket handler for managing producto notification connections
/// Java Spring equivalent: WebSocketHandler or @ServerEndpoint
/// Manages connected clients and broadcasts notifications
/// </summary>
public class ProductoWebSocketHandler
{
    private readonly ConcurrentDictionary<string, WebSocket> _connections;
    private readonly ILogger<ProductoWebSocketHandler> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public ProductoWebSocketHandler(ILogger<ProductoWebSocketHandler> logger)
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
        
        _logger.LogInformation("WebSocket connection established: {ConnectionId}", connectionId);

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
            _logger.LogError(ex, "WebSocket connection error for: {ConnectionId}", connectionId);
        }
        finally
        {
            _connections.TryRemove(connectionId, out _);
            _logger.LogInformation("WebSocket connection closed: {ConnectionId}", connectionId);
        }
    }

    /// <summary>
    /// Notify all connected clients about producto creation
    /// </summary>
    public async Task NotifyProductoCreatedAsync(long productoId, string productoNombre, object? data = null)
    {
        var notification = new ProductoNotificationDto
        {
            Type = NotificationType.CREATED,
            ProductoId = productoId,
            ProductoNombre = productoNombre,
            Timestamp = DateTime.UtcNow,
            Data = data
        };

        await BroadcastNotificationAsync(notification);
    }

    /// <summary>
    /// Notify all connected clients about producto update
    /// </summary>
    public async Task NotifyProductoUpdatedAsync(long productoId, string productoNombre, object? data = null)
    {
        var notification = new ProductoNotificationDto
        {
            Type = NotificationType.UPDATED,
            ProductoId = productoId,
            ProductoNombre = productoNombre,
            Timestamp = DateTime.UtcNow,
            Data = data
        };

        await BroadcastNotificationAsync(notification);
    }

    /// <summary>
    /// Notify all connected clients about producto deletion
    /// </summary>
    public async Task NotifyProductoDeletedAsync(long productoId, string productoNombre)
    {
        var notification = new ProductoNotificationDto
        {
            Type = NotificationType.DELETED,
            ProductoId = productoId,
            ProductoNombre = productoNombre,
            Timestamp = DateTime.UtcNow
        };

        await BroadcastNotificationAsync(notification);
    }

    /// <summary>
    /// Broadcast notification to all connected clients
    /// Fire-and-forget pattern - swallow exceptions to not break main flow
    /// </summary>
    private async Task BroadcastNotificationAsync(ProductoNotificationDto notification)
    {
        if (_connections.IsEmpty)
        {
            _logger.LogDebug("No WebSocket clients connected, skipping notification");
            return;
        }

        var json = JsonSerializer.Serialize(notification, _jsonOptions);
        var bytes = Encoding.UTF8.GetBytes(json);
        var buffer = new ArraySegment<byte>(bytes);

        _logger.LogInformation(
            "Broadcasting notification: {Type} for producto {ProductoId}", 
            notification.Type, 
            notification.ProductoId);

        var disconnectedConnections = new List<string>();

        foreach (var kvp in _connections)
        {
            try
            {
                if (kvp.Value.State == WebSocketState.Open)
                {
                    await kvp.Value.SendAsync(
                        buffer,
                        WebSocketMessageType.Text,
                        endOfMessage: true,
                        CancellationToken.None);
                }
                else
                {
                    disconnectedConnections.Add(kvp.Key);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send notification to connection: {ConnectionId}", kvp.Key);
                disconnectedConnections.Add(kvp.Key);
            }
        }

        // Clean up disconnected clients
        foreach (var connectionId in disconnectedConnections)
        {
            _connections.TryRemove(connectionId, out _);
            _logger.LogInformation("Removed disconnected WebSocket client: {ConnectionId}", connectionId);
        }
    }
}

using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using TiendaApi.Dtos.Productos;

namespace TiendaApi.WebSockets.Productos;

/// <summary>
/// WebSocket handler for managing producto notification connections
/// Uses generic Notificacion<T> pattern for all notifications
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

    public async Task HandleConnectionAsync(HttpContext context, WebSocket webSocket)
    {
        var connectionId = Guid.NewGuid().ToString();
        _connections.TryAdd(connectionId, webSocket);
        
        _logger.LogInformation("WebSocket connection established for productos: {ConnectionId}", connectionId);

        try
        {
            var buffer = new byte[1024 * 4];
            var result = await webSocket.ReceiveAsync(
                new ArraySegment<byte>(buffer), 
                CancellationToken.None);

            while (!result.CloseStatus.HasValue)
            {
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
            _logger.LogError(ex, "WebSocket connection error for productos: {ConnectionId}", connectionId);
        }
        finally
        {
            _connections.TryRemove(connectionId, out _);
            _logger.LogInformation("WebSocket connection closed for productos: {ConnectionId}", connectionId);
        }
    }

    public async Task NotifyProductoCreatedAsync(ProductoDto producto)
    {
        var notification = Notificacion<ProductoDto>.Create(
            "productos",
            Notificacion<ProductoDto>.Tipo.CREATE,
            producto
        );
        await BroadcastNotificationAsync(notification);
    }

    public async Task NotifyProductoUpdatedAsync(ProductoDto producto)
    {
        var notification = Notificacion<ProductoDto>.Create(
            "productos",
            Notificacion<ProductoDto>.Tipo.UPDATE,
            producto
        );
        await BroadcastNotificationAsync(notification);
    }

    public async Task NotifyProductoDeletedAsync(long productoId)
    {
        var data = new { productoId };
        var notification = Notificacion<object>.Create(
            "productos",
            Notificacion<object>.Tipo.DELETE,
            data
        );
        await BroadcastNotificationAsync(notification);
    }

    private async Task BroadcastNotificationAsync<T>(Notificacion<T> notification)
    {
        if (_connections.IsEmpty)
        {
            _logger.LogDebug("No WebSocket clients connected for productos, skipping notification");
            return;
        }

        var json = JsonSerializer.Serialize(notification, _jsonOptions);
        var bytes = Encoding.UTF8.GetBytes(json);
        var buffer = new ArraySegment<byte>(bytes);

        _logger.LogInformation(
            "Broadcasting producto notification: {Type} for entity {Entity}", 
            notification.Type, 
            notification.Entity);

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

        foreach (var connectionId in disconnectedConnections)
        {
            _connections.TryRemove(connectionId, out _);
            _logger.LogDebug("Removed disconnected producto WebSocket client: {ConnectionId}", connectionId);
        }
    }
}

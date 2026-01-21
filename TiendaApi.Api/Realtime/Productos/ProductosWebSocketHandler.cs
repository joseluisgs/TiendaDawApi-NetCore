using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using TiendaApi.Api.Dtos.Productos;
using TiendaApi.Api.Realtime.Common;

namespace TiendaApi.Api.Realtime.Productos;

/// <summary>
/// Tipos de notificación para productos.
/// </summary>
public static class ProductoNotificationType
{
    /// <summary>Producto creado.</summary>
    public const string CREATED = "PRODUCTO_CREADO";

    /// <summary>Producto actualizado.</summary>
    public const string UPDATED = "PRODUCTO_ACTUALIZADO";

    /// <summary>Producto eliminado.</summary>
    public const string DELETED = "PRODUCTO_ELIMINADO";
}

/// <summary>
/// Datos de notificación para eventos de productos.
/// </summary>
public record ProductoNotificacion(
    string Tipo,
    long ProductoId,
    ProductoDto? Producto
);

/// <summary>
/// Handler de WebSocket para notificaciones de productos (público).
/// </summary>
/// <example>
/// ws://localhost:5000/ws/productos
/// const ws = new WebSocket('ws://localhost:5000/ws/productos');
/// ws.onmessage = (event) => console.log('Notificación:', JSON.parse(event.data));
/// // Respuesta: {"entity":"productos","type":"PRODUCTO_CREADO","productoId":123,"producto":{...},"timestamp":"2025-01-18T10:30:00Z"}
/// </example>
public class ProductosWebSocketHandler(ILogger<ProductosWebSocketHandler> logger)
{
    private readonly ConcurrentDictionary<string, WebSocket> _connections = new();
    private readonly ILogger<ProductosWebSocketHandler> _logger = logger;
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    /// <summary>Maneja una conexión WebSocket.</summary>
    /// <param name="context">Contexto HTTP.</param>
    /// <param name="webSocket">Instancia del WebSocket.</param>
    public async Task HandleConnectionAsync(HttpContext context, WebSocket webSocket)
    {
        var connectionId = Guid.NewGuid().ToString();
        _connections.TryAdd(connectionId, webSocket);
        _logger.LogInformation("Conexión WebSocket: {ConnectionId}", connectionId);

        try
        {
            var buffer = new byte[1024 * 4];
            var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

            while (!result.CloseStatus.HasValue)
                result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

            await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en WebSocket: {ConnectionId}", connectionId);
        }
        finally
        {
            _connections.TryRemove(connectionId, out _);
            _logger.LogInformation("Conexión cerrada: {ConnectionId}", connectionId);
        }
    }

    /// <summary>Notifica a todos los clientes.</summary>
    /// <param name="notificacion">Datos de la notificación.</param>
    public async Task NotifyAsync(ProductoNotificacion notificacion)
    {
        var wrapper = new
        {
            entity = "productos",
            type = notificacion.Tipo,
            productoId = notificacion.ProductoId,
            producto = notificacion.Producto,
            timestamp = DateTime.UtcNow
        };
        await BroadcastNotificationAsync(wrapper);
    }

    /// <summary>Obtiene el número de conexiones activas.</summary>
    /// <returns>Número de conexiones.</returns>
    public int GetConnectionCount() => _connections.Count;

    private async Task BroadcastNotificationAsync<T>(T notification)
    {
        if (_connections.IsEmpty) return;

        var json = JsonSerializer.Serialize(notification, _jsonOptions);
        var bytes = Encoding.UTF8.GetBytes(json);
        var buffer = new ArraySegment<byte>(bytes);
        var disconnected = new List<string>();

        foreach (var kvp in _connections)
        {
            try
            {
                if (kvp.Value.State == WebSocketState.Open)
                    await kvp.Value.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
                else
                    disconnected.Add(kvp.Key);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error al enviar a: {ConnectionId}", kvp.Key);
                disconnected.Add(kvp.Key);
            }
        }

        foreach (var id in disconnected)
            _connections.TryRemove(id, out _);
    }
}

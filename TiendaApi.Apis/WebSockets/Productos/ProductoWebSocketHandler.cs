using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using TiendaApi.Apis.Dtos.Productos;

namespace TiendaApi.Apis.WebSockets.Productos;

/// <summary>
/// Tipos de notificación para eventos de productos.
/// </summary>
public static class ProductoNotificationType
{
    public const string CREATED = "PRODUCTO_CREATED";
    public const string UPDATED = "PRODUCTO_UPDATED";
    public const string DELETED = "PRODUCTO_DELETED";
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
/// Handler de WebSocket para gestionar conexiones de notificaciones de productos.
/// </summary>
public class ProductoWebSocketHandler(ILogger<ProductoWebSocketHandler> logger)
{
    private readonly ConcurrentDictionary<string, WebSocket> _connections = new();
    private readonly ILogger<ProductoWebSocketHandler> _logger = logger;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Maneja una nueva conexión WebSocket para productos.
    /// </summary>
    /// <param name="context">Contexto HTTP de la conexión.</param>
    /// <param name="webSocket">Instancia del WebSocket.</param>
    /// <returns>Tarea asíncrona representando la conexión.</returns>
    public async Task HandleConnectionAsync(HttpContext context, WebSocket webSocket)
    {
        var connectionId = Guid.NewGuid().ToString();
        _connections.TryAdd(connectionId, webSocket);

        _logger.LogInformation("Conexión WebSocket establecida para productos: {ConnectionId}", connectionId);

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
            _logger.LogError(ex, "Error en conexión WebSocket para productos: {ConnectionId}", connectionId);
        }
        finally
        {
            _connections.TryRemove(connectionId, out _);
            _logger.LogInformation("Conexión WebSocket cerrada para productos: {ConnectionId}", connectionId);
        }
    }

    /// <summary>
    /// Notifica a todos los clientes conectados un evento de producto.
    /// </summary>
    /// <param name="notificacion">Datos de la notificación.</param>
    /// <returns>Tarea asíncrona de la notificación.</returns>
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

    /// <summary>
    /// Envía una notificación a todos los clientes WebSocket conectados.
    /// </summary>
    /// <param name="notification">Notificación a broadcast.</param>
    /// <returns>Tarea asíncrona del broadcast.</returns>
    private async Task BroadcastNotificationAsync<T>(T notification)
    {
        if (_connections.IsEmpty)
        {
            _logger.LogDebug("No hay clientes WebSocket conectados para productos, omitiendo notificación");
            return;
        }

        var json = JsonSerializer.Serialize(notification, _jsonOptions);
        var bytes = Encoding.UTF8.GetBytes(json);
        var buffer = new ArraySegment<byte>(bytes);

        _logger.LogInformation(
            "Broadcasting notificación de producto a {Count} clientes",
            _connections.Count);

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
                _logger.LogWarning(ex, "Error al enviar notificación a la conexión: {ConnectionId}", kvp.Key);
                disconnectedConnections.Add(kvp.Key);
            }
        }

        foreach (var connectionId in disconnectedConnections)
        {
            _connections.TryRemove(connectionId, out _);
            _logger.LogDebug("Eliminado cliente WebSocket de producto desconectado: {ConnectionId}", connectionId);
        }
    }
}

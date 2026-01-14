using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using TiendaApi.Apis.Models;

namespace TiendaApi.Apis.WebSockets.Pedidos;

/// <summary>
/// Tipos de notificación para eventos de pedidos.
/// </summary>
public static class PedidoNotificationType
{
    public const string CREATED = "PEDIDO_CREATED";
    public const string ESTADO_UPDATED = "PEDIDO_ESTADO_UPDATED";
}

/// <summary>
/// Datos de notificación para eventos de pedidos.
/// </summary>
public record PedidoNotificacion(
    string Tipo,
    string PedidoId,
    long UserId,
    string Estado,
    object? Data
);

/// <summary>
/// Handler de WebSocket para notificaciones de pedidos.
/// Gestiona clientes conectados y emite eventos de pedidos.
/// </summary>
public class PedidoWebSocketHandler(ILogger<PedidoWebSocketHandler> logger)
{
    private readonly ConcurrentDictionary<string, WebSocket> _connections = new();
    private readonly ILogger<PedidoWebSocketHandler> _logger = logger;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Maneja una nueva conexión WebSocket para pedidos.
    /// </summary>
    /// <param name="context">Contexto HTTP de la conexión.</param>
    /// <param name="webSocket">Instancia del WebSocket.</param>
    /// <returns>Tarea asíncrona representando la conexión.</returns>
    public async Task HandleConnectionAsync(HttpContext context, WebSocket webSocket)
    {
        var connectionId = Guid.NewGuid().ToString();
        _connections.TryAdd(connectionId, webSocket);

        _logger.LogInformation("Conexión WebSocket establecida para pedidos: {ConnectionId}", connectionId);

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
            _logger.LogError(ex, "Error en conexión WebSocket para pedidos: {ConnectionId}", connectionId);
        }
        finally
        {
            _connections.TryRemove(connectionId, out _);
            _logger.LogInformation("Conexión WebSocket cerrada para pedidos: {ConnectionId}", connectionId);
        }
    }

    /// <summary>
    /// Notifica a todos los clientes conectados un evento de pedido.
    /// </summary>
    /// <param name="notificacion">Datos de la notificación.</param>
    /// <returns>Tarea asíncrona de la notificación.</returns>
    public async Task NotifyAsync(PedidoNotificacion notificacion)
    {
        var wrapper = new
        {
            entity = "pedidos",
            type = notificacion.Tipo,
            pedidoId = notificacion.PedidoId,
            userId = notificacion.UserId,
            estado = notificacion.Estado,
            data = notificacion.Data,
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
            _logger.LogDebug("No hay clientes WebSocket conectados para pedidos, omitiendo notificación");
            return;
        }

        var json = JsonSerializer.Serialize(notification, _jsonOptions);
        var bytes = Encoding.UTF8.GetBytes(json);
        var buffer = new ArraySegment<byte>(bytes);

        _logger.LogInformation(
            "Broadcasting notificación de pedido a {Count} clientes",
            _connections.Count);

        var disconnectedConnections = new List<string>();

        foreach (var (connectionId, webSocket) in _connections)
        {
            if (webSocket.State == WebSocketState.Open)
            {
                try
                {
                    await webSocket.SendAsync(
                        buffer,
                        WebSocketMessageType.Text,
                        endOfMessage: true,
                        cancellationToken: CancellationToken.None);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error al enviar notificación a la conexión: {ConnectionId}", connectionId);
                    disconnectedConnections.Add(connectionId);
                }
            }
            else
            {
                disconnectedConnections.Add(connectionId);
            }
        }

        foreach (var connectionId in disconnectedConnections)
        {
            _connections.TryRemove(connectionId, out _);
            _logger.LogDebug("Eliminada conexión WebSocket de pedido desconectada: {ConnectionId}", connectionId);
        }
    }
}

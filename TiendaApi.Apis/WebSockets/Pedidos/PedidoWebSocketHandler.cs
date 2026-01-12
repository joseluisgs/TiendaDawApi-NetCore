using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace TiendaApi.Apis.WebSockets.Pedidos;

/// <summary>
/// Tipos de notificación para eventos de pedidos.
/// </summary>
public static class PedidoNotificationType
{
    /// <summary>
    /// Notificación de pedido creado.
    /// </summary>
    public const string CREATED = "PEDIDO_CREATED";

    /// <summary>
    /// Notificación de cambio de estado de pedido.
    /// </summary>
    public const string ESTADO_UPDATED = "PEDIDO_ESTADO_UPDATED";
}

/// <summary>
/// Handler de WebSocket para notificaciones de pedidos.
/// Gestiona clientes conectados y emite eventos de pedidos.
/// </summary>
public class PedidoWebSocketHandler
{
    private readonly ConcurrentDictionary<string, WebSocket> _connections;
    private readonly ILogger<PedidoWebSocketHandler> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    /// <summary>
    /// Inicializa una nueva instancia del handler de WebSocket para pedidos.
    /// </summary>
    /// <param name="logger">Instancia del logger.</param>
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
    /// Notifica a todos los clientes conectados la creación de un pedido.
    /// </summary>
    /// <param name="pedidoId">ID del pedido creado.</param>
    /// <param name="userId">ID del usuario.</param>
    /// <param name="data">Datos adicionales opcionales.</param>
    /// <returns>Tarea asíncrona de la notificación.</returns>
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
    /// Notifica a todos los clientes conectados el cambio de estado de un pedido.
    /// </summary>
    /// <param name="pedidoId">ID del pedido.</param>
    /// <param name="userId">ID del usuario.</param>
    /// <param name="nuevoEstado">Nuevo estado del pedido.</param>
    /// <param name="data">Datos adicionales opcionales.</param>
    /// <returns>Tarea asíncrona de la notificación.</returns>
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
    /// Envía una notificación a todos los clientes WebSocket conectados.
    /// </summary>
    /// <param name="notification">Notificación a broadcast.</param>
    /// <returns>Tarea asíncrona del broadcast.</returns>
    private async Task BroadcastAsync(PedidoNotificationDto notification)
    {
        var json = JsonSerializer.Serialize(notification, _jsonOptions);
        var bytes = Encoding.UTF8.GetBytes(json);
        var arraySegment = new ArraySegment<byte>(bytes);

        _logger.LogInformation("Broadcasting notificación de pedido: {Type}, PedidoId: {PedidoId} a {Count} clientes",
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

using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
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
/// <param name="Tipo">Tipo de notificación (CREATED, UPDATED, DELETED).</param>
/// <param name="ProductoId">Identificador del producto.</param>
/// <param name="Producto">Datos del producto (null para DELETED).</param>
public record ProductoNotificacion(
    string Tipo,
    long ProductoId,
    ProductoDto? Producto
);

/// <summary>
/// Handler de WebSocket para gestionar conexiones de notificaciones de productos.
/// </summary>
/// <remarks>
/// <para><b>Características:</b></para>
/// <list type="bullet">
///   <item><description>Notificaciones de broadcast a TODOS los clientes conectados.</description></item>
///   <item><description>No requiere autenticación (público).</description></item>
///   <item><description>Ideal para dashboards públicos y catálogos en tiempo real.</description></item>
/// </list>
/// 
/// <para><b>EndPoint de conexión:</b></para>
/// <code>ws://localhost:5000/ws/v1/productos</code>
/// 
/// <para><b>Ejemplo de conexión desde cliente JavaScript:</b></para>
/// <code>
/// // Sin autenticación requerida
/// const ws = new WebSocket('ws://localhost:5000/ws/v1/productos');
/// 
/// ws.onmessage = (event) => {
///     const data = JSON.parse(event.data);
///     console.log('Notificación de producto:', data);
/// };
/// </code>
/// 
/// <para><b>Ejemplo de URL completa:</b></para>
/// <code>
/// ws://localhost:5000/ws/v1/productos
/// </code>
/// 
/// <para><b>Casos de uso:</b></para>
/// <list type="bullet">
///   <item><description>Dashboards públicos que muestran nuevos productos.</description></item>
///   <item><description>Actualización de catálogos en tiempo real.</description></item>
///   <item><description>Sistemas de inventario que monitorean cambios.</description></item>
/// </list>
/// 
/// <para><b>Ejemplo de respuesta de notificación:</b></para>
/// <code>
/// {
///   "entity": "productos",
///   "type": "PRODUCTO_CREATED",
///   "productoId": 123,
///   "producto": {
///     "id": 123,
///     "nombre": "Nuevo Producto",
///     "precio": 99.99,
///     "stock": 50
///   },
///   "timestamp": "2025-01-16T10:30:00Z"
/// }
/// </code>
/// 
/// <para><b>Tipos de eventos:</b></para>
/// <list type="table">
///   <item>
///     <term>PRODUCTO_CREATED</term>
///     <description>Se creó un nuevo producto. Incluye datos del producto.</description>
///   </item>
///   <item>
///     <term>PRODUCTO_UPDATED</term>
///     <description>Se actualizó un producto. Incluye datos actualizados.</description>
///   </item>
///   <item>
///     <term>PRODUCTO_DELETED</term>
///     <description>Se eliminó un producto. producto es null, solo envía productoId.</description>
///   </item>
/// </list>
/// </remarks>
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
    /// <remarks>
    /// <para><b>Proceso de conexión:</b></para>
    /// <list type="number">
    ///   <item><description>El cliente se conecta sin necesidad de autenticación.</description></item>
    ///   <item><description>Se genera un connectionId único para la sesión.</description></item>
    ///   <item><description>La conexión se almacena en el diccionario de conexiones.</description></item>
    ///   <item><description>Cuando se cierra la conexión, se elimina del diccionario.</description></item>
    /// </list>
    /// 
    /// <para><b>Ejemplo de conexión:</b></para>
    /// <code>
    /// // JavaScript
    /// const ws = new WebSocket('ws://localhost:5000/ws/v1/productos');
    /// 
    /// ws.onopen = () => {
    ///     console.log('Conectado al WebSocket de productos');
    /// };
    /// </code>
    /// </remarks>
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
    /// <remarks>
    /// <para><b>Ejemplo de uso:</b></para>
    /// <code>
    /// // Notificar que se creó un nuevo producto
    /// await NotifyAsync(new ProductoNotificacion(
    ///     ProductoNotificationType.CREATED,
    ///     123,
    ///     productoDto  // Datos del producto
    /// ));
    /// // TODOS los clientes conectados recibirán esta notificación
    /// 
    /// // Notificar que se eliminó un producto
    /// await NotifyAsync(new ProductoNotificacion(
    ///     ProductoNotificationType.DELETED,
    ///     123,
    ///     null  // No hay datos del producto eliminado
    /// ));
    /// </code>
    /// 
    /// <para><b>Casos de uso:</b></para>
    /// <list type="bullet">
    ///   <item><description>Admin crea un producto → Notificar a todos los clientes.</description></item>
    ///   <item><description>Admin actualiza precio → Notificar cambio a todos.</description></item>
    ///   <item><description>Admin elimina producto → Notificar eliminación a todos.</description></item>
    /// </list>
    /// </remarks>
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
    /// Obtiene el número de conexiones activas.
    /// </summary>
    /// <returns>Número de conexiones activas.</returns>
    public int GetConnectionCount() => _connections.Count;

    #region Métodos Privados

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

    #endregion
}

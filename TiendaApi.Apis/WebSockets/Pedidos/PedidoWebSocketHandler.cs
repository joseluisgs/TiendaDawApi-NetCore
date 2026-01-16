using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using TiendaApi.Apis.Services.Auth;

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
/// <param name="Tipo">Tipo de notificación (CREATED, ESTADO_UPDATED).</param>
/// <param name="PedidoId">Identificador del pedido.</param>
/// <param name="UserId">ID del usuario afectado.</param>
/// <param name="Estado">Estado actual del pedido.</param>
/// <param name="Data">Datos adicionales del pedido.</param>
public record PedidoNotificacion(
    string Tipo,
    string PedidoId,
    long UserId,
    string Estado,
    object? Data
);

/// <summary>
/// Handler de WebSocket para notificaciones de pedidos.
/// Gestiona clientes conectados y emite eventos de pedidos de forma selectiva.
/// </summary>
/// <remarks>
/// <para><b>Arquitectura de notificaciones:</b></para>
/// <list type="bullet">
///   <item><description>Los usuarios normales solo reciben notificaciones de SUS pedidos.</description></item>
///   <item><description>Los administradores reciben notificaciones de TODOS los pedidos.</description></item>
///   <item><description>Las conexiones anónimas son rechazadas (requiere JWT).</description></item>
/// </list>
/// 
/// <para><b>EndPoint de conexión:</b></para>
/// <code>ws://localhost:5000/ws/v1/pedidos?token=JWT_TOKEN</code>
/// 
/// <para><b>Ejemplo de conexión desde cliente JavaScript:</b></para>
/// <code>
/// const token = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."; // JWT del usuario
/// const ws = new WebSocket(`ws://localhost:5000/ws/v1/pedidos?token=${token}`);
/// 
/// ws.onmessage = (event) => {
///     const data = JSON.parse(event.data);
///     console.log('Notificación:', data);
/// };
/// </code>
/// 
/// <para><b>Ejemplo de URL completa con token:</b></para>
/// <code>
/// ws://localhost:5000/ws/v1/pedidos?token=eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c
/// </code>
/// 
/// <para><b>Roles y permisos (extraídos del claim 'role' del JWT):</b></para>
/// <list type="table">
///   <item>
///     <term>Usuario Normal</term>
///     <description>Solicita ?token=JWT_USUARIO (role=cliente). Recibe solo sus pedidos.</description>
///   </item>
///   <item>
///     <term>Administrador</term>
///     <description>Solicita ?token=JWT_ADMIN (role=admin). Recibe TODOS los pedidos del sistema.</description>
///   </item>
/// </list>
/// 
/// <para><b>Ejemplo de respuesta de notificación:</b></para>
/// <code>
/// {
///   "entity": "pedidos",
///   "type": "PEDIDO_ESTADO_UPDATED",
///   "pedidoId": "PED-ABC123",
///   "userId": 123,
///   "estado": "Enviado",
///   "data": { "id": "PED-ABC123", "estado": "Enviado", ... },
///   "timestamp": "2025-01-16T10:30:00Z"
/// }
/// </code>
/// </remarks>
public class PedidoWebSocketHandler
{
    /// <summary>
    /// Estructura de conexión que incluye el WebSocket, userId y rol del usuario.
    /// </summary>
    private record struct ConnectionInfo(WebSocket WebSocket, long UserId, bool IsAdmin);

    private readonly ConcurrentDictionary<string, ConnectionInfo> _connections = new();
    private readonly ILogger<PedidoWebSocketHandler> _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly IJwtTokenExtractor _tokenExtractor;

    public PedidoWebSocketHandler(
        ILogger<PedidoWebSocketHandler> logger,
        IJwtTokenExtractor tokenExtractor)
    {
        _logger = logger;
        _tokenExtractor = tokenExtractor;
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
    /// <remarks>
    /// <para><b>Proceso de autenticación:</b></para>
    /// <list type="number">
    ///   <item><description>Extrae el token JWT del query string 'token'.</description></item>
    ///   <item><description>Valida el token y extrae userId y rol del claim JWT.</description></item>
    ///   <item><description>Almacena la conexión junto con userId y si es admin.</description></item>
    ///   <item><description>Si no hay token o es inválido, cierra la conexión.</description></item>
    /// </list>
    /// 
    /// <para><b>Claims extraídos del JWT:</b></para>
    /// <list type="bullet">
    ///   <item><description>ClaimTypes.NameIdentifier → userId</description></item>
    ///   <item><description>ClaimTypes.Role → rol del usuario (admin/cliente)</description></item>
    /// </list>
    /// 
    /// <para><b>Por qué usamos IJwtTokenExtractor en lugar de consultar la base de datos:</b></para>
    /// <list type="bullet">
    ///   <item><description>
    ///     <b>Rendimiento en conexiones WebSocket:</b> Las conexiones WebSocket son persistentes y de larga duración.
    ///     Cada nueva conexión requiere autenticación inmediata. Consultar la base de datos para obtener el usuario
    ///     y sus roles añade latencia significativa (típicamente 10-100ms por query) y sobrecarga a la BD.
    ///   </description></item>
    ///   <item><description>
    ///     <b>El JWT ya contiene la información necesaria:</b> El token JWT incluye los claims esenciales
    ///     (userId y rol) firmados digitalmente. Esta información es suficiente para determinar los permisos
    ///     de notificación sin necesidad de consultar datos adicionales.
    ///   </description></item>
    ///   <item><description>
    ///     <b>Escalabilidad:</b> En escenarios con muchas conexiones WebSocket simultáneas, evitar consultas a BD
    ///     por conexión reduce drásticamente la carga en la base de datos y mejora el tiempo de conexión.
    ///   </description></item>
    ///   <item><description>
    ///     <b>Seguridad por diseño:</b> Los tokens JWT están firmados y su validez puede verificarse
    ///     criptográficamente sin depender de un origen externo. Esto es ideal para sistemas distribuidos.
    ///   </description></item>
    ///   <item><description>
    ///     <b>Trade-off:</b> Esta aproximación asume que los roles no cambian frecuentemente durante la sesión.
    ///     Si un rol se revoca mientras el usuario está conectado, el cambio no se reflejará hasta que
    ///     el usuario se reconecte con un nuevo token. Este comportamiento es aceptable para notificaciones.
    ///   </description></item>
    /// </list>
    /// 
    /// <para><b>Ejemplo de conexión exitosa:</b></para>
    /// <code>
    /// // El cliente envía: ws://localhost:5000/ws/v1/pedidos?token=JWT
    /// // El servidor extrae userId y rol del JWT y almacena la conexión
    /// </code>
    /// 
    /// <para><b>Códigos de cierre de WebSocket:</b></para>
    /// <list type="bullet">
    ///   <item><description>PolicyViolation: No se proporcionó token o es inválido.</description></item>
    /// </list>
    /// </remarks>
    public async Task HandleConnectionAsync(HttpContext context, WebSocket webSocket)
    {
        var token = context.Request.Query["token"].FirstOrDefault();
        
        if (string.IsNullOrEmpty(token))
        {
            _logger.LogWarning("Conexión WebSocket rechazada: no se proporcionó token");
            await CloseWebSocketAsync(webSocket, WebSocketCloseStatus.PolicyViolation, "Token requerido");
            return;
        }

        var (userId, isAdmin, _) = _tokenExtractor.ExtractUserInfo(token);
        
        if (userId == null)
        {
            _logger.LogWarning("Conexión WebSocket rechazada: token inválido");
            await CloseWebSocketAsync(webSocket, WebSocketCloseStatus.PolicyViolation, "Token inválido");
            return;
        }

        var connectionId = Guid.NewGuid().ToString();
        _connections.TryAdd(connectionId, new ConnectionInfo(webSocket, userId.Value, isAdmin));

        _logger.LogInformation(
            "Conexión WebSocket establecida para pedidos: {ConnectionId}, UserId: {UserId}, IsAdmin: {IsAdmin}",
            connectionId, userId, isAdmin);

        await HandleWebSocketLoopAsync(webSocket, connectionId);
    }

    /// <summary>
    /// Notifica a un usuario específico sobre un evento de su pedido.
    /// </summary>
    /// <param name="userId">ID del usuario a notificar.</param>
    /// <param name="notificacion">Datos de la notificación.</param>
    /// <returns>Tarea asíncrona de la notificación.</returns>
    /// <remarks>
    /// <para><b>Ejemplo de uso:</b></para>
    /// <code>
    /// // El usuario 123 creó un pedido
    /// await NotifyUserAsync(123, new PedidoNotificacion(
    ///     PedidoNotificationType.CREATED,
    ///     "PED-001",
    ///     123,
    ///     "Pendiente",
    ///     pedidoDto
    /// ));
    /// // Solo el usuario con userId=123 recibirá esta notificación
    /// </code>
    /// 
    /// <para><b>Casos de uso:</b></para>
    /// <list type="bullet">
    ///   <item><description>Notificar al usuario cuando su pedido cambia de estado.</description></item>
    ///   <item><description>Confirmar al usuario que su pedido fue creado exitosamente.</description></item>
    /// </list>
    /// </remarks>
    public async Task NotifyUserAsync(long userId, PedidoNotificacion notificacion)
    {
        var wrapper = CreateWrapper(notificacion);
        
        var sentCount = 0;
        var disconnectedConnections = new List<string>();

        foreach (var connection in _connections)
        {
            if (connection.Value.UserId != userId) continue;
            
            try
            {
                if (connection.Value.WebSocket.State == WebSocketState.Open)
                {
                    await SendToSocketAsync(connection.Value.WebSocket, wrapper);
                    sentCount++;
                }
                else
                {
                    disconnectedConnections.Add(connection.Key);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error al enviar notificación a la conexión: {ConnectionId}", connection.Key);
                disconnectedConnections.Add(connection.Key);
            }
        }

        CleanupDisconnectedConnections(disconnectedConnections);
        
        _logger.LogDebug(
            "Notificación enviada a {Count} conexiones del usuario {UserId}",
            sentCount, userId);
    }

    /// <summary>
    /// Notifica a todos los administradores conectados sobre un evento de pedido.
    /// </summary>
    /// <param name="notificacion">Datos de la notificación.</param>
    /// <returns>Tarea asíncrona de la notificación.</returns>
    /// <remarks>
    /// <para><b>Ejemplo de uso:</b></para>
    /// <code>
    /// // Un usuario creó un pedido, notificar a todos los admins
    /// await NotifyAdminsAsync(new PedidoNotificacion(
    ///     PedidoNotificationType.CREATED,
    ///     "PED-001",
    ///     123,  // userId del usuario que creó el pedido
    ///     "Pendiente",
    ///     pedidoDto
    /// ));
    /// // Todos los administradores conectados recibirán esta notificación
    /// </code>
    /// 
    /// <para><b>Casos de uso:</b></para>
    /// <list type="bullet">
    ///   <item><description>Notificar al admin cuando se crea un nuevo pedido.</description></item>
    ///   <item><description>Alertar al admin cuando cambia el estado de un pedido.</description></item>
    /// </list>
    /// </remarks>
    public async Task NotifyAdminsAsync(PedidoNotificacion notificacion)
    {
        var wrapper = CreateWrapper(notificacion);
        
        var sentCount = 0;
        var disconnectedConnections = new List<string>();

        foreach (var connection in _connections)
        {
            if (!connection.Value.IsAdmin) continue;
            
            try
            {
                if (connection.Value.WebSocket.State == WebSocketState.Open)
                {
                    await SendToSocketAsync(connection.Value.WebSocket, wrapper);
                    sentCount++;
                }
                else
                {
                    disconnectedConnections.Add(connection.Key);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error al enviar notificación a admin: {ConnectionId}", connection.Key);
                disconnectedConnections.Add(connection.Key);
            }
        }

        CleanupDisconnectedConnections(disconnectedConnections);
        
        _logger.LogDebug(
            "Notificación enviada a {Count} administradores",
            sentCount);
    }

    /// <summary>
    /// Notifica a un usuario Y a todos los administradores.
    /// Útil para eventos donde ambos deben ser notificados.
    /// </summary>
    /// <param name="userId">ID del usuario a notificar.</param>
    /// <param name="notificacion">Datos de la notificación.</param>
    /// <returns>Tarea asíncrona de la notificación.</returns>
    /// <remarks>
    /// <para><b>Ejemplo de uso:</b></para>
    /// <code>
    /// // El admin cambió el estado de un pedido, notificar al usuario y a los admins
    /// await NotifyUserAndAdminsAsync(123, new PedidoNotificacion(
    ///     PedidoNotificationType.ESTADO_UPDATED,
    ///     "PED-001",
    ///     123,
    ///     "Enviado",
    ///     pedidoDto
    /// ));
    /// // El usuario 123 y todos los admins recibirán la notificación
    /// </code>
    /// </remarks>
    public async Task NotifyUserAndAdminsAsync(long userId, PedidoNotificacion notificacion)
    {
        await Task.WhenAll(
            NotifyUserAsync(userId, notificacion),
            NotifyAdminsAsync(notificacion)
        );
    }

    /// <summary>
    /// Obtiene el número de conexiones activas.
    /// </summary>
    /// <returns>Número de conexiones activas.</returns>
    public int GetConnectionCount() => _connections.Count;

    /// <summary>
    /// Obtiene el número de administradores conectados.
    /// </summary>
    /// <returns>Número de administradores conectados.</returns>
    public int GetAdminConnectionCount()
    {
        return _connections.Count(c => c.Value.IsAdmin);
    }

    #region Métodos Privados

    private async Task HandleWebSocketLoopAsync(WebSocket webSocket, string connectionId)
    {
        try
        {
            var buffer = new byte[1024 * 4];
            var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

            while (!result.CloseStatus.HasValue)
            {
                result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            }

            await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
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

    private object CreateWrapper(PedidoNotificacion notificacion)
    {
        return new
        {
            entity = "pedidos",
            type = notificacion.Tipo,
            pedidoId = notificacion.PedidoId,
            userId = notificacion.UserId,
            estado = notificacion.Estado,
            data = notificacion.Data,
            timestamp = DateTime.UtcNow
        };
    }

    private async Task SendToSocketAsync(WebSocket webSocket, object data)
    {
        var json = JsonSerializer.Serialize(data, _jsonOptions);
        var bytes = Encoding.UTF8.GetBytes(json);
        var buffer = new ArraySegment<byte>(bytes);
        
        await webSocket.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
    }

    private async Task CloseWebSocketAsync(WebSocket webSocket, WebSocketCloseStatus status, string description)
    {
        try
        {
            if (webSocket.State == WebSocketState.Open)
            {
                await webSocket.CloseAsync(status, description, CancellationToken.None);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error cerrando WebSocket");
        }
    }

    private void CleanupDisconnectedConnections(List<string> connectionIds)
    {
        foreach (var connectionId in connectionIds)
        {
            _connections.TryRemove(connectionId, out _);
            _logger.LogDebug("Eliminado cliente WebSocket de pedido desconectado: {ConnectionId}", connectionId);
        }
    }

    #endregion
}

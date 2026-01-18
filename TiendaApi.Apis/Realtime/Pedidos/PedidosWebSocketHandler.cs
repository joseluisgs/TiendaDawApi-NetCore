using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TiendaApi.Apis.Realtime.Common;
using TiendaApi.Apis.Services.Auth;
using TiendaApi.Apis.Services.Cache;

namespace TiendaApi.Apis.Realtime.Pedidos;

/// <summary>
/// Datos de notificación para eventos de pedidos.
/// </summary>
/// <param name="Tipo">Tipo de notificación (CREADO, ESTADO_ACTUALIZADO).</param>
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
/// <code>ws://localhost:5000/ws/pedidos?token=JWT_TOKEN</code>
/// 
/// <para><b>Ejemplo de conexión desde cliente JavaScript:</b></para>
/// <code>
/// const token = "eyJhbGciOiJub25lIiwidHlwIjoiSldUIn0..."; // JWT del usuario
/// const ws = new WebSocket(`ws://localhost:5000/ws/pedidos?token=${token}`);
///
/// ws.onmessage = (event) => {
///     const data = JSON.parse(event.data);
///     console.log('Notificación:', data);
/// };
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
///   "type": "PEDIDO_ESTADO_ACTUALIZADO",
///   "pedidoId": "PED-ABC123",
///   "userId": 123,
///   "estado": "Enviado",
///   "data": { "id": "PED-ABC123", "estado": "Enviado", ... },
///   "timestamp": "2025-01-18T10:30:00Z"
/// }
/// </code>
/// 
/// <para><b>Sistema de caché para optimización de rendimiento:</b></para>
/// <list type="bullet">
///   <item>
///     <description>
///       <b>¿Por qué caché?</b> Las notificaciones WebSocket se envían frecuentemente (décimas de segundo).
///       Validar el JWT completo cada vez es costoso (1-5ms por validación). Usar caché reduce esto a
///       lecturas de memoria Redis (0.1-0.5ms) o memoria local (0.01ms), mejorando el rendimiento 10-50x.
///     </description>
///   </item>
///   <item>
///     <description>
///       <b>Patrón cache-aside:</b> Al conectar, se valida el JWT y se guarda el rol (admin/cliente) en caché.
///       Las notificaciones posteriores leen directamente de la caché.
///     </description>
///   </item>
///   <item>
///     <description>
///       <b>TTL (Time To Live):</b> El rol caduca según la configuración.
///     </description>
///   </item>
/// </list>
/// </remarks>
public class PedidosWebSocketHandler
{
    /// <summary>
    /// Clave de caché para almacenar si un usuario es admin.
    /// Formato: "ws:pedidos:admin:{userId}"
    /// </summary>
    private const string ADMIN_CACHE_KEY_PREFIX = "ws:pedidos:admin:";

    /// <summary>
    /// Estructura de conexión que incluye el WebSocket, userId y token del usuario.
    /// </summary>
    private record struct ConnectionInfo(WebSocket WebSocket, long UserId, string Token);

    private readonly ConcurrentDictionary<string, ConnectionInfo> _connections = new();
    private readonly ILogger<PedidosWebSocketHandler> _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly IJwtTokenExtractor _tokenExtractor;
    private readonly ICacheService _cacheService;
    private readonly TimeSpan _roleCacheTTL;

    public PedidosWebSocketHandler(
        ILogger<PedidosWebSocketHandler> logger,
        IJwtTokenExtractor tokenExtractor,
        ICacheService cacheService,
        IConfiguration configuration)
    {
        _logger = logger;
        _tokenExtractor = tokenExtractor;
        _cacheService = cacheService;
        
        var ttlMinutes = configuration.GetValue<int>("WebSocket:RoleCacheTTLMinutes", 5);
        _roleCacheTTL = TimeSpan.FromMinutes(ttlMinutes);
        
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        _logger.LogInformation(
            "PedidosWebSocketHandler inicializado con TTL de caché de roles: {TTL} minutos",
            ttlMinutes);
    }

    /// <summary>
    /// Maneja una nueva conexión WebSocket para pedidos.
    /// </summary>
    /// <param name="context">Contexto HTTP de la conexión.</param>
    /// <param name="webSocket">Instancia del WebSocket.</param>
    /// <returns>Tarea asíncrona representando la conexión.</returns>
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

        var cacheKey = $"{ADMIN_CACHE_KEY_PREFIX}{userId}";
        await _cacheService.SetAsync(cacheKey, isAdmin, _roleCacheTTL);
        
        var connectionId = Guid.NewGuid().ToString();
        _connections.TryAdd(connectionId, new ConnectionInfo(webSocket, userId.Value, token));

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
    public async Task NotifyAdminsAsync(PedidoNotificacion notificacion)
    {
        var wrapper = CreateWrapper(notificacion);
        
        var sentCount = 0;
        var disconnectedConnections = new List<string>();

        foreach (var connection in _connections)
        {
            var userId = connection.Value.UserId;
            var isAdmin = await GetUserAdminStatusAsync(userId, connection.Value.Token);
            
            if (!isAdmin) continue;
            
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
        
        _logger.LogDebug("Notificación enviada a {Count} administradores", sentCount);
    }

    /// <summary>
    /// Notifica a un usuario Y a todos los administradores.
    /// </summary>
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
    public int GetConnectionCount() => _connections.Count;

    #region Métodos Privados

    private async Task<bool> GetUserAdminStatusAsync(long userId, string token)
    {
        var cacheKey = $"{ADMIN_CACHE_KEY_PREFIX}{userId}";
        
        bool? cachedValue = await _cacheService.GetAsync<bool>(cacheKey);
        
        if (cachedValue.HasValue)
        {
            return cachedValue.Value;
        }

        var (_, isAdmin, _) = _tokenExtractor.ExtractUserInfo(token);
        
        if (isAdmin)
        {
            await _cacheService.SetAsync(cacheKey, isAdmin, _roleCacheTTL);
        }
        
        return isAdmin;
    }

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
            if (_connections.TryRemove(connectionId, out var connection))
            {
                var cacheKey = $"{ADMIN_CACHE_KEY_PREFIX}{connection.UserId}";
                await _cacheService.RemoveAsync(cacheKey);
            }
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
            if (_connections.TryRemove(connectionId, out var connection))
            {
                var cacheKey = $"{ADMIN_CACHE_KEY_PREFIX}{connection.UserId}";
                _cacheService.RemoveAsync(cacheKey).ConfigureAwait(false).GetAwaiter().GetResult();
            }
        }
    }

    #endregion
}

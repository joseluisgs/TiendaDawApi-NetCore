using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TiendaApi.Apis.Services.Auth;
using TiendaApi.Apis.Services.Cache;

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
/// const token = "eyJhbGciOiJub25lIiwidHlwIjoiSldUIn0..."; // JWT del usuario
/// const ws = new WebSocket(`ws://localhost:5000/ws/v1/pedidos?token=${token}`);
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
///   "type": "PEDIDO_ESTADO_UPDATED",
///   "pedidoId": "PED-ABC123",
///   "userId": 123,
///   "estado": "Enviado",
///   "data": { "id": "PED-ABC123", "estado": "Enviado", ... },
///   "timestamp": "2025-01-16T10:30:00Z"
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
///       <b>TTL (Time To Live):</b> El rol caduca según la configuración:
///       - Desarrollo: 5 minutos
///       - Producción: 3 minutos
///       Esto balancea rendimiento con seguridad (rol puede estar desactualizado max TTL).
///     </description>
///   </item>
///   <item>
///     <description>
///       <b>Backend de caché:</b> El sistema usa ICacheService, permitiendo:
///       - Desarrollo: MemoryCacheService (en proceso)
///       - Producción: RedisCacheService (distribuido, compartido entre instancias)
///     </description>
///   </item>
/// </list>
/// 
/// <para><b>Configuración (appsettings.json):</b></para>
/// <code>
/// {
///   "WebSocket": {
///     "RoleCacheTTLMinutes": 5  // Desarrollo: 5, Producción: 3
///   }
/// }
/// </code>
/// </remarks>
public class PedidoWebSocketHandler
{
    /// <summary>
    /// Clave de caché para almacenar si un usuario es admin.
    /// Formato: "ws:pedidos:admin:{userId}"
    /// </summary>
    private const string ADMIN_CACHE_KEY_PREFIX = "ws:pedidos:admin:";

    /// <summary>
    /// Estructura de conexión que incluye el WebSocket, userId y token del usuario.
    /// El token se almacena para poder revalidar si el caché expira.
    /// </summary>
    private record struct ConnectionInfo(WebSocket WebSocket, long UserId, string Token);

    private readonly ConcurrentDictionary<string, ConnectionInfo> _connections = new();
    private readonly ILogger<PedidoWebSocketHandler> _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly IJwtTokenExtractor _tokenExtractor;
    private readonly ICacheService _cacheService;
    private readonly TimeSpan _roleCacheTTL;

    /// <summary>
    /// Constructor del handler de WebSocket para pedidos.
    /// </summary>
    /// <param name="logger">Logger para eventos del handler.</param>
    /// <param name="tokenExtractor">Servicio para extraer información del JWT.</param>
    /// <param name="cacheService">Servicio de caché para optimizar notificaciones.</param>
    /// <param name="configuration">Configuración de la aplicación.</param>
    /// <remarks>
    /// <para><b>Inyección de dependencias:</b></para>
    /// <list type="bullet">
    ///   <item><description>ICacheService: MemoryCacheService (desarrollo) o RedisCacheService (producción).</description></item>
    ///   <item><description>IJwtTokenExtractor: Extrae userId y rol del JWT sin acceder a BD.</description></item>
    /// </list>
    /// </remarks>
    public PedidoWebSocketHandler(
        ILogger<PedidoWebSocketHandler> logger,
        IJwtTokenExtractor tokenExtractor,
        ICacheService cacheService,
        IConfiguration configuration)
    {
        _logger = logger;
        _tokenExtractor = tokenExtractor;
        _cacheService = cacheService;
        
        // Leer TTL de configuración, con valores por entorno
        var ttlMinutes = configuration.GetValue<int>("WebSocket:RoleCacheTTLMinutes", 5);
        _roleCacheTTL = TimeSpan.FromMinutes(ttlMinutes);
        
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        _logger.LogInformation(
            "PedidoWebSocketHandler inicializado con TTL de caché de roles: {TTL} minutos",
            ttlMinutes);
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
    ///   <item><description>Almacena el rol en caché para optimizar notificaciones futuras.</description></item>
    ///   <item><description>Almacena la conexión junto con userId y token.</description></item>
    ///   <item><description>Si no hay token o es inválido, cierra la conexión.</description></item>
    /// </list>
    /// 
    /// <para><b>Claims extraídos del JWT:</b></para>
    /// <list type="bullet">
    ///   <item><description>ClaimTypes.NameIdentifier → userId</description></item>
    ///   <item><description>ClaimTypes.Role → rol del usuario (admin/cliente)</description></item>
    /// </list>
    /// 
    /// <para><b>Almacenamiento en caché:</b></para>
    /// <code>
    /// // Guardar en caché para notificaciones rápidas
    /// await _cacheService.SetAsync($"ws:pedidos:admin:{userId}", isAdmin, _roleCacheTTL);
    /// </code>
    /// 
    /// <para><b>Ejemplo de conexión exitosa:</b></para>
    /// <code>
    /// // El cliente envía: ws://localhost:5000/ws/v1/pedidos?token=JWT
    /// // El servidor:
    /// // 1. Extrae userId y rol del JWT
    /// // 2. Guarda "ws:pedidos:admin:123" -> true en caché
    /// // 3. Almacena la conexión para notificaciones
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

        // Guardar rol en caché para optimizar notificaciones
        // Esto evita validar el JWT completo en cada notificación
        var cacheKey = $"{ADMIN_CACHE_KEY_PREFIX}{userId}";
        await _cacheService.SetAsync(cacheKey, isAdmin, _roleCacheTTL);
        
        _logger.LogDebug(
            "Rol de usuario {UserId} cacheado: IsAdmin={IsAdmin}, TTL={TTL} minutos",
            userId, isAdmin, _roleCacheTTL.TotalMinutes);

        var connectionId = Guid.NewGuid().ToString();
        _connections.TryAdd(connectionId, new ConnectionInfo(webSocket, userId.Value, token));

        _logger.LogInformation(
            "Conexión WebSocket establecida para pedidos: {ConnectionId}, UserId: {UserId}, IsAdmin: {IsAdmin}",
            connectionId, userId, isAdmin);

        await HandleWebSocketLoopAsync(webSocket, connectionId);
    }

    /// <summary>
    /// Notifica a un usuario específico sobre un evento de su pedido.
    /// Optimizado para usar caché en lugar de validar JWT completo.
    /// </summary>
    /// <param name="userId">ID del usuario a notificar.</param>
    /// <param name="notificacion">Datos de la notificación.</param>
    /// <returns>Tarea asíncrona de la notificación.</returns>
    /// <remarks>
    /// <para><b>Optimización con caché:</b></para>
    /// <code>
    /// // En lugar de validar JWT completo cada vez:
    /// var isAdmin = await _cacheService.GetAsync&lt;bool&gt;(cacheKey);
    /// 
    /// // Solo si el caché expira, usamos la conexión almacenada para revalidar
    /// if (!isAdmin.HasValue)
    /// {
    ///     var connection = _connections.Values.First(c => c.UserId == userId);
    ///     var (uid, admin, _) = _tokenExtractor.ExtractUserInfo(connection.Token);
    ///     // Renovar caché
    /// }
    /// </code>
    /// 
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
    /// Optimizado usando caché para determinar quién es admin.
    /// </summary>
    /// <param name="notificacion">Datos de la notificación.</param>
    /// <returns>Tarea asíncrona de la notificación.</returns>
    /// <remarks>
    /// <para><b>Flujo de notificaciones a admins:</b></para>
    /// <list type="number">
    ///   <item><description>Para cada conexión activa, obtener el rol de la caché.</description></item>
    ///   <item><description>Si es admin, enviar notificación.</description></item>
    ///   <item><description>Si la caché expiró, usar el token almacenado para revalidar.</description></item>
    /// </list>
    /// 
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
    /// <para><b>Rendimiento:</b></para>
    /// <list type="bullet">
    ///   <item><description>Sin caché: 1-5ms por validación JWT × N admins = N×(1-5ms)</description></item>
    ///   <item><description>Con caché: 0.01-0.5ms por lectura × N admins = N×(0.01-0.5ms)</description></item>
    ///   <item><description>Mejora típica: 10-50x más rápido</description></item>
    /// </list>
    /// </remarks>
    public async Task NotifyAdminsAsync(PedidoNotificacion notificacion)
    {
        var wrapper = CreateWrapper(notificacion);
        
        var sentCount = 0;
        var disconnectedConnections = new List<string>();

        foreach (var connection in _connections)
        {
            var userId = connection.Value.UserId;
            
            // Obtener rol de la caché (rápido) o revalidar si expiró
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
        var count = 0;
        foreach (var connection in _connections)
        {
            var userId = connection.Value.UserId;
            var isAdmin = GetUserAdminStatusAsync(userId, connection.Value.Token).GetAwaiter().GetResult();
            if (isAdmin) count++;
        }
        return count;
    }

    #region Métodos Privados

    /// <summary>
    /// Obtiene el estado de administrador de un usuario.
    /// Primero intenta leer de la caché; si no existe, revalida con el token.
    /// </summary>
    /// <param name="userId">ID del usuario.</param>
    /// <param name="token">Token JWT del usuario (para revalidar si caché expira).</param>
    /// <returns>True si es admin, False si no lo es.</returns>
    /// <remarks>
    /// <para><b>Patrón cache-aside:</b></para>
    /// <list type="number">
    ///   <item><description>Leer de caché (rápido).</description></item>
    ///   <item><description>Si cache hit → devolver valor.</description></item>
    ///   <item><description>Si cache miss → revalidar JWT y renovar caché.</description></item>
    /// </list>
    /// </remarks>
    private async Task<bool> GetUserAdminStatusAsync(long userId, string token)
    {
        var cacheKey = $"{ADMIN_CACHE_KEY_PREFIX}{userId}";
        
        // Intentar leer de caché
        bool? cachedValue = await _cacheService.GetAsync<bool>(cacheKey);
        
        if (cachedValue.HasValue)
        {
            _logger.LogTrace("Cache hit para usuario {UserId}: IsAdmin={IsAdmin}", userId, cachedValue);
            return cachedValue.Value;
        }

        // Cache miss: revalidar JWT y renovar caché
        _logger.LogDebug("Cache miss para usuario {UserId}, revalidando JWT", userId);
        
        var (uid, isAdmin, _) = _tokenExtractor.ExtractUserInfo(token);
        
        if (uid.HasValue)
        {
            // Renovar caché con TTL configurado
            await _cacheService.SetAsync(cacheKey, isAdmin, _roleCacheTTL);
            _logger.LogDebug(
                "Caché renovada para usuario {UserId}: IsAdmin={IsAdmin}, TTL={TTL} minutos",
                userId, isAdmin, _roleCacheTTL.TotalMinutes);
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
                // Limpiar caché al desconectar (opcional)
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
                // Limpiar caché al desconectar
                var cacheKey = $"{ADMIN_CACHE_KEY_PREFIX}{connection.UserId}";
                _cacheService.RemoveAsync(cacheKey).ConfigureAwait(false).GetAwaiter().GetResult();
            }
            _logger.LogDebug("Eliminado cliente WebSocket de pedido desconectado: {ConnectionId}", connectionId);
        }
    }

    #endregion
}

using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TiendaApi.Api.Realtime.Common;
using TiendaApi.Api.Services.Auth;
using TiendaApi.Api.Services.Cache;

namespace TiendaApi.Api.Realtime.Pedidos;

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
/// Handler de WebSocket para notificaciones de pedidos (requiere JWT).
/// Usuarios ven SUS pedidos, admins ven TODOS.
/// </summary>
/// <example>
/// ws://localhost:5000/ws/pedidos?token=JWT_TOKEN
/// const token = "eyJhbGciOiJub25lIiwidHlwIjoiSldUIn0...";
/// const ws = new WebSocket(`ws://localhost:5000/ws/pedidos?token=${token}`);
/// ws.onmessage = (event) => console.log('Notificación:', JSON.parse(event.data));
/// // Respuesta: {"entity":"pedidos","type":"PEDIDO_ESTADO_ACTUALIZADO","pedidoId":"PED-ABC123","estado":"Enviado","timestamp":"2025-01-18T10:30:00Z"}
/// </example>
public class PedidosWebSocketHandler
{
    private const string ADMIN_CACHE_KEY_PREFIX = "ws:pedidos:admin:";
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
        _jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        _logger.LogInformation("PedidosWebSocketHandler inicializado con TTL: {TTL} minutos", ttlMinutes);
    }

    /// <summary>Maneja una conexión WebSocket (requiere token JWT).</summary>
    /// <param name="context">Contexto HTTP.</param>
    /// <param name="webSocket">Instancia del WebSocket.</param>
    public async Task HandleConnectionAsync(HttpContext context, WebSocket webSocket)
    {
        var token = context.Request.Query["token"].FirstOrDefault();

        if (string.IsNullOrEmpty(token))
        {
            await CloseWebSocketAsync(webSocket, WebSocketCloseStatus.PolicyViolation, "Token requerido");
            return;
        }

        var (userId, isAdmin, _) = _tokenExtractor.ExtractUserInfo(token);

        if (userId == null)
        {
            await CloseWebSocketAsync(webSocket, WebSocketCloseStatus.PolicyViolation, "Token inválido");
            return;
        }

        var cacheKey = $"{ADMIN_CACHE_KEY_PREFIX}{userId}";
        await _cacheService.SetAsync(cacheKey, isAdmin, _roleCacheTTL);

        var connectionId = Guid.NewGuid().ToString();
        _connections.TryAdd(connectionId, new ConnectionInfo(webSocket, userId.Value, token));
        _logger.LogInformation("Conexión WebSocket: {ConnectionId}, UserId: {UserId}, IsAdmin: {IsAdmin}",
            connectionId, userId, isAdmin);

        await HandleWebSocketLoopAsync(webSocket, connectionId);
    }

    /// <summary>Notifica a un usuario específico.</summary>
    /// <param name="userId">ID del usuario.</param>
    /// <param name="notificacion">Datos de la notificación.</param>
    public async Task NotifyUserAsync(long userId, PedidoNotificacion notificacion)
    {
        var wrapper = CreateWrapper(notificacion);
        var sentCount = 0;
        var disconnected = new List<string>();

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
                    disconnected.Add(connection.Key);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error enviando a: {ConnectionId}", connection.Key);
                disconnected.Add(connection.Key);
            }
        }

        CleanupDisconnectedConnections(disconnected);
        _logger.LogDebug("Notificación enviada a {Count} conexiones del usuario {UserId}", sentCount, userId);
    }

    /// <summary>Notifica a todos los administradores.</summary>
    /// <param name="notificacion">Datos de la notificación.</param>
    public async Task NotifyAdminsAsync(PedidoNotificacion notificacion)
    {
        var wrapper = CreateWrapper(notificacion);
        var sentCount = 0;
        var disconnected = new List<string>();

        foreach (var connection in _connections)
        {
            var isAdmin = await GetUserAdminStatusAsync(connection.Value.UserId, connection.Value.Token);
            if (!isAdmin) continue;

            try
            {
                if (connection.Value.WebSocket.State == WebSocketState.Open)
                {
                    await SendToSocketAsync(connection.Value.WebSocket, wrapper);
                    sentCount++;
                }
                else
                    disconnected.Add(connection.Key);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error enviando a admin: {ConnectionId}", connection.Key);
                disconnected.Add(connection.Key);
            }
        }

        CleanupDisconnectedConnections(disconnected);
        _logger.LogDebug("Notificación enviada a {Count} administradores", sentCount);
    }

    /// <summary>Notifica a usuario Y administradores.</summary>
    public async Task NotifyUserAndAdminsAsync(long userId, PedidoNotificacion notificacion)
    {
        await Task.WhenAll(NotifyUserAsync(userId, notificacion), NotifyAdminsAsync(notificacion));
    }

    /// <summary>Obtiene el número de conexiones activas.</summary>
    /// <returns>Número de conexiones.</returns>
    public int GetConnectionCount() => _connections.Count;

    private async Task<bool> GetUserAdminStatusAsync(long userId, string token)
    {
        var cacheKey = $"{ADMIN_CACHE_KEY_PREFIX}{userId}";
        var cachedValue = await _cacheService.GetAsync<bool?>(cacheKey);
        
        if (cachedValue.HasValue)
            return cachedValue.Value;

        var (_, isAdmin, _) = _tokenExtractor.ExtractUserInfo(token);
        if (isAdmin) await _cacheService.SetAsync(cacheKey, isAdmin, _roleCacheTTL);
        return isAdmin;
    }

    private async Task HandleWebSocketLoopAsync(WebSocket webSocket, string connectionId)
    {
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
            if (_connections.TryRemove(connectionId, out var connection))
            {
                var cacheKey = $"{ADMIN_CACHE_KEY_PREFIX}{connection.UserId}";
                await _cacheService.RemoveAsync(cacheKey);
            }
            _logger.LogInformation("Conexión cerrada: {ConnectionId}", connectionId);
        }
    }

    private object CreateWrapper(PedidoNotificacion notificacion)
    {
        var wrapper = new Dictionary<string, object?>
        {
            ["entity"] = "pedidos",
            ["type"] = notificacion.Tipo,
            ["pedidoId"] = notificacion.PedidoId,
            ["userId"] = notificacion.UserId,
            ["estado"] = notificacion.Estado,
            ["timestamp"] = DateTime.UtcNow
        };
        
        if (notificacion.Data != null)
            wrapper["data"] = notificacion.Data;
        
        return wrapper;
    }

    private async Task SendToSocketAsync(WebSocket webSocket, object data)
    {
        var json = JsonSerializer.Serialize(data, _jsonOptions);
        var bytes = Encoding.UTF8.GetBytes(json);
        await webSocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
    }

    private async Task CloseWebSocketAsync(WebSocket webSocket, WebSocketCloseStatus status, string description)
    {
        try
        {
            if (webSocket.State == WebSocketState.Open)
                await webSocket.CloseAsync(status, description, CancellationToken.None);
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
}

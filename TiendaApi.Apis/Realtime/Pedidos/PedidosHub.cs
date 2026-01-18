using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using TiendaApi.Apis.Realtime.Common;

namespace TiendaApi.Apis.Realtime.Pedidos;

/// <summary>
/// Hub de SignalR para notificaciones en tiempo real de pedidos.
/// Requiere autenticación JWT y filtra por usuario/rol.
/// </summary>
/// <remarks>
/// <para><b>Características:</b></para>
/// <list type="bullet">
///   <item><description>Usuarios normales reciben notificaciones de SUS pedidos.</description></item>
///   <item><description>Administradores reciben notificaciones de TODOS los pedidos.</description></item>
///   <item><description>Requiere autenticación JWT.</description></item>
/// </list>
/// 
/// <para><b>Endpoint:</b></para>
/// <code>ws://localhost:5000/hubs/pedidos</code>
/// 
/// <para><b>Conexión desde cliente JavaScript:</b></para>
/// <code>
/// const connection = new HubConnectionBuilder()
///     .withUrl("/hubs/pedidos", {
///         accessTokenFactory: () => jwtToken
///     })
///     .build();
///
/// connection.on("PedidoCreado", (pedido) => {
///     console.log("Nuevo pedido:", pedido);
/// });
///
/// connection.on("PedidoEstadoActualizado", (pedido) => {
///     console.log("Pedido actualizado:", pedido);
/// });
///
/// await connection.start();
/// </code>
/// 
/// <para><b>Grupos automáticos:</b></para>
/// <list type="bullet">
///   <item><description>user-{userId}: Notificaciones privadas del usuario.</description></item>
///   <item><description>admins: Notificaciones para todos los administradores.</description></item>
/// </list>
/// 
/// <para><b>Eventos recibidos:</b></para>
/// <list type="table">
///   <item>
///     <term>PedidoCreado</term>
///     <description>Se creó un nuevo pedido.</description>
///   </item>
///   <item>
///     <term>PedidoEstadoActualizado</term>
///     <description>Se cambió el estado de un pedido.</description>
///   </item>
/// </list>
/// 
/// <para><b>Ejemplo de respuesta:</b></para>
/// <code>
/// {
///   "pedidoId": "PED-001",
///   "userId": 123,
///   "estado": "Pendiente",
///   "tipo": "PEDIDO_CREADO",
///   "timestamp": "2025-01-18T10:30:00Z"
/// }
/// </code>
/// 
/// <para><b>Lógica de notificaciones:</b></para>
/// <list type="bullet">
///   <item><description>Usuario normal: Solo ve notificaciones de SUS pedidos.</description></item>
///   <item><description>Administrador: Ve notificaciones de TODOS los pedidos.</description></item>
/// </list>
/// </remarks>
[Authorize]
public class PedidosHub : Hub
{
    private readonly ILogger<PedidosHub> _logger;

    public PedidosHub(ILogger<PedidosHub> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Se ejecuta cuando un cliente se conecta.
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        var connectionId = Context.ConnectionId;
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var userName = Context.User?.Identity?.Name;
        var isAdmin = Context.User?.IsInRole("Admin") == true;

        _logger.LogInformation(
            "Cliente SignalR conectado a PedidosHub: {ConnectionId}, UserId: {UserId}, UserName: {UserName}, IsAdmin: {IsAdmin}",
            connectionId, userId, userName, isAdmin);

        // Suscribir automáticamente al usuario a sus notificaciones privadas
        if (userId != null)
        {
            await Groups.AddToGroupAsync(connectionId, $"user-{userId}");
        }

        // Si es admin, suscribir al canal de administradores
        if (isAdmin)
        {
            await Groups.AddToGroupAsync(connectionId, "admins");
        }

        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Se ejecuta cuando un cliente se desconecta.
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var connectionId = Context.ConnectionId;
        if (exception != null)
        {
            _logger.LogWarning(exception, "Cliente SignalR desconectado con error: {ConnectionId}", connectionId);
        }
        else
        {
            _logger.LogInformation("Cliente SignalR desconectado: {ConnectionId}", connectionId);
        }
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Obtiene información de la conexión actual.
    /// </summary>
    [Authorize]
    public object GetConnectionInfo()
    {
        return new
        {
            connectionId = Context.ConnectionId,
            userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value,
            userName = Context.User?.Identity?.Name,
            isAdmin = Context.User?.IsInRole("Admin") == true
        };
    }
}

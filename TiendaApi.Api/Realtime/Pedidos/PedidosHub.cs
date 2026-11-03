using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using TiendaApi.Api.Realtime.Common;


namespace TiendaApi.Api.Realtime.Pedidos;

/// <summary>
/// Hub de SignalR para notificaciones de pedidos (requiere JWT).
/// Usuarios normales ven SUS pedidos, admins ven TODOS.
/// </summary>
/// <example>
/// ws://localhost:5000/hubs/pedidos
/// const connection = new HubConnectionBuilder().withUrl("/hubs/pedidos", { accessTokenFactory: () => jwtToken }).build();
/// connection.on("PedidoCreado", (pedido) => console.log("Nuevo:", pedido));
/// // Grupos: user-{userId} (privado), admins (administradores)
/// // Respuesta: {"pedidoId":"PED-001","userId":123,"estado":"Pendiente","tipo":"PEDIDO_CREADO","timestamp":"2025-01-18T10:30:00Z"}
/// </example>
[Authorize]
public class PedidosHub(ILogger<PedidosHub> logger) : Hub
{
    /// <summary>Cliente conectado - se suscribe a grupos.</summary>
    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var isAdmin = EsAdmin(Context.User);

        logger.LogInformation("Cliente conectado: {ConnectionId}, UserId: {UserId}, IsAdmin: {IsAdmin}",
            Context.ConnectionId, userId, isAdmin);

        if (userId != null)
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user-{userId}");

        if (isAdmin)
            await Groups.AddToGroupAsync(Context.ConnectionId, "admins");

        await base.OnConnectedAsync();
    }

    /// <summary>
    /// 🎓 Detecta el rol admin de forma case-insensitive, igual que hace el
    /// canal WebSocket (<c>JwtTokenExtractor.IsAdmin</c>). El rol real que
    /// emite el JWT es <c>UserRoles.ADMIN = "ADMIN"</c> y <c>IsInRole</c>
    /// compara ordinal: con <c>IsInRole("Admin")</c> los administradores NO
    /// entraban al grupo <c>admins</c> y no recibían nada por SignalR.
    /// </summary>
    private static bool EsAdmin(ClaimsPrincipal? user) =>
        user?.Claims.Any(c =>
            (c.Type == ClaimTypes.Role || c.Type == "role") &&
            string.Equals(c.Value, "admin", StringComparison.OrdinalIgnoreCase)) == true;

    /// <summary>Cliente desconectado.</summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (exception != null)
            logger.LogWarning(exception, "Cliente desconectado: {ConnectionId}", Context.ConnectionId);
        else
            logger.LogInformation("Cliente desconectado: {ConnectionId}", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>Obtiene información de la conexión.</summary>
    /// <returns>Datos de conexión.</returns>
    [Authorize]
    public object GetConnectionInfo() => new
    {
        connectionId = Context.ConnectionId,
        userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value,
        userName = Context.User?.Identity?.Name,
        isAdmin = EsAdmin(Context.User)
    };
}

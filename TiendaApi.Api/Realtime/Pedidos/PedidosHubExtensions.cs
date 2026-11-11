using Microsoft.AspNetCore.SignalR;

namespace TiendaApi.Api.Realtime.Pedidos;

/// <summary>
/// 🎓 Envío selectivo de notificaciones de pedidos por SignalR: el usuario
/// dueño (grupo <c>user-{userId}</c>) y todos los administradores (grupo
/// <c>admins</c>). Es el equivalente por SignalR de
/// <c>PedidosWebSocketHandler.NotifyUserAndAdminsAsync</c>: UNA sola función
/// que llama a los dos destinatarios, para que ningún punto de llamada pueda
/// olvidar uno de ellos. Antes se usaba <c>Clients.All</c> y cualquier
/// usuario autenticado recibía todos los pedidos ajenos.
/// </summary>
public static class PedidosHubExtensions {
    /// <summary>
    /// Envía <paramref name="payload"/> en el evento <paramref name="method"/>
    /// del hub de pedidos a los grupos del dueño y de administradores.
    /// </summary>
    /// <param name="hubContext">Contexto del hub de pedidos.</param>
    /// <param name="userId">ID del usuario dueño del pedido.</param>
    /// <param name="method">Nombre del evento que escucha el cliente.</param>
    /// <param name="payload">Datos de la notificación.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    public static Task SendPedidoToUserAndAdminsAsync(
        this IHubContext<PedidosHub> hubContext,
        long userId,
        string method,
        object payload,
        CancellationToken cancellationToken = default) {
        var clientes = hubContext.Clients;
        return Task.WhenAll(
            clientes.Group($"user-{userId}").SendAsync(method, payload, cancellationToken),
            clientes.Group("admins").SendAsync(method, payload, cancellationToken));
    }
}

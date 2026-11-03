using TiendaApi.Api.Dtos.Pedidos;

namespace TiendaApi.Api.Services.Pedidos;

/// <summary>
/// 🎓 Contrato de los efectos secundarios de PEDIDOS (tiempo real WS + SignalR
/// y emails de estado). La abstracción sobre la que depende <c>PedidosService</c>
/// (DIP): el servicio de negocio no conoce los canales, solo este contrato; la
/// implementación y los mocks de test se sustituyen por DI sin tocar la
/// lógica de transacción y stock.
/// </summary>
/// <remarks>
/// 🎓 Cada evento de tiempo real expone UN único método combinado (igual que
/// <c>PedidosWebSocketHandler.NotifyUserAndAdminsAsync</c> combina usuario y
/// admins dentro del canal WS): así es imposible que un punto de llamada
/// olvide un canal y acabe emitiendo solo WS o solo SignalR. El reparto
/// equivalente en el proyecto CQRS son sus notifications + handlers, donde
/// un único <c>mediator.Publish</c> dispara todos los canales.
/// </remarks>
public interface IPedidoEffects
{
    /// <summary>
    /// Notifica la creación de un pedido por WebSocket Y SignalR
    /// (la recibe el usuario dueño y todos los administradores).
    /// </summary>
    void NotificarPedidoCreado(string pedidoId, long userId, string estado, PedidoDto pedido);

    /// <summary>
    /// Notifica la actualización de un pedido por WebSocket Y SignalR
    /// (la recibe el usuario dueño y todos los administradores).
    /// </summary>
    void NotificarPedidoActualizado(string pedidoId, long userId, string estado, PedidoDto pedido);

    /// <summary>
    /// Notifica la eliminación/cancelación de un pedido por WebSocket Y SignalR
    /// (la recibe el usuario dueño y todos los administradores).
    /// </summary>
    void NotificarPedidoEliminado(string pedidoId, long userId, string estado, decimal total);

    /// <summary>
    /// Encola el email de confirmación AL CLIENTE que hizo el pedido y el
    /// aviso de nuevo pedido AL ADMINISTRADOR.
    /// </summary>
    void EnviarEmailPedidoCreado(string pedidoId, decimal total, int itemCount, long userId, DestinatarioDto? destinatario);

    /// <summary>Encola el email de cambio de estado (cliente).</summary>
    void EnviarEmailPedidoEstadoActualizado(string pedidoId, string estadoAnterior, string nuevoEstado, decimal total, long userId);

    /// <summary>Encola el email de aviso al administrador al actualizar un pedido.</summary>
    void EnviarEmailPedidoActualizadoAdmin(string pedidoId, string estado, decimal total, long userId);

    /// <summary>Encola el email de aviso al administrador al eliminar un pedido.</summary>
    void EnviarEmailPedidoEliminadoAdmin(string pedidoId, decimal total, long userId);
}

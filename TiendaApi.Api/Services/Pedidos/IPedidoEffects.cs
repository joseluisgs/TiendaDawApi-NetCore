using TiendaApi.Api.Dtos.Pedidos;

namespace TiendaApi.Api.Services.Pedidos;

/// <summary>
/// 🎓 Contrato de los efectos secundarios de PEDIDOS (tiempo real WS +
/// SignalR y emails de estado). La abstracción sobre la que depende
/// <c>PedidosService</c> (DIP): el servicio de negocio no conoce los canales,
/// solo este contrato; la implementación y los mocks de test se sustituyen por
/// DI sin tocar la lógica de transacción y stock.
/// </summary>
/// <remarks>
/// 🎓 Cada evento de tiempo real expone UN único método combinado (igual que
/// <c>PedidosWebSocketHandler.NotifyUserAndAdminsAsync</c> combina usuario y
/// admins dentro del canal WS): así es imposible que un punto de llamada
/// olvide un canal y acabe emitiendo solo WS o solo SignalR. El reparto
/// equivalente en el proyecto CQRS son sus notifications + handlers, donde
/// un único <c>mediator.Publish</c> dispara todos los canales.
///
/// 📧 Matriz de emails de pedidos (ver <c>doc/21-email-services.md</c>):
/// el USUARIO recibe todo lo que pasa a su pedido (creación, cambio de
/// estado, modificación por admin, cancelación) y el ADMIN solo recibe lo
/// que debe gestionar y no proviene de él: creación y cancelación por
/// usuario. Las acciones que el admin realiza él mismo no le generan email.
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
    /// Encola la confirmación AL CLIENTE y el aviso de nuevo pedido AL
    /// ADMINISTRADOR tras crear un pedido.
    /// </summary>
    void EnviarEmailPedidoCreado(string pedidoId, decimal total, int itemCount, long userId, DestinatarioDto? destinatario);

    /// <summary>
    /// Encola el email de cambio de estado AL CLIENTE (la plantilla está
    /// redactada en su voz: "Tu pedido pasó de A a B").
    /// </summary>
    void EnviarEmailPedidoEstadoActualizado(string pedidoId, string estadoAnterior, string nuevoEstado, decimal total, DestinatarioDto? destinatario);

    /// <summary>
    /// Encola el email de aviso AL CLIENTE cuando un administrador modifica
    /// datos de su pedido sin cambiar el estado.
    /// </summary>
    void EnviarEmailPedidoModificadoCliente(string pedidoId, DestinatarioDto? destinatario);

    /// <summary>
    /// Encola el email de cancelación AL CLIENTE (cancelación propia o
    /// eliminación por administrador).
    /// </summary>
    void EnviarEmailPedidoCanceladoCliente(string pedidoId, decimal total, DestinatarioDto? destinatario);

    /// <summary>
    /// Encola el aviso AL ADMINISTRADOR cuando un usuario cancela su propio
    /// pedido (es lo único que el admin debe gestionar y no proviene de él,
    /// aparte de la creación).
    /// </summary>
    void EnviarEmailPedidoCanceladoAdmin(string pedidoId, decimal total, long userId);
}

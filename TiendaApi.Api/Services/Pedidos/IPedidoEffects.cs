using TiendaApi.Api.Dtos.Pedidos;

namespace TiendaApi.Api.Services.Pedidos;

/// <summary>
/// 🎓 Contrato de los efectos secundarios de PEDIDOS (WebSocket, SignalR y
/// emails de estado). La abstracción sobre la que depende <c>PedidosService</c>
/// (DIP): el servicio de negocio no conoce los canales, solo este contrato; la
/// implementación y los mocks de test se sustituyen por DI sin tocar la
/// lógica de transacción y stock.
/// </summary>
public interface IPedidoEffects
{
    /// <summary>Notifica vía WebSocket la creación de un pedido.</summary>
    void NotificarWebSocketPedidoCreado(string pedidoId, long userId, string estado);

    /// <summary>Notifica vía WebSocket la actualización de un pedido.</summary>
    void NotificarWebSocketPedidoActualizado(string pedidoId, long userId, string estado, PedidoDto pedido);

    /// <summary>Notifica vía SignalR la creación de un pedido.</summary>
    void NotificarSignalRPedidoCreado(string pedidoId, long userId, string estado, PedidoDto pedido);

    /// <summary>Notifica vía SignalR la actualización de un pedido.</summary>
    void NotificarSignalRPedidoActualizado(string pedidoId, long userId, string estado, PedidoDto pedido);

    /// <summary>Notifica vía SignalR la eliminación de un pedido.</summary>
    void NotificarSignalRPedidoEliminado(string pedidoId, long userId, string estado);

    /// <summary>Encola el email de bienvenida al cliente tras crear un pedido.</summary>
    void EnviarEmailPedidoCreado(string pedidoId, decimal total, int itemCount, long userId);

    /// <summary>Encola el email de cambio de estado (cliente).</summary>
    void EnviarEmailPedidoEstadoActualizado(string pedidoId, string estadoAnterior, string nuevoEstado, decimal total, long userId);

    /// <summary>Encola el email de aviso al administrador al actualizar un pedido.</summary>
    void EnviarEmailPedidoActualizadoAdmin(string pedidoId, string estado, decimal total, long userId);

    /// <summary>Encola el email de aviso al administrador al eliminar un pedido.</summary>
    void EnviarEmailPedidoEliminadoAdmin(string pedidoId, decimal total, long userId);
}

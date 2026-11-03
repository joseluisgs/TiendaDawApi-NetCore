using TiendaApi.Api.Dtos.Productos;
using TiendaApi.Api.Models;

namespace TiendaApi.Api.Services.Productos;

/// <summary>
/// 🎓 Contrato de los efectos secundarios de PRODUCTOS (tiempo real WS +
/// SignalR, email y suscripciones GraphQL). La abstracción sobre la que
/// depende <c>ProductoService</c> (DIP): el servicio de negocio no conoce los
/// canales, solo este contrato; la implementación y los mocks de test se
/// sustituyen por DI sin tocar la lógica de negocio.
/// </summary>
/// <remarks>
/// 🎓 Cada evento de tiempo real expone UN único método combinado (igual que
/// <c>PedidosWebSocketHandler.NotifyUserAndAdminsAsync</c> combina usuario y
/// admins dentro del canal WS): así es imposible que un punto de llamada
/// olvide un canal y acabe emitiendo solo WS o solo SignalR. Los productos
/// se difunden a todos los clientes en ambos canales (catálogo público). El
/// reparto equivalente en el proyecto CQRS son sus notifications + handlers,
/// donde un único <c>mediator.Publish</c> dispara todos los canales.
/// </remarks>
public interface IProductoEffects
{
    /// <summary>
    /// Notifica la creación de un producto por WebSocket Y SignalR
    /// (broadcast: catálogo público).
    /// </summary>
    void NotificarProductoCreado(ProductoDto producto);

    /// <summary>
    /// Notifica la actualización de un producto por WebSocket Y SignalR
    /// (broadcast: catálogo público).
    /// </summary>
    void NotificarProductoActualizado(ProductoDto producto);

    /// <summary>
    /// Notifica la eliminación de un producto por WebSocket Y SignalR
    /// (broadcast: catálogo público).
    /// </summary>
    void NotificarProductoEliminado(long productoId);

    /// <summary>Encola el email de aviso al administrador tras crear un producto.</summary>
    void EnviarEmailProductoCreado(Producto producto);

    /// <summary>Publica el evento de suscripción GraphQL de creación.</summary>
    void EventoSuscripcionProductoCreado(ProductoDto producto);

    /// <summary>Publica el evento de suscripción GraphQL de actualización.</summary>
    void EventoSuscripcionProductoActualizado(ProductoDto producto);

    /// <summary>Publica el evento de suscripción GraphQL de eliminación.</summary>
    void EventoSuscripcionProductoEliminado(long productoId);

    /// <summary>Publica el evento de suscripción GraphQL cuando el stock baja del umbral.</summary>
    void EventoSuscripcionStockBajo(ProductoDto producto, int umbralStock);
}

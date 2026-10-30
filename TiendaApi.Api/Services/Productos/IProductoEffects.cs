using TiendaApi.Api.Dtos.Productos;
using TiendaApi.Api.Models;

namespace TiendaApi.Api.Services.Productos;

/// <summary>
/// 🎓 Contrato de los efectos secundarios de PRODUCTOS (WebSocket, SignalR,
/// email y suscripciones GraphQL). La abstracción sobre la que depende
/// <c>ProductoService</c> (DIP): el servicio de negocio no conoce los canales,
/// solo este contrato; la implementación y los mocks de test se sustituyen por
/// DI sin tocar la lógica de negocio.
/// </summary>
public interface IProductoEffects
{
    /// <summary>Notifica vía WebSocket la creación de un producto.</summary>
    void NotificarWebSocketProductoCreado(ProductoDto producto);

    /// <summary>Notifica vía WebSocket la actualización de un producto.</summary>
    void NotificarWebSocketProductoActualizado(ProductoDto producto);

    /// <summary>Notifica vía WebSocket la eliminación de un producto.</summary>
    void NotificarWebSocketProductoEliminado(long productoId);

    /// <summary>Notifica vía SignalR la creación de un producto.</summary>
    void NotificarSignalRProductoCreado(ProductoDto producto);

    /// <summary>Notifica vía SignalR la actualización de un producto.</summary>
    void NotificarSignalRProductoActualizado(ProductoDto producto);

    /// <summary>Notifica vía SignalR la eliminación de un producto.</summary>
    void NotificarSignalRProductoEliminado(long productoId);

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

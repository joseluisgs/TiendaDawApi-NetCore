using HotChocolate;
using HotChocolate.Authorization;
using HotChocolate.Types;
using TiendaApi.Apis.GraphQL.Events;

namespace TiendaApi.Apis.GraphQL.Subscriptions;

/// <summary>
/// Suscripciones de GraphQL para eventos de productos en tiempo real.
/// </summary>
/// <remarks>
/// Los clientes pueden suscribirse a eventos de productos.
/// Todas las suscripciones requieren autenticación JWT.
/// <para><b>Uso:</b></para>
/// <code>
/// subscription {
///   onProductoCreado { productoId nombre precio }
///   onProductoActualizado { productoId stock }
///   onProductoEliminado { productoId }
///   onStockBajo { productoId stockActual }
/// }
/// </code>
/// </remarks>
public class ProductoSubscription
{
    /// <summary>
    /// Se dispara cuando se crea un nuevo producto.
    /// Requiere autenticación JWT.
    /// </summary>
    /// <param name="message">El evento publicado</param>
    /// <returns>Los datos del producto creado</returns>
    [Authorize]
    [Subscribe]
    [Topic]
    public ProductoCreadoEvent OnProductoCreado(
        [EventMessage] ProductoCreadoEvent message)
    {
        return message;
    }

    /// <summary>
    /// Se dispara cuando se actualiza un producto.
    /// Requiere autenticación JWT.
    /// </summary>
    /// <param name="message">El evento publicado</param>
    /// <returns>Los datos del producto actualizado</returns>
    [Authorize]
    [Subscribe]
    [Topic]
    public ProductoActualizadoEvent OnProductoActualizado(
        [EventMessage] ProductoActualizadoEvent message)
    {
        return message;
    }

    /// <summary>
    /// Se dispara cuando se elimina un producto.
    /// Requiere autenticación JWT.
    /// </summary>
    /// <param name="message">El evento publicado</param>
    /// <returns>El ID del producto eliminado</returns>
    [Authorize]
    [Subscribe]
    [Topic]
    public ProductoEliminadoEvent OnProductoEliminado(
        [EventMessage] ProductoEliminadoEvent message)
    {
        return message;
    }

    /// <summary>
    /// Se dispara cuando el stock de un producto está bajo.
    /// Útil para dashboards de inventario en tiempo real.
    /// Requiere autenticación JWT.
    /// </summary>
    /// <param name="message">El evento publicado</param>
    /// <returns>El ID y stock del producto</returns>
    [Authorize]
    [Subscribe]
    [Topic]
    public ProductoStockBajoEvent OnStockBajo(
        [EventMessage] ProductoStockBajoEvent message)
    {
        return message;
    }
}

using HotChocolate;
using HotChocolate.Types;
using TiendaApi.Api.GraphQL.Events;

namespace TiendaApi.Api.GraphQL.Subscriptions;

/// <summary>
/// Suscripciones GraphQL para eventos de productos en tiempo real.
/// </summary>
public class ProductoSubscription
{
    /// <summary>Evento cuando se crea un producto.</summary>
    /// <param name="message">Datos del evento.</param>
    /// <returns>Evento publicado.</returns>
    [Subscribe]
    [Topic("onProductoCreado")]
    public ProductoCreadoEvent OnProductoCreado([EventMessage] ProductoCreadoEvent message) => message;

    /// <summary>Evento cuando se actualiza un producto.</summary>
    /// <param name="message">Datos del evento.</param>
    /// <returns>Evento publicado.</returns>
    [Subscribe]
    [Topic("onProductoActualizado")]
    public ProductoActualizadoEvent OnProductoActualizado([EventMessage] ProductoActualizadoEvent message) => message;

    /// <summary>Evento cuando se elimina un producto.</summary>
    /// <param name="message">Datos del evento.</param>
    /// <returns>Evento publicado.</returns>
    [Subscribe]
    [Topic("onProductoEliminado")]
    public ProductoEliminadoEvent OnProductoEliminado([EventMessage] ProductoEliminadoEvent message) => message;

    /// <summary>Evento cuando el stock está bajo.</summary>
    /// <param name="message">Datos del evento.</param>
    /// <returns>Evento publicado.</returns>
    [Subscribe]
    [Topic("onStockBajo")]
    public ProductoStockBajoEvent OnStockBajo([EventMessage] ProductoStockBajoEvent message) => message;
}
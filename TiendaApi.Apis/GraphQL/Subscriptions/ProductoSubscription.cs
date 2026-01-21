using HotChocolate;
using HotChocolate.Authorization;
using HotChocolate.Types;
using TiendaApi.Apis.GraphQL.Events;

namespace TiendaApi.Apis.GraphQL.Subscriptions;

/// <summary>
/// Suscripciones GraphQL para eventos de productos en tiempo real.
/// </summary>
public class ProductoSubscription
{
    /// <summary>Evento cuando se crea un producto.</summary>
    /// <param name="message">Datos del evento.</param>
    /// <returns>Evento publicado.</returns>
    [Authorize]
    [Subscribe]
    [Topic]
    public ProductoCreadoEvent OnProductoCreado([EventMessage] ProductoCreadoEvent message) => message;

    /// <summary>Evento cuando se actualiza un producto.</summary>
    /// <param name="message">Datos del evento.</param>
    /// <returns>Evento publicado.</returns>
    [Authorize]
    [Subscribe]
    [Topic]
    public ProductoActualizadoEvent OnProductoActualizado([EventMessage] ProductoActualizadoEvent message) => message;

    /// <summary>Evento cuando se elimina un producto.</summary>
    /// <param name="message">Datos del evento.</param>
    /// <returns>Evento publicado.</returns>
    [Authorize]
    [Subscribe]
    [Topic]
    public ProductoEliminadoEvent OnProductoEliminado([EventMessage] ProductoEliminadoEvent message) => message;

    /// <summary>Evento cuando el stock está bajo.</summary>
    /// <param name="message">Datos del evento.</param>
    /// <returns>Evento publicado.</returns>
    [Authorize]
    [Subscribe]
    [Topic]
    public ProductoStockBajoEvent OnStockBajo([EventMessage] ProductoStockBajoEvent message) => message;
}

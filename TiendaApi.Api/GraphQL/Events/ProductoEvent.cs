namespace TiendaApi.Api.GraphQL.Events;

/// <summary>
/// Evento publicado cuando se crea un nuevo producto.
/// </summary>
public record ProductoCreadoEvent
{
    public long ProductoId { get; init; }
    public string Nombre { get; init; } = string.Empty;
    public decimal Precio { get; init; }
    public int Stock { get; init; }
    public DateTime CreatedAt { get; init; }
}

/// <summary>
/// Evento publicado cuando se actualiza un producto.
/// </summary>
public record ProductoActualizadoEvent
{
    public long ProductoId { get; init; }
    public string? Nombre { get; init; }
    public decimal? Precio { get; init; }
    public int? Stock { get; init; }
    public DateTime UpdatedAt { get; init; }
}

/// <summary>
/// Evento publicado cuando se elimina un producto.
/// </summary>
public record ProductoEliminadoEvent
{
    public long ProductoId { get; init; }
    public DateTime DeletedAt { get; init; }
}

/// <summary>
/// Evento publicado cuando el stock de un producto está bajo.
/// </summary>
public record ProductoStockBajoEvent
{
    public long ProductoId { get; init; }
    public string Nombre { get; init; } = string.Empty;
    public int StockActual { get; init; }
    public int UmbralStock { get; init; }
    public DateTime DetectedAt { get; init; }
}

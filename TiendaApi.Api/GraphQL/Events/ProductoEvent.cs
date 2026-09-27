namespace TiendaApi.Api.GraphQL.Events;

/// <summary>
/// Evento publicado cuando se crea un nuevo producto.
/// </summary>
public record ProductoCreadoEvent
{
    /// <summary>Identificador del producto creado.</summary>
    public long ProductoId { get; init; }

    /// <summary>Nombre del producto creado.</summary>
    public string Nombre { get; init; } = string.Empty;

    /// <summary>Precio del producto creado.</summary>
    public decimal Precio { get; init; }

    /// <summary>Stock inicial del producto creado.</summary>
    public int Stock { get; init; }

    /// <summary>Fecha y hora de creación del producto (UTC).</summary>
    public DateTime CreatedAt { get; init; }
}

/// <summary>
/// Evento publicado cuando se actualiza un producto.
/// </summary>
public record ProductoActualizadoEvent
{
    /// <summary>Identificador del producto actualizado.</summary>
    public long ProductoId { get; init; }

    /// <summary>Nuevo nombre del producto, si cambió.</summary>
    public string? Nombre { get; init; }

    /// <summary>Nuevo precio del producto, si cambió.</summary>
    public decimal? Precio { get; init; }

    /// <summary>Nuevo stock del producto, si cambió.</summary>
    public int? Stock { get; init; }

    /// <summary>Fecha y hora de la actualización (UTC).</summary>
    public DateTime UpdatedAt { get; init; }
}

/// <summary>
/// Evento publicado cuando se elimina un producto.
/// </summary>
public record ProductoEliminadoEvent
{
    /// <summary>Identificador del producto eliminado.</summary>
    public long ProductoId { get; init; }

    /// <summary>Fecha y hora de la eliminación (UTC).</summary>
    public DateTime DeletedAt { get; init; }
}

/// <summary>
/// Evento publicado cuando el stock de un producto está bajo.
/// </summary>
public record ProductoStockBajoEvent
{
    /// <summary>Identificador del producto con stock bajo.</summary>
    public long ProductoId { get; init; }

    /// <summary>Nombre del producto afectado.</summary>
    public string Nombre { get; init; } = string.Empty;

    /// <summary>Stock disponible en el momento de detectar la situación.</summary>
    public int StockActual { get; init; }

    /// <summary>Umbral configurado que ha sido superado a la baja.</summary>
    public int UmbralStock { get; init; }

    /// <summary>Fecha y hora de detección (UTC).</summary>
    public DateTime DetectedAt { get; init; }
}

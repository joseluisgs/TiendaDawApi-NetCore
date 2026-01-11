namespace TiendaApi.Dtos.Productos;

/// <summary>
/// DTO de producto para respuestas de API.
/// </summary>
public record ProductoDto
{
    /// <summary>
    /// Identificador único del producto.
    /// </summary>
    public long Id { get; init; }

    /// <summary>
    /// Nombre del producto.
    /// </summary>
    public string Nombre { get; init; } = string.Empty;

    /// <summary>
    /// Descripción detallada del producto.
    /// </summary>
    public string Descripcion { get; init; } = string.Empty;

    /// <summary>
    /// Precio del producto.
    /// </summary>
    public decimal Precio { get; init; }

    /// <summary>
    /// Cantidad en stock disponible.
    /// </summary>
    public int Stock { get; init; }

    /// <summary>
    /// URL de la imagen del producto.
    /// </summary>
    public string? Imagen { get; init; }

    /// <summary>
    /// Identificador de la categoría del producto.
    /// </summary>
    public long CategoriaId { get; init; }

    /// <summary>
    /// Nombre de la categoría del producto.
    /// </summary>
    public string CategoriaNombre { get; init; } = string.Empty;

    /// <summary>
    /// Fecha de creación del registro.
    /// </summary>
    public DateTime CreatedAt { get; init; }

    /// <summary>
    /// Fecha de última actualización del registro.
    /// </summary>
    public DateTime UpdatedAt { get; init; }
}

/// <summary>
/// DTO de producto para solicitudes de creación o actualización.
/// </summary>
public record ProductoRequestDto
{
    /// <summary>
    /// Nombre del producto.
    /// </summary>
    public string Nombre { get; init; } = string.Empty;

    /// <summary>
    /// Descripción detallada del producto.
    /// </summary>
    public string Descripcion { get; init; } = string.Empty;

    /// <summary>
    /// Precio del producto.
    /// </summary>
    public decimal Precio { get; init; }

    /// <summary>
    /// Cantidad en stock disponible.
    /// </summary>
    public int Stock { get; init; }

    /// <summary>
    /// URL de la imagen del producto.
    /// </summary>
    public string? Imagen { get; init; }

    /// <summary>
    /// Identificador de la categoría del producto.
    /// </summary>
    public long CategoriaId { get; init; }
}

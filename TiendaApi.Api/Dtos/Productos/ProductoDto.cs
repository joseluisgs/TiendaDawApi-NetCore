namespace TiendaApi.Api.Dtos.Productos;

/// <summary>
/// Objeto de transferencia de datos para la visualización de un producto.
/// </summary>
public record ProductoDto
{
    /// <summary>Identificador único del producto.</summary>
    public long Id { get; init; }

    /// <summary>Nombre comercial del producto.</summary>
    public string Nombre { get; init; } = string.Empty;

    /// <summary>Descripción detallada de las características.</summary>
    public string Descripcion { get; init; } = string.Empty;

    /// <summary>Precio unitario de venta.</summary>
    public decimal Precio { get; init; }

    /// <summary>Cantidad de unidades disponibles en almacén.</summary>
    public int Stock { get; init; }

    /// <summary>Ruta o URL de la imagen representativa.</summary>
    public string? Imagen { get; init; }

    /// <summary>Identificador de la categoría a la que pertenece.</summary>
    public long CategoriaId { get; init; }

    /// <summary>Nombre legible de la categoría asociada.</summary>
    public string CategoriaNombre { get; init; } = string.Empty;

    /// <summary>Fecha y hora de registro en el sistema.</summary>
    public DateTime CreatedAt { get; init; }

    /// <summary>Fecha y hora de la última modificación.</summary>
    public DateTime UpdatedAt { get; init; }
}

/// <summary>
/// Objeto de transferencia para la creación o actualización de un producto.
/// </summary>
public record ProductoRequestDto
{
    /// <summary>Nombre del producto.</summary>
    public string Nombre { get; init; } = string.Empty;

    /// <summary>Descripción del producto.</summary>
    public string Descripcion { get; init; } = string.Empty;

    /// <summary>Precio de venta.</summary>
    public decimal Precio { get; init; }

    /// <summary>Unidades iniciales o actualizadas.</summary>
    public int Stock { get; init; }

    /// <summary>Ruta de la imagen (opcional).</summary>
    public string? Imagen { get; init; }

    /// <summary>ID de la categoría obligatoria.</summary>
    public long CategoriaId { get; init; }
}
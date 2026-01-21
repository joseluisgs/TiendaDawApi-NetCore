namespace TiendaApi.Api.GraphQL.Inputs;

/// <summary>
    /// Datos de entrada para crear un producto.
    /// </summary>
    public record CreateProductoInput
{
    /// <summary>
    /// Nombre del producto. Obligatorio y único.
    /// </summary>
    /// <example>Laptop Dell XPS 15</example>
    public string Nombre { get; init; } = string.Empty;

    /// <summary>
    /// Descripción detallada del producto. Opcional.
    /// </summary>
    /// <example>Portátil de alta gama con procesador Intel Core i7</example>
    public string? Descripcion { get; init; }

    /// <summary>
    /// Precio del producto. Debe ser mayor a 0.
    /// </summary>
    /// <example>1299.99</example>
    public decimal Precio { get; init; }

    /// <summary>
    /// Cantidad en stock. No puede ser negativo.
    /// </summary>
    /// <example>10</example>
    public int Stock { get; init; }

    /// <summary>
    /// URL de la imagen. Opcional, debe ser URL válida.
    /// </summary>
    /// <example>https://ejemplo.com/imagen.jpg</example>
    public string? Imagen { get; init; }

    /// <summary>
    /// ID de la categoría. Obligatorio y debe existir.
    /// </summary>
    /// <example>1</example>
    public long CategoriaId { get; init; }
}

/// <summary>
    /// Datos de entrada para actualizar un producto.
    /// </summary>
    public record UpdateProductoInput
{
    /// <summary>
    /// Nuevo nombre del producto (opcional).
    /// Si es null, no se modifica el nombre actual.
    /// </summary>
    /// <example>Laptop Dell XPS 15 Actualizado</example>
    public string? Nombre { get; init; }

    /// <summary>
    /// Nueva descripción (opcional).
    /// Si es null, no se modifica la descripción actual.
    /// </summary>
    /// <example>Nueva descripción del producto</example>
    public string? Descripcion { get; init; }

    /// <summary>
    /// Nuevo precio (opcional).
    /// Si es null, no se modifica el precio actual.
    /// Debe ser mayor a 0 si se proporciona.
    /// </summary>
    /// <example>1199.99</example>
    public decimal? Precio { get; init; }

    /// <summary>
    /// Nuevo stock (opcional).
    /// Si es null, no se modifica el stock actual.
    /// No puede ser negativo.
    /// </summary>
    /// <example>15</example>
    public int? Stock { get; init; }

    /// <summary>
    /// Nueva URL de imagen (opcional).
    /// Si es null, no se modifica la imagen actual.
    /// </summary>
    /// <example>https://ejemplo.com/nueva-imagen.jpg</example>
    public string? Imagen { get; init; }

    /// <summary>
    /// Nuevo ID de categoría (opcional).
    /// Si es null, no se modifica la categoría actual.
    /// </summary>
    /// <example>2</example>
    public long? CategoriaId { get; init; }
}

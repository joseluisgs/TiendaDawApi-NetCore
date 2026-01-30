namespace ClientBlazor.Cliente.DTOs.GraphQL;

/// <summary>
/// DTO para respuestas GraphQL de productos.
/// </summary>
public record GraphQLProductoDto(
    long Id,
    string Nombre,
    string Descripcion,
    decimal Precio,
    int Stock,
    string? Imagen,
    GraphQLCategoriaDto? Categoria,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

/// <summary>
/// DTO para respuestas GraphQL de categorías.
/// </summary>
public record GraphQLCategoriaDto(
    long Id,
    string Nombre,
    string Descripcion,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

/// <summary>
/// DTO para input de crear producto en GraphQL.
/// </summary>
public record CreateProductoInput(
    string Nombre,
    string Descripcion,
    decimal Precio,
    int Stock,
    long CategoriaId,
    string? Imagen = null
);

/// <summary>
/// DTO para input de actualizar producto en GraphQL.
/// </summary>
public record UpdateProductoInput(
    string? Nombre = null,
    string? Descripcion = null,
    decimal? Precio = null,
    int? Stock = null,
    long? CategoriaId = null,
    string? Imagen = null
);

/// <summary>
/// Resultado de operaciones GraphQL.
/// </summary>
public record GraphQLResult<T>(
    T? Data,
    List<GraphQLError>? Errors
);

/// <summary>
/// Error de GraphQL.
/// </summary>
public record GraphQLError(
    string Message,
    string? Code = null,
    string? Path = null
);

/// <summary>
/// Evento de subscription para producto creado.
/// </summary>
public record ProductoCreadoEvent(
    GraphQLProductoDto Producto,
    DateTime Timestamp
);

/// <summary>
/// Evento de subscription para producto actualizado.
/// </summary>
public record ProductoActualizadoEvent(
    GraphQLProductoDto Producto,
    DateTime Timestamp
);

/// <summary>
/// Evento de subscription para producto eliminado.
/// </summary>
public record ProductoEliminadoEvent(
    long ProductoId,
    DateTime Timestamp
);

/// <summary>
/// Evento de subscription para stock bajo.
/// </summary>
public record StockBajoEvent(
    long ProductoId,
    string NombreProducto,
    int StockActual,
    DateTime Timestamp
);
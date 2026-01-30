namespace TiendaApi.Api.Dtos.Productos;

/// <summary>
/// DTO de filtros para búsqueda y paginación de productos.
/// Proporciona criterios flexibles para filtrar productos por múltiples condiciones.
///
/// <remarks>
/// Uso típico:
/// - GET /api/productos?nombre=Laptop&amp;precioMax=1500&amp;stockMin=5
/// - Catálogos con filtros dinámicos
/// - Búsquedas avanzadas en tienda
/// </remarks>
/// </summary>
public record ProductoFilterDto
(
    /// <summary>
    /// Filtrar productos por nombre (búsqueda parcial, sensible a mayúsculas/minúsculas).
    /// Busca coincidencias en el nombre del producto.
    /// </summary>
    /// <example>Laptop</example>
    string? Nombre,

    /// <summary>
    /// Filtrar productos por nombre de categoría.
    /// Permite ver productos de una categoría específica por su nombre.
    /// </summary>
    /// <example>Electrónica</example>
    string? Categoria,

    /// <summary>
    /// Filtrar por estado de eliminación lógica.
    /// </summary>
    /// <example>false</example>
    bool? IsDeleted,

    /// <summary>
    /// Filtrar por precio máximo (productos menores o iguales al valor).
    /// Útil para filtros de rango de precio.
    /// </summary>
    /// <example>1000.00</example>
    decimal? PrecioMax,

    /// <summary>
    /// Filtrar por stock mínimo (productos con stock igual o superior).
    /// Útil para mostrar solo productos disponibles.
    /// </summary>
    /// <example>10</example>
    int? StockMin,

    /// <summary>
    /// Número de página (base 0).
    /// </summary>
    /// <default>0</default>
    /// <example>0</example>
    int Page = 0,

    /// <summary>
    /// Cantidad de elementos por página.
    /// </summary>
    /// <default>10</default>
    /// <example>10</example>
    int Size = 10,

    /// <summary>
    /// Campo para ordenar resultados.
    /// </summary>
    /// <default>id</default>
    /// <example>precio</example>
    string SortBy = "id",

    /// <summary>
    /// Dirección de ordenamiento (asc/desc).
    /// </summary>
    /// <default>asc</default>
    /// <example>desc</example>
    string Direction = "asc"
);

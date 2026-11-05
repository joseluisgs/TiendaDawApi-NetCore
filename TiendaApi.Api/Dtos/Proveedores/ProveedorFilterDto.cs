namespace TiendaApi.Api.Dtos.Proveedores;

/// <summary>
/// DTO de filtros para paginación y búsqueda de proveedores.
/// Permite filtrar por nombre y estado de eliminación, con paginación y ordenamiento.
/// </summary>
/// <remarks>
/// Uso típico: GET /api/v1/proveedores?nombre=DAW&amp;page=0&amp;size=10&amp;sortBy=nombre&amp;direction=asc
/// </remarks>
public record ProveedorFilterDto
{
    /// <summary>Filtrar por nombre (búsqueda parcial, sensible a mayúsculas/minúsculas).</summary>
    /// <example>Distribuciones</example>
    public string? Nombre { get; init; }

    /// <summary>
    /// Filtrar por borrado lógico: null todas, false solo activos, true solo eliminados.
    /// </summary>
    /// <example>false</example>
    public bool? IsDeleted { get; init; }

    /// <summary>Número de página (base 0).</summary>
    /// <value>0 por defecto</value>
    /// <example>0</example>
    [System.ComponentModel.DataAnnotations.Range(0, int.MaxValue, ErrorMessage = "La página no puede ser negativa")]
    public int Page { get; init; } = 0;

    /// <summary>Elementos por página, entre 1 y 100.</summary>
    /// <value>10 por defecto</value>
    /// <example>10</example>
    [System.ComponentModel.DataAnnotations.Range(1, 100, ErrorMessage = "El tamaño de página debe estar entre 1 y 100")]
    public int Size { get; init; } = 10;

    /// <summary>Campo de ordenación: codigo, nombre o createdAt.</summary>
    /// <value>"codigo" por defecto</value>
    /// <example>nombre</example>
    public string SortBy { get; init; } = "codigo";

    /// <summary>Dirección de ordenación: asc o desc.</summary>
    /// <value>"asc" por defecto</value>
    /// <example>asc</example>
    public string Direction { get; init; } = "asc";
}

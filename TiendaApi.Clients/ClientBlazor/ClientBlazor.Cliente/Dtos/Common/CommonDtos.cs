namespace ClientBlazor.Cliente.DTOs.Common;

/// <summary>
/// Representa el envoltorio genérico para cualquier respuesta de la API que incluya paginación.
/// </summary>
/// <typeparam name="T">Tipo de los elementos contenidos en la página.</typeparam>
public record PagedResult<T>
{
    /// <summary>Colección de elementos de la página actual.</summary>
    public IEnumerable<T> Items { get; init; } = Enumerable.Empty<T>();

    /// <summary>Número total de elementos que existen en el servidor para la consulta realizada.</summary>
    public int TotalCount { get; init; }

    /// <summary>Índice de la página actual (0-indexed).</summary>
    public int Page { get; init; }

    /// <summary>Cantidad de elementos por página solicitada.</summary>
    public int PageSize { get; init; }

    /// <summary>Calcula el número total de páginas disponibles basadas en <see cref="TotalCount"/> y <see cref="PageSize"/>.</summary>
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;

    /// <summary>Indica si existe una página posterior a la actual.</summary>
    public bool HasNextPage => Page < TotalPages - 1;

    /// <summary>Indica si existe una página anterior a la actual.</summary>
    public bool HasPreviousPage => Page > 0;
}

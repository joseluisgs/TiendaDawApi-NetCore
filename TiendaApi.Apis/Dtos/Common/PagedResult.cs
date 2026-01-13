namespace TiendaApi.Apis.Dtos.Common;

/// <summary>
/// DTO para respuestas paginadas.
/// </summary>
public record PagedResult<T>
{
    /// <summary>
    /// Elementos de la página actual.
    /// </summary>
    public IEnumerable<T> Items { get; init; } = Enumerable.Empty<T>();

    /// <summary>
    /// Número total de elementos.
    /// </summary>
    public int TotalCount { get; init; }

    /// <summary>
    /// Número de página actual (basado en 1).
    /// </summary>
    public int Page { get; init; }

    /// <summary>
    /// Tamaño de página.
    /// </summary>
    public int PageSize { get; init; }

    /// <summary>
    /// Número total de páginas.
    /// </summary>
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;

    /// <summary>
    /// Indica si hay una página siguiente.
    /// </summary>
    public bool HasNextPage => Page < TotalPages;

    /// <summary>
    /// Indica si hay una página anterior.
    /// </summary>
    public bool HasPreviousPage => Page > 1;
}

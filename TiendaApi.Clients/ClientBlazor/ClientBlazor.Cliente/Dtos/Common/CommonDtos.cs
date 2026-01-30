namespace ClientBlazor.Cliente.DTOs.Common;

/// <summary>
/// DTO genérico para respuestas paginadas.
/// Copia exacta del PagedResult de la API.
/// </summary>
public record PagedResult<T>(
    IEnumerable<T> Items,
    int TotalCount,
    int Page,
    int PageSize
)
{
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
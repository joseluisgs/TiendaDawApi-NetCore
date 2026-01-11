namespace TiendaApi.Dtos.Categorias;

/// <summary>
/// DTO de categoría para respuestas de API.
/// Separa la entidad interna del contrato de API.
/// </summary>
public record CategoriaDto
{
    /// <summary>
    /// Identificador único de la categoría.
    /// </summary>
    public long Id { get; init; }

    /// <summary>
    /// Nombre de la categoría.
    /// </summary>
    public string Nombre { get; init; } = string.Empty;

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
/// DTO de categoría para solicitudes de creación o actualización.
/// </summary>
public record CategoriaRequestDto
{
    /// <summary>
    /// Nombre de la categoría.
    /// </summary>
    public string Nombre { get; init; } = string.Empty;
}

using System.ComponentModel.DataAnnotations;

namespace TiendaApi.Apis.Dtos.Categorias;

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
    [Required(ErrorMessage = "El nombre es obligatorio")]
    [MinLength(3, ErrorMessage = "El nombre debe tener al menos 3 caracteres")]
    [MaxLength(100, ErrorMessage = "El nombre no puede exceder 100 caracteres")]
    public string Nombre { get; init; } = string.Empty;
}

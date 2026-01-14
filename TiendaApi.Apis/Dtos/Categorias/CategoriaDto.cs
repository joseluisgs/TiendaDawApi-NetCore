using System.ComponentModel.DataAnnotations;

namespace TiendaApi.Apis.Dtos.Categorias;

/// <summary>
/// DTO de categoría para respuestas de API.
/// Separa la entidad interna del contrato de API.
/// </summary>
public record CategoriaDto(long Id, string Nombre, DateTime CreatedAt, DateTime UpdatedAt);

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

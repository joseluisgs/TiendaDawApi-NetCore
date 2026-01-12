using System.ComponentModel.DataAnnotations;

namespace TiendaApi.Apis.Dtos.Productos;

/// <summary>
/// DTO de producto para respuestas de API.
/// </summary>
public record ProductoDto
{
    /// <summary>
    /// Identificador único del producto.
    /// </summary>
    public long Id { get; init; }

    /// <summary>
    /// Nombre del producto.
    /// </summary>
    public string Nombre { get; init; } = string.Empty;

    /// <summary>
    /// Descripción detallada del producto.
    /// </summary>
    public string Descripcion { get; init; } = string.Empty;

    /// <summary>
    /// Precio del producto.
    /// </summary>
    public decimal Precio { get; init; }

    /// <summary>
    /// Cantidad en stock disponible.
    /// </summary>
    public int Stock { get; init; }

    /// <summary>
    /// URL de la imagen del producto.
    /// </summary>
    public string? Imagen { get; init; }

    /// <summary>
    /// Identificador de la categoría del producto.
    /// </summary>
    public long CategoriaId { get; init; }

    /// <summary>
    /// Nombre de la categoría del producto.
    /// </summary>
    public string CategoriaNombre { get; init; } = string.Empty;

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
/// DTO de producto para solicitudes de creación o actualización.
/// </summary>
public record ProductoRequestDto
{
    /// <summary>
    /// Nombre del producto.
    /// </summary>
    [Required(ErrorMessage = "El nombre es obligatorio")]
    [MinLength(3, ErrorMessage = "El nombre debe tener al menos 3 caracteres")]
    [MaxLength(200, ErrorMessage = "El nombre no puede exceder 200 caracteres")]
    public string Nombre { get; init; } = string.Empty;

    /// <summary>
    /// Descripción detallada del producto.
    /// </summary>
    [MaxLength(1000, ErrorMessage = "La descripción no puede exceder 1000 caracteres")]
    public string Descripcion { get; init; } = string.Empty;

    /// <summary>
    /// Precio del producto.
    /// </summary>
    [Required(ErrorMessage = "El precio es obligatorio")]
    [Range(0.01, double.MaxValue, ErrorMessage = "El precio debe ser mayor a 0")]
    public decimal Precio { get; init; }

    /// <summary>
    /// Cantidad en stock disponible.
    /// </summary>
    [Required(ErrorMessage = "El stock es obligatorio")]
    [Range(0, int.MaxValue, ErrorMessage = "El stock no puede ser negativo")]
    public int Stock { get; init; }

    /// <summary>
    /// URL de la imagen del producto.
    /// </summary>
    [MaxLength(500, ErrorMessage = "La URL de la imagen no puede exceder 500 caracteres")]
    [Url(ErrorMessage = "Debe ser una URL válida")]
    public string? Imagen { get; init; }

    /// <summary>
    /// Identificador de la categoría del producto.
    /// </summary>
    [Required(ErrorMessage = "La categoría es obligatoria")]
    [Range(1, long.MaxValue, ErrorMessage = "Debe seleccionar una categoría válida")]
    public long CategoriaId { get; init; }
}

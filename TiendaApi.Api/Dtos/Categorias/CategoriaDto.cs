using System.ComponentModel.DataAnnotations;

namespace TiendaApi.Api.Dtos.Categorias;

/// <summary>
/// DTO (Data Transfer Object) de categoría para respuestas de API.
/// Este objeto se utiliza para transferir datos de categorías desde el servidor hacia el cliente.
/// Separa la entidad interna de dominio del contrato de API, permitiendo independencia de la estructura de base de datos.
///
/// Esta clase es inmutable (record) para garantizar la integridad de los datos en tránsito.
/// Se genera automáticamente a partir de la entidad de dominio mediante mapeo en la capa de aplicación.
///
/// <remarks>
/// Uso típico:
/// - Listar categorías en catálogos
/// - Mostrar detalles de una categoría específica
/// - Construir menús de navegación
/// - Relacionar productos con sus categorías
/// </remarks>
/// </summary>
/// <example>
/// Respuesta JSON típica:
/// <code>
/// {
///   "id": 1,
///   "nombre": "Electrónica",
///   "createdAt": "2024-01-15T10:30:00Z",
///   "updatedAt": "2024-01-15T10:30:00Z"
/// }
/// </code>
/// </example>
public record CategoriaDto(
    /// <summary>
    /// Identificador único de la categoría.
    /// Generado automáticamente por la base de datos al crear la entidad.
    /// Valor único e incremental, nunca se reutiliza después de eliminar una categoría.
    /// </summary>
    /// <example>1</example>
    long Id,

    /// <summary>
    /// Nombre descriptivo de la categoría.
    /// Utilizado para identificar la categoría en interfaces de usuario.
    /// Debe ser único en el sistema para evitar ambigüedades.
    /// </summary>
    /// <example>Electrónica</example>
    string Nombre,

    /// <summary>
    /// Descripción de la categoría.
    /// </summary>
    string? Descripcion,

    /// <summary>
    /// Fecha y hora de creación del registro en formato UTC.
    /// Asignada automáticamente por el sistema al crear el registro.
    /// </summary>
    /// <example>2024-01-15T10:30:00Z</example>
    DateTime CreatedAt,

    /// <summary>
    /// Fecha y hora de última modificación del registro en formato UTC.
    /// Se actualiza automáticamente cada vez que se modifica el registro.
    /// </summary>
    /// <example>2024-01-15T12:00:00Z</example>
    DateTime UpdatedAt
);

/// <summary>
/// DTO de categoría para solicitudes de creación o actualización.
/// Objeto de transferencia utilizado para recibir datos del cliente hacia el servidor.
/// Designado para operaciones POST (creación) y PUT/PATCH (actualización).
///
/// <remarks>
/// Diferencias con CategoriaDto:
/// - No incluye campos de solo lectura (Id, CreatedAt, UpdatedAt)
/// - Enfocado únicamente en datos modificables por el usuario
/// - Validaciones específicas para garantizar integridad de datos
///
/// Validaciones aplicadas:
/// - Nombre: Obligatorio, entre 3 y 100 caracteres
/// </remarks>
/// </summary>
public record CategoriaRequestDto
{
    /// <summary>
    /// Nombre de la categoría.
    /// Representa el nombre público que verán los usuarios.
    /// Debe ser descriptivo y conciso para facilitar la navegación.
    /// </summary>
    /// <remarks>
    /// Restricciones de negocio:
    /// - No puede contener solo espacios en blanco
    /// - Se recomienda evitar caracteres especiales
    /// - El sistema puede normalizar espacios extras
    /// </remarks>
    /// <example>Electrónica</example>
    [Required(ErrorMessage = "El nombre es obligatorio")]
    [MinLength(3, ErrorMessage = "El nombre debe tener al menos 3 caracteres")]
    [MaxLength(100, ErrorMessage = "El nombre no puede exceder 100 caracteres")]
    public string Nombre { get; init; } = string.Empty;

    /// <summary>
    /// Descripción de la categoría.
    /// </summary>
    [MaxLength(500, ErrorMessage = "La descripción no puede exceder 500 caracteres")]
    public string? Descripcion { get; init; }
}

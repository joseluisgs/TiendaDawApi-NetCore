using System.ComponentModel.DataAnnotations;
using TiendaApi.Api.IdGenerators;

namespace TiendaApi.Api.Dtos.Proveedores;

/// <summary>
/// DTO de proveedor para respuestas de API.
/// Inmutable (record) y separado de la entidad de dominio.
///
/// Su clave es un identificador estilo YouTube: 11 caracteres Base64url no correlativos,
/// generados por el servidor en la creación y legibles en URL.
/// </summary>
/// <example>
/// <code>
/// {
///   "codigo": "XqZsoesa55w",
///   "nombre": "Distribuciones DAW",
///   "descripcion": "Material para los laboratorios",
///   "contacto": "pedidos@distribucionesdaw.example",
///   "createdAt": "2026-11-01T10:30:00Z",
///   "updatedAt": "2026-11-01T10:30:00Z"
/// }
/// </code>
/// </example>
public record ProveedorDto(
    // <summary>
    // Identificador único de 11 caracteres estilo YouTube.
    // Generado por el servidor al crear el proveedor; no es correlativo ni enumerable.
    // </summary>
    // <example>XqZsoesa55w</example>
    string Codigo,

    // <summary>Nombre comercial del proveedor, único en el sistema.</summary>
    // <example>Distribuciones DAW</example>
    string Nombre,

    // <summary>Descripción opcional del proveedor.</summary>
    string? Descripcion,

    // <summary>Dato de contacto opcional: email, teléfono o persona de referencia.</summary>
    string? Contacto,

    // <summary>Fecha y hora de creación en UTC.</summary>
    // <example>2026-11-01T10:30:00Z</example>
    DateTime CreatedAt,

    // <summary>Fecha y hora de última modificación en UTC.</summary>
    // <example>2026-11-01T12:00:00Z</example>
    DateTime UpdatedAt
);

/// <summary>
/// DTO de proveedor para solicitudes de creación (POST) o actualización (PUT).
///
/// Validaciones: Nombre obligatorio de 3 a 100 caracteres; Descripcion hasta 500;
/// Contacto hasta 200; Codigo opcional pero, si viene, debe ser un identificador de
/// 11 caracteres Base64url (su uso principal es el import de backups: sin código el
/// servidor genera uno nuevo).
/// </summary>
public record ProveedorRequestDto
{
    /// <summary>
    /// Identificador de 11 caracteres. Opcional: si se omite, el servidor lo genera.
    /// Solo se envía explícito al importar un backup existente.
    /// El formato lo comprueba <see cref="IdGenerators.IdYouTubeAttribute"/>; que el código
    /// exista ya en base de datos lo comprueba el servicio (conflicto, no validación).
    /// </summary>
    /// <example>XqZsoesa55w</example>
    [IdYouTube]
    public string? Codigo { get; init; }

    /// <summary>Nombre comercial del proveedor. Obligatorio, de 3 a 100 caracteres.</summary>
    /// <example>Distribuciones DAW</example>
    [Required(ErrorMessage = "El nombre es obligatorio")]
    [MinLength(3, ErrorMessage = "El nombre debe tener al menos 3 caracteres")]
    [MaxLength(100, ErrorMessage = "El nombre no puede exceder 100 caracteres")]
    public string Nombre { get; init; } = string.Empty;

    /// <summary>Descripción opcional del proveedor, hasta 500 caracteres.</summary>
    [MaxLength(500, ErrorMessage = "La descripción no puede exceder 500 caracteres")]
    public string? Descripcion { get; init; }

    /// <summary>Dato de contacto opcional, hasta 200 caracteres.</summary>
    [MaxLength(200, ErrorMessage = "El contacto no puede exceder 200 caracteres")]
    public string? Contacto { get; init; }
}

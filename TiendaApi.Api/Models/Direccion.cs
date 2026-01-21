using System.ComponentModel.DataAnnotations;

namespace TiendaApi.Api.Models;

/// <summary>
/// Dirección postal estructurada para envíos.
/// Todos los campos son opcionales para flexibilidad internacional.
/// </summary>
public class Direccion
{
    /// <summary>Nombre de la calle, avenida, plaza (máx 200 caracteres).</summary>
    [MaxLength(200)]
    public string? Calle { get; set; }

    /// <summary>Número del edificio (máx 20 caracteres, ej: "42", "12A", "S/N").</summary>
    [MaxLength(20)]
    public string? Numero { get; set; }

    /// <summary>Ciudad o municipio (máx 100 caracteres).</summary>
    [MaxLength(100)]
    public string? Ciudad { get; set; }

    /// <summary>Provincia o región (máx 100 caracteres).</summary>
    [MaxLength(100)]
    public string? Provincia { get; set; }

    /// <summary>País (máx 100 caracteres).</summary>
    [MaxLength(100)]
    public string? Pais { get; set; }

    /// <summary>Código postal (5 dígitos, máx 20 para internacionales).</summary>
    [MaxLength(20)]
    [RegularExpression(@"^[0-9]{5}$", ErrorMessage = "El código postal debe tener exactamente 5 dígitos.")]
    public string? CodigoPostal { get; set; }
}

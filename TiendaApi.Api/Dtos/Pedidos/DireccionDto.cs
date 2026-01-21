using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace TiendaApi.Api.Dtos.Pedidos;

/// <summary>
    /// DTO de dirección postal para envíos.
    /// </summary>
    /// <example>
    /// {
    ///   "calle": "Gran Vía",
    ///   "numero": "42",
    ///   "ciudad": "Madrid",
    ///   "provincia": "Madrid",
    ///   "pais": "España",
    ///   "codigoPostal": "28013"
    /// }
    /// </example>
    public class DireccionDto
{
    /// <summary>
    /// Nombre de la calle, avenida, plaza o vía pública.
    /// </summary>
    /// <example>Gran Vía</example>
    [JsonPropertyName("calle")]
    [Required(ErrorMessage = "La calle es obligatoria.")]
    [MaxLength(200, ErrorMessage = "La calle no puede superar los 200 caracteres.")]
    public string Calle { get; set; } = string.Empty;

    /// <summary>
    /// Número del edificio o casa en la calle.
    /// </summary>
    /// <example>42</example>
    [JsonPropertyName("numero")]
    [MaxLength(20, ErrorMessage = "El número no puede superar los 20 caracteres.")]
    public string? Numero { get; set; }

    /// <summary>
    /// Nombre de la ciudad, pueblo o municipio.
    /// </summary>
    /// <example>Madrid</example>
    [JsonPropertyName("ciudad")]
    [Required(ErrorMessage = "La ciudad es obligatoria.")]
    [MaxLength(100, ErrorMessage = "La ciudad no puede superar los 100 caracteres.")]
    public string Ciudad { get; set; } = string.Empty;

    /// <summary>
    /// Nombre de la provincia o región administrativa.
    /// </summary>
    /// <example>Madrid</example>
    [JsonPropertyName("provincia")]
    [MaxLength(100, ErrorMessage = "La provincia no puede superar los 100 caracteres.")]
    public string? Provincia { get; set; }

    /// <summary>
    /// Nombre del país.
    /// </summary>
    /// <example>España</example>
    [JsonPropertyName("pais")]
    [Required(ErrorMessage = "El país es obligatorio.")]
    [MaxLength(100, ErrorMessage = "El país no puede superar los 100 caracteres.")]
    public string Pais { get; set; } = string.Empty;

    /// <summary>
    /// Código postal de 5 dígitos.
    /// </summary>
    /// <example>28013</example>
    [JsonPropertyName("codigoPostal")]
    [MaxLength(20, ErrorMessage = "El código postal no puede superar los 20 caracteres.")]
    [RegularExpression(@"^[0-9]{5}$", ErrorMessage = "El código postal debe tener exactamente 5 dígitos.")]
    public string? CodigoPostal { get; set; }
}

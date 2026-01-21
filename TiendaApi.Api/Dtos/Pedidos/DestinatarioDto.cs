using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace TiendaApi.Api.Dtos.Pedidos;

/// <summary>
    /// DTO de destinatario para pedidos.
    /// </summary>
    /// <example>
    /// {
    ///   "nombreCompleto": "María García López",
    ///   "email": "maria.garcia@email.com",
    ///   "telefono": "+34612345678",
    ///   "direccion": {
    ///     "calle": "Gran Vía",
    ///     "numero": "42",
    ///     "ciudad": "Madrid",
    ///     "provincia": "Madrid",
    ///     "pais": "España",
    ///     "codigoPostal": "28013"
    ///   }
    /// }
    /// </example>
    public class DestinatarioDto
{
    /// <summary>
    /// Nombre completo del destinatario.
    /// </summary>
    /// <example>María García López</example>
    [JsonPropertyName("nombreCompleto")]
    [Required(ErrorMessage = "El nombre completo del destinatario es obligatorio.")]
    [MaxLength(200, ErrorMessage = "El nombre completo no puede superar los 200 caracteres.")]
    public string NombreCompleto { get; set; } = string.Empty;

    /// <summary>
    /// Correo electrónico del destinatario.
    /// </summary>
    /// <example>maria.garcia@email.com</example>
    [JsonPropertyName("email")]
    [Required(ErrorMessage = "El email del destinatario es obligatorio.")]
    [MaxLength(254, ErrorMessage = "El email no puede superar los 254 caracteres.")]
    [EmailAddress(ErrorMessage = "El email del destinatario no es válido.")]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Número de teléfono del destinatario.
    /// </summary>
    /// <example>+34612345678</example>
    [JsonPropertyName("telefono")]
    [MaxLength(20, ErrorMessage = "El teléfono no puede superar los 20 caracteres.")]
    [RegularExpression(@"^\+?[0-9]{9,15}$", ErrorMessage = "El teléfono debe tener entre 9 y 15 dígitos.")]
    public string? Telefono { get; set; }

    /// <summary>
    /// Dirección de entrega del destinatario.
    /// </summary>
    [JsonPropertyName("direccion")]
    [Required(ErrorMessage = "La dirección de entrega es obligatoria.")]
    public DireccionDto Direccion { get; set; } = new();
}

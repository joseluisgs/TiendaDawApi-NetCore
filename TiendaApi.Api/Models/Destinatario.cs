using System.ComponentModel.DataAnnotations;

namespace TiendaApi.Api.Models;

/// <summary>
/// Información del destinatario de un pedido (nombre, email, teléfono, dirección).
/// Si es null, el destinatario es el mismo usuario que realizó el pedido.
/// </summary>
public class Destinatario
{
    /// <summary>Nombre completo del destinatario (máx 200 caracteres).</summary>
    [MaxLength(200)]
    public string? NombreCompleto { get; set; }

    /// <summary>Email del destinatario para notificaciones (máx 254 caracteres).</summary>
    [MaxLength(254)]
    [EmailAddress(ErrorMessage = "El email del destinatario no es válido.")]
    public string? Email { get; set; }

    /// <summary>Teléfono de contacto (máx 20 caracteres, formato internacional).</summary>
    [MaxLength(20)]
    [RegularExpression(@"^\+?[0-9]{9,15}$", ErrorMessage = "El teléfono debe tener entre 9 y 15 dígitos.")]
    public string? Telefono { get; set; }

    /// <summary>Dirección de entrega estructurada.</summary>
    public Direccion? Direccion { get; set; }
}

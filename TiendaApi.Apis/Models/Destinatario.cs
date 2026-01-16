using System.ComponentModel.DataAnnotations;

namespace TiendaApi.Apis.Models;

/// <summary>
/// Representa la información del destinatario de un pedido.
/// </summary>
/// <remarks>
/// <para>
/// Este modelo almacena los datos de la persona que recibirá el pedido,
/// permitiendo que el comprador/envíe el pedido a una dirección diferente
/// a la suya propia (como en Amazon).
/// </para>
/// <para>
/// <b>Casos de uso:</b>
/// <list type="bullet">
///   <item><description>Regalo: El comprador envía a otra persona.</description></item>
///   <item><description>Trabajo: Envío a la oficina en lugar de casa.</description></item>
///   <item><description>Viajes: Envío a un hotel o residencia temporal.</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Notas:</b> Si <c>Destinatario</c> es null, se asume que el destinatario
/// es el mismo usuario que realiza el pedido.
/// </para>
/// <example>
/// Crear un destinatario para envío a otra persona:
/// <code>
/// var destinatario = new Destinatario
/// {
///     NombreCompleto = "María García López",
///     Email = "maria.garcia@email.com",
///     Telefono = "+34612345678",
///     Direccion = new Direccion
///     {
///         Calle = "Gran Vía",
///         Numero = "42",
///         Ciudad = "Madrid",
///         Provincia = "Madrid",
///         Pais = "España",
///         CodigoPostal = "28013"
///     }
/// };
/// </code>
/// </example>
/// </remarks>
public class Destinatario
{
    /// <summary>
    /// Nombre completo del destinatario.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Nombre y apellidos de la persona que recibirá el paquete.
    /// </para>
    /// <para>
    /// Longitud máxima: 200 caracteres
    /// </para>
    /// </remarks>
    [MaxLength(200)]
    public string? NombreCompleto { get; set; }

    /// <summary>
    /// Correo electrónico del destinatario.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Email válido para recibir notificaciones sobre el envío.
    /// </para>
    /// <para>
    /// Longitud máxima: 254 caracteres
    /// </para>
    /// </remarks>
    [MaxLength(254)]
    [EmailAddress(ErrorMessage = "El email del destinatario no es válido.")]
    public string? Email { get; set; }

    /// <summary>
    /// Número de teléfono del destinatario.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Teléfono de contacto para el reparto. Preferiblemente móvil.
    /// </para>
    /// <para>
    /// Longitud máxima: 20 caracteres
    /// </para>
    /// </remarks>
    [MaxLength(20)]
    [RegularExpression(@"^\+?[0-9]{9,15}$", ErrorMessage = "El teléfono debe tener entre 9 y 15 dígitos.")]
    public string? Telefono { get; set; }

    /// <summary>
    /// Dirección de entrega del destinatario.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Objeto <see cref="Direccion"/> con la información estructurada
    /// de la dirección de entrega.
    /// </para>
    /// <para>
    /// Este campo es obligatorio si se proporciona un destinatario.
    /// </para>
    /// </remarks>
    public Direccion? Direccion { get; set; }
}

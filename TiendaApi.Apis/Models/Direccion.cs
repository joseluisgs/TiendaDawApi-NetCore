using System.ComponentModel.DataAnnotations;

namespace TiendaApi.Apis.Models;

/// <summary>
/// Representa una dirección postal completa para el envío de pedidos.
/// </summary>
/// <remarks>
/// <para>
/// Este modelo almacena la información estructurada de una dirección postal,
/// permitiendo validar y formatear correctamente los datos de envío.
/// </para>
/// <para>
/// <b>Características:</b>
/// <list type="bullet">
///   <item><description>Validación de longitud para todos los campos de texto.</description></item>
///   <item><description>Validación de formato para código postal (5 dígitos).</description></item>
///   <item><description>Todos los campos son opcionales para mayor flexibilidad.</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Ejemplo de uso:</b>
/// <code>
/// var direccion = new Direccion
/// {
///     Calle = "Gran Vía",
///     Numero = "42",
///     Ciudad = "Madrid",
///     Provincia = "Madrid",
///     Pais = "España",
///     CodigoPostal = "28013"
/// };
/// </code>
/// </para>
/// <para>
/// <b>Notas:</b> Los campos de dirección son opcionales para permitir
/// direcciones parciales o internacionales con formatos diferentes.
/// </para>
/// </remarks>
public class Direccion
{
    /// <summary>
    /// Nombre de la calle, avenida, plaza o vía pública.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Ejemplos: "Gran Vía", "Calle Mayor", "Avenida de la Constitución"
    /// </para>
    /// <para>
    /// Longitud máxima: 200 caracteres
    /// </para>
    /// </remarks>
    [MaxLength(200)]
    public string? Calle { get; set; }

    /// <summary>
    /// Número del edificio o casa en la calle.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Ejemplos: "42", "12A", "S/N" (sin número)
    /// </para>
    /// <para>
    /// Longitud máxima: 20 caracteres
    /// </para>
    /// </remarks>
    [MaxLength(20)]
    public string? Numero { get; set; }

    /// <summary>
    /// Nombre de la ciudad, pueblo o municipio.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Ejemplos: "Madrid", "Barcelona", "Sevilla"
    /// </para>
    /// <para>
    /// Longitud máxima: 100 caracteres
    /// </para>
    /// </remarks>
    [MaxLength(100)]
    public string? Ciudad { get; set; }

    /// <summary>
    /// Nombre de la provincia o región administrativa.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Ejemplos: "Madrid", "Cataluña", "Andalucía"
    /// </para>
    /// <para>
    /// Longitud máxima: 100 caracteres
    /// </para>
    /// </remarks>
    [MaxLength(100)]
    public string? Provincia { get; set; }

    /// <summary>
    /// Nombre del país.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Ejemplos: "España", "Francia", "Portugal"
    /// </para>
    /// <para>
    /// Longitud máxima: 100 caracteres
    /// </para>
    /// </remarks>
    [MaxLength(100)]
    public string? Pais { get; set; }

    /// <summary>
    /// Código postal de 5 dígitos.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Formato: Exactly 5 dígitos numéricos.
    /// </para>
    /// <para>
    /// Ejemplos: "28013", "41001", "08001"
    /// </para>
    /// <para>
    /// Longitud máxima: 20 caracteres (para códigos internacionales)
    /// </para>
    /// </remarks>
    [MaxLength(20)]
    [RegularExpression(@"^[0-9]{5}$", ErrorMessage = "El código postal debe tener exactamente 5 dígitos.")]
    public string? CodigoPostal { get; set; }
}

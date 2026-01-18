namespace TiendaApi.Apis.Realtime.Common;

/// <summary>
/// DTO para mensajes de notificación de productos en tiempo real.
/// </summary>
/// <remarks>
/// <para><b>Ejemplo de uso:</b></para>
/// <code>
/// var notification = new ProductoNotificationDto
/// {
///     Type = NotificationType.CREATED,
///     ProductoId = 123,
///     ProductoNombre = "Laptop",
///     Data = productoDto
/// };
/// </code>
/// 
/// <para><b>Serialización:</b></para>
/// <code>
/// {
///   "type": "CREATED",
///   "productoId": 123,
///   "productoNombre": "Laptop",
///   "timestamp": "2025-01-18T10:30:00Z"
/// }
/// </code>
/// </remarks>
public class ProductoNotificationDto
{
    /// <summary>
    /// Tipo de notificación (CREATED, UPDATED, DELETED).
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// ID del producto.
    /// </summary>
    public long ProductoId { get; set; }

    /// <summary>
    /// Nombre del producto.
    /// </summary>
    public string ProductoNombre { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp de la notificación.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Datos adicionales de la notificación (opcional).
    /// </summary>
    public object? Data { get; set; }
}

/// <summary>
/// Tipos de notificación para eventos de productos.
/// </summary>
public static class NotificationType
{
    /// <summary>
    /// Notificación de producto creado.
    /// </summary>
    public const string CREATED = "CREATED";

    /// <summary>
    /// Notificación de producto actualizado.
    /// </summary>
    public const string UPDATED = "UPDATED";

    /// <summary>
    /// Notificación de producto eliminado.
    /// </summary>
    public const string DELETED = "DELETED";
}

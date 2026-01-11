namespace TiendaApi.WebSockets.Productos;

/// <summary>
/// DTO para mensajes de notificación de productos vía WebSocket.
/// </summary>
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

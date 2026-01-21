namespace TiendaApi.Api.Realtime.Common;

/// <summary>
/// DTO para notificaciones de productos en tiempo real.
/// </summary>
/// <example>
/// var notification = new ProductoNotificationDto { Type = "CREATED", ProductoId = 123, ProductoNombre = "Laptop" };
/// </example>
public class ProductoNotificationDto
{
    /// <summary>Tipo de notificación (CREATED, UPDATED, DELETED).</summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>ID del producto.</summary>
    public long ProductoId { get; set; }

    /// <summary>Nombre del producto.</summary>
    public string ProductoNombre { get; set; } = string.Empty;

    /// <summary>Timestamp de la notificación.</summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>Datos adicionales (opcional).</summary>
    public object? Data { get; set; }
}

/// <summary>
/// Tipos de notificación para productos.
/// </summary>
public static class NotificationType
{
    /// <summary>Producto creado.</summary>
    public const string CREATED = "CREATED";

    /// <summary>Producto actualizado.</summary>
    public const string UPDATED = "UPDATED";

    /// <summary>Producto eliminado.</summary>
    public const string DELETED = "DELETED";
}

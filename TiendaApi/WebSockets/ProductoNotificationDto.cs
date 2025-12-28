namespace TiendaApi.WebSockets;

/// <summary>
/// DTO for producto notification messages over WebSocket
/// </summary>
public class ProductoNotificationDto
{
    public string Type { get; set; } = string.Empty; // CREATED, UPDATED, DELETED
    public long ProductoId { get; set; }
    public string ProductoNombre { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public object? Data { get; set; } // Additional data (optional)
}

/// <summary>
/// Notification types for producto events
/// </summary>
public static class NotificationType
{
    public const string CREATED = "CREATED";
    public const string UPDATED = "UPDATED";
    public const string DELETED = "DELETED";
}

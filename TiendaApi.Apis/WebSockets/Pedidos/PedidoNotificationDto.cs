namespace TiendaApi.Apis.WebSockets.Pedidos;

/// <summary>
/// DTO para notificaciones WebSocket sobre cambios de estado de pedidos.
/// </summary>
public class PedidoNotificationDto
{
    /// <summary>
    /// Tipo de notificación.
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// ID del pedido.
    /// </summary>
    public string PedidoId { get; set; } = string.Empty;

    /// <summary>
    /// ID del usuario asociado al pedido.
    /// </summary>
    public long UserId { get; set; }

    /// <summary>
    /// Estado actual del pedido.
    /// </summary>
    public string Estado { get; set; } = string.Empty;

    /// <summary>
    /// Datos adicionales de la notificación.
    /// </summary>
    public object? Data { get; set; }

    /// <summary>
    /// Timestamp de la notificación.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

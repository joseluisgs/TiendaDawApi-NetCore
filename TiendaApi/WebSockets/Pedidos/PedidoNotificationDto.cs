namespace TiendaApi.WebSockets.Pedidos;

/// <summary>
/// DTO for WebSocket notifications about pedido status changes
/// </summary>
public class PedidoNotificationDto
{
    public string Type { get; set; } = string.Empty;
    public string PedidoId { get; set; } = string.Empty;
    public long UserId { get; set; }
    public string Estado { get; set; } = string.Empty;
    public object? Data { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

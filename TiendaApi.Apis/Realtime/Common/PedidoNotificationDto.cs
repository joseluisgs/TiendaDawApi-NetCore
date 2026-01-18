namespace TiendaApi.Apis.Realtime.Common;

/// <summary>
/// DTO para notificaciones de pedidos en tiempo real.
/// </summary>
/// <remarks>
/// <para><b>Ejemplo de uso:</b></para>
/// <code>
/// var notification = new PedidoNotificationDto
/// {
///     Type = "PEDIDO_ESTADO_UPDATED",
///     PedidoId = "PED-001",
///     UserId = 123,
///     Estado = "Enviado",
///     Data = pedidoDto
/// };
/// </code>
/// 
/// <para><b>Serialización:</b></para>
/// <code>
/// {
///   "type": "PEDIDO_ESTADO_UPDATED",
///   "pedidoId": "PED-001",
///   "userId": 123,
///   "estado": "Enviado",
///   "timestamp": "2025-01-18T10:30:00Z"
/// }
/// </code>
/// </remarks>
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

/// <summary>
/// Tipos de notificación para eventos de pedidos.
/// </summary>
public static class PedidoNotificationType
{
    /// <summary>
    /// Notificación de pedido creado.
    /// </summary>
    public const string CREADO = "PEDIDO_CREADO";

    /// <summary>
    /// Notificación de cambio de estado.
    /// </summary>
    public const string ESTADO_ACTUALIZADO = "PEDIDO_ESTADO_ACTUALIZADO";
}

namespace TiendaApi.Api.Realtime.Common;

/// <summary>
/// DTO para notificaciones de pedidos en tiempo real.
/// </summary>
/// <example>
/// var notification = new PedidoNotificationDto { Type = "PEDIDO_ESTADO_ACTUALIZADO", PedidoId = "PED-001", Estado = "Enviado" };
/// </example>
public class PedidoNotificationDto
{
    /// <summary>Tipo de notificación.</summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>ID del pedido.</summary>
    public string PedidoId { get; set; } = string.Empty;

    /// <summary>ID del usuario.</summary>
    public long UserId { get; set; }

    /// <summary>Estado del pedido.</summary>
    public string Estado { get; set; } = string.Empty;

    /// <summary>Datos adicionales.</summary>
    public object? Data { get; set; }

    /// <summary>Timestamp.</summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Tipos de notificación para pedidos.
/// </summary>
public static class PedidoNotificationType
{
    /// <summary>Pedido creado.</summary>
    public const string CREADO = "PEDIDO_CREADO";

    /// <summary>Cambio de estado.</summary>
    public const string ESTADO_ACTUALIZADO = "PEDIDO_ESTADO_ACTUALIZADO";
}

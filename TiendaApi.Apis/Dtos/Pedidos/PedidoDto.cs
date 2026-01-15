namespace TiendaApi.Apis.Dtos.Pedidos;

/// <summary>
/// DTO de pedido para respuestas de API.
/// Representa un pedido completo incluyendo identificación, usuario, items, totals y estado.
///
/// <remarks>
/// Estados posibles del pedido:
/// - "Pendiente": Pedido creado, esperando procesamiento
/// - "Procesando": Pedido confirmado, preparándose para envío
/// - "Enviado": Pedido en tránsito al cliente
/// - "Entregado": Pedido entregado exitosamente
/// - "Cancelado": Pedido cancelado por el usuario o administrador
/// </remarks>
/// </summary>
/// <example>
/// Respuesta JSON típica:
/// <code>
/// {
///   "id": "PED-2024-0001",
///   "userId": 1,
///   "items": [
///     { "productoId": 101, "nombreProducto": "Laptop", "cantidad": 1, "precio": 999.99, "subtotal": 999.99 }
///   ],
///   "total": 999.99,
///   "estado": "Pendiente",
///   "direccionEnvio": "Calle Principal 123, Ciudad",
///   "createdAt": "2024-01-15T10:30:00Z"
/// }
/// </code>
/// </example>
public record PedidoDto(
    /// <summary>
    /// Identificador único del pedido con formato "PED-YYYY-NNNN".
    /// Generado automáticamente siguiendo el patrón de secuencial anual.
    /// </summary>
    /// <example>PED-2024-0001</example>
    string Id,

    /// <summary>
    /// Identificador del usuario que realizó el pedido.
    /// Referencia a la tabla de usuarios del sistema.
    /// </summary>
    /// <example>1</example>
    long UserId,

    /// <summary>
    /// Lista de artículos incluidos en el pedido.
    /// Cada ítem representa un producto con su cantidad y precio.
    /// </summary>
    List<PedidoItemDto> Items,

    /// <summary>
    /// Total del pedido en la moneda base del sistema.
    /// Suma de todos los subtotales de items más impuestos.
    /// </summary>
    /// <example>999.99</example>
    decimal Total,

    /// <summary>
    /// Estado actual del pedido en el flujo de procesamiento.
    /// </summary>
    /// <example>Pendiente</example>
    string Estado,

    /// <summary>
    /// Dirección de entrega del pedido.
    /// Opcional, valor null para pedidos con entrega en tienda.
    /// </summary>
    /// <example>Calle Principal 123, Ciudad</example>
    string? DireccionEnvio,

    /// <summary>
    /// Fecha y hora de creación del pedido en formato UTC.
    /// Utilizado para auditoría y tracking temporal.
    /// </summary>
    /// <example>2024-01-15T10:30:00Z</example>
    DateTime CreatedAt
);

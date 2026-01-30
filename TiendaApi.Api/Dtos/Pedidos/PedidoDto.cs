namespace TiendaApi.Api.Dtos.Pedidos;

/// <summary>
    /// DTO de respuesta para pedidos.
    /// </summary>
    /// <example>
    /// {
    ///   "id": "PED-2024-0001",
    ///   "userId": 1,
    ///   "destinatario": {
    ///     "nombreCompleto": "María García",
    ///     "email": "maria@email.com",
    ///     "telefono": "+34612345678",
    ///     "direccion": {
    ///       "calle": "Gran Vía",
    ///       "numero": "42",
    ///       "ciudad": "Madrid",
    ///       "provincia": "Madrid",
    ///       "pais": "España",
    ///       "codigoPostal": "28013"
    ///     }
    ///   },
    ///   "items": [
    ///     { "productoId": 101, "nombreProducto": "Laptop", "cantidad": 1, "precio": 999.99, "subtotal": 999.99 }
    ///   ],
    ///   "total": 999.99,
    ///   "estado": "Pendiente",
    ///   "direccionEnvio": "Calle Principal 123, Ciudad",
    ///   "createdAt": "2024-01-15T10:30:00Z"
    /// }
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
    /// Información del destinatario del pedido.
    /// Siempre está presente y define quién recibirá el paquete.
    /// </summary>
    DestinatarioDto Destinatario,

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
    /// Dirección de entrega del pedido (deprecated, usar Destinatario.Direccion).
    /// Se mantiene por compatibilidad con versiones anteriores.
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

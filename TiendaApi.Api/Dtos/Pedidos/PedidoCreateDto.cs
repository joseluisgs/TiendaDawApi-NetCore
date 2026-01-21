using System.ComponentModel.DataAnnotations;

namespace TiendaApi.Api.Dtos.Pedidos;

/// <summary>
    /// DTO para crear un pedido.
    /// </summary>
    /// <example>
    /// {
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
    ///     { "productoId": 101, "cantidad": 2 },
    ///     { "productoId": 102, "cantidad": 1 }
    ///   ]
    /// }
    /// </example>
    public record PedidoRequestDto
{
    /// <summary>
    /// Información del destinatario del pedido.
    /// Este campo es obligatorio y define quién recibirá el paquete.
    /// </summary>
    /// <example>
    /// {
    ///   "nombreCompleto": "María García",
    ///   "email": "maria@email.com",
    ///   "telefono": "+34612345678",
    ///   "direccion": {
    ///     "calle": "Gran Vía",
    ///     "numero": "42",
    ///     "ciudad": "Madrid",
    ///     "provincia": "Madrid",
    ///     "pais": "España",
    ///     "codigoPostal": "28013"
    ///   }
    /// }
    /// </example>
    [Required(ErrorMessage = "El destinatario es obligatorio.")]
    public DestinatarioDto Destinatario { get; init; } = new();

    /// <summary>
    /// Lista de artículos a incluir en el pedido.
    /// Cada ítem especifica un producto y la cantidad deseada.
    /// </summary>
    /// <remarks>
    /// Restricciones:
    /// - Mínimo 1 artículo por pedido
    /// - Máximo 50 artículos por pedido
    /// - No se permiten productos duplicados (mismo ProductoId)
    /// </remarks>
    /// <example>[{"productoId": 101, "cantidad": 2}, {"productoId": 102, "cantidad": 1}]</example>
    [Required(ErrorMessage = "El pedido debe contener al menos un artículo")]
    [MinLength(1, ErrorMessage = "El pedido debe contener al menos un artículo")]
    public List<PedidoItemRequestDto> Items { get; init; } = new();
}

/// <summary>
    /// DTO de artículo de pedido para solicitudes.
    /// </summary>
    public record PedidoItemRequestDto
{
    /// <summary>
    /// Identificador del producto a incluir en el pedido.
    /// Debe corresponder a un producto existente y activo en el catálogo.
    /// </summary>
    /// <example>101</example>
    [Required(ErrorMessage = "El producto es obligatorio")]
    [Range(1, long.MaxValue, ErrorMessage = "Debe seleccionar un producto válido")]
    public long ProductoId { get; init; }

    /// <summary>
    /// Cantidad solicitada del producto.
    /// Debe ser un número entero positivo.
    /// </summary>
    /// <example>2</example>
    [Required(ErrorMessage = "La cantidad es obligatoria")]
    [Range(1, int.MaxValue, ErrorMessage = "La cantidad debe ser mayor a 0")]
    public int Cantidad { get; init; }
}

/// <summary>
    /// DTO para actualizar el estado de un pedido.
    /// </summary>
    public record UpdateEstadoDto
{
    /// <summary>
    /// Nuevo estado para el pedido.
    /// Debe ser un estado válido según las transiciones permitidas.
    /// </summary>
    /// <remarks>
    /// Estados válidos: Pendiente, Procesando, Enviado, Entregado, Cancelado
    /// </remarks>
    /// <example>Enviado</example>
    [Required(ErrorMessage = "El estado es obligatorio")]
    public string Estado { get; init; } = string.Empty;
}

/// <summary>
    /// DTO para actualizar datos de un pedido.
    /// </summary>
    public record UpdatePedidoDto
{
    /// <summary>
    /// Nuevo estado del pedido (opcional).
    /// Solo administradores pueden modificar estados.
    /// </summary>
    /// <example>Procesando</example>
    public string? Estado { get; init; }

    /// <summary>
    /// Nueva dirección de envío (opcional).
    /// Solo modificable si el pedido no ha sido enviado.
    /// </summary>
    /// <example>Nueva Calle 456, Ciudad</example>
    public string? DireccionEnvio { get; init; }
}

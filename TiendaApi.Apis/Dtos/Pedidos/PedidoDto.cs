namespace TiendaApi.Apis.Dtos.Pedidos;

/// <summary>
/// DTO de pedido para respuestas de API.
/// </summary>
public record PedidoDto
{
    /// <summary>
    /// Identificador único del pedido.
    /// </summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>
    /// Identificador del usuario que realizó el pedido.
    /// </summary>
    public long UserId { get; init; }

    /// <summary>
    /// Lista de artículos incluidos en el pedido.
    /// </summary>
    public List<PedidoItemDto> Items { get; init; } = new();

    /// <summary>
    /// Total del pedido.
    /// </summary>
    public decimal Total { get; init; }

    /// <summary>
    /// Estado actual del pedido.
    /// </summary>
    public string Estado { get; init; } = string.Empty;

    /// <summary>
    /// Dirección de envío del pedido.
    /// </summary>
    public string? DireccionEnvio { get; init; }

    /// <summary>
    /// Fecha de creación del pedido.
    /// </summary>
    public DateTime CreatedAt { get; init; }
}

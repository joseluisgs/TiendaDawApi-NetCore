namespace TiendaApi.Apis.Dtos.Pedidos;

/// <summary>
/// DTO para crear un nuevo pedido.
/// </summary>
public record PedidoRequestDto
{
    /// <summary>
    /// Lista de artículos a incluir en el pedido.
    /// </summary>
    public List<PedidoItemRequestDto> Items { get; init; } = new();
}

/// <summary>
/// DTO de artículo de pedido para solicitudes.
/// </summary>
public record PedidoItemRequestDto
{
    /// <summary>
    /// Identificador del producto.
    /// </summary>
    public long ProductoId { get; init; }

    /// <summary>
    /// Cantidad solicitada del producto.
    /// </summary>
    public int Cantidad { get; init; }
}

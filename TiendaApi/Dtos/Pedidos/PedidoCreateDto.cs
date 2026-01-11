namespace TiendaApi.Dtos.Pedidos;

/// <summary>
/// DTO for creating a Pedido (request)
/// </summary>
public record PedidoRequestDto
{
    public List<PedidoItemRequestDto> Items { get; init; } = new();
}

/// <summary>
/// DTO for Pedido item in request
/// </summary>
public record PedidoItemRequestDto
{
    public long ProductoId { get; init; }
    public int Cantidad { get; init; }
}

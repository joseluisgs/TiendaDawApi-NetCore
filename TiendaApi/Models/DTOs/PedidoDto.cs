namespace TiendaApi.Models.DTOs;

/// <summary>
/// DTO for Pedido responses
/// </summary>
public record PedidoDto
{
    public string Id { get; init; } = string.Empty;
    public long UserId { get; init; }
    public List<PedidoItemDto> Items { get; init; } = new();
    public decimal Total { get; init; }
    public string Estado { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
}

/// <summary>
/// DTO for Pedido item
/// </summary>
public record PedidoItemDto
{
    public long ProductoId { get; init; }
    public string NombreProducto { get; init; } = string.Empty;
    public int Cantidad { get; init; }
    public decimal Precio { get; init; }
    public decimal Subtotal { get; init; }
}

/// <summary>
/// DTO for creating a Pedido
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

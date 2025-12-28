namespace TiendaApi.DTOs;

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

namespace TiendaApi.DTOs;

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

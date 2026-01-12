namespace TiendaApi.Apis.Dtos.Pedidos;

/// <summary>
/// DTO de artículo de pedido.
/// </summary>
public record PedidoItemDto
{
    /// <summary>
    /// Identificador del producto.
    /// </summary>
    public long ProductoId { get; init; }

    /// <summary>
    /// Nombre del producto.
    /// </summary>
    public string NombreProducto { get; init; } = string.Empty;

    /// <summary>
    /// Cantidad solicitada.
    /// </summary>
    public int Cantidad { get; init; }

    /// <summary>
    /// Precio unitario del producto.
    /// </summary>
    public decimal Precio { get; init; }

    /// <summary>
    /// Subtotal del artículo (precio × cantidad).
    /// </summary>
    public decimal Subtotal { get; init; }
}

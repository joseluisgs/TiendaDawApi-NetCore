namespace TiendaApi.Apis.Dtos.Pedidos;

/// <summary>
/// DTO de artículo de pedido.
/// </summary>
public record PedidoItemDto(long ProductoId, string NombreProducto, int Cantidad, decimal Precio, decimal Subtotal);

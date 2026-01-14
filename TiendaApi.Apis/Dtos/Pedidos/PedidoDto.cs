namespace TiendaApi.Apis.Dtos.Pedidos;

/// <summary>
/// DTO de pedido para respuestas de API.
/// </summary>
public record PedidoDto(
    string Id,
    long UserId,
    List<PedidoItemDto> Items,
    decimal Total,
    string Estado,
    string? DireccionEnvio,
    DateTime CreatedAt
);

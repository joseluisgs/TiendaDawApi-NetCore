using System.ComponentModel.DataAnnotations;

namespace TiendaApi.Apis.Dtos.Pedidos;

/// <summary>
/// DTO para crear un nuevo pedido.
/// </summary>
public record PedidoRequestDto
{
    /// <summary>
    /// Lista de artículos a incluir en el pedido.
    /// </summary>
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
    /// Identificador del producto.
    /// </summary>
    [Required(ErrorMessage = "El producto es obligatorio")]
    [Range(1, long.MaxValue, ErrorMessage = "Debe seleccionar un producto válido")]
    public long ProductoId { get; init; }

    /// <summary>
    /// Cantidad solicitada del producto.
    /// </summary>
    [Required(ErrorMessage = "La cantidad es obligatoria")]
    [Range(1, int.MaxValue, ErrorMessage = "La cantidad debe ser mayor a 0")]
    public int Cantidad { get; init; }
}

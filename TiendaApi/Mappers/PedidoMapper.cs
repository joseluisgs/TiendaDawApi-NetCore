using TiendaApi.Dtos.Pedidos;
using TiendaApi.Models;

namespace TiendaApi.Mappers;

/// <summary>
/// Extension methods for Pedido entity-DTO conversions
/// Alternative to AutoMapper for educational purposes
/// </summary>
public static class PedidoMapper
{
    /// <summary>
    /// Converts Pedido entity to PedidoDto
    /// </summary>
    public static Dtos.Pedidos.PedidoDto ToDto(this Pedido pedido)
    {
        return new Dtos.Pedidos.PedidoDto
        {
            Id = pedido.Id ?? string.Empty,
            UserId = pedido.UserId,
            Items = pedido.Items?.Select(i => i.ToDto()).ToList() ?? new(),
            Total = pedido.Total,
            Estado = pedido.Estado ?? string.Empty,
            CreatedAt = pedido.CreatedAt
        };
    }

    /// <summary>
    /// Converts IEnumerable<Pedido> to IEnumerable<PedidoDto>
    /// </summary>
    public static IEnumerable<Dtos.Pedidos.PedidoDto> ToDtoList(this IEnumerable<Pedido> pedidos)
    {
        return pedidos.Select(p => p.ToDto());
    }

    /// <summary>
    /// Converts PedidoItem entity to PedidoItemDto
    /// </summary>
    public static Dtos.Pedidos.PedidoItemDto ToDto(this PedidoItem item)
    {
        return new Dtos.Pedidos.PedidoItemDto
        {
            ProductoId = item.ProductoId,
            NombreProducto = item.NombreProducto ?? string.Empty,
            Cantidad = item.Cantidad,
            Precio = item.Precio,
            Subtotal = item.Precio * item.Cantidad
        };
    }

    /// <summary>
    /// Converts PedidoRequestDto to Pedido entity
    /// </summary>
    public static Pedido ToEntity(this Dtos.Pedidos.PedidoRequestDto dto, long userId)
    {
        return new Pedido
        {
            UserId = userId,
            Items = dto.Items.Select(i => i.ToEntity()).ToList(),
            Estado = "PENDIENTE",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Converts PedidoItemRequestDto to PedidoItem entity
    /// </summary>
    public static PedidoItem ToEntity(this Dtos.Pedidos.PedidoItemRequestDto dto, string? nombreProducto = null, decimal? precio = null)
    {
        return new PedidoItem
        {
            ProductoId = dto.ProductoId,
            NombreProducto = nombreProducto ?? string.Empty,
            Cantidad = dto.Cantidad,
            Precio = precio ?? 0,
            Subtotal = (precio ?? 0) * dto.Cantidad
        };
    }
}

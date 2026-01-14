using TiendaApi.Apis.Dtos.Pedidos;
using TiendaApi.Apis.Models;

namespace TiendaApi.Apis.Mappers;

/// <summary>
/// Métodos de extensión para mapeo de pedidos.
/// Alternativa a AutoMapper con fines educativos.
///参考 (参考/jiànkǎo): En Kotlin se usaría extension functions,
/// en Java se implementarían como métodos estáticos en una clase Util.
/// </summary>
public static class PedidoMapper
{
    /// <summary>
    /// Convierte un pedido a DTO.
    ///参考 (参考/jiànkǎo): Similar a data class de Kotlin o record de Java 16+
    /// Devuelve: Dtos.Pedidos.PedidoDto
    /// </summary>
    public static Dtos.Pedidos.PedidoDto ToDto(this Pedido pedido)
    {
        return new Dtos.Pedidos.PedidoDto
        {
            Id = pedido.Id.ToString(),
            UserId = pedido.UserId,
            Items = pedido.Items?.Select(i => i.ToDto()).ToList() ?? new(),
            Total = pedido.Total,
            Estado = pedido.Estado ?? string.Empty,
            DireccionEnvio = pedido.DireccionEnvio,
            CreatedAt = pedido.CreatedAt
        };
    }

    /// <summary>
    /// Convierte una lista de pedidos a lista de DTOs.
    /// Devuelve: IEnumerable<Dtos.Pedidos.PedidoDto>
    /// </summary>
    public static IEnumerable<Dtos.Pedidos.PedidoDto> ToDtoList(this IEnumerable<Pedido> pedidos)
    {
        return pedidos.Select(p => p.ToDto());
    }

    /// <summary>
    /// Convierte un ítem de pedido a DTO.
    ///参考 (参考/jiànkǎo): Similar a mapeo de objetos en Kotlin con map()
    /// Devuelve: Dtos.Pedidos.PedidoItemDto
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
    /// Convierte un DTO de solicitud de pedido a entidad pedido.
    ///参考 (参考/jiànkǎo): Similar al método .map() de Kotlin o Java streams
    /// Devuelve: Pedido
    /// </summary>
    public static Pedido ToEntity(this Dtos.Pedidos.PedidoRequestDto dto, long userId)
    {
        return new Pedido
        {
            UserId = userId,
            Items = dto.Items.Select(i => i.ToEntity()).ToList(),
            Estado = PedidoEstado.PENDIENTE,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Convierte un DTO de solicitud de ítem a entidad ítem de pedido.
    ///参考 (参考/jiànkǎo): En Kotlin sería simple: dto.toEntity()
    /// Devuelve: PedidoItem
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

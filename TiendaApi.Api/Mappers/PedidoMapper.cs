using TiendaApi.Api.Dtos.Pedidos;
using TiendaApi.Api.Models;

namespace TiendaApi.Api.Mappers;

/// <summary>
/// Facilita la transformación entre entidades de pedidos y sus DTOs.
/// </summary>
public static class PedidoMapper
{
    /// <summary>
    /// Convierte una entidad <see cref="Pedido"/> en su DTO de visualización.
    /// </summary>
    /// <param name="pedido">Entidad origen.</param>
    /// <returns>DTO mapeado.</returns>
    public static PedidoDto ToDto(this Pedido pedido) =>
        new()
        {
            Id = pedido.Id.ToString(),
            UserId = pedido.UserId,
            Destinatario = pedido.Destinatario?.ToDto(),
            Items = pedido.Items.Select(i => i.ToDto()).ToList(),
            Total = pedido.Total,
            Estado = pedido.Estado,
            CreatedAt = pedido.CreatedAt,
            UpdatedAt = pedido.UpdatedAt
        };

    /// <summary>
    /// Convierte una lista de entidades en una lista de DTOs.
    /// </summary>
    public static IEnumerable<PedidoDto> ToDtoList(this IEnumerable<Pedido> pedidos) =>
        pedidos.Select(p => p.ToDto());

    /// <summary>
    /// Convierte un ítem de pedido en su DTO correspondiente.
    /// </summary>
    public static PedidoItemDto ToDto(this PedidoItem item) =>
        new()
        {
            ProductoId = item.ProductoId,
            NombreProducto = item.NombreProducto,
            Cantidad = item.Cantidad,
            Precio = item.Precio,
            Subtotal = item.Subtotal
        };

    /// <summary>
    /// Convierte los datos de envío en su DTO representativo.
    /// </summary>
    public static DestinatarioDto ToDto(this Destinatario dest) =>
        new()
        {
            Nombre = dest.Nombre,
            Email = dest.Email,
            Telefono = dest.Telefono,
            Direccion = dest.Direccion?.ToDto()
        };

    /// <summary>
    /// Convierte la dirección de envío en su DTO.
    /// </summary>
    public static DireccionDto ToDto(this Direccion dir) =>
        new()
        {
            Calle = dir.Calle,
            Numero = dir.Numero,
            Ciudad = dir.Ciudad,
            Provincia = dir.Provincia,
            CodigoPostal = dir.CodigoPostal
        };
}
using TiendaApi.Api.Dtos.Common;
using TiendaApi.Api.Dtos.Pedidos;
using TiendaApi.Api.Models;

namespace TiendaApi.Api.Mappers;

/// <summary>
    /// Mapper para convertir entre entidades de Pedido y DTOs.
    /// </summary>
    public static class PedidoMapper
{
    /// <summary>
    /// Convierte una entidad Pedido a DTO.
    /// </summary>
    /// <param name="pedido">Entidad a convertir.</param>
    /// <returns>DTO del pedido.</returns>
    public static PedidoDto ToDto(this Pedido pedido)
    {
        return new PedidoDto(
            pedido.Id.ToString(),
            pedido.UserId,
            pedido.Destinatario?.ToDto() ?? new DestinatarioDto(),
            pedido.Items?.Select(i => i.ToDto()).ToList() ?? new(),
            pedido.Total,
            pedido.Estado ?? string.Empty,
            pedido.DireccionEnvio,
            pedido.CreatedAt
        );
    }

    /// <summary>
    /// Convierte una entidad Destinatario a DTO.
    /// </summary>
    /// <param name="destinatario">Entidad a convertir.</param>
    /// <returns>DTO del destinatario.</returns>
    public static DestinatarioDto ToDto(this Destinatario? destinatario)
    {
        return new DestinatarioDto
        {
            NombreCompleto = destinatario?.NombreCompleto ?? string.Empty,
            Email = destinatario?.Email ?? string.Empty,
            Telefono = destinatario?.Telefono,
            Direccion = destinatario?.Direccion?.ToDto() ?? new DireccionDto()
        };
    }

    /// <summary>
    /// Convierte una entidad Direccion a DTO.
    /// </summary>
    /// <param name="direccion">Entidad a convertir.</param>
    /// <returns>DTO de la dirección.</returns>
    public static DireccionDto ToDto(this Direccion? direccion)
    {
        return new DireccionDto
        {
            Calle = direccion?.Calle ?? string.Empty,
            Numero = direccion?.Numero,
            Ciudad = direccion?.Ciudad ?? string.Empty,
            Provincia = direccion?.Provincia,
            Pais = direccion?.Pais ?? string.Empty,
            CodigoPostal = direccion?.CodigoPostal
        };
    }

    /// <summary>
    /// Convierte un DTO Destinatario a entidad.
    /// </summary>
    /// <param name="dto">DTO a convertir.</param>
    /// <returns>Entidad del destinatario.</returns>
    public static Destinatario ToEntity(this DestinatarioDto dto)
    {
        return new Destinatario
        {
            NombreCompleto = dto.NombreCompleto,
            Email = dto.Email,
            Telefono = dto.Telefono,
            Direccion = dto.Direccion?.ToEntity()
        };
    }

    /// <summary>
    /// Convierte un DTO Direccion a entidad.
    /// </summary>
    /// <param name="dto">DTO a convertir.</param>
    /// <returns>Entidad de la dirección.</returns>
    public static Direccion? ToEntity(this DireccionDto? dto)
    {
        if (dto == null)
            return null;

        return new Direccion
        {
            Calle = dto.Calle,
            Numero = dto.Numero,
            Ciudad = dto.Ciudad,
            Provincia = dto.Provincia,
            Pais = dto.Pais,
            CodigoPostal = dto.CodigoPostal
        };
    }

    /// <summary>
    /// Convierte una lista de Pedidos a lista de DTOs.
    /// </summary>
    /// <param name="pedidos">Lista de entidades.</param>
    /// <returns>Lista de DTOs.</returns>
    public static IEnumerable<PedidoDto> ToDtoList(this IEnumerable<Pedido> pedidos)
    {
        return pedidos.Select(p => p.ToDto());
    }

    /// <summary>
    /// Convierte un PedidoItem a DTO.
    /// </summary>
    /// <param name="item">Entidad a convertir.</param>
    /// <returns>DTO del ítem.</returns>
    public static PedidoItemDto ToDto(this PedidoItem item)
    {
        return new PedidoItemDto(
            item.ProductoId,
            item.NombreProducto ?? string.Empty,
            item.Cantidad,
            item.Precio,
            item.Precio * item.Cantidad
        );
    }

    /// <summary>
    /// Convierte un PedidoRequestDto a entidad Pedido.
    /// </summary>
    /// <param name="dto">DTO del pedido.</param>
    /// <param name="userId">ID del usuario.</param>
    /// <returns>Entidad del pedido.</returns>
    public static Pedido ToEntity(this PedidoRequestDto dto, long userId)
    {
        return new Pedido
        {
            UserId = userId,
            Destinatario = dto.Destinatario?.ToEntity(),
            Items = dto.Items.Select(i => i.ToEntity()).ToList(),
            Estado = PedidoEstado.PENDIENTE,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Convierte un PedidoItemRequestDto a entidad PedidoItem.
    /// </summary>
    /// <param name="dto">DTO del ítem.</param>
    /// <param name="nombreProducto">Nombre del producto.</param>
    /// <param name="precio">Precio del producto.</param>
    /// <returns>Entidad del ítem.</returns>
    public static PedidoItem ToEntity(this PedidoItemRequestDto dto, string? nombreProducto = null, decimal? precio = null)
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

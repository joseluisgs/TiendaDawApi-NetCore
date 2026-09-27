using AutoMapper;
using TiendaApi.Api.Dtos.Pedidos;
using TiendaApi.Api.Models;

namespace TiendaApi.Api.Mappers;

/// <summary>
/// Perfil de AutoMapper para mapeos de entidad Pedido a DTOs.
/// Asigna entre entidades Pedido y sus DTOs correspondientes.
/// </summary>
public class PedidoProfile : Profile
{
    /// <summary>
    /// Registra los mapeos de la entidad Pedido y sus ítems con sus DTOs.
    /// </summary>
    public PedidoProfile()
    {
        // Mapeos de entidad Pedido a DTO
        CreateMap<Pedido, PedidoDto>();
        CreateMap<PedidoItem, PedidoItemDto>();

        // Mapeos de DTO de solicitud a entidad
        CreateMap<PedidoRequestDto, Pedido>();
        CreateMap<PedidoItemRequestDto, PedidoItem>();
    }
}

using AutoMapper;
using TiendaApi.Dtos.Pedidos;
using TiendaApi.Models;

namespace TiendaApi.Mappers;

/// <summary>
/// AutoMapper profile for Pedido entity-DTO mappings
/// Maps between Pedido entities and their corresponding DTOs
/// </summary>
public class PedidoProfile : Profile
{
    public PedidoProfile()
    {
        // Pedido entity to DTO mappings
        CreateMap<Pedido, PedidoDto>();
        CreateMap<PedidoItem, PedidoItemDto>();
        
        // Request DTO to entity mappings
        CreateMap<PedidoRequestDto, Pedido>();
        CreateMap<PedidoItemRequestDto, PedidoItem>();
    }
}

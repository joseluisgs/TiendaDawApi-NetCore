using AutoMapper;
using TiendaApi.Models.DTOs;
using TiendaApi.Models.Entities;

namespace TiendaApi.Services;

/// <summary>
/// AutoMapper profiles for entity-DTO mappings
/// 
/// Java equivalent: ModelMapper or MapStruct configuration
/// Automatically converts between entities and DTOs
/// </summary>
public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // Categoria mappings
        CreateMap<Categoria, CategoriaDto>();
        CreateMap<CategoriaRequestDto, Categoria>();

        // Producto mappings
        CreateMap<Producto, ProductoDto>()
            .ForMember(dest => dest.CategoriaNombre,
                opt => opt.MapFrom(src => src.Categoria.Nombre));
        CreateMap<ProductoRequestDto, Producto>();

        // User mappings
        CreateMap<User, UserDto>();
        CreateMap<RegisterDto, User>();

        // Pedido mappings
        CreateMap<Pedido, PedidoDto>();
        CreateMap<PedidoItem, PedidoItemDto>();
        CreateMap<PedidoRequestDto, Pedido>();
        CreateMap<PedidoItemRequestDto, PedidoItem>();
    }
}

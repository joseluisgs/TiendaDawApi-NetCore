using AutoMapper;
using TiendaApi.Dtos.Categorias;
using TiendaApi.Dtos.Productos;
using TiendaApi.Dtos.Usuarios;
using TiendaApi.Dtos.Pedidos;
using TiendaApi.Models;

namespace TiendaApi.Mappers;

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
        CreateMap<PedidoRequestDto, Pedido>()
            .ForMember(dest => dest.Items, opt => opt.MapFrom(src => src.Items));
        CreateMap<PedidoItemRequestDto, PedidoItem>();
    }
}

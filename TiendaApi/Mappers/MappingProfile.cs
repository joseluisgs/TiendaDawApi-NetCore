using AutoMapper;
using TiendaApi.Dtos.Categorias;
using TiendaApi.Dtos.Productos;
using TiendaApi.Dtos.Usuarios;
using TiendaApi.Dtos.Pedidos;
using TiendaApi.Models;

namespace TiendaApi.Mappers;

/// <summary>
/// Perfiles de AutoMapper para mapeos entidad-DTO.
/// Convierte automáticamente entre entidades y DTOs.
/// </summary>
public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // Mapeos de categoría
        CreateMap<Categoria, CategoriaDto>();
        CreateMap<CategoriaRequestDto, Categoria>();

        // Mapeos de producto
        CreateMap<Producto, ProductoDto>()
            .ForMember(dest => dest.CategoriaNombre,
                opt => opt.MapFrom(src => src.Categoria.Nombre));
        CreateMap<ProductoRequestDto, Producto>();

        // Mapeos de usuario
        CreateMap<User, UserDto>();
        CreateMap<RegisterDto, User>();

        // Mapeos de pedido
        CreateMap<Pedido, PedidoDto>();
        CreateMap<PedidoItem, PedidoItemDto>();
        CreateMap<PedidoRequestDto, Pedido>()
            .ForMember(dest => dest.Items, opt => opt.MapFrom(src => src.Items));
        CreateMap<PedidoItemRequestDto, PedidoItem>();
    }
}

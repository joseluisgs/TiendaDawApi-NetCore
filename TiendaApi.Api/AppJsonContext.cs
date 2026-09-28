using System.Text.Json.Serialization;
using TiendaApi.Api.Dtos.Categorias;
using TiendaApi.Api.Dtos.Common;
using TiendaApi.Api.Dtos.Pedidos;
using TiendaApi.Api.Dtos.Productos;
using TiendaApi.Api.Dtos.Usuarios;
using TiendaApi.Api.Models;

namespace TiendaApi.Api;

/// <summary>
/// Contexto de serialización JSON con source-gen para AOT/trimming.
/// </summary>
[JsonSerializable(typeof(CategoriaDto))]
[JsonSerializable(typeof(ProductoDto))]
[JsonSerializable(typeof(PedidoDto))]
[JsonSerializable(typeof(PedidoItemDto))]
[JsonSerializable(typeof(UserDto))]
[JsonSerializable(typeof(RegisterDto))]
[JsonSerializable(typeof(LoginDto))]
[JsonSerializable(typeof(AuthResponseDto))]
[JsonSerializable(typeof(UserUpdateDto))]
[JsonSerializable(typeof(UserPatchDto))]
[JsonSerializable(typeof(ProductoFilterDto))]
[JsonSerializable(typeof(UserFilterDto))]
[JsonSerializable(typeof(PedidoFilterDto))]
[JsonSerializable(typeof(CategoriaFilterDto))]
[JsonSerializable(typeof(MyPedidoFilterDto))]
[JsonSerializable(typeof(CategoriaRequestDto))]
[JsonSerializable(typeof(ProductoRequestDto))]
[JsonSerializable(typeof(PedidoRequestDto))]
[JsonSerializable(typeof(UpdateEstadoDto))]
[JsonSerializable(typeof(UpdatePedidoDto))]
[JsonSerializable(typeof(ProductoPatchDto))]
[JsonSerializable(typeof(AvatarUpdateDto))]
[JsonSerializable(typeof(PagedResult<CategoriaDto>))]
[JsonSerializable(typeof(PagedResult<ProductoDto>))]
[JsonSerializable(typeof(PagedResult<PedidoDto>))]
[JsonSerializable(typeof(PagedResult<UserDto>))]
[JsonSerializable(typeof(Categoria))]
[JsonSerializable(typeof(Producto))]
[JsonSerializable(typeof(Pedido))]
[JsonSerializable(typeof(User))]
[JsonSerializable(typeof(List<CategoriaDto>))]
[JsonSerializable(typeof(List<ProductoDto>))]
[JsonSerializable(typeof(List<PedidoDto>))]
[JsonSerializable(typeof(List<UserDto>))]
public partial class AppJsonContext : JsonSerializerContext;

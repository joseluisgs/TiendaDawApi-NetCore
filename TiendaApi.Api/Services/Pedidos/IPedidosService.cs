using CSharpFunctionalExtensions;
using TiendaApi.Api.Dtos.Common;
using TiendaApi.Api.Dtos.Pedidos;
using TiendaApi.Api.Errors;

namespace TiendaApi.Api.Services.Pedidos;

/// <summary>
/// Contrato del servicio de pedidos.
/// </summary>
public interface IPedidosService
{
    /// <summary>Obtiene todos los pedidos (admin).</summary>
    /// <returns>Resultado con colección de pedidos.</returns>
    Task<Result<IEnumerable<PedidoDto>, DomainError>> FindAllAsync();

    /// <summary>Obtiene pedidos paginados (admin).</summary>
    /// <param name="page">Número de página.</param>
    /// <param name="size">Elementos por página.</param>
    /// <returns>Resultado con pedidos paginados.</returns>
    Task<Result<PagedResult<PedidoDto>, DomainError>> FindAllPagedAsync(int page, int size);

    /// <summary>Busca un pedido por ID (admin).</summary>
    /// <param name="id">ID del pedido.</param>
    /// <returns>Resultado con el pedido o error.</returns>
    Task<Result<PedidoDto, DomainError>> FindByIdAsync(string id);

    /// <summary>Actualiza un pedido (admin).</summary>
    /// <param name="id">ID del pedido.</param>
    /// <param name="dto">Datos a actualizar.</param>
    /// <returns>Resultado con el pedido actualizado.</returns>
    Task<Result<PedidoDto, DomainError>> UpdateAdminAsync(string id, UpdatePedidoDto dto);

    /// <summary>Elimina un pedido (admin).</summary>
    /// <param name="id">ID del pedido.</param>
    /// <returns>Resultado de la operación.</returns>
    Task<UnitResult<DomainError>> DeleteAdminAsync(string id);

    /// <summary>Actualiza el estado de un pedido (admin).</summary>
    /// <param name="id">ID del pedido.</param>
    /// <param name="nuevoEstado">Nuevo estado.</param>
    /// <returns>Resultado con el pedido actualizado.</returns>
    Task<Result<PedidoDto, DomainError>> UpdateEstadoAsync(string id, string nuevoEstado);

    /// <summary>Obtiene pedidos de un usuario.</summary>
    /// <param name="userId">ID del usuario.</param>
    /// <returns>Resultado con colección de pedidos.</returns>
    Task<Result<IEnumerable<PedidoDto>, DomainError>> FindByUserIdAsync(long userId);

    /// <summary>Obtiene pedidos paginados de un usuario.</summary>
    /// <param name="userId">ID del usuario.</param>
    /// <param name="page">Número de página.</param>
    /// <param name="size">Elementos por página.</param>
    /// <returns>Resultado con pedidos paginados.</returns>
    Task<Result<PagedResult<PedidoDto>, DomainError>> FindMyPedidosAsync(long userId, int page, int size);

    /// <summary>Busca un pedido propio por ID.</summary>
    /// <param name="id">ID del pedido.</param>
    /// <param name="userId">ID del usuario.</param>
    /// <returns>Resultado con el pedido o error.</returns>
    Task<Result<PedidoDto, DomainError>> FindMyPedidoAsync(string id, long userId);

    /// <summary>Crea un nuevo pedido.</summary>
    /// <param name="userId">ID del usuario.</param>
    /// <param name="dto">Datos del pedido.</param>
    /// <returns>Resultado con el pedido creado.</returns>
    Task<Result<PedidoDto, DomainError>> CreateAsync(long userId, PedidoRequestDto dto);

    /// <summary>Actualiza un pedido propio.</summary>
    /// <param name="id">ID del pedido.</param>
    /// <param name="userId">ID del usuario.</param>
    /// <param name="dto">Datos a actualizar.</param>
    /// <returns>Resultado con el pedido actualizado.</returns>
    Task<Result<PedidoDto, DomainError>> UpdateMyPedidoAsync(string id, long userId, UpdatePedidoDto dto);

    /// <summary>Cancela un pedido propio.</summary>
    /// <param name="id">ID del pedido.</param>
    /// <param name="userId">ID del usuario.</param>
    /// <returns>Resultado de la operación.</returns>
    Task<UnitResult<DomainError>> DeleteMyPedidoAsync(string id, long userId);
}

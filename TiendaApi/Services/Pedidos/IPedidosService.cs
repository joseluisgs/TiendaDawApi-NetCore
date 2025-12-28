using TiendaApi.Common;
using TiendaApi.Models.DTOs;

namespace TiendaApi.Services.Pedidos;

/// <summary>
/// Service interface for Pedidos business logic
/// </summary>
public interface IPedidosService
{
    Task<Result<IEnumerable<PedidoDto>, AppError>> FindAllAsync();
    Task<Result<IEnumerable<PedidoDto>, AppError>> FindByUserIdAsync(long userId);
    Task<Result<PedidoDto, AppError>> FindByIdAsync(string id);
    Task<Result<PedidoDto, AppError>> CreateAsync(long userId, PedidoRequestDto dto);
    Task<Result<PedidoDto, AppError>> UpdateEstadoAsync(string id, string nuevoEstado);
}

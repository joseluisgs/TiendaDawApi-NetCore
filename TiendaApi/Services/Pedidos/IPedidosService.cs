using CSharpFunctionalExtensions;
using TiendaApi.Dtos.Pedidos;
using TiendaApi.Errors;

namespace TiendaApi.Services.Pedidos;

/// <summary>
/// Service interface for Pedidos business logic
/// </summary>
public interface IPedidosService
{
    Task<Result<IEnumerable<PedidoDto>, DomainError>> FindAllAsync();
    Task<Result<IEnumerable<PedidoDto>, DomainError>> FindByUserIdAsync(long userId);
    Task<Result<PedidoDto, DomainError>> FindByIdAsync(string id);
    Task<Result<PedidoDto, DomainError>> CreateAsync(long userId, PedidoRequestDto dto);
    Task<Result<PedidoDto, DomainError>> UpdateEstadoAsync(string id, string nuevoEstado);
}

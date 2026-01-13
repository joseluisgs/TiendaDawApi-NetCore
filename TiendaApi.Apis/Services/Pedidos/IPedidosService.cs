using CSharpFunctionalExtensions;
using TiendaApi.Apis.Dtos.Pedidos;
using TiendaApi.Apis.Errors;

namespace TiendaApi.Apis.Services.Pedidos;

/// <summary>
/// Interfaz del servicio de pedidos usando Patrón Result.
/// Maneja la lógica de negocio: verificación de stock, reservas, almacenamiento MongoDB, notificaciones.
/// </summary>
public interface IPedidosService
{
    /// <summary>
    /// Obtiene todos los pedidos.
    /// Returns: Result.Success(List) | Result.Failure nunca
    /// </summary>
    Task<Result<IEnumerable<PedidoDto>, DomainError>> FindAllAsync();

    /// <summary>
    /// Obtiene los pedidos de un usuario.
    /// Returns: Result.Success(List) | Result.Failure nunca
    /// </summary>
    Task<Result<IEnumerable<PedidoDto>, DomainError>> FindByUserIdAsync(long userId);

    /// <summary>
    /// Obtiene un pedido por su ID.
    /// Returns: Result.Success(PedidoDto) | Result.Failure(NotFound)
    /// </summary>
    Task<Result<PedidoDto, DomainError>> FindByIdAsync(string id);

    /// <summary>
    /// Crea un nuevo pedido con verificación de stock.
    /// Returns: Result.Success(PedidoDto) | Result.Failure(Validation/NotFound/BusinessRule)
    /// </summary>
    Task<Result<PedidoDto, DomainError>> CreateAsync(long userId, PedidoRequestDto dto);

    /// <summary>
    /// Actualiza el estado de un pedido.
    /// Returns: Result.Success(PedidoDto) | Result.Failure(NotFound/Validation)
    /// </summary>
    Task<Result<PedidoDto, DomainError>> UpdateEstadoAsync(string id, string nuevoEstado);

    /// <summary>
    /// Actualiza un pedido (el usuario puede actualizar sus propios pedidos).
    /// Returns: Result.Success(PedidoDto) | Result.Failure(NotFound/Validation/Forbidden)
    /// </summary>
    Task<Result<PedidoDto, DomainError>> UpdateAsync(string id, long userId, UpdatePedidoDto dto);

    /// <summary>
    /// Elimina un pedido (el usuario puede eliminar sus propios pedidos).
    /// Returns: UnitResult.Success | UnitResult.Failure(NotFound/Forbidden)
    /// </summary>
    Task<UnitResult<DomainError>> DeleteAsync(string id, long userId);
}

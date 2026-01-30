using CSharpFunctionalExtensions;
using TiendaApi.Api.Dtos.Common;
using TiendaApi.Api.Dtos.Pedidos;
using TiendaApi.Api.Errors;

namespace TiendaApi.Api.Services.Pedidos;

/// <summary>
/// Define el contrato para la gestión de pedidos de compra en el sistema.
/// Proporciona una capa de abstracción para manejar transacciones serializables,
/// control de stock, notificaciones en tiempo real y persistencia políglota (PostgreSQL y MongoDB).
/// </summary>
public interface IPedidosService
{
    /// <summary>
    /// Recupera la totalidad de pedidos del sistema. Solo para uso administrativo.
    /// </summary>
    /// <returns>Resultado con la colección de <see cref="PedidoDto"/>.</returns>
    Task<Result<IEnumerable<PedidoDto>, DomainError>> FindAllAsync();

    /// <summary>
    /// Recupera pedidos paginados para la vista de administración.
    /// </summary>
    /// <param name="page">Número de página (0-indexed).</param>
    /// <param name="size">Elementos por página.</param>
    /// <returns>Resultado con el objeto paginado.</returns>
    Task<Result<PagedResult<PedidoDto>, DomainError>> FindAllPagedAsync(int page, int size);

    /// <summary>
    /// Busca un pedido por su identificador único (GUID).
    /// </summary>
    /// <param name="id">Identificador del pedido.</param>
    /// <returns>Resultado con el pedido o error 404.</returns>
    Task<Result<PedidoDto, DomainError>> FindByIdAsync(string id);

    /// <summary>
    /// Actualiza la información de un pedido por parte de un administrador.
    /// Permite modificar el estado y la dirección de envío sin restricciones.
    /// </summary>
    /// <param name="id">ID del pedido.</param>
    /// <param name="dto">Nuevos datos.</param>
    /// <returns>Resultado con el pedido actualizado.</returns>
    Task<Result<PedidoDto, DomainError>> UpdateAdminAsync(string id, UpdatePedidoDto dto);

    /// <summary>
    /// Realiza un borrado lógico del pedido. Solo accesible para administradores.
    /// </summary>
    /// <param name="id">ID del pedido.</param>
    /// <returns>Resultado de la operación.</returns>
    Task<UnitResult<DomainError>> DeleteAdminAsync(string id);

    /// <summary>
    /// Cambia el estado de procesamiento de un pedido.
    /// </summary>
    /// <param name="id">ID del pedido.</param>
    /// <param name="nuevoEstado">Nuevo estado (PENDIENTE, ENVIADO, etc).</param>
    /// <returns>Resultado con el pedido actualizado.</returns>
    Task<Result<PedidoDto, DomainError>> UpdateEstadoAsync(string id, string nuevoEstado);

    /// <summary>
    /// Recupera todos los pedidos pertenecientes a un usuario específico.
    /// </summary>
    /// <param name="userId">ID del usuario propietario.</param>
    /// <returns>Resultado con la colección de pedidos.</returns>
    Task<Result<IEnumerable<PedidoDto>, DomainError>> FindByUserIdAsync(long userId);

    /// <summary>
    /// Obtiene los pedidos de un usuario de forma paginada.
    /// </summary>
    /// <param name="userId">ID del usuario.</param>
    /// <param name="page">Número de página.</param>
    /// <param name="size">Elementos por página.</param>
    /// <returns>Resultado con los pedidos paginados.</returns>
    Task<Result<PagedResult<PedidoDto>, DomainError>> FindMyPedidosAsync(long userId, int page, int size);

    /// <summary>
    /// Busca un pedido propio verificando la titularidad.
    /// </summary>
    /// <param name="id">ID del pedido.</param>
    /// <param name="userId">ID del usuario solicitante.</param>
    /// <returns>Resultado con el pedido o error de prohibido si no es el dueño.</returns>
    Task<Result<PedidoDto, DomainError>> FindMyPedidoAsync(string id, long userId);

    /// <summary>
    /// Registra un nuevo pedido en el sistema.
    /// Realiza una transacción serializable con control de concurrencia y stock.
    /// </summary>
    /// <param name="userId">ID del usuario que realiza la compra.</param>
    /// <param name="dto">Datos de la compra (productos y cantidades).</param>
    /// <returns>Resultado con el pedido creado.</returns>
    Task<Result<PedidoDto, DomainError>> CreateAsync(long userId, PedidoRequestDto dto);

    /// <summary>
    /// Permite al usuario modificar un pedido propio si todavía está en estado PENDIENTE.
    /// </summary>
    /// <param name="id">ID del pedido.</param>
    /// <param name="userId">ID del usuario.</param>
    /// <param name="dto">Nuevos datos (solo dirección).</param>
    /// <returns>Resultado con el pedido actualizado.</returns>
    Task<Result<PedidoDto, DomainError>> UpdateMyPedidoAsync(string id, long userId, UpdatePedidoDto dto);

    /// <summary>
    /// Permite al usuario cancelar un pedido propio si todavía no ha sido procesado.
    /// </summary>
    /// <param name="id">ID del pedido.</param>
    /// <param name="userId">ID del usuario.</param>
    /// <returns>Resultado de la operación.</returns>
    Task<UnitResult<DomainError>> DeleteMyPedidoAsync(string id, long userId);
}
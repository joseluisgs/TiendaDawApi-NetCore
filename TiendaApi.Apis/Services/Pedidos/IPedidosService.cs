using CSharpFunctionalExtensions;
using TiendaApi.Apis.Dtos.Common;
using TiendaApi.Apis.Dtos.Pedidos;
using TiendaApi.Apis.Errors;

namespace TiendaApi.Apis.Services.Pedidos;

/// <summary>
/// Interfaz del servicio de pedidos que implementa el patrón de arquitectura por capas (Service Layer).
/// Coordina toda la lógica de negocio relacionada con el proceso de pedidos: verificación de stock,
/// reservas de productos, almacenamiento en MongoDB, generación de notificaciones y gestión de estados.
///
/// <para><b>Patrón Service Layer:</b></para>
/// <list type="bullet">
///   <item><description>Orquesta operaciones complejas que involucran múltiples entidades</description></item>
///   <item><description>Centraliza reglas de negocio del dominio de pedidos</description></item>
///   <item><description>Abstrae la coordinación entre SQL (productos/usuarios) y MongoDB (pedidos)</description></item>
/// </list>
///
/// <para><b>Patrón Result:</b></para>
/// <list type="bullet">
///   <item><description>Permite representar operaciones que pueden fallar por múltiples razones</description></item>
///   <item><description>Facilita el encadenamiento de validaciones y operaciones de negocio</description></item>
///   <item><description>Los errores incluyen código, mensaje, detalles y estado HTTP</description></item>
///   <item><description>Soporta el patrón UnitResult para operaciones sin valor de retorno</description></item>
/// </list>
///
/// <para><b>Estados de Pedido:</b></para>
/// <list type="bullet">
///   <item><description><c>PENDIENTE</c>: Pedido creado, esperando procesamiento</description></item>
///   <item><description><c>PROCESANDO</c>: Preparando para envío</description></item>
///   <item><description><c>ENVIADO</c>: En camino al cliente</description></item>
///   <item><description><c>ENTREGADO</c>: Entregado exitosamente</description></item>
///   <item><description><c>CANCELADO</c>: Cancelado</description></item>
/// </list>
/// </summary>
/// <remarks>
/// <para><b>Manejo de Errores:</b></para>
/// <list type="bullet">
///   <item><description><c>NotFoundError</c>: Pedido no encontrado</description></item>
///   <item><description><c>ForbiddenError</c>: No tiene permiso para la operación</description></item>
///   <item><description><c>ValidationError</c>: Datos inválidos</description></item>
///   <item><description><c>BusinessRuleError</c>: Regla de negocio violada (ej: stock)</description></item>
/// </list>
/// <para><b>Gestión de Stock:</b></para>
/// <list type="bullet">
///   <item><description>Create: Decrementa stock de productos</description></item>
///   <item><description>Update: Restaura stock anterior y decrementa nuevo si cambian items</description></item>
///   <item><description>Delete/Cancel: Restaura stock de todos los items</description></item>
/// </list>
/// <para><b>Notificaciones:</b></para>
/// <list type="bullet">
///   <item><description>Create: WebSocket + Email al admin</description></item>
///   <item><description>Update (Admin): WebSocket al cliente + Email al admin</description></item>
///   <item><description>Update (Usuario): WebSocket al cliente</description></item>
///   <item><description>Delete (Admin): Email al admin</description></item>
///   <item><description>Delete (Usuario): Email al admin</description></item>
/// </list>
/// </remarks>
public interface IPedidosService
{
    #region ========== MÉTODOS PARA ADMINISTRADORES ==========

    /// <summary>
    /// Recupera todos los pedidos del sistema (solo administradores).
    /// </summary>
    /// <returns>Enumerable con todos los pedidos.</returns>
    Task<Result<IEnumerable<PedidoDto>, DomainError>> FindAllAsync();

    /// <summary>
    /// Recupera los pedidos del sistema de forma paginada (solo administradores).
    /// </summary>
    /// <param name="page">Número de página (0-based).</param>
    /// <param name="size">Cantidad de pedidos por página.</param>
    /// <returns>Lista paginada de pedidos.</returns>
    Task<Result<PagedResult<PedidoDto>, DomainError>> FindAllPagedAsync(int page, int size);

    /// <summary>
    /// Busca un pedido por su ID (solo administradores pueden ver cualquier pedido).
    /// </summary>
    /// <param name="id">ID del pedido</param>
    /// <returns>Pedido encontrado o error NotFound.</returns>
    Task<Result<PedidoDto, DomainError>> FindByIdAsync(string id);

    /// <summary>
    /// Actualiza un pedido (solo administradores).
    /// Los administradores pueden actualizar cualquier pedido sin restricciones de propiedad.
    /// Envía WebSocket al cliente y Email al admin.
    /// </summary>
    /// <param name="id">ID del pedido</param>
    /// <param name="dto">Campos a actualizar</param>
    /// <returns>Pedido actualizado o error.</returns>
    Task<Result<PedidoDto, DomainError>> UpdateAdminAsync(string id, UpdatePedidoDto dto);

    /// <summary>
    /// Elimina un pedido (solo administradores).
    /// Los administradores pueden eliminar cualquier pedido.
    /// Envía Email al admin.
    /// </summary>
    /// <param name="id">ID del pedido</param>
    /// <returns>Éxito o error.</returns>
    Task<UnitResult<DomainError>> DeleteAdminAsync(string id);

    /// <summary>
    /// Actualiza el estado de un pedido (solo administradores).
    /// </summary>
    /// <param name="id">ID del pedido</param>
    /// <param name="nuevoEstado">Nuevo estado</param>
    /// <returns>Pedido con estado actualizado o error.</returns>
    Task<Result<PedidoDto, DomainError>> UpdateEstadoAsync(string id, string nuevoEstado);

    #endregion

    #region ========== MÉTODOS PARA USUARIOS (MIS PEDIDOS) ==========

    /// <summary>
    /// Obtiene todos los pedidos del usuario autenticado (sin paginación).
    /// </summary>
    /// <param name="userId">ID del usuario</param>
    /// <returns>Enumerable con todos los pedidos del usuario.</returns>
    Task<Result<IEnumerable<PedidoDto>, DomainError>> FindByUserIdAsync(long userId);

    /// <summary>
    /// Obtiene los pedidos del usuario autenticado de forma paginada.
    /// </summary>
    /// <param name="userId">ID del usuario</param>
    /// <param name="page">Número de página (1-indexed)</param>
    /// <param name="size">Cantidad de pedidos por página</param>
    /// <returns>Lista paginada de pedidos del usuario.</returns>
    Task<Result<PagedResult<PedidoDto>, DomainError>> FindMyPedidosAsync(long userId, int page, int size);

    /// <summary>
    /// Busca un pedido propio por su ID.
    /// Valida que el pedido pertenezca al usuario solicitante.
    /// </summary>
    /// <param name="id">ID del pedido</param>
    /// <param name="userId">ID del usuario propietario</param>
    /// <returns>Pedido encontrado o error NotFound/Forbidden.</returns>
    Task<Result<PedidoDto, DomainError>> FindMyPedidoAsync(string id, long userId);

    /// <summary>
    /// Crea un nuevo pedido para el usuario autenticado.
    /// </summary>
    /// <param name="userId">ID del usuario que realiza el pedido</param>
    /// <param name="dto">Datos del pedido</param>
    /// <returns>Pedido creado o error.</returns>
    Task<Result<PedidoDto, DomainError>> CreateAsync(long userId, PedidoRequestDto dto);

    /// <summary>
    /// Actualiza un pedido propio.
    /// Solo permite modificar pedidos en estado PENDIENTE.
    /// Envía WebSocket al cliente.
    /// </summary>
    /// <param name="id">ID del pedido</param>
    /// <param name="userId">ID del usuario propietario</param>
    /// <param name="dto">Campos a actualizar</param>
    /// <returns>Pedido actualizado o error (NotFound, Forbidden, Validation).</returns>
    /// <remarks>
    /// Si se modifican los items, se restaura el stock de los items anteriores
    /// y se decrementa el stock de los nuevos items.
    /// </remarks>
    Task<Result<PedidoDto, DomainError>> UpdateMyPedidoAsync(string id, long userId, UpdatePedidoDto dto);

    /// <summary>
    /// Cancela y elimina un pedido propio.
    /// Solo permite eliminar pedidos en estado PENDIENTE.
    /// Envía Email al admin.
    /// </summary>
    /// <param name="id">ID del pedido</param>
    /// <param name="userId">ID del usuario propietario</param>
    /// <returns>Éxito o error (NotFound, Forbidden, InvalidState).</returns>
    /// <remarks>
    /// Al eliminar, se restaura el stock de todos los productos del pedido.
    /// </remarks>
    Task<UnitResult<DomainError>> DeleteMyPedidoAsync(string id, long userId);

    #endregion
}

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
///   <item><description><c>Pending</c>: Pedido creado, esperando pago</description></item>
///   <item><description><c>Confirmed</c>: Pago confirmado</description></item>
///   <item><description><c>Processing</c>: Preparando para envío</description></item>
///   <item><description><c>Shipped</c>: Enviado al cliente</description></item>
///   <item><description><c>Delivered</c>: Entregado</description></item>
///   <item><description><c>Cancelled</c>: Cancelado por usuario o sistema</description></item>
/// </list>
/// </summary>
/// <remarks>
/// <para><b>Manejo de Errores de Negocio:</b></para>
/// <list type="bullet">
///   <item><description><c>OutOfStock</c>: Producto sin stock suficiente</description></item>
///   <item><description><c>InvalidStateTransition</c>: Cambio de estado no permitido</description></item>
///   <item><description><c>OrderNotOwned</c>: Usuario no es propietario del pedido</description></item>
///   <item><description><c>ExpiredReservation</c>: Reserva de stock expirada</description></item>
/// </list>
/// <para><b>Transacciones:</b></para>
/// <list type="bullet">
///   <item><description>Las reservas de stock son temporales (15 minutos por defecto)</description></item>
///   <item><description>Si el pago no se confirma, el stock se libera automáticamente</description></item>
///   <item><description>Cada pedido se almacena como documento en MongoDB</description></item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// // Crear pedido con manejo de errores
/// [HttpPost]
/// public async Task&lt;ActionResult&gt; CreateOrder(PedidoRequestDto dto)
/// {
///     var userId = GetCurrentUserId();
///     var resultado = await _pedidosService.CreateAsync(userId, dto);
///
///     return resultado.Match(
///         pedido =&gt; CreatedAtAction(nameof(GetOrder), new { id = pedido.Id }, pedido),
///         error =&gt; {
///             return error.Code switch
///             {
///                 "OUT_OF_STOCK" =&gt; BadRequest(new {
///                     message = "Productos sin stock",
///                     details = error.Details
///                 }),
///                 "INVALID_PRODUCT" =&gt; BadRequest("Producto no válido"),
///                 _ =&gt; Problem(error.Message)
///             };
///         }
///     );
/// }
///
/// // Consultar pedidos propios
/// [HttpGet("my-orders")]
/// public async Task&lt;ActionResult&lt;PagedResult&lt;PedidoDto&gt;&gt;&gt; GetMyOrders(int page = 1, int size = 10)
/// {
///     var userId = GetCurrentUserId();
///     var resultado = await _pedidosService.FindByUserIdPagedAsync(userId, page, size);
///     return Ok(resultado.Value);
/// }
///
/// // Cambiar estado (solo administradores o lógica automática)
/// [HttpPut("{id}/estado")]
/// public async Task&lt;ActionResult&gt; UpdateEstado(string id, [FromBody] string nuevoEstado)
/// {
///     var resultado = await _pedidosService.UpdateEstadoAsync(id, nuevoEstado);
///     return resultado.Match(Ok, error =&gt; BadRequest(error.Message));
/// }
/// </code>
public interface IPedidosService
{
    /// <summary>
    /// Recupera todos los pedidos del sistema (solo administradores).
    /// </summary>
    /// <returns>
    /// <list type="bullet">
    ///   <item><term><c>Result.Success</c></term><description>Enumerable con todos los pedidos</description></item>
    ///   <item><term><c>Result.Failure</c></term><description>Nunca ocurre</description></item>
    /// </list>
    /// </returns>
    /// <remarks>
    /// Para grandes volúmenes, implementar paginación o usar filtros por fecha/estado.
    /// </remarks>
    /// <example>
    /// <code>
    /// var resultado = await _pedidosService.FindAllAsync();
    /// return Ok(resultado.Value);
    /// </code>
    /// </example>
    Task<Result<IEnumerable<PedidoDto>, DomainError>> FindAllAsync();

    /// <summary>
    /// Obtiene todos los pedidos de un usuario específico.
    /// </summary>
    /// <param name="userId">ID del usuario</param>
    /// <returns>
    /// <list type="bullet">
    ///   <item><term><c>Result.Success</c></term><description>Lista de pedidos del usuario</description></item>
    ///   <item><term><c>Result.Failure</c></term><description>Nunca ocurre</description></item>
    /// </list>
    /// </returns>
    /// <example>
    /// <code>
    /// var resultado = await _pedidosService.FindByUserIdAsync(userId);
    /// return Ok(resultado.Value);
    /// </code>
    /// </example>
    Task<Result<IEnumerable<PedidoDto>, DomainError>> FindByUserIdAsync(long userId);

    /// <summary>
    /// Obtiene los pedidos de un usuario de forma paginada.
    /// </summary>
    /// <param name="userId">ID del usuario</param>
    /// <param name="page">Número de página (1-indexed)</param>
    /// <param name="size">Cantidad de pedidos por página</param>
    /// <returns>
    /// <list type="bullet">
    ///   <item><term><c>Result.Success</c></term><description><c>PagedResult</c> con pedidos del usuario</description></item>
    ///   <item><term><c>Result.Failure</c></term><description>Nunca con parámetros válidos</description></item>
    /// </list>
    /// </returns>
    /// <example>
    /// <code>
    /// var resultado = await _pedidosService.FindByUserIdPagedAsync(userId, 1, 10);
    /// return Ok(resultado.Value);
    /// </code>
    /// </example>
    Task<Result<PagedResult<PedidoDto>, DomainError>> FindByUserIdPagedAsync(long userId, int page, int size);

    /// <summary>
    /// Busca un pedido por su identificador único (ObjectId de MongoDB).
    /// </summary>
    /// <param name="id">ID del pedido en formato MongoDB ObjectId</param>
    /// <returns>
    /// <list type="bullet">
    ///   <item><term><c>Result.Success</c></term><description>Datos completos del pedido</description></item>
    ///   <item><term><c>Result.Failure</c></term><description>NotFound si no existe</description></item>
    /// </list>
    /// </returns>
    /// <remarks>
    /// El ID es el ObjectId de MongoDB, no un ID numérico.
    /// </remarks>
    /// <example>
    /// <code>
    /// var resultado = await _pedidosService.FindByIdAsync("507f1f77bcf86cd799439011");
    /// return resultado.Match(Ok, error =&gt; NotFound());
    /// </code>
    /// </example>
    Task<Result<PedidoDto, DomainError>> FindByIdAsync(string id);

    /// <summary>
    /// Crea un nuevo pedido verificando stock y reservando productos.
    /// Orquesta la creación del pedido con validación de negocio completa.
    /// </summary>
    /// <param name="userId">ID del usuario que realiza el pedido</param>
    /// <param name="dto">Datos del pedido (productos, cantidades, dirección)</param>
    /// <returns>
    /// <list type="bullet">
    ///   <item><term><c>Result.Success</c></term><description>Pedido creado con estado Pending y productos reservados</description></item>
    ///   <item><term><c>Result.Failure</c></term><description>OutOfStock, InvalidProduct, UserNotFound, Validation</description></item>
    /// </list>
    /// </returns>
    /// <remarks>
    /// <para><b>Flujo de creación:</b></para>
    /// <list type="bullet">
    ///   <item><description>1. Validar usuario existe y está activo</description></item>
    ///   <item><description>2. Validar cada producto existe y está activo</description></item>
    ///   <item><description>3. Verificar stock suficiente</description></item>
    ///   <item><description>4. Reservar stock (temporal, con timeout)</description></item>
    ///   <item><description>5. Calcular totales</description></item>
    ///   <item><description>6. Guardar pedido en MongoDB</description></item>
    ///   <item><description>7. Generar número de pedido</description></item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// var pedidoRequest = new PedidoRequestDto
    /// {
    ///     Items = new List&lt;PedidoItemDto&gt;
    ///     {
    ///         new() { ProductoId = 1, Cantidad = 2 },
    ///         new() { ProductoId = 5, Cantidad = 1 }
    ///     },
    ///     DireccionEnvio = "Calle Principal 123, Madrid",
    ///     Notas = "Entregar por la tarde"
    /// };
    ///
    /// var resultado = await _pedidosService.CreateAsync(userId, pedidoRequest);
    ///
    /// return resultado.Match(
    ///     pedido =&gt; {
    ///         // El stock está reservado, pendiente de pago
    ///         _notificationService.EnviarConfirmacion(userId, pedido.Numero);
    ///         return CreatedAtAction(nameof(GetOrder), new { id = pedido.Id }, pedido);
    ///     },
    ///     error =&gt; {
    ///         if (error.Code == "OUT_OF_STOCK")
    ///         {
    ///             var detalles = error.Details?.ToObject&lt;OutOfStockDetails&gt;();
    ///             return BadRequest(new {
    ///                 message = "No hay suficiente stock",
    ///                 productosSinStock = detalles?.Productos
    ///             });
    ///         }
    ///         return BadRequest(error.Message);
    ///     }
    /// );
    /// </code>
    /// </example>
    Task<Result<PedidoDto, DomainError>> CreateAsync(long userId, PedidoRequestDto dto);

    /// <summary>
    /// Actualiza el estado de un pedido (solo administradores o sistema).
    /// Valida que la transición de estado sea válida.
    /// </summary>
    /// <param name="id">ID del pedido</param>
    /// <param name="nuevoEstado">Nuevo estado a aplicar</param>
    /// <returns>
    /// <list type="bullet">
    ///   <item><term><c>Result.Success</c></term><description>Pedido con estado actualizado</description></item>
    ///   <item><term><c>Result.Failure</c></term><description>NotFound o InvalidStateTransition</description></item>
    /// </list>
    /// </returns>
    /// <remarks>
    /// <para><b>Transiciones válidas:</b></para>
    /// <list type="bullet">
    ///   <item><description>Pending → Confirmed, Cancelled</description></item>
    ///   <item><description>Confirmed → Processing, Cancelled</description></item>
    ///   <item><description>Processing → Shipped</description></item>
    ///   <item><description>Shipped → Delivered</description></item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// var resultado = await _pedidosService.UpdateEstadoAsync(pedidoId, "Confirmed");
    ///
    /// return resultado.Match(
    ///     pedido =&gt; {
    ///         if (nuevoEstado == "Shipped")
    ///             _emailService.EnviarNotificacionEnvio(pedido.UsuarioEmail, pedido.Numero);
    ///         return Ok(pedido);
    ///     },
    ///     error =&gt; BadRequest(error.Message)
    /// );
    /// </code>
    /// </example>
    Task<Result<PedidoDto, DomainError>> UpdateEstadoAsync(string id, string nuevoEstado);

    /// <summary>
    /// Actualiza un pedido (solo el propietario puede actualizar sus pedidos).
    /// Permite modificar dirección de envío, notas y estados permitidos.
    /// </summary>
    /// <param name="id">ID del pedido</param>
    /// <param name="userId">ID del usuario que realiza la modificación</param>
    /// <param name="dto">Campos a actualizar</param>
    /// <returns>
    /// <list type="bullet">
    ///   <item><term><c>Result.Success</c></term><description>Pedido actualizado</description></item>
    ///   <item><term><c>Result.Failure</c></term><description>NotFound, Forbidden (no es propietario), InvalidStateTransition</description></item>
    /// </list>
    /// </returns>
    /// <remarks>
    /// Solo se pueden modificar pedidos en estado Pending.
    /// </remarks>
    /// <example>
    /// <code>
    /// var updateDto = new UpdatePedidoDto
    /// {
    ///     DireccionEnvio = "Nueva Dirección 456",
    ///     Notas = "Cambiado a la tarde"
    /// };
    ///
    /// var resultado = await _pedidosService.UpdateAsync(pedidoId, userId, updateDto);
    /// return resultado.Match(Ok, error =&gt; {
    ///     if (error.Code == ErrorCodes.Forbidden)
    ///         return Forbid();
    ///     return BadRequest(error.Message);
    /// });
    /// </code>
    /// </example>
    Task<Result<PedidoDto, DomainError>> UpdateAsync(string id, long userId, UpdatePedidoDto dto);

    /// <summary>
    /// Elimina un pedido (solo el propietario o administradores).
    /// </summary>
    /// <param name="id">ID del pedido</param>
    /// <param name="userId">ID del usuario que solicita la eliminación</param>
    /// <returns>
    /// <list type="bullet">
    ///   <item><term><c>UnitResult.Success</c></term><description>Pedido eliminado correctamente</description></item>
    ///   <item><term><c>UnitResult.Failure</c></term><description>NotFound, Forbidden, o InvalidState (pedido ya avanzado)</description></item>
    /// </list>
    /// </returns>
    /// <remarks>
    /// Solo se pueden eliminar pedidos en estado Pending o Cancelled.
    /// La liberación de stock ocurre automáticamente al cancelar.
    /// </remarks>
    /// <example>
    /// <code>
    /// var resultado = await _pedidosService.DeleteAsync(pedidoId, userId);
    ///
    /// if (resultado.IsFailure)
    /// {
    ///     var error = resultado.Error;
    ///     if (error.Code == ErrorCodes.Forbidden)
    ///         return Forbid();
    ///     if (error.Code == "CANNOT_DELETE_ORDER")
    ///         return BadRequest("No se puede eliminar un pedido confirmado o enviado");
    ///     return NotFound();
    /// }
    ///
    /// return NoContent();
    /// </code>
    /// </example>
    Task<UnitResult<DomainError>> DeleteAsync(string id, long userId);
}

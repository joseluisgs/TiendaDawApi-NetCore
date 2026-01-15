namespace TiendaApi.Apis.Errors.Pedidos;

/// <summary>
/// Fábrica de errores específicos del dominio de pedidos.
/// 
/// <para>
/// Esta clase contiene métodos estáticos para crear errores relacionados
/// con operaciones sobre pedidos en la tienda.
/// </para>
/// 
/// <para>
/// <b>Casos de uso cubiertos:</b>
/// <list type="bullet">
///   <item><description>Pedido no encontrado por ID.</description></item>
///   <item><description>Producto dentro del pedido no encontrado.</description></item>
///   <item><description>Estado de pedido inválido para la transición.</description></item>
///   <item><description>Usuario sin permisos sobre el pedido.</description></item>
///   <item><description>Pedido ya adquirido por otro usuario (concurrencia).</description></item>
///   <item><description>Stock insuficiente para procesar el pedido.</description></item>
///   <item><description>Error inesperado al procesar el pedido.</description></item>
///   <item><description>Errores de validación de datos de pedido.</description></item>
/// </list>
/// </para>
/// 
/// <para>
/// <b>Ejemplo de uso en un servicio de pedidos:</b>
/// <code>
/// public async Task&lt;Result&gt; ActualizarEstadoAsync(string pedidoId, string nuevoEstado)
/// {
///     var pedido = await _repo.GetByIdAsync(pedidoId);
///     if (pedido == null)
///         return Result.Fail(PedidoError.NotFound(pedidoId));
///         
///     var estadosPermitidos = new[] { "Pendiente", "EnProceso", "Enviado", "Entregado" };
///     if (!estadosPermitidos.Contains(nuevoEstado))
///         return Result.Fail(PedidoError.EstadoInvalido(nuevoEstado, estadosPermitidos));
///         
///     pedido.ActualizarEstado(nuevoEstado);
///     await _repo.UpdateAsync(pedido);
///     return Result.Ok();
/// }
/// </code>
/// </para>
/// </summary>
public static class PedidoError
{
    /// <summary>
    /// Crea un error de tipo "no encontrado" para un pedido inexistente.
    /// 
    /// <para>
    /// Se usa cuando se intenta acceder, actualizar o eliminar un pedido
    /// que no existe en la base de datos.
    /// </para>
    /// </summary>
    /// <param name="id">Identificador del pedido que no fue encontrado.</param>
    /// <returns>NotFoundError con mensaje formateado para pedidos.</returns>
    /// <example>
    /// return PedidoError.NotFound("PED-12345");
    /// // Genera: "Pedido con ID PED-12345 no encontrado"
    /// </example>
    public static NotFoundError NotFound(string id) =>
        new($"Pedido con ID {id} no encontrado");

    /// <summary>
    /// Crea un error de tipo "no encontrado" para un producto dentro de un pedido.
    /// 
    /// <para>
    /// Se usa cuando se intenta agregar o modificar un producto en un pedido
    /// pero el producto no existe en el catálogo.
    /// </para>
    /// </summary>
    /// <param name="productoId">Identificador del producto que no fue encontrado.</param>
    /// <returns>NotFoundError indicando que el producto no existe.</returns>
    /// <example>
    /// return PedidoError.ProductoNoEncontrado(999);
    /// // Genera: "Recurso con ID 999 no encontrado"
    /// </example>
    public static NotFoundError ProductoNoEncontrado(long productoId) =>
        NotFoundError.FromId(productoId, "Producto");

    /// <summary>
    /// Crea un error de validación cuando se intenta establecer un estado inválido.
    /// 
    /// <para>
    /// Los pedidos tienen un flujo de estados definido (máquina de estados).
    /// No todas las transiciones son válidas (ej: de "Entregado" a "Pendiente").
    /// </para>
    /// 
    /// <para>
    /// <b>Flujo de estados típico:</b>
    /// <list type="number">
///     <item><description>Pendiente → EnProceso → Enviado → Entregado</description></item>
///     <item><description>Cancelado puede venir de cualquier estado (depende del negocio).</description></item>
///   </list>
///   </para>
///   </summary>
///   <param name="estado">Estado inválido que se intentó establecer.</param>
///   <param name="estadosPermitidos">Array de estados válidos disponibles.</param>
///   <returns>ValidationError indicando el estado inválido y los válidos.</returns>
///   <example>
///   var estadosValidos = new[] { "Pendiente", "EnProceso", "Enviado", "Entregado", "Cancelado" };
///   return PedidoError.EstadoInvalido("Completado", estadosValidos);
///   // Genera: "Estado inválido 'Completado'. Valores permitidos: Pendiente, EnProceso, Enviado, Entregado, Cancelado"
///   </example>
public static ValidationError EstadoInvalido(string estado, string[] estadosPermitidos) =>
    new($"Estado inválido '{estado}'. Valores permitidos: {string.Join(", ", estadosPermitidos)}", new Dictionary<string, string[]>());

/// <summary>
/// Crea un error de autorización cuando el usuario no es propietario del pedido.
/// 
/// <para>
/// Se usa para verificar que el usuario que intenta acceder o modificar
/// un pedido sea realmente el dueño de dicho pedido.
/// </para>
/// </summary>
/// <param name="usuarioId">ID del usuario que intenta acceder al pedido.</param>
/// <param name="pedidoId">ID del pedido al que se intenta acceder.</param>
/// <returns>ForbiddenError indicando que no es propietario.</returns>
/// <example>
/// return PedidoError.NoPropietario(42, "PED-12345");
/// // Genera: "No tienes permisos para acceder a este pedido (ID: 12345)"
/// </example>
public static ForbiddenError NoPropietario(long usuarioId, string pedidoId) =>
    ForbiddenError.NotOwner("pedido", long.Parse(pedidoId));

/// <summary>
/// Crea un error de conflicto cuando el pedido ya fue adquirido por otro usuario.
/// 
/// <para>
/// Se usa en escenarios de concurrencia donde múltiples usuarios vendedores
/// intentan aceptar o modificar el mismo pedido simultáneamente.
/// </para>
/// </summary>
/// <param name="pedidoId">ID del pedido que ya fue adquirido.</param>
/// <returns>ConflictError indicando que el pedido no está disponible.</returns>
/// <example>
/// return PedidoError.PedidoAdquirido("PED-12345");
/// // Genera: "El pedido fue adquirido por otro usuario. Por favor, reintente la operación."
/// </example>
public static ConflictError PedidoAdquirido(string pedidoId) =>
    new("El pedido fue adquirido por otro usuario. Por favor, reintente la operación.");

/// <summary>
/// Crea un error de regla de negocio cuando el stock es insuficiente para procesar.
/// 
/// <para>
/// Se usa cuando se intenta procesar un pedido pero uno o más productos
/// no tienen suficiente stock para completar la orden.
/// </para>
/// </summary>
/// <param name="nombreProducto">Nombre del producto con stock insuficiente.</param>
/// <param name="disponible">Cantidad actual en stock.</param>
/// <param name="solicitado">Cantidad solicitada en el pedido.</param>
/// <returns>BusinessRuleError con detalles del conflicto de stock.</returns>
/// <example>
/// return PedidoError.StockInsuficiente("Camiseta Roja", 3, 5);
/// // Genera: "Stock insuficiente para el producto 'Camiseta Roja'. Disponible: 3, Solicitado: 5"
/// </example>
public static BusinessRuleError StockInsuficiente(string nombreProducto, int disponible, int solicitado) =>
    new($"Stock insuficiente para el producto '{nombreProducto}'. Disponible: {disponible}, Solicitado: {solicitado}");

/// <summary>
/// Crea un error interno inesperado al procesar un pedido.
/// 
/// <para>
/// Se usa para capturar errores inesperados que no deberían ocurrir en
/// operación normal pero pueden ocurrir por fallos externos.
/// </para>
/// 
/// <para>
/// <b>Ejemplos de uso:</b>
/// <list type="bullet">
///   <item><description>Fallo de conexión con la pasarela de pago.</description></item>
///   <item><description>Error de base de datos inesperado.</description></item>
///   <item><description>Fallo de servicio externo (email, notificaciones).</description></item>
/// </list>
/// </para>
/// </summary>
/// <returns>InternalError con mensaje genérico de error al procesar.</returns>
/// <example>
/// return PedidoError.ErrorProcesando();
/// // Genera: "Error inesperado al procesar el pedido"
/// </example>
public static InternalError ErrorProcesando() =>
    new("Error inesperado al procesar el pedido");

/// <summary>
/// Crea un error de validación simple para operaciones sobre pedidos.
/// 
/// <para>
/// Útil cuando se necesita reportar un error de validación sin detalles
/// específicos por campo, solo un mensaje general.
/// </para>
/// </summary>
/// <param name="mensaje">Descripción del error de validación.</param>
/// <returns>ValidationError con diccionario vacío de detalles por campo.</returns>
/// <example>
/// return PedidoError.Validacion("La fecha de entrega no puede ser anterior a hoy");
/// </example>
public static ValidationError Validacion(string mensaje) =>
    new(mensaje, new Dictionary<string, string[]>());

/// <summary>
/// Crea un error de validación con detalles específicos por campo.
/// 
/// <para>
/// Se usa cuando la validación de datos de pedido genera múltiples
/// errores en diferentes campos del modelo.
/// </para>
/// </summary>
/// <param name="errores">
/// Diccionario donde la clave es el nombre del campo y el valor es un array
/// de mensajes de error para ese campo.
/// </param>
/// <returns>ValidationError con todos los errores por campo.</returns>
/// <example>
/// var errores = new Dictionary&lt;string, string[]&gt;
/// {
///     { "DireccionEnvio", new[] { "La dirección es obligatoria", "Máximo 200 caracteres" } },
///     { "MetodoPago", new[] { "Debe seleccionar un método de pago válido" } },
///     { "Items", new[] { "El pedido debe tener al menos un producto" } }
/// };
/// return PedidoError.ValidacionConCampos(errores);
/// </example>
public static ValidationError ValidacionConCampos(Dictionary<string, string[]> errores) =>
    ValidationError.WithFieldErrors(errores);
}

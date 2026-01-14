namespace TiendaApi.Apis.Errors.Pedidos;

/// <summary>
/// Errores específicos del dominio de pedidos.
/// </summary>
public static class PedidoError
{
    /// <summary>
    /// Pedido no encontrado por ID.
    /// </summary>
    public static NotFoundError NotFound(string id) =>
        new($"Pedido con ID {id} no encontrado");

    /// <summary>
    /// Producto no encontrado dentro de un pedido.
    /// </summary>
    public static NotFoundError ProductoNoEncontrado(long productoId) =>
        NotFoundError.FromId(productoId, "Producto");

    /// <summary>
    /// Estado de pedido inválido.
    /// </summary>
    public static ValidationError EstadoInvalido(string estado, string[] estadosPermitidos) =>
        new($"Estado inválido '{estado}'. Valores permitidos: {string.Join(", ", estadosPermitidos)}", new Dictionary<string, string[]>()); // new Dictionary<string, string[]>() = diccionario vacío

    /// <summary>
    /// No tienes permisos sobre este pedido.
    /// </summary>
    public static ForbiddenError NoPropietario(long usuarioId, string pedidoId) =>
        ForbiddenError.NotOwner("pedido", long.Parse(pedidoId));

    /// <summary>
    /// El pedido ya fue adquirido por otro usuario.
    /// </summary>
    public static ConflictError PedidoAdquirido(string pedidoId) =>
        new("El pedido fue adquirido por otro usuario. Por favor, reintente la operación.");

    /// <summary>
    /// Stock insuficiente para procesar el pedido.
    /// </summary>
    public static BusinessRuleError StockInsuficiente(string nombreProducto, int disponible, int solicitado) =>
        new($"Stock insuficiente para el producto '{nombreProducto}'. Disponible: {disponible}, Solicitado: {solicitado}");

    /// <summary>
    /// Error inesperado al procesar el pedido.
    /// </summary>
    public static InternalError ErrorProcesando() =>
        new("Error inesperado al procesar el pedido");

    /// <summary>
    /// Error de validación al procesar pedido.
    /// </summary>
    public static ValidationError Validacion(string mensaje) =>
        new(mensaje, new Dictionary<string, string[]>()); // new Dictionary<string, string[]>() = diccionario vacío

    /// <summary>
    /// Error de validación con errores por campo.
    /// </summary>
    public static ValidationError ValidacionConCampos(Dictionary<string, string[]> errores) =>
        ValidationError.WithFieldErrors(errores);
}

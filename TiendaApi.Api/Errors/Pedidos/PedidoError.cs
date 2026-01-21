namespace TiendaApi.Api.Errors.Pedidos;

/// <summary>
/// Errores del dominio de pedidos (HTTP 404, 409, 400, 403).
/// </summary>
public static class PedidoError
{
    /// <summary>Crea error para pedido no encontrado.</summary>
    /// <param name="id">ID del pedido.</param>
    /// <returns>NotFoundError (HTTP 404).</returns>
    public static NotFoundError NotFound(string id) =>
        new($"Pedido con ID {id} no encontrado");

    /// <summary>Crea error para producto en pedido no encontrado.</summary>
    /// <param name="productoId">ID del producto.</param>
    /// <returns>NotFoundError (HTTP 404).</returns>
    public static NotFoundError ProductoNoEncontrado(long productoId) =>
        NotFoundError.FromId(productoId, "Producto");

    /// <summary>Crea error para estado de pedido inválido.</summary>
    /// <param name="estado">Estado inválido.</param>
    /// <param name="estadosPermitidos">Array de estados válidos.</param>
    /// <returns>ValidationError (HTTP 400).</returns>
    public static ValidationError EstadoInvalido(string estado, string[] estadosPermitidos) =>
        new($"Estado inválido '{estado}'. Valores permitidos: {string.Join(", ", estadosPermitidos)}", new Dictionary<string, string[]>());

    /// <summary>Crea error si el usuario no es propietario del pedido.</summary>
    /// <param name="usuarioId">ID del usuario.</param>
    /// <param name="pedidoId">ID del pedido.</param>
    /// <returns>ForbiddenError (HTTP 403).</returns>
    public static ForbiddenError NoPropietario(long usuarioId, string pedidoId) =>
        ForbiddenError.NotOwner("pedido", pedidoId);

    /// <summary>Crea error si el pedido fue adquirido por otro.</summary>
    /// <param name="pedidoId">ID del pedido.</param>
    /// <returns>ConflictError (HTTP 409).</returns>
    public static ConflictError PedidoAdquirido(string pedidoId) =>
        new("El pedido fue adquirido por otro usuario. Por favor, reintente la operación.");

    /// <summary>Crea error para stock insuficiente.</summary>
    /// <param name="nombreProducto">Nombre del producto.</param>
    /// <param name="disponible">Stock disponible.</param>
    /// <param name="solicitado">Stock solicitado.</param>
    /// <returns>BusinessRuleError (HTTP 400).</returns>
    public static BusinessRuleError StockInsuficiente(string nombreProducto, int disponible, int solicitado) =>
        new($"Stock insuficiente para el producto '{nombreProducto}'. Disponible: {disponible}, Solicitado: {solicitado}");

    /// <summary>Crea error interno al procesar pedido.</summary>
    /// <returns>InternalError (HTTP 500).</returns>
    public static InternalError ErrorProcesando() =>
        new("Error inesperado al procesar el pedido");

    /// <summary>Crea error de validación simple.</summary>
    /// <param name="mensaje">Mensaje de error.</param>
    /// <returns>ValidationError (HTTP 400).</returns>
    public static ValidationError Validacion(string mensaje) =>
        ValidationError.Create(mensaje);

    /// <summary>Crea error de validación con detalles por campo.</summary>
    /// <param name="errores">Diccionario de errores por campo.</param>
    /// <returns>ValidationError (HTTP 400).</returns>
    public static ValidationError ValidacionConCampos(Dictionary<string, string[]> errores) =>
        ValidationError.WithFieldErrors(errores);
}

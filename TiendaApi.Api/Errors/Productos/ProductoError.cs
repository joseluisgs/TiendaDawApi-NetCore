namespace TiendaApi.Api.Errors.Productos;

/// <summary>
/// Errores del dominio de productos (HTTP 404, 409, 400).
/// </summary>
public static class ProductoError
{
    /// <summary>Crea error para producto no encontrado.</summary>
    /// <param name="id">ID del producto.</param>
    /// <returns>NotFoundError (HTTP 404).</returns>
    public static NotFoundError NotFound(long id) =>
        NotFoundError.FromId(id, "Producto");

    /// <summary>Crea error para categoría no encontrada.</summary>
    /// <param name="categoriaId">ID de la categoría.</param>
    /// <returns>NotFoundError (HTTP 404).</returns>
    public static NotFoundError CategoriaNoEncontrada(long categoriaId) =>
        NotFoundError.FromId(categoriaId, "Categoria");

    /// <summary>Crea error para stock insuficiente.</summary>
    /// <param name="nombre">Nombre del producto.</param>
    /// <param name="disponible">Stock disponible.</param>
    /// <param name="solicitado">Stock solicitado.</param>
    /// <returns>BusinessRuleError (HTTP 400).</returns>
    public static BusinessRuleError StockInsuficiente(string nombre, int disponible, int solicitado) =>
        new($"Stock insuficiente para el producto '{nombre}'. Disponible: {disponible}, Solicitado: {solicitado}");

    /// <summary>Crea error si el producto tiene pedidos asociados.</summary>
    /// <param name="id">ID del producto.</param>
    /// <returns>BusinessRuleError (HTTP 400).</returns>
    public static BusinessRuleError NoSePuedeEliminarConPedidos(long id) =>
        new($"No se puede eliminar el producto con ID {id} porque tiene pedidos asociados");

    /// <summary>Crea error si el producto fue adquirido por otro.</summary>
    /// <param name="productoId">ID del producto.</param>
    /// <returns>ConflictError (HTTP 409).</returns>
    public static ConflictError ProductoAdquirido(long productoId) =>
        new("El producto fue adquirido por otro usuario. Por favor, reintente la operación.");

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

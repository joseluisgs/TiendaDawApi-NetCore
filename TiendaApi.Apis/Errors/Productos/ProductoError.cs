namespace TiendaApi.Apis.Errors.Productos;

/// <summary>
/// Errores específicos del dominio de productos.
/// </summary>
public static class ProductoError
{
    /// <summary>
    /// Producto no encontrado por ID.
    /// </summary>
    public static NotFoundError NotFound(long id) =>
        NotFoundError.FromId(id, "Producto");

    /// <summary>
    /// Categoría no encontrada al crear/actualizar producto.
    /// </summary>
    public static NotFoundError CategoriaNoEncontrada(long categoriaId) =>
        NotFoundError.FromId(categoriaId, "Categoria");

    /// <summary>
    /// Stock insuficiente para una operación.
    /// </summary>
    public static BusinessRuleError StockInsuficiente(string nombre, int disponible, int solicitado) =>
        new($"Stock insuficiente para el producto '{nombre}'. Disponible: {disponible}, Solicitado: {solicitado}");

    /// <summary>
    /// No se puede eliminar un producto con pedidos asociados.
    /// </summary>
    public static BusinessRuleError NoSePuedeEliminarConPedidos(long id) =>
        new($"No se puede eliminar el producto con ID {id} porque tiene pedidos asociados");

    /// <summary>
    /// El producto ya fue adquirido por otro usuario.
    /// </summary>
    public static ConflictError ProductoAdquirido(long productoId) =>
        new($"El producto fue adquirido por otro usuario. Por favor, reintente la operación.");

    /// <summary>
    /// Error de validación al procesar producto.
    /// </summary>
    public static ValidationError Validacion(string mensaje) =>
        new(mensaje, new Dictionary<string, string[]>()); // new Dictionary<string, string[]>() = diccionario vacío

    /// <summary>
    /// Error de validación con errores por campo.
    /// </summary>
    public static ValidationError ValidacionConCampos(Dictionary<string, string[]> errores) =>
        ValidationError.WithFieldErrors(errores);
}

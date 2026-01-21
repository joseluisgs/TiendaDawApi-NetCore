namespace TiendaApi.Api.Errors.Categorias;

/// <summary>
/// Errores del dominio de categorías (HTTP 404, 409, 400).
/// </summary>
public static class CategoriaError
{
    /// <summary>Crea error para categoría no encontrada.</summary>
    /// <param name="id">ID de la categoría.</param>
    /// <returns>NotFoundError (HTTP 404).</returns>
    public static NotFoundError NotFound(long id) =>
        NotFoundError.FromId(id, "Categoria");

    /// <summary>Crea error para nombre de categoría duplicado.</summary>
    /// <param name="nombre">Nombre duplicado.</param>
    /// <returns>ConflictError (HTTP 409).</returns>
    public static ConflictError NombreDuplicado(string nombre) =>
        ConflictError.Duplicate("categoria", nombre);

    /// <summary>Crea error si la categoría tiene productos asociados.</summary>
    /// <param name="id">ID de la categoría.</param>
    /// <param name="productosCount">Número de productos asociados.</param>
    /// <returns>BusinessRuleError (HTTP 400).</returns>
    public static BusinessRuleError TieneProductos(long id, int productosCount) =>
        new($"No se puede eliminar la categoría con ID {id} porque tiene {productosCount} productos asociados");

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

namespace TiendaApi.Apis.Errors.Categorias;

/// <summary>
/// Errores específicos del dominio de categorías.
/// </summary>
public static class CategoriaError
{
    /// <summary>
    /// Categoría no encontrada por ID.
    /// </summary>
    public static NotFoundError NotFound(long id) =>
        NotFoundError.FromId(id, "Categoria");

    /// <summary>
    /// Ya existe una categoría con ese nombre.
    /// </summary>
    public static ConflictError NombreDuplicado(string nombre) =>
        ConflictError.Duplicate("categoria", nombre);

    /// <summary>
    /// No se puede eliminar una categoría con productos asociados.
    /// </summary>
    public static BusinessRuleError TieneProductos(long id, int productosCount) =>
        new($"No se puede eliminar la categoría con ID {id} porque tiene {productosCount} productos asociados");

    /// <summary>
    /// Error de validación al procesar categoría.
    /// </summary>
    public static ValidationError Validacion(string mensaje) =>
        new(mensaje, new Dictionary<string, string[]>()); // new Dictionary<string, string[]>() = diccionario vacío

    /// <summary>
    /// Error de validación con errores por campo.
    /// </summary>
    public static ValidationError ValidacionConCampos(Dictionary<string, string[]> errores) =>
        ValidationError.WithFieldErrors(errores);
}

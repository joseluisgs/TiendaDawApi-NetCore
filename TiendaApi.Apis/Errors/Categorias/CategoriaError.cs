namespace TiendaApi.Apis.Errors.Categorias;

/// <summary>
/// Fábrica de errores específicos del dominio de categorías.
/// 
/// <para>
/// Esta clase contiene métodos estáticos para crear errores relacionados
/// con operaciones sobre categorías en la tienda.
/// </para>
/// 
/// <para>
/// <b>Casos de uso cubiertos:</b>
/// <list type="bullet">
///   <item><description>Categoría no encontrada al buscar por ID.</description></item>
///   <item><description>Conflicto por nombre de categoría duplicado.</description></item>
///   <item><description>Error de negocio al intentar eliminar categoría con productos.</description></item>
///   <item><description>Errores de validación de datos de categoría.</description></item>
/// </list>
/// </para>
/// 
/// <para>
/// <b>Ejemplo de uso en un servicio:</b>
/// <code>
/// public async Task&lt;Result&gt; EliminarCategoriaAsync(long id)
/// {
///     var categoria = await _repo.GetByIdAsync(id);
///     if (categoria == null)
///         return Result.Fail(CategoriaError.NotFound(id));
///         
///     if (categoria.Productos.Any())
///         return Result.Fail(CategoriaError.TieneProductos(id, categoria.Productos.Count));
///         
///     await _repo.DeleteAsync(id);
///     return Result.Ok();
/// }
/// </code>
/// </para>
/// </summary>
public static class CategoriaError
{
    /// <summary>
    /// Crea un error de tipo "no encontrado" para una categoría inexistente.
    /// 
    /// <para>
    /// Se usa cuando se intenta acceder, actualizar o eliminar una categoría
    /// que no existe en la base de datos.
    /// </para>
    /// </summary>
    /// <param name="id">Identificador de la categoría que no fue encontrada.</param>
    /// <returns>NotFoundError con mensaje formateado para categorías.</returns>
    /// <example>
    /// return CategoriaError.NotFound(42);
    /// // Genera: "Recurso con ID 42 no encontrado"
    /// </example>
    public static NotFoundError NotFound(long id) =>
        NotFoundError.FromId(id, "Categoria");

    /// <summary>
    /// Crea un error de conflicto cuando ya existe una categoría con el mismo nombre.
    /// 
    /// <para>
    /// Se usa durante la creación o actualización de categorías para garantizar
    /// que los nombres sean únicos en el sistema.
    /// </para>
    /// </summary>
    /// <param name="nombre">Nombre de la categoría que generó el conflicto.</param>
    /// <returns>ConflictError indicando duplicado de nombre.</returns>
    /// <example>
    /// return CategoriaError.NombreDuplicado("Electrónica");
    /// // Genera: "Ya existe un categoria con el valor 'Electrónica'"
    /// </example>
    public static ConflictError NombreDuplicado(string nombre) =>
        ConflictError.Duplicate("categoria", nombre);

    /// <summary>
    /// Crea un error de regla de negocio al intentar eliminar una categoría con productos asociados.
    /// 
    /// <para>
    /// Las categorías no se pueden eliminar si tienen productos vinculados
    /// para mantener la integridad referencial y evitar datos huérfanos.
    /// </para>
    /// 
    /// <para>
    /// <b>Flujo típico:</b>
    /// <list type="number">
    ///   <item><description>Usuario intenta eliminar categoría.</description></item>
    ///   <item><description>Servicio verifica si tiene productos asociados.</description></item>
    ///   <item><description>Si tiene productos, retorna este error.</description></item>
    /// </list>
    /// </para>
    /// </summary>
    /// <param name="id">ID de la categoría que no se puede eliminar.</param>
    /// <param name="productosCount">Número de productos asociados a la categoría.</param>
    /// <returns>BusinessRuleError con explicación del conflicto.</returns>
    /// <example>
    /// return CategoriaError.TieneProductos(5, 12);
    /// // Genera: "No se puede eliminar la categoría con ID 5 porque tiene 12 productos asociados"
    /// </example>
    public static BusinessRuleError TieneProductos(long id, int productosCount) =>
        new($"No se puede eliminar la categoría con ID {id} porque tiene {productosCount} productos asociados");

    /// <summary>
    /// Crea un error de validación simple para operaciones sobre categorías.
    /// 
    /// <para>
    /// Útil cuando se necesita reportar un error de validación sin detalles
    /// específicos por campo, solo un mensaje general.
    /// </para>
    /// </summary>
    /// <param name="mensaje">Descripción del error de validación.</param>
    /// <returns>ValidationError con diccionario vacío de detalles por campo.</returns>
    /// <example>
    /// return CategoriaError.Validacion("El nombre de categoría no puede estar vacío");
    /// </example>
    public static ValidationError Validacion(string mensaje) =>
        new(mensaje, new Dictionary<string, string[]>());

    /// <summary>
    /// Crea un error de validación con detalles específicos por campo.
    /// 
    /// <para>
    /// Se usa cuando la validación de datos de categoría genera múltiples
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
    ///     { "Nombre", new[] { "El nombre es obligatorio", "Máximo 50 caracteres" } },
    ///     { "Descripcion", new[] { "La descripción no puede exceder 200 caracteres" } }
    /// };
    /// return CategoriaError.ValidacionConCampos(errores);
    /// </example>
    public static ValidationError ValidacionConCampos(Dictionary<string, string[]> errores) =>
        ValidationError.WithFieldErrors(errores);
}

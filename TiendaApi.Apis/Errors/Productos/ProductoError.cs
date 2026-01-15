namespace TiendaApi.Apis.Errors.Productos;

/// <summary>
/// Fábrica de errores específicos del dominio de productos.
/// 
/// <para>
/// Esta clase contiene métodos estáticos para crear errores relacionados
/// con operaciones sobre productos en la tienda.
/// </para>
/// 
/// <para>
/// <b>Casos de uso cubiertos:</b>
/// <list type="bullet">
///   <item><description>Producto no encontrado por ID.</description></item>
///   <item><description>Categoría referenciada no existe.</description></item>
///   <item><description>Stock insuficiente para la operación.</description></item>
///   <item><description>Producto tiene pedidos asociados (no se puede eliminar).</description></item>
///   <item><description>Conflicto por producto ya adquirido.</description></item>
///   <item><description>Errores de validación de datos de producto.</description></item>
/// </list>
/// </para>
/// 
/// <para>
/// <b>Ejemplo de uso en un servicio:</b>
/// <code>
/// public async Task&lt;Result&gt; DisminuirStockAsync(long productoId, int cantidad)
/// {
///     var producto = await _repo.GetByIdAsync(productoId);
///     if (producto == null)
///         return Result.Fail(ProductoError.NotFound(productoId));
///         
///     if (producto.Stock &lt; cantidad)
///         return Result.Fail(ProductoError.StockInsuficiente(
///             producto.Nombre, producto.Stock, cantidad));
///         
///     producto.DisminuirStock(cantidad);
///     await _repo.UpdateAsync(producto);
///     return Result.Ok();
/// }
/// </code>
/// </para>
/// </summary>
public static class ProductoError
{
    /// <summary>
    /// Crea un error de tipo "no encontrado" para un producto inexistente.
    /// 
    /// <para>
    /// Se usa cuando se intenta acceder, actualizar o eliminar un producto
    /// que no existe en la base de datos.
    /// </para>
    /// </summary>
    /// <param name="id">Identificador del producto que no fue encontrado.</param>
    /// <returns>NotFoundError con mensaje formateado para productos.</returns>
    /// <example>
    /// return ProductoError.NotFound(123);
    /// // Genera: "Recurso con ID 123 no encontrado"
    /// </example>
    public static NotFoundError NotFound(long id) =>
        NotFoundError.FromId(id, "Producto");

    /// <summary>
    /// Crea un error de tipo "no encontrado" cuando la categoría de un producto no existe.
    /// 
    /// <para>
    /// Se usa durante la creación o actualización de productos para verificar
    /// que la categoría referenciada sea válida y exista en el sistema.
    /// </para>
    /// </summary>
    /// <param name="categoriaId">Identificador de la categoría que no fue encontrada.</param>
    /// <returns>NotFoundError indicando que la categoría no existe.</returns>
    /// <example>
    /// return ProductoError.CategoriaNoEncontrada(99);
    /// // Genera: "Recurso con ID 99 no encontrado"
    /// </example>
    public static NotFoundError CategoriaNoEncontrada(long categoriaId) =>
        NotFoundError.FromId(categoriaId, "Categoria");

    /// <summary>
    /// Crea un error de regla de negocio cuando el stock disponible es insuficiente.
    /// 
    /// <para>
    /// Se usa en operaciones que requieren reducir el stock de un producto
    /// (ventas, transferencias, etc.) y la cantidad solicitada excede lo disponible.
    /// </para>
    /// 
    /// <para>
    /// <b>Escenarios comunes:</b>
    /// <list type="bullet">
///     <item><description>Procesar un pedido con más unidades de las disponibles.</description></item>
///     <item><description>Transferir stock a otra bodega excediendo el disponible.</description></item>
///     <item><description>Actualizar inventario después de una venta fallida.</description></item>
///   </list>
///   </para>
///   </summary>
///   <param name="nombre">Nombre del producto con stock insuficiente.</param>
///   <param name="disponible">Cantidad actual en stock.</param>
///   <param name="solicitado">Cantidad que se intentó utilizar/vender.</param>
///   <returns>BusinessRuleError con detalles del conflicto de stock.</returns>
///   <example>
///   return ProductoError.StockInsuficiente("Laptop Dell", 5, 10);
///   // Genera: "Stock insuficiente para el producto 'Laptop Dell'. Disponible: 5, Solicitado: 10"
///   </example>
public static BusinessRuleError StockInsuficiente(string nombre, int disponible, int solicitado) =>
    new($"Stock insuficiente para el producto '{nombre}'. Disponible: {disponible}, Solicitado: {solicitado}");

/// <summary>
/// Crea un error de regla de negocio al intentar eliminar un producto con pedidos asociados.
/// 
/// <para>
/// Los productos no se pueden eliminar si tienen pedidos que los referencian
/// para mantener la trazabilidad del historial de compras.
/// </para>
/// </summary>
/// <param name="id">ID del producto que no se puede eliminar.</param>
/// <returns>BusinessRuleError indicando que el producto tiene pedidos.</returns>
/// <example>
/// return ProductoError.NoSePuedeEliminarConPedidos(456);
/// // Genera: "No se puede eliminar el producto con ID 456 porque tiene pedidos asociados"
/// </example>
public static BusinessRuleError NoSePuedeEliminarConPedidos(long id) =>
    new($"No se puede eliminar el producto con ID {id} porque tiene pedidos asociados");

/// <summary>
/// Crea un error de conflicto cuando el producto ya fue adquirido por otro usuario.
/// 
/// <para>
/// Se usa en escenarios de concurrencia donde múltiples usuarios intentan
/// comprar el mismo producto simultáneamente y uno de ellos lo consigue primero.
/// </para>
/// </summary>
/// <param name="productoId">ID del producto que ya fue adquirido.</param>
/// <returns>ConflictError indicando que el producto no está disponible.</returns>
/// <example>
/// return ProductoError.ProductoAdquirido(789);
/// // Genera: "El producto fue adquirido por otro usuario. Por favor, reintente la operación."
/// </example>
public static ConflictError ProductoAdquirido(long productoId) =>
    new($"El producto fue adquirido por otro usuario. Por favor, reintente la operación.");

/// <summary>
/// Crea un error de validación simple para operaciones sobre productos.
/// 
/// <para>
/// Útil cuando se necesita reportar un error de validación sin detalles
/// específicos por campo, solo un mensaje general.
/// </para>
/// </summary>
/// <param name="mensaje">Descripción del error de validación.</param>
/// <returns>ValidationError con diccionario vacío de detalles por campo.</returns>
/// <example>
/// return ProductoError.Validacion("El precio del producto debe ser mayor a cero");
/// </example>
public static ValidationError Validacion(string mensaje) =>
    new(mensaje, new Dictionary<string, string[]>());

/// <summary>
/// Crea un error de validación con detalles específicos por campo.
/// 
/// <para>
/// Se usa cuando la validación de datos de producto genera múltiples
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
///     { "Nombre", new[] { "El nombre es obligatorio", "Máximo 100 caracteres" } },
///     { "Precio", new[] { "El precio debe ser mayor a 0" } },
///     { "CategoriaId", new[] { "Debe seleccionar una categoría válida" } }
/// };
/// return ProductoError.ValidacionConCampos(errores);
/// </example>
public static ValidationError ValidacionConCampos(Dictionary<string, string[]> errores) =>
    ValidationError.WithFieldErrors(errores);
}

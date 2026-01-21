using System.Data;
using TiendaApi.Apis.Dtos.Productos;
using TiendaApi.Apis.Models;

namespace TiendaApi.Apis.Repositories.Productos;

/// <summary>
/// Define el contrato para el repositorio de productos.
/// 
/// El patrón Repository para productos implementa la abstracción del acceso a datos
/// específica para la entidad Producto. Este repositorio encapsula todas las operaciones
/// relacionadas con el catálogo de productos de la tienda.
/// 
/// Principios aplicados:
/// 
/// 1. **Encapsulamiento**: Toda la lógica de acceso a datos de productos está contenida
///    aquí, aislando los detalles de implementación del resto de la aplicación.
/// 
/// 2. **Consistencia**: Las operaciones de productos siguen un contrato coherente,
///    facilitando el mantenimiento y la evolución del código.
/// 
/// 3. **Transaccionalidad**: Operaciones complejas como decremento de stock utilizan
///    transacciones explícitas para garantizar la integridad de los datos.
/// 
/// 4. **Optimización de concurrencia**: El método DecrementStockAsync implementa
///    control de concurrencia optimista usando byte[] RowVersion.
/// 
/// Este repositorio es responsable de mantener la integridad del catálogo de productos,
/// incluyendo la gestión del stock, la relación con categorías, y la consistencia
/// durante operaciones concurrentes.
/// </summary>
public interface IProductoRepository
{
    /// <summary>
    /// Recupera todos los productos de la base de datos ordenados por nombre.
    /// 
    /// <remarks>
    /// Este método carga todos los productos en memoria. Para conjuntos de datos
    /// grandes, es preferible usar paginación o consultas filtradas.
    /// 
    /// El ordenamiento por nombre proporciona una presentación consistente y
    /// facilita la navegación para los usuarios.
    /// 
    /// Consideraciones de rendimiento:
    /// - Las relaciones (categoría, etc.) no se cargan automáticamente (lazy loading).
    /// - Use Include() en el DbContext si necesita datos relacionados.
    /// - Para solo lectura, use FindAllAsNoTracking() para mejor rendimiento.
    /// </remarks>
    /// 
    /// <example>
    /// <code>
    /// // Obtener todos los productos
    /// var productos = await _productoRepository.FindAllAsync();
    /// var productosElectronica = productos.Where(p => p.Categoria?.Nombre == "Electrónica");
    /// </code>
    /// </example>
    /// 
    /// <returns>Colección enumerable de todos los productos ordenados por nombre.</returns>
    Task<IEnumerable<Producto>> FindAllAsync();

    /// <summary>
    /// Recupera productos de forma paginada con soporte para filtros avanzados.
    /// 
    /// <remarks>
    /// La paginación es crítica para el rendimiento en catálogos de productos grandes.
    /// El filtro ProductoFilterDto permite:
    /// 
    /// - Búsqueda por nombre o descripción (búsqueda full-text).
    /// - Filtrado por rango de precios.
    /// - Filtrado por categoría.
    /// - Filtrado por disponibilidad (stock > 0).
    /// - Ordenación por precio, nombre, fecha de creación.
    /// 
    /// El retorno incluye el total de registros para calcular UI de paginación.
    /// </remarks>
    /// 
    /// <example>
    /// <code>
    /// // Catálogo paginado con filtros
    /// var filter = new ProductoFilterDto
    /// {
    ///     Page = 0,
    ///     Size = 20,
    ///     CategoriaId = 1,
    ///     PrecioMin = 10,
    ///     PrecioMax = 100,
    ///     OnlyAvailable = true,
    ///     SortBy = "Precio",
    ///     SortDescending = false
    /// };
    /// 
    /// var (items, total) = await _productoRepository.FindAllPagedAsync(filter);
    /// </code>
    /// </example>
    /// 
    /// <param name="filter">Objeto con criterios de filtrado, paginación y ordenación.</param>
    /// <returns>Tupla con productos de la página y total de registros coincidentes.</returns>
    Task<(IEnumerable<Producto> Items, int TotalCount)> FindAllPagedAsync(ProductoFilterDto filter);

    /// <summary>
    /// Busca un producto específico por su identificador único.
    /// 
    /// <remarks>
    /// Este método es fundamental para operaciones de visualización de detalles,
    /// edición y gestión de stock. La búsqueda por ID es altamente optimizada.
    /// 
    /// Si el producto tiene relaciones configuradas (como Categoria), estas
    /// no se cargan automáticamente. Use el DbContext con Include() si las necesita.
    /// </remarks>
    /// 
    /// <example>
    /// <code>
    /// // Ver detalles de producto
    /// var producto = await _productoRepository.FindByIdAsync(123);
    /// if (producto == null)
    /// {
    ///     return NotFound();
    /// }
    /// return View(producto);
    /// </code>
    /// </example>
    /// 
    /// <param name="id">Identificador único del producto (clave primaria).</param>
    /// <returns>El producto encontrado o null si no existe.</returns>
    Task<Producto?> FindByIdAsync(long id);

    /// <summary>
    /// Recupera todos los productos pertenecientes a una categoría específica.
    /// 
    /// <remarks>
    /// Este método filtra productos por su categoría, útil para:
    /// 
    /// - Navegación por categorías en el catálogo.
    /// - Filtrado de productos para usuarios.
    /// - Reportes de inventario por categoría.
    /// 
    /// El rendimiento es óptimo ya que típicamente usa un índice foráneo.
    /// </remarks>
    /// 
    /// <example>
    /// <code>
    /// // Mostrar productos de una categoría
    /// var productos = await _productoRepository.FindByCategoriaIdAsync(5);
    /// foreach (var producto in productos)
    /// {
    ///     Console.WriteLine($"{producto.Nombre} - ${producto.Precio}");
    /// }
    /// </code>
    /// </example>
    /// 
    /// <param name="categoriaId">Identificador de la categoría cuyo productos se quieren obtener.</param>
    /// <returns>Colección de productos de la categoría especificada.</returns>
    Task<IEnumerable<Producto>> FindByCategoriaIdAsync(long categoriaId);

    /// <summary>
    /// Persiste un nuevo producto en la base de datos.
    /// 
    /// <remarks>
    /// Inserta un nuevo registro de producto. El objeto retornado contendrá
    /// el ID asignado automáticamente y cualquier valor generado por la base de datos.
    /// 
    /// Valide el producto antes de guardarlo:
    /// - Nombre requerido y único.
    /// - Precio mayor a cero.
    /// - Stock no negativo.
    /// - Categoría válida si se especifica.
    /// </remarks>
    /// 
    /// <example>
    /// <code>
    /// // Crear nuevo producto
    /// var producto = new Producto
    /// {
    ///     Nombre = "Laptop HP",
    ///     Descripcion = "Laptop con procesador i7",
    ///     Precio = 999.99m,
    ///     Stock = 10,
    ///     CategoriaId = 1
    /// };
    /// 
    /// var guardado = await _productoRepository.SaveAsync(producto);
    /// </code>
    /// </example>
    /// 
    /// <param name="producto">Producto a persistir. No debe tener ID preasignado.</param>
    /// <returns>El producto guardado con datos actualizados (ID, timestamps, etc.).</returns>
    Task<Producto> SaveAsync(Producto producto);

    /// <summary>
    /// Actualiza un producto existente.
    /// 
    /// <remarks>
    /// Actualiza los datos de un producto ya persistido. El producto debe tener
    /// un ID válido correspondiente a un registro existente.
    /// 
    /// Para actualizaciones concurrentes, asegúrese de que el usuario esté
    /// trabajando con la versión más reciente del producto.
    /// </remarks>
    /// 
    /// <example>
    /// <code>
    /// // Actualizar precio de producto
    /// var producto = await _productoRepository.FindByIdAsync(123);
    /// producto.Precio = 899.99m;
    /// producto.Stock = 5;
    /// await _productoRepository.UpdateAsync(producto);
    /// </code>
    /// </example>
    /// 
    /// <param name="producto">Producto con datos actualizados. Debe tener ID válido.</param>
    /// <returns>El producto actualizado.</returns>
    Task<Producto> UpdateAsync(Producto producto);

    /// <summary>
    /// Elimina un producto de forma suave (soft delete).
    /// 
    /// <remarks>
    /// La eliminación suave marca el producto como inactivo sin eliminarlo físicamente.
    /// Esto mantiene la integridad de pedidos históricos y permite auditoría.
    /// 
    /// Los productos eliminados suavemente no aparecen en consultas normales
    /// pero siguen existiendo en la base de datos para referencia histórica.
    /// </remarks>
    /// 
    /// <example>
    /// <code>
    /// // Descontinuar producto
    /// await _productoRepository.DeleteAsync(123);
    /// 
    /// // El producto ya no aparecerá en el catálogo
    /// </code>
    /// </example>
    /// 
    /// <param name="id">Identificador del producto a eliminar.</param>
    /// <returns>Tarea asíncrona completada tras la eliminación.</returns>
    Task DeleteAsync(long id);

    /// <summary>
    /// Verifica si existe un producto con el identificador especificado.
    /// 
    /// <remarks>
    /// Útil para validaciones rápidas antes de operaciones que requieren
    /// un producto existente. Más eficiente que recuperar el producto completo.
    /// </remarks>
    /// 
    /// <example>
    /// <code>
    /// // Validar existencia antes de operar
    /// if (!await _productoRepository.ExistsAsync(productoId))
    /// {
    ///     throw new KeyNotFoundException("Producto no encontrado");
    /// }
    /// </code>
    /// </example>
    /// 
    /// <param name="id">Identificador del producto a verificar.</param>
    /// <returns>True si existe, False en caso contrario.</returns>
    Task<bool> ExistsAsync(long id);

    /// <summary>
    /// Decrementa el stock de un producto de forma atómica con control de concurrencia optimista.
    /// 
    /// <remarks>
    /// Este método implementa un patrón crítico para operaciones de inventario:
    /// 
    /// 1. **Atomicidad**: La operación de decremento se ejecuta en una sola sentencia SQL,
    ///    evitando condiciones de carrera.
    /// 
    /// 2. **Control de Concurrencia Optimista**: Usa el campo RowVersion para detectar
    ///    modificaciones concurrentes. Si otro procesomodificó el producto después de
    ///    que el cliente lo leyó, se lanza DbUpdateConcurrencyException.
    /// 
    /// 3. **Validación de Stock**: Verifica que haya suficiente stock antes de decrementar.
    /// 
    /// Flujo de uso típico:
    /// - El cliente obtiene el producto y su RowVersion.
    /// - El cliente decide comprar una cantidad.
    /// - Se llama a este método con la cantidad y el RowVersion original.
    /// - Si el stock se decrementó exitosamente, continúa con el pedido.
    /// - Si hay conflicto, el cliente debe releer los datos y reintentar.
    /// 
    /// <example>
    /// <code>
    /// // Proceso de compra con control de concurrencia
    /// var producto = await _productoRepository.FindByIdAsync(productoId);
    /// if (producto.Stock &lt; cantidad)
    /// {
    ///     throw new InvalidOperationException("Stock insuficiente");
    /// }
    /// 
    /// var resultado = await _productoRepository.DecrementStockAsync(
    ///     productoId, cantidad, producto.RowVersion);
    /// 
    /// if (!resultado)
    /// {
    ///     // Otro procesomodificó el producto, reintentar o notificar al usuario
    ///     throw new ConcurrencyException("El producto fue modificado por otro usuario");
    /// }
    /// </code>
    /// </example>
    /// 
    /// <param name="productoId">Identificador del producto cuyo stock se decrementará.</param>
    /// <param name="cantidad">Cantidad a restar del stock (debe ser positiva).</param>
    /// <param name="expectedRowVersion">RowVersion del producto al momento de leerlo.</param>
    /// <returns>True si el stock fue decrementado exitosamente; False si el producto no existe.</returns>
    Task<bool> DecrementStockAsync(long productoId, int cantidad, byte[] expectedRowVersion);

    /// <summary>
    /// Inicia una transacción explícita con el nivel de aislamiento especificado.
    /// 
    /// <remarks>
    /// Este método permite control manual sobre transacciones para operaciones
    /// que abarcan múltiples tablas o repositorios. El patrón implementado
    /// usa Serializable + Retry para manejar conflictos de concurrencia.
    /// 
    /// Niveles de aislamiento comunes:
    /// - **ReadCommitted**: Valor predeterminado, evita lecturas sucias.
    /// - **Serializable**: Máxima aislamiento, evita lecturas no repetibles y fantasmás.
    /// - **RepeatableRead**: Evita lecturas no repetibles.
    /// 
    /// Use este método para operaciones que requieren atomicidad跨 múltiples
    /// cambios de datos, como el procesamiento de pedidos completo.
    /// 
    /// <example>
    /// <code>
    /// // Procesar pedido con transacción explícita
    /// using var transaction = await _productoRepository.BeginTransactionAsync(
    ///     IsolationLevel.Serializable);
    /// 
    /// try
    /// {
    ///     foreach (var item in pedido.Items)
    ///     {
    ///         var producto = await _productoRepository.FindByIdAsync(item.ProductoId);
    ///         await _productoRepository.DecrementStockAsync(
    ///             item.ProductoId, item.Cantidad, producto.RowVersion);
    ///     }
    ///     
    ///     await _pedidosRepository.SaveAsync(pedido);
    ///     await transaction.CommitAsync();
    /// }
    /// catch (Exception)
    /// {
    ///     await transaction.RollbackAsync();
    ///     throw;
    /// }
    /// </code>
    /// </example>
    /// 
    /// <param name="isolationLevel">Nivel de aislamiento de la transacción.</param>
    /// <returns>La transacción iniciada para completar o revertir manualmente.</returns>
    Task<Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction> BeginTransactionAsync(IsolationLevel isolationLevel);

    /// <summary>
    /// Proporciona una consulta IQueryable para uso con HotChocolate (GraphQL).
    /// 
    /// <remarks>
    /// Retorna un IQueryable para composición延迟 de consultas GraphQL.
    /// HotChocolate puede aplicar filtros, ordenación y paginación directamente
    /// en el servidor, optimizando las consultas a la base de datos.
    /// 
    /// Configure habilitación de filtrado y ordenación en el esquema GraphQL.
    /// </remarks>
    /// 
    /// <example>
    /// <code>
    /// // En el tipo de extensión GraphQL
    /// public IQueryable&lt;Producto&gt; GetProductos([Service] IProductoRepository repo)
    /// {
    ///     return repo.FindAllAsNoTracking();
    /// }
    /// 
    /// // Consulta GraphQL
    /// // query {
    /// //   productos(where: {precio: {gte: 100}}, take: 10) {
    /// //     id nombre precio
    /// //   }
    /// // }
    /// </code>
    /// </example>
    /// 
    /// <returns>IQueryable de productos para composición de consultas GraphQL.</returns>
    IQueryable<Producto> FindAllAsNoTracking();

    /// <summary>
    /// Recupera productos creados en los últimos X días.
    /// 
    /// <remarks>
    /// Este método es útil para reportes semanales de nuevos productos.
    /// Solo retorna productos que:
    /// - Han sido creados dentro del rango de días especificado.
    /// - No han sido eliminados (IsDeleted = false).
    /// 
    /// Los resultados se ordenan por fecha de creación descendente.
    /// </remarks>
    /// 
    /// <example>
    /// <code>
    /// // Obtener productos de los últimos 7 días
    /// var productos = await _productoRepository.GetRecentlyCreatedAsync(7);
    /// foreach (var p in productos)
    /// {
    ///     Console.WriteLine($"{p.Nombre} - {p.CreatedAt}");
    /// }
    /// </code>
    /// </example>
    /// 
    /// <param name="days">Número de días hacia atrás para buscar productos.</param>
    /// <returns>Colección de productos ordenados por fecha de creación descendente.</returns>
    Task<IEnumerable<Producto>> GetRecentlyCreatedAsync(int days);
}

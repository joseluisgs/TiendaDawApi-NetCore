using TiendaApi.Apis.Dtos.Common;
using TiendaApi.Apis.Models;

namespace TiendaApi.Apis.Repositories.Categorias;

/// <summary>
/// Define el contrato para el repositorio de categorías.
/// 
/// El patrón Repository es un patrón de diseño que actúa como una capa de abstracción
/// entre la lógica de negocio y la capa de acceso a datos. Su propósito principal es:
/// 
/// 1. **Abstracción de la persistencia**: Oculta los detalles de cómo se almacenan
///    y recuperan los datos, permitiendo cambiar la implementación sin afectar el código
///    que usa el repositorio.
/// 
/// 2. **Centralización del acceso a datos**: Toda la lógica relacionada con consultas
///    y operaciones CRUD está contenida en un solo lugar, facilitando el mantenimiento.
/// 
/// 3. **Testabilidad**: Al depender de abstracciones (interfaces), es fácil crear
///    implementaciones mock para pruebas unitarias sin الحاجة de una base de datos real.
/// 
/// 4. **Separación de responsabilidades**: La lógica de negocio no necesita conocer
///    los detalles de Entity Framework, SQL u otras tecnologías de persistencia.
/// 
/// Esta interfaz sigue el principio de Inversión de Dependencias (DIP) del SOLID,
/// donde las capas superiores (servicios, controladores) dependen de abstracciones
/// y no de implementaciones concretas.
/// </summary>
public interface ICategoriaRepository
{
    /// <summary>
    /// Recupera todas las categorías de la base de datos ordenadas alfabéticamente por nombre.
    /// 
    /// <remarks>
    /// Este método es ideal para escenarios donde se necesita mostrar un listado completo
    /// de categorías, como en menús de navegación o filtros de búsqueda.
    /// 
    /// Consideraciones de rendimiento:
    /// - Para conjuntos de datos grandes (más de 1000 registros), considere usar
    ///   <see cref="FindAllPagedAsync"/> en su lugar para evitar cargar grandes
    ///   cantidades de datos en memoria.
    /// - Si solo necesita consultar las categorías sin intención de modificarlas,
    ///   considere usar <see cref="FindAllAsNoTracking"/> para mejor rendimiento.
    /// </remarks>
    /// 
    /// <example>
    /// <code>
    /// // Obtener todas las categorías para mostrar en un menú
    /// var categorias = await _categoriaRepository.FindAllAsync();
    /// foreach (var categoria in categorias)
    /// {
    ///     Console.WriteLine($"{categoria.Id}: {categoria.Nombre}");
    /// }
    /// </code>
    /// </example>
    /// 
    /// <returns>Una colección enumerable de todas las categorías ordenadas por nombre.</returns>
    Task<IEnumerable<Categoria>> FindAllAsync();

    /// <summary>
    /// Recupera un subconjunto de categorías de forma paginada con soporte para filtros opcionales.
    /// 
    /// <remarks>
    /// La paginación es esencial para manejar grandes conjuntos de datos de manera eficiente.
    /// Este método retorna tanto los elementos de la página actual como el total de registros
    /// que coinciden con los filtros, información necesaria para calcular el número de páginas.
    /// 
    /// Filtros disponibles en CategoriaFilterDto:
    /// - Búsqueda por nombre (búsqueda parcial, sensible a mayúsculas/minúsculas)
    /// - Filtrado por estado (activo/inactivo)
    /// - Ordenación por diferentes campos
    /// - Tamaño de página personalizable
    /// </remarks>
    /// 
    /// <example>
    /// <code>
    /// // Obtener segunda página de categorías con 10 elementos por página
    /// var filter = new CategoriaFilterDto
    /// {
    ///     Page = 1,        // Segunda página (0-based)
    ///     Size = 10,       // 10 elementos por página
    ///     Search = "elec"  // Filtrar por nombre que contenga "elec"
    /// };
    /// 
    /// var (items, totalCount) = await _categoriaRepository.FindAllPagedAsync(filter);
    /// var totalPages = (int)Math.Ceiling(totalCount / (double)filter.Size);
    /// </code>
    /// </example>
    /// 
    /// <param name="filter">Objeto con los criterios de filtrado, paginación y ordenación.</param>
    /// <returns>Una tupla conteniendo: 
    /// - Items: Las categorías de la página solicitada.
    /// - TotalCount: El total de categorías que coinciden con los filtros.</returns>
    Task<(IEnumerable<Categoria> Items, int TotalCount)> FindAllPagedAsync(CategoriaFilterDto filter);

    /// <summary>
    /// Busca una categoría específica por su identificador único.
    /// 
    /// <remarks>
    /// Este es el método más común para recuperar un registro individual. La búsqueda
    /// por ID es extremadamente eficiente ya que típicamente utiliza el índice primario
    /// de la tabla en la base de datos.
    /// 
    /// Comportamiento:
    /// - Retorna null si no se encuentra ninguna categoría con el ID especificado.
    /// - Use verificación de null para manejar el caso de "no encontrado".
    /// - El tiempo de ejecución es O(1) en la mayoría de bases de datos.
    /// </remarks>
    /// 
    /// <example>
    /// <code>
    /// // Buscar categoría por ID para editar
    /// var categoria = await _categoriaRepository.FindByIdAsync(5);
    /// if (categoria == null)
    /// {
    ///     return NotFound("La categoría no existe");
    /// }
    /// 
    /// categoria.Nombre = "Electrónica Actualizada";
    /// await _categoriaRepository.UpdateAsync(categoria);
    /// </code>
    /// </example>
    /// 
    /// <param name="id">El identificador único de la categoría a buscar (clave primaria).</param>
    /// <returns>La categoría encontrada, o null si no existe ningún registro con ese ID.</returns>
    Task<Categoria?> FindByIdAsync(long id);

    /// <summary>
    /// Persiste una nueva categoría en la base de datos.
    /// 
    /// <remarks>
    /// Este método inserta un nuevo registro de categoría. La entidad proporcionada
    /// debe tener sus propiedades requeridas inicializadas (al menos el nombre).
    /// 
    /// Después de la inserción exitosa, el objeto retornado contendrá:
    /// - El ID asignado automáticamente por la base de datos.
    /// - Valores de campos calculados (timestamps, etc.)
    /// - Cualquier默认值 aplicada por la base de datos.
    /// 
    /// Valide la entidad antes de llamar a este método para evitar excepciones.
    /// </remarks>
    /// 
    /// <example>
    /// <code>
    /// // Crear nueva categoría
    /// var nuevaCategoria = new Categoria
    /// {
    ///     Nombre = "Hogar",
    ///     Descripcion = "Productos para el hogar",
    ///     Activo = true
    /// };
    /// 
    /// var categoriaGuardada = await _categoriaRepository.SaveAsync(nuevaCategoria);
    /// Console.WriteLine($"Categoría creada con ID: {categoriaGuardada.Id}");
    /// </code>
    /// </example>
    /// 
    /// <param name="categoria">La categoría a persistir. No debe tener un ID preasignado.</param>
    /// <returns>La categoría guardada con los datos actualizados (incluido el ID asignado).</returns>
    Task<Categoria> SaveAsync(Categoria categoria);

    /// <summary>
    /// Actualiza una categoría existente en la base de datos.
    /// 
    /// <remarks>
    /// Este método actualiza un registro existente. La entidad debe tener un ID válido
    /// que corresponda a un registro ya persistido.
    /// 
    /// Consideraciones:
    /// - Solo se actualizan las propiedades que han cambiado (change tracking).
    /// - Si usa AsNoTracking, todas las propiedades se actualizan.
    /// - Use este método después de recuperar la entidad con FindByIdAsync.
    /// - Para actualizaciones concurrentes, considere implementar control de optimista.
    /// </remarks>
    /// 
    /// <example>
    /// <code>
    /// // Actualizar categoría existente
    /// var categoria = await _categoriaRepository.FindByIdAsync(1);
    /// if (categoria != null)
    /// {
    ///     categoria.Descripcion = "Descripción actualizada";
    ///     var actualizada = await _categoriaRepository.UpdateAsync(categoria);
    /// }
    /// </code>
    /// </example>
    /// 
    /// <param name="categoria">La categoría con los datos actualizados. Debe tener un ID válido.</param>
    /// <returns>La categoría actualizada con los valores más recientes de la base de datos.</returns>
    Task<Categoria> UpdateAsync(Categoria categoria);

    /// <summary>
    /// Elimina una categoría de forma suave (soft delete) marcándola como inactiva.
    /// 
    /// <remarks>
    /// La eliminación suave preserva los datos históricos y mantiene la integridad referencial.
    /// En lugar de eliminar físicamente el registro de la base de datos, se marca como
    /// inactivo estableciendo un campo como Activo = false o equivalente.
    /// 
    /// Beneficios de la eliminación suave:
    /// - Los pedidos históricos siguen siendo válidos y pueden mostrar la categoría original.
    /// - Posibilidad de restaurar categorías accidentalmente eliminadas.
    /// - Auditoría completa del historial de cambios.
    /// - Cumplimiento con regulaciones de retención de datos.
    /// 
    /// Si necesita eliminación física permanente, implemente un método adicional.
    /// </remarks>
    /// 
    /// <example>
    /// <code>
    /// // Eliminar categoría de forma suave
    /// await _categoriaRepository.DeleteAsync(5);
    /// 
    /// // La categoría ya no aparecerá en listados activos
    /// var activas = (await _categoriaRepository.FindAllAsync())
    ///     .Where(c => c.Activo);
    /// </code>
    /// </example>
    /// 
    /// <param name="id">El identificador de la categoría a eliminar.</param>
    /// <returns>Tarea asíncrona que se completa cuando la eliminación es exitosa.</returns>
    Task DeleteAsync(long id);

    /// <summary>
    /// Verifica si existe una categoría con el nombre especificado.
    /// 
    /// <remarks>
    /// Este método es útil para validación antes de crear o actualizar registros.
    /// El parámetro excludeId permite excluir un registro específico de la búsqueda,
    /// útil al actualizar para evitar conflictos con el registro actual.
    /// 
    /// Casos de uso:
    /// - Validación de unicidad al crear nueva categoría.
    /// - Validación de unicidad al actualizar (evitando el registro actual).
    /// - Búsqueda de duplicados antes de operaciones masivas.
    /// 
    /// La búsqueda es insensible a mayúsculas/minúsculas según la configuración
    /// de la base de datos (típicamente collation case-insensitive en SQL Server).
    /// </remarks>
    /// 
    /// <example>
    /// <code>
    /// // Verificar si nombre ya existe al crear
    /// bool existe = await _categoriaRepository.ExistsByNombreAsync("Electrónica");
    /// if (existe)
    /// {
    ///     throw new ValidationException("Ya existe una categoría con ese nombre");
    /// }
    /// 
    /// // Verificar al actualizar (excluyendo el registro actual)
    /// var categoria = await _categoriaRepository.FindByIdAsync(1);
    /// bool existeAlActualizar = await _categoriaRepository.ExistsByNombreAsync(
    ///     "Nuevo Nombre", excludeId: categoria.Id);
    /// </code>
    /// </example>
    /// 
    /// <param name="nombre">El nombre de la categoría a buscar.</param>
    /// <param name="excludeId">Opcional. ID de categoría a excluir de la búsqueda.</param>
    /// <returns>True si existe al menos una categoría con ese nombre, False en caso contrario.</returns>
    Task<bool> ExistsByNombreAsync(string nombre, long? excludeId = null);

    /// <summary>
    /// Proporciona una consulta IQueryable para uso con HotChocolate (GraphQL).
    /// 
    /// <remarks>
    /// Este método retorna un IQueryable en lugar de una colección materializada,
    /// permitiendo que HotChocolate construya y ejecute consultas LINQ de forma延迟
    /// (lazy) en el servidor GraphQL. Esto habilita:
    /// 
    /// - **Consultas flexibles**: El cliente GraphQL puede especificar qué campos obtener.
    /// - **Filtrado dinámico**: HotChocolate puede aplicar filtros basados en argumentos.
    /// - **Paginación nativa**: Soporte para conexiónes de cursor o offset.
    /// - **Ordenación customizable**: El cliente puede definir criterios de ordenación.
    /// 
    /// Importante: Use AsNoTracking() para operaciones de solo lectura, mejorando
    /// el rendimiento y evitando el seguimiento de cambios innecesario.
    /// 
    /// Este método está diseñado específicamente para la integración con HotChocolate
    /// y no debe usarse en lógica de negocio regular.
    /// </remarks>
    /// 
    /// <example>
    /// <code>
    /// // En el tipo de extensión GraphQL
    /// public IQueryable&lt;Categoria&gt; GetCategorias([Service] ICategoriaRepository repo)
    /// {
    ///     return repo.FindAllAsNoTracking();
    /// }
    /// 
    /// // Consulta GraphQL del cliente:
    /// // query { categorias(where: {nombre: {contains: "elec"}}) { id nombre } }
    /// </code>
    /// </example>
    /// 
    /// <returns>Un IQueryable de categorías para composición de consultas.</returns>
    IQueryable<Categoria> FindAllAsNoTracking();
}

using CSharpFunctionalExtensions;
using TiendaApi.Apis.Dtos.Categorias;
using TiendaApi.Apis.Dtos.Common;
using TiendaApi.Apis.Errors;

namespace TiendaApi.Apis.Services.Categorias;

/// <summary>
/// Interfaz del servicio de categorías que implementa el patrón de arquitectura por capas (Service Layer).
/// Este patrón encapsula toda la lógica de negocio relacionada con categorías en una capa intermedia
/// entre los controladores de API y el acceso a datos, promoviendo la separación de responsabilidades.
///
/// <para><b>Patrón Result:</b> Utiliza el biblioteca CSharpFunctionalExtensions para manejar operaciones
/// que pueden resultar en éxito o fracaso de forma explícita. Esto permite:</para>
/// <list type="bullet">
///   <item><description>Manejo de errores tipado y sin excepciones para flujo de control</description></item>
///   <item><description>Encadenamiento de operaciones con métodos Map y Bind</description></item>
///   <item><description>Validación de dominio antes de ejecutar operaciones</description></item>
///   <item><description>Código más mantenible y testeable</description></item>
/// </list>
///
/// <para><b>Tipos de Result:</b></para>
/// <list type="bullet">
///   <item><description><c>Result&lt;T, DomainError&gt;</c>: Operaciones que devuelven un valor en éxito</description></item>
///   <item><description><c>UnitResult&lt;DomainError&gt;</c>: Operaciones que solo indican éxito o fracaso sin valor</description></item>
/// </list>
/// </summary>
/// <remarks>
/// <para><b>Manejo de Errores:</b> Los errores se representan mediante el tipo <c>DomainError</c> que contiene:
/// código de error, mensaje descriptivo y detalles opcionales. Nunca se lanzan excepciones para errores de negocio.</para>
/// <para><b>Errores Comunes:</b></para>
/// <list type="bullet">
///   <item><description><c>ErrorCodes.NotFound</c>: La categoría no existe</description></item>
///   <item><description><c>ErrorCodes.Conflict</c>: Ya existe una categoría con el mismo nombre</description></item>
///   <item><description><c>ErrorCodes.Validation</c>: Datos inválidos en la solicitud</description></item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// // Ejemplo de uso del patrón Result en un controlador
/// [HttpGet]
/// public async Task&lt;ActionResult&lt;PagedResult&lt;CategoriaDto&gt;&gt;&gt; GetCategorias([FromQuery] CategoriaFilterDto filter)
/// {
///     var result = await _categoriaService.FindAllPagedAsync(filter);
///
///     return result.Match(
///         success => Ok(success),
///         failure => Problem(statusCode: failure.StatusCode, detail: failure.Message)
///     );
/// }
///
/// // Ejemplo de encadenamiento con Map
/// public async Task&lt;Result&lt;CategoriaDto, DomainError&gt;&gt; CrearYNotificar(CategoriaRequestDto dto)
/// {
///     return await _categoriaService.CreateAsync(dto)
///         .Tap(categoria =&gt; _notificador.EnviarNotificacion($"Nueva categoría: {categoria.Nombre}"))
///         .Map(categoria =&gt; categoria);
/// }
/// </code>
/// </example>
public interface ICategoriaService
{
    /// <summary>
    /// Obtiene todas las categorías disponibles en el sistema.
    /// Esta operación nunca falla y siempre devuelve una lista (vacía si no hay categorías).
    /// </summary>
    /// <returns>
    /// <list type="bullet">
    ///   <item><term><c>Result.Success</c></term><description>Contiene un enumerable con todas las categorías</description></item>
    ///   <item><term><c>Result.Failure</c></term><description>Nunca ocurre en esta operación</description></item>
    /// </list>
    /// </returns>
    /// <remarks>
    /// Utiliza paginación implícita si el número de categorías excede un límite configurable.
    /// Para obtener resultados paginados con control explícito, usar <see cref="FindAllPagedAsync(CategoriaFilterDto)"/> en su lugar.
    /// </remarks>
    /// <example>
    /// <code>
    /// var resultado = await _categoriaService.FindAllAsync();
    /// if (resultado.IsSuccess)
    /// {
    ///     var categorias = resultado.Value;
    ///     foreach (var categoria in categorias)
    ///     {
    ///         Console.WriteLine($"{categoria.Id}: {categoria.Nombre}");
    ///     }
    /// }
    /// </code>
    /// </example>
    Task<Result<IEnumerable<CategoriaDto>, DomainError>> FindAllAsync();

    /// <summary>
    /// Obtiene las categorías de forma paginada con soporte para filtros de búsqueda.
    /// Permite buscar por nombre, estado activo/inactivo, y ordenamiento.
    /// </summary>
    /// <param name="filter">Objeto con los criterios de filtrado, paginación y ordenamiento</param>
    /// <returns>
    /// <list type="bullet">
    ///   <item><term><c>Result.Success</c></term><description>Contiene un <c>PagedResult&lt;CategoriaDto&gt;</c> con los datos paginados</description></item>
    ///   <item><term><c>Result.Failure</c></term><description>Nunca ocurre si los filtros son válidos</description></item>
    /// </list>
    /// </returns>
    /// <remarks>
    /// Si <paramref name="filter"/> es null, se aplica filtrado por defecto (solo activas, ordenadas por nombre).
    /// Los parámetros de paginación (<c>Page</c> y <c>Size</c>) tienen valores por defecto si no se especifican.
    /// </remarks>
    /// <example>
    /// <code>
    /// var filter = new CategoriaFilterDto
    /// {
    ///     Search = "elec",
    ///     Page = 1,
    ///     Size = 10,
    ///     SortBy = "Nombre",
    ///     SortDescending = false
    /// };
    ///
    /// var resultado = await _categoriaService.FindAllPagedAsync(filter);
    /// resultado.Match(
    ///     success =&gt; {
    ///         Console.WriteLine($"Total: {success.TotalCount}");
    ///         Console.WriteLine($"Página: {success.CurrentPage}/{success.TotalPages}");
    ///         foreach (var item in success.Items)
    ///             Console.WriteLine($"  - {item.Nombre}");
    ///     },
    ///     failure =&gt; Console.WriteLine($"Error: {failure.Message}")
    /// );
    /// </code>
    /// </example>
    Task<Result<PagedResult<CategoriaDto>, DomainError>> FindAllPagedAsync(CategoriaFilterDto filter);

    /// <summary>
    /// Obtiene una categoría específica por su identificador único.
    /// </summary>
    /// <param name="id">Identificador numérico de la categoría a buscar</param>
    /// <returns>
    /// <list type="bullet">
    ///   <item><term><c>Result.Success</c></term><description>Contiene la categoría encontrada</description></item>
    ///   <item><term><c>Result.Failure</c></term><description>Contiene <c>DomainError</c> con <c>ErrorCodes.NotFound</c> si no existe</description></item>
    /// </list>
    /// </returns>
    /// <remarks>
    /// Búsqueda por clave primaria. Para búsquedas por otros criterios, usar filtrado en <see cref="FindAllPagedAsync(CategoriaFilterDto)"/> o crear un método específico.
    /// </remarks>
    /// <example>
    /// <code>
    /// var resultado = await _categoriaService.FindByIdAsync(5);
    ///
    /// if (resultado.IsFailure)
    /// {
    ///     var error = resultado.Error;
    ///     if (error.Code == ErrorCodes.NotFound)
    ///         return NotFound($"Categoría con ID {id} no encontrada");
    ///     return Problem(error.Message);
    /// }
    ///
    /// return Ok(resultado.Value);
    /// </code>
    /// </example>
    Task<Result<CategoriaDto, DomainError>> FindByIdAsync(long id);

    /// <summary>
    /// Crea una nueva categoría en el sistema.
    /// Valida que no exista otra categoría con el mismo nombre antes de crear.
    /// </summary>
    /// <param name="dto">Objeto con los datos de la nueva categoría (nombre, descripción, estado)</param>
    /// <returns>
    /// <list type="bullet">
    ///   <item><term><c>Result.Success</c></term><description>Contiene la categoría creada con su ID asignado</description></item>
    ///   <item><term><c>Result.Failure</c></term><description>Contiene error de validación, conflicto o error de persistencia</description></item>
    /// </list>
    /// </returns>
    /// <remarks>
    /// <para><b>Validaciones:</b> El nombre es obligatorio y debe ser único.</para>
    /// <para><b>Errores posibles:</b></para>
    /// <list type="bullet">
    ///   <item><description><c>Validation</c>: Nombre vacío o muy largo</description></item>
    ///   <item><description><c>Conflict</c>: Ya existe categoría con ese nombre</description></item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// var request = new CategoriaRequestDto
    /// {
    ///     Nombre = "Electrónica",
    ///     Descripcion = "Productos electrónicos y gadgets",
    ///     Activo = true
    /// };
    ///
    /// var resultado = await _categoriaService.CreateAsync(request);
    ///
    /// return resultado.Match(
    ///     created =&gt; CreatedAtAction(nameof(GetById), new { id = created.Id }, created),
    ///     error =&gt; BadRequest(new { error = error.Message, code = error.Code })
    /// );
    /// </code>
    /// </example>
    Task<Result<CategoriaDto, DomainError>> CreateAsync(CategoriaRequestDto dto);

    /// <summary>
    /// Actualiza una categoría existente con nuevos datos.
    /// Mantiene el ID original de la categoría y solo modifica los campos proporcionados.
    /// </summary>
    /// <param name="id">Identificador de la categoría a actualizar</param>
    /// <param name="dto">Objeto con los nuevos datos de la categoría</param>
    /// <returns>
    /// <list type="bullet">
    ///   <item><term><c>Result.Success</c></term><description>Contiene la categoría actualizada</description></item>
    ///   <item><term><c>Result.Failure</c></term><description>Contiene error de no encontrado, validación o conflicto</description></item>
    /// </list>
    /// </returns>
    /// <remarks>
    /// Si el nombre se modifica, se valida unicidad. El ID en <paramref name="dto"/> es ignorado;
    /// el ID de la ruta es el que se utiliza. No se puede cambiar el ID de una categoría existente.
    /// </remarks>
    /// <example>
    /// <code>
    /// var request = new CategoriaRequestDto
    /// {
    ///     Nombre = "Electrónica y Computación",
    ///     Descripcion = "Productos electrónicos actualizada"
    /// };
    ///
    /// var resultado = await _categoriaService.UpdateAsync(5, request);
    ///
    /// return resultado.Match(
    ///     updated =&gt; Ok(updated),
    ///     error =&gt; error.Code switch
    ///     {
    ///         ErrorCodes.NotFound =&gt; NotFound(),
    ///         ErrorCodes.Conflict =&gt; Conflict(new { message = "Ya existe categoría con ese nombre" }),
    ///         _ =&gt; BadRequest(new { message = error.Message })
    ///     }
    /// );
    /// </code>
    /// </example>
    Task<Result<CategoriaDto, DomainError>> UpdateAsync(long id, CategoriaRequestDto dto);

    /// <summary>
    /// Elimina una categoría del sistema (eliminación lógica o soft delete).
    /// La categoría no se elimina físicamente de la base de datos sino que se marca como inactiva.
    /// </summary>
    /// <param name="id">Identificador de la categoría a eliminar</param>
    /// <returns>
    /// <list type="bullet">
    ///   <item><term><c>UnitResult.Success</c></term><description>La categoría fue eliminada correctamente</description></item>
    ///   <item><term><c>UnitResult.Failure</c></term><description>Contiene error de no encontrado u operación no permitida</description></item>
    /// </list>
    /// </returns>
    /// <remarks>
    /// <para><b>Comportamiento:</b> Eliminación lógica (soft delete). La categoría permanece en BD pero no aparece en consultas.</para>
    /// <para><b>Validaciones:</b> No se puede eliminar si tiene productos asociados (configurable según requisitos).</para>
    /// <para><b>Nota:</b> Utiliza <c>UnitResult</c> porque no necesitamos返回值, solo saber si la operación fue exitosa.</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var resultado = await _categoriaService.DeleteAsync(5);
    ///
    /// if (resultado.IsFailure)
    /// {
    ///     var error = resultado.Error;
    ///     return error.Code switch
    ///     {
    ///         ErrorCodes.NotFound =&gt; NotFound(),
    ///         "CATEGORY_HAS_PRODUCTS" =&gt; BadRequest("No se puede eliminar: categoría tiene productos asociados"),
    ///         _ =&gt; Problem(error.Message)
    ///     };
    /// }
    ///
    /// return NoContent();
    /// </code>
    /// </example>
    Task<UnitResult<DomainError>> DeleteAsync(long id);
}

using CSharpFunctionalExtensions;
using Microsoft.AspNetCore.Http;
using TiendaApi.Apis.Dtos.Common;
using TiendaApi.Apis.Dtos.Productos;
using TiendaApi.Apis.Errors;

namespace TiendaApi.Apis.Services.Productos;

/// <summary>
/// Interfaz del servicio de productos que implementa el patrón de arquitectura por capas (Service Layer).
/// Centraliza toda la lógica de negocio relacionada con la gestión del catálogo de productos,
/// incluyendo operaciones CRUD, gestión de imágenes y consultas especializadas.
///
/// <para><b>Patrón Result:</b> Utiliza el biblioteca CSharpFunctionalExtensions para proporcionar un enfoque
/// funcional al manejo de operaciones que pueden fallar. Esto reemplaza el uso tradicional de excepciones
/// para control de flujo con un sistema de resultados tipados.</para>
/// <list type="bullet">
///   <item><description>Mayor previsibilidad: el tipo de retorno indica explícitamente el posible fallo</description></item>
///   <item><description>Composabilidad: los resultados se pueden encadenar con Map, Bind, y Tap</description></item>
///   <item><description>Seguridad de tipos: los errores de dominio están tipados como <c>DomainError</c></description></item>
///   <item><description>Testabilidad: es fácil simular resultados de éxito o fallo en pruebas</description></item>
/// </list>
///
/// <para><b>Tipos de Result disponibles:</b></para>
/// <list type="bullet">
///   <item><description><c>Result&lt;T, DomainError&gt;</c>: Operaciones que retornan un valor (lectura, creación, actualización)</description></item>
///   <item><description><c>UnitResult&lt;DomainError&gt;</c>: Operaciones sin valor de retorno (eliminación)</description></item>
/// </list>
/// </summary>
/// <remarks>
/// <para><b>Manejo de Errores:</b></para>
/// <list type="bullet">
///   <item><description>Los errores de dominio heredan de <c>DomainError</c> y contienen: código, mensaje y detalles</description></item>
///   <item><description>Códigos de error comunes: NotFound, Validation, Conflict, BusinessRuleViolation</description></item>
///   <item><description>Los errores se mapean a códigos de estado HTTP en el controlador</description></item>
/// </list>
/// <para><b>Validaciones:</b></para>
/// <list type="bullet">
///   <item><description>Precio: debe ser mayor a cero</description></item>
///   <item><description>Stock: no puede ser negativo</description></item>
///   <item><description>Nombre: obligatorio, longitud entre 1 y 200 caracteres</description></item>
///   <item><description>SKU: obligatorio y único</description></item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// // Uso básico con Match (recomendado)
/// var result = await _productoService.FindByIdAsync(id);
/// return result.Match(
///     producto =&gt; Ok(producto),
///     error =&gt; StatusCode(error.StatusCode, new { error.Message })
/// );
///
/// // Encadenamiento de operaciones
/// public async Task&lt;Result&lt;ProductoDto, DomainError&gt;&gt; CrearProductoConImagen(ProductoRequestDto dto, IFormFile imagen)
/// {
///     return await _productoService.CreateAsync(dto)
///         .Bind(producto =&gt; _productoService.UpdateImageAsync(producto.Id, imagen));
/// }
///
/// // Manejo de errores por código
/// if (result.IsFailure)
/// {
///     return result.Error.Code switch
///     {
///         ErrorCodes.NotFound =&gt; NotFound(),
///         ErrorCodes.Validation =&gt; BadRequest(result.Error.Message),
///         ErrorCodes.Conflict =&gt; Conflict("SKU ya existe"),
///         _ =&gt; Problem(result.Error.Message)
///     };
/// }
/// </code>
/// </example>
public interface IProductoService
{
    /// <summary>
    /// Obtiene todos los productos activos del catálogo.
    /// Esta operación recupera todos los productos sin filtrado ni paginación.
    /// </summary>
    /// <returns>
    /// <list type="bullet">
    ///   <item><term><c>Result.Success</c></term><description>Enumerable con todos los productos (puede estar vacío)</description></item>
    ///   <item><term><c>Result.Failure</c></term><description>Nunca ocurre - siempre retorna una lista</description></item>
    /// </list>
    /// </returns>
    /// <remarks>
    /// Para grandes volúmenes de datos, utilizar <see cref="FindAllPagedAsync(ProductoFilterDto)"/> en su lugar
    /// para evitar problemas de rendimiento y consumo de memoria.
    /// </remarks>
    /// <example>
    /// <code>
    /// var resultado = await _productoService.FindAllAsync();
    /// var productos = resultado.Value; // Always available
    /// return Ok(productos);
    /// </code>
    /// </example>
    Task<Result<IEnumerable<ProductoDto>, DomainError>> FindAllAsync();

    /// <summary>
    /// Obtiene productos de forma paginada con filtros avanzados.
    /// Soporta búsqueda por nombre, categoría, rango de precios, stock y estado.
    /// </summary>
    /// <param name="filter">Objeto con criterios de búsqueda, paginación y ordenamiento</param>
    /// <returns>
    /// <list type="bullet">
    ///   <item><term><c>Result.Success</c></term><description><c>PagedResult</c> con productos filtrados y metadatos de paginación</description></item>
    ///   <item><term><c>Result.Failure</c></term><description>Nunca ocurre con parámetros válidos</description></item>
    /// </list>
    /// </returns>
    /// <remarks>
    /// Los filtros nulos aplican por defecto: productos activos, ordenados por ID descendente.
    /// El resultado incluye <c>TotalCount</c>, <c>TotalPages</c>, <c>CurrentPage</c> para construir UI de paginación.
    /// </remarks>
    /// <example>
    /// <code>
    /// var filtro = new ProductoFilterDto
    /// {
    ///     Search = "iphone",
    ///     CategoriaId = 5,
    ///     MinPrecio = 100,
    ///     MaxPrecio = 1000,
    ///     EnStock = true,
    ///     Page = 1,
    ///     Size = 20,
    ///     SortBy = "Precio",
    ///     SortDescending = true
    /// };
    ///
    /// var resultado = await _productoService.FindAllPagedAsync(filtro);
    /// return resultado.Match(
    ///     paged =&gt; {
    ///         Response.Headers["X-Total-Count"] = paged.TotalCount.ToString();
    ///         return Ok(paged.Items);
    ///     },
    ///     error =&gt; Problem(error.Message)
    /// );
    /// </code>
    /// </example>
    Task<Result<PagedResult<ProductoDto>, DomainError>> FindAllPagedAsync(ProductoFilterDto filter);

    /// <summary>
    /// Busca un producto específico por su identificador único.
    /// </summary>
    /// <param name="id">Identificador numérico del producto</param>
    /// <returns>
    /// <list type="bullet">
    ///   <item><term><c>Result.Success</c></term><description>El producto encontrado con todos sus datos</description></item>
    ///   <item><term><c>Result.Failure</c></term><description><c>DomainError</c> con <c>ErrorCodes.NotFound</c> si no existe</description></item>
    /// </list>
    /// </returns>
    /// <example>
    /// <code>
    /// var resultado = await _productoService.FindByIdAsync(123);
    ///
    /// if (resultado.IsFailure)
    /// {
    ///     if (resultado.Error.Code == ErrorCodes.NotFound)
    ///         return NotFound($"Producto {id} no encontrado");
    ///     throw new Exception(resultado.Error.Message);
    /// }
    ///
    /// return Ok(resultado.Value);
    /// </code>
    /// </example>
    Task<Result<ProductoDto, DomainError>> FindByIdAsync(long id);

    /// <summary>
    /// Recupera todos los productos pertenecientes a una categoría específica.
    /// Útil para mostrar el catálogo filtrado por categoría en la tienda.
    /// </summary>
    /// <param name="categoriaId">Identificador de la categoría</param>
    /// <returns>
    /// <list type="bullet">
    ///   <item><term><c>Result.Success</c></term><description>Lista de productos de esa categoría</description></item>
    ///   <item><term><c>Result.Failure</c></term><description>Nunca falla (retorna lista vacía si no hay productos)</description></item>
    /// </list>
    /// </returns>
    /// <remarks>
    /// Si la categoría no existe, retorna lista vacía. Para verificar existencia de categoría,
    /// usar <see cref="Categorias.ICategoriaService.FindByIdAsync(long)"/> primero.
    /// </remarks>
    /// <example>
    /// <code>
    /// var resultado = await _productoService.FindByCategoriaIdAsync(5);
    /// return Ok(resultado.Value);
    /// </code>
    /// </example>
    Task<Result<IEnumerable<ProductoDto>, DomainError>> FindByCategoriaIdAsync(long categoriaId);

    /// <summary>
    /// Crea un nuevo producto en el catálogo.
    /// Valida SKU único, precio positivo, categoría existente y datos obligatorios.
    /// </summary>
    /// <param name="dto">Datos del nuevo producto</param>
    /// <returns>
    /// <list type="bullet">
    ///   <item><term><c>Result.Success</c></term><description>El producto creado con ID asignado</description></item>
    ///   <item><term><c>Result.Failure</c></term><description>Error de validación, conflicto de SKU, o categoría inexistente</description></item>
    /// </list>
    /// </returns>
    /// <remarks>
    /// <para><b>Validaciones automático:</b></para>
    /// <list type="bullet">
    ///   <item><description>SKU único (error: Conflict)</description></item>
    ///   <item><description>Precio &gt; 0 (error: Validation)</description></item>
    ///   <item><description>Stock ≥ 0 (error: Validation)</description></item>
    ///   <item><description>Categoría existente (error: NotFound)</description></item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// var request = new ProductoRequestDto
    /// {
    ///     Nombre = "iPhone 15 Pro",
    ///     Descripcion = "Último modelo de Apple",
    ///     Precio = 999.99m,
    ///     Stock = 100,
    ///     SKU = "IPH15PRO-001",
    ///     CategoriaId = 5,
    ///     Activo = true
    /// };
    ///
    /// var resultado = await _productoService.CreateAsync(request);
    /// return resultado.Match(
    ///     producto =&gt; CreatedAtAction(nameof(GetById), new { id = producto.Id }, producto),
    ///     error =&gt; BadRequest(new { code = error.Code, message = error.Message })
    /// );
    /// </code>
    /// </example>
    Task<Result<ProductoDto, DomainError>> CreateAsync(ProductoRequestDto dto);

    /// <summary>
    /// Actualiza todos los datos de un producto existente.
    /// </summary>
    /// <param name="id">ID del producto a actualizar</param>
    /// <param name="dto">Nuevos datos del producto</param>
    /// <returns>
    /// <list type="bullet">
    ///   <item><term><c>Result.Success</c></term><description>Producto actualizado con datos nuevos</description></item>
    ///   <item><term><c>Result.Failure</c></term><description>NotFound, Validation, o Conflict de SKU</description></item>
    /// </list>
    /// </returns>
    /// <remarks>
    /// Si se modifica el SKU, se verifica que no exista otro producto con ese SKU.
    /// Si se modifica la categoría, se verifica que exista.
    /// </remarks>
    /// <example>
    /// <code>
    /// var resultado = await _productoService.UpdateAsync(123, productoUpdateDto);
    /// return resultado.Match(Ok, error =&gt; BadRequest(error.Message));
    /// </code>
    /// </example>
    Task<Result<ProductoDto, DomainError>> UpdateAsync(long id, ProductoRequestDto dto);

    /// <summary>
    /// Elimina un producto del catálogo (soft delete).
    /// El producto deja de aparecer en búsquedas pero permanece en la base de datos.
    /// </summary>
    /// <param name="id">ID del producto a eliminar</param>
    /// <returns>
    /// <list type="bullet">
    ///   <item><term><c>UnitResult.Success</c></term><description>Producto eliminado correctamente</description></item>
    ///   <item><term><c>UnitResult.Failure</c></term><description>NotFound o error de integridad referencial</description></item>
    /// </list>
    /// </returns>
    /// <remarks>
    /// No se elimina físicamente. Si el producto tiene pedidos asociados,
    /// se puede optar por no permitir eliminación (error BusinessRuleViolation).
    /// </remarks>
    /// <example>
    /// <code>
    /// var resultado = await _productoService.DeleteAsync(123);
    /// return resultado.IsSuccess ? NoContent() : BadRequest(resultado.Error.Message);
    /// </code>
    /// </example>
    Task<UnitResult<DomainError>> DeleteAsync(long id);

    /// <summary>
    /// Actualiza la imagen principal de un producto.
    /// Procesa y almacena la imagen, actualizando la URL en el producto.
    /// </summary>
    /// <param name="id">ID del producto</param>
    /// <param name="image">Archivo de imagen a subir</param>
    /// <returns>
    /// <list type="bullet">
    ///   <item><term><c>Result.Success</c></term><description>Producto con la nueva URL de imagen</description></item>
    ///   <item><term><c>Result.Failure</c></term><description>NotFound, Validation (archivo inválido), o error de almacenamiento</description></item>
    /// </list>
    /// </returns>
    /// <remarks>
    /// <para><b>Validaciones de imagen:</b></para>
    /// <list type="bullet">
    ///   <item><description>Formatos permitidos: JPG, PNG, WebP</description></item>
    ///   <item><description>Tamaño máximo: 5MB</description></item>
    ///   <item><description>Dimensiones recomendadas: entre 200x200 y 2000x2000 píxeles</description></item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// [HttpPost("{id}/imagen")]
    /// public async Task&lt;ActionResult&gt; UploadImage(long id, IFormFile imagen)
    /// {
    ///     if (imagen == null || imagen.Length == 0)
    ///         return BadRequest("No se proporcionó imagen");
    ///
    ///     var resultado = await _productoService.UpdateImageAsync(id, imagen);
    ///     return resultado.Match(
    ///         producto =&gt; Ok(new { url = producto.ImagenUrl }),
    ///         error =&gt; StatusCode(500, "Error al procesar imagen")
    ///     );
    /// }
    /// </code>
    /// </example>
    Task<Result<ProductoDto, DomainError>> UpdateImageAsync(long id, IFormFile image);

    /// <summary>
    /// Actualiza parcialmente un producto (PATCH).
    /// Solo actualiza los campos proporcionados en el DTO, manteniendo los demás sin cambios.
    /// </summary>
    /// <param name="id">ID del producto</param>
    /// <param name="dto">Campos a actualizar (null significa "no cambiar")</param>
    /// <returns>
    /// <list type="bullet">
    ///   <item><term><c>Result.Success</c></term><description>Producto con los campos actualizados</description></item>
    ///   <item><term><c>Result.Failure</c></term><description>NotFound, Validation, o Conflict de SKU</description></item>
    /// </list>
    /// </returns>
    /// <remarks>
    /// Útil para actualizaciones incrementales donde solo se cambian uno o dos campos.
    /// Los campos null en <paramref name="dto"/> se ignoran (no actualizan).
    /// </remarks>
    /// <example>
    /// <code>
    /// var patch = new ProductoPatchDto
    /// {
    ///     Precio = 899.99m,  // Solo actualiza precio
    ///     Stock = null       // No modifica stock
    /// };
    ///
    /// var resultado = await _productoService.UpdatePartialAsync(123, patch);
    /// return resultado.Match(Ok, error =&gt; BadRequest(error.Message));
    /// </code>
    /// </example>
    Task<Result<ProductoDto, DomainError>> UpdatePartialAsync(long id, ProductoPatchDto dto);
}

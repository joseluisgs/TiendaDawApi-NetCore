using CSharpFunctionalExtensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TiendaApi.Apis.Dtos.Common;
using TiendaApi.Apis.Dtos.Productos;
using TiendaApi.Apis.Errors;
using TiendaApi.Apis.Services.Productos;
using TiendaApi.Apis.Utils.Pagination;

namespace TiendaApi.Apis.Controllers;

/// <summary>
/// Controlador REST para la gestión de productos.
/// Implementa el patrón de diseño Result para el manejo de operaciones y errores.
/// </summary>
/// <remarks>
/// <para><b>API REST:</b> Este controlador expone endpoints que siguen los principios de RESTful.</para>
/// <para><b>Métodos HTTP:</b></para>
/// <list type="table">
/// <item>
/// <term>GET</term>
/// <description>Recuperar recursos (productos)</description>
/// </item>
/// <item>
/// <term>POST</term>
/// <description>Crear nuevos recursos</description>
/// </item>
/// <item>
/// <term>PUT</term>
/// <description>Actualizar recursos existentes completamente</description>
/// </item>
/// <item>
/// <term>PATCH</term>
/// <description>Actualizar parcialmente recursos (campos específicos)</description>
/// </item>
/// <item>
/// <term>DELETE</term>
/// <description>Eliminar recursos</description>
/// </item>
/// </list>
/// <para><b>Códigos de estado HTTP:</b></para>
/// <list type="table">
/// <item>
/// <term>200 OK</term>
/// <description>Petición exitosa, retorna datos</description>
/// </item>
/// <item>
/// <term>201 Created</term>
/// <description>Recurso creado exitosamente</description>
/// </item>
/// <item>
/// <term>204 No Content</term>
/// <description>Petición exitosa sin contenido que retornar</description>
/// </item>
/// <item>
/// <term>400 Bad Request</term>
/// <description>Error en los datos enviados por el cliente</description>
/// </item>
/// <item>
/// <term>401 Unauthorized</term>
/// <description>Usuario no autenticado</description>
/// </item>
/// <item>
/// <term>403 Forbidden</term>
/// <description>Usuario autenticado sin permisos suficientes</description>
/// </item>
/// <item>
/// <term>404 Not Found</term>
/// <description>Recurso no encontrado</description>
/// </item>
/// <item>
/// <term>409 Conflict</term>
/// <description>Conflicto con el estado actual del recurso</description>
/// </item>
/// <item>
/// <term>500 Internal Server Error</term>
/// <description>Error interno del servidor</description>
/// </item>
/// </list>
/// <para><b>Autorización:</b></para>
/// <para>Las operaciones de lectura (GET) son públicas. Las operaciones de escritura (POST, PUT, PATCH, DELETE) requieren rol de Administrador.</para>
/// </remarks>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ProductosController(
    IProductoService service,
    ILogger<ProductosController> logger
) : ControllerBase
{

    /// <summary>
    /// Obtiene todos los productos de forma paginada con filtros opcionales.
    /// </summary>
    /// <param name="nombre">Filtrar por nombre de producto (búsqueda parcial, opcional).</param>
    /// <param name="categoria">Filtrar por nombre de categoría (opcional).</param>
    /// <param name="isDeleted">Filtrar por estado de eliminación (opcional): true para eliminados, false para activos, null para todos.</param>
    /// <param name="precioMax">Filtrar por precio máximo (opcional).</param>
    /// <param name="stockMin">Filtrar por stock mínimo (opcional).</param>
    /// <param name="page">Número de página (base 0). Por defecto: 0.</param>
    /// <param name="size">Cantidad de elementos por página. Por defecto: 10.</param>
    /// <param name="sortBy">Campo por el cual ordenar. Por defecto: "id".</param>
    /// <param name="direction">Dirección de ordenamiento: "asc" o "desc". Por defecto: "asc".</param>
    /// <returns>Resultado paginado con la lista de productos.</returns>
    /// <remarks>
    /// <para><b>Endpoint:</b> GET /api/productos</para>
    /// <para><b>Descripción:</b> Retorna una lista paginada de productos con soporte para múltiples filtros y ordenamiento.</para>
    /// <para><b>Autenticación:</b> No requerida (público).</para>
    /// <para><b>Ejemplo de respuesta exitosa:</b></para>
    /// <example>
    /// ```json
    /// {
    ///   "items": [
    ///     {
    ///       "id": 1,
    ///       "nombre": "Laptop HP",
    ///       "descripcion": "Laptop profesional",
    ///       "precio": 999.99,
    ///       "stock": 50,
    ///       "categoriaId": 1,
    ///       "categoriaNombre": "Electrónica",
    ///       "imagenUrl": "https://ejemplo.com/imagen.jpg",
    ///       "isDeleted": false
    ///     }
    ///   ],
    ///   "page": 0,
    ///   "size": 10,
    ///   "totalItems": 25,
    ///   "totalPages": 3
    /// }
    /// ```
    /// </example>
    /// <para><b>Ejemplo de solicitud cURL:</b></para>
    /// <example>
    /// ```bash
    /// curl -X GET "http://localhost:5000/api/productos?nombre=Laptop&precioMax=1500&page=0&size=10" \
    ///   -H "Accept: application/json"
    /// ```
    /// </example>
    /// </remarks>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ProductoDto>), StatusCodes.Status200OK)]
    [AllowAnonymous]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? nombre = null,
        [FromQuery] string? categoria = null,
        [FromQuery] bool? isDeleted = null,
        [FromQuery] decimal? precioMax = null,
        [FromQuery] int? stockMin = null,
        [FromQuery] int page = 0,
        [FromQuery] int size = 10,
        [FromQuery] string sortBy = "id",
        [FromQuery] string direction = "asc")
    {
        logger.LogInformation("Obteniendo productos paginados - Página: {Page}, Tamaño: {Size}", page, size);

        var filter = new ProductoFilterDto(nombre, categoria, isDeleted, precioMax, stockMin, page, size, sortBy, direction);

        var resultado = await service.FindAllPagedAsync(filter);

        return resultado.Match(
            onSuccess: productos =>
            {
                var linkHeader = PaginationLinksHelper.CreateLinkHeader(productos, Request, sortBy, direction);
                if (!string.IsNullOrEmpty(linkHeader))
                    Response.Headers.Append("Link", linkHeader);
                return Ok(productos);
            },
            onFailure: error => StatusCode(500, new { message = error.Message })
        );
    }

    /// <summary>
    /// Obtiene un producto específico por su identificador único.
    /// </summary>
    /// <param name="id">Identificador único del producto.</param>
    /// <returns>Los datos del producto encontrado.</returns>
    /// <remarks>
    /// <para><b>Endpoint:</b> GET /api/productos/{id}</para>
    /// <para><b>Descripción:</b> Busca y retorna un producto específico usando su ID.</para>
    /// <para><b>Autenticación:</b> No requerida (público).</para>
    /// <para><b>Códigos de respuesta:</b></para>
    /// <list type="table">
    /// <item><term>200 OK</term><description>Producto encontrado exitosamente.</description></item>
    /// <item><term>404 Not Found</term><description>No existe producto con el ID especificado.</description></item>
    /// </list>
    /// <para><b>Ejemplo de respuesta exitosa:</b></para>
    /// <example>
    /// ```json
    /// {
    ///   "id": 1,
    ///   "nombre": "Laptop HP",
    ///   "descripcion": "Laptop profesional con procesador i7",
    ///   "precio": 999.99,
    ///   "stock": 50,
    ///   "categoriaId": 1,
    ///   "categoriaNombre": "Electrónica",
    ///   "imagenUrl": "https://ejemplo.com/laptop.jpg",
    ///   "isDeleted": false
    /// }
    /// ```
    /// </example>
    /// <para><b>Ejemplo de solicitud cURL:</b></para>
    /// <example>
    /// ```bash
    /// curl -X GET "http://localhost:5000/api/productos/1" \
    ///   -H "Accept: application/json"
    /// ```
    /// </example>
    /// </remarks>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ProductoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(long id)
    {
        logger.LogInformation("Obteniendo producto con ID: {Id}", id);

        var resultado = await service.FindByIdAsync(id);

        return resultado.Match(
            onSuccess: producto => Ok(producto),
            onFailure: error => error switch
            {
                NotFoundError => NotFound(new { message = error.Message }),
                _ => StatusCode(500, new { message = error.Message })
            }
        );
    }

    /// <summary>
    /// Obtiene todos los productos pertenecientes a una categoría específica.
    /// </summary>
    /// <param name="categoriaId">Identificador único de la categoría.</param>
    /// <returns>Lista de productos de la categoría.</returns>
    /// <remarks>
    /// <para><b>Endpoint:</b> GET /api/productos/categoria/{categoriaId}</para>
    /// <para><b>Descripción:</b> Recupera todos los productos asociados a una categoría específica.</para>
    /// <para><b>Autenticación:</b> No requerida (público).</para>
    /// <para><b>Códigos de respuesta:</b></para>
    /// <list type="table">
    /// <item><term>200 OK</term><description>Lista de productos retornada exitosamente (puede estar vacía).</description></item>
    /// <item><term>404 Not Found</term><description>No existe categoría con el ID especificado.</description></item>
    /// </list>
    /// <para><b>Ejemplo de respuesta exitosa:</b></para>
    /// <example>
    /// ```json
    /// [
    ///   {
    ///     "id": 1,
    ///     "nombre": "Laptop HP",
    ///     "precio": 999.99,
    ///     "stock": 50
    ///   },
    ///   {
    ///     "id": 2,
    ///     "nombre": "Mouse Inalámbrico",
    ///     "precio": 29.99,
    ///     "stock": 100
    ///   }
    /// ]
    /// ```
    /// </example>
    /// <para><b>Ejemplo de solicitud cURL:</b></para>
    /// <example>
    /// ```bash
    /// curl -X GET "http://localhost:5000/api/productos/categoria/1" \
    ///   -H "Accept: application/json"
    /// ```
    /// </example>
    /// </remarks>
    [HttpGet("categoria/{categoriaId}")]
    [ProducesResponseType(typeof(IEnumerable<ProductoDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [AllowAnonymous]
    public async Task<IActionResult> GetByCategoria(long categoriaId)
    {
        logger.LogInformation("Obteniendo productos de categoría: {CategoriaId}", categoriaId);

        var resultado = await service.FindByCategoriaIdAsync(categoriaId);

        return resultado.Match(
            onSuccess: productos => Ok(productos),
            onFailure: error => error switch
            {
                NotFoundError => NotFound(new { message = error.Message }),
                _ => StatusCode(500, new { message = error.Message })
            }
        );
    }

    /// <summary>
    /// Crea un nuevo producto en el sistema.
    /// </summary>
    /// <param name="dto">Datos del producto a crear.</param>
    /// <returns>Los datos del producto creado.</returns>
    /// <remarks>
    /// <para><b>Endpoint:</b> POST /api/productos</para>
    /// <para><b>Descripción:</b> Registra un nuevo producto en el catálogo. Requiere que la categoría especificada exista.</para>
    /// <para><b>Autenticación:</b> Requiere JWT token con rol de Administrador.</para>
    /// <para><b>Códigos de respuesta:</b></para>
    /// <list type="table">
    /// <item><term>201 Created</term><description>Producto creado exitosamente. Incluye Location header.</description></item>
    /// <item><term>400 Bad Request</term><description>Datos inválidos, errores de validación, o categoría no encontrada.</description></item>
    /// <item><term>401 Unauthorized</term><description>Token de autenticación inválido o expirado.</description></item>
    /// <item><term>403 Forbidden</term><description>Usuario autenticado sin permisos de administrador.</description></item>
    /// <item><term>404 Not Found</term><description>La categoría especificada no existe.</description></item>
    /// </list>
    /// <para><b>Ejemplo de cuerpo de solicitud:</b></para>
    /// <example>
    /// ```json
    /// {
    ///   "nombre": "Laptop HP ProBook",
    ///   "descripcion": "Laptop profesional con procesador i7, 16GB RAM",
    ///   "precio": 1299.99,
    ///   "stock": 25,
    ///   "categoriaId": 1,
    ///   "imagenUrl": "https://ejemplo.com/laptop.jpg"
    /// }
    /// ```
    /// </example>
    /// <para><b>Ejemplo de solicitud cURL:</b></para>
    /// <example>
    /// ```bash
    /// curl -X POST "http://localhost:5000/api/productos" \
    ///   -H "Content-Type: application/json" \
    ///   -H "Authorization: Bearer {token}" \
    ///   -d '{"nombre": "Laptop HP ProBook", "descripcion": "Laptop profesional", "precio": 1299.99, "stock": 25, "categoriaId": 1}'
    /// ```
    /// </example>
    /// </remarks>
    [HttpPost]
    [ProducesResponseType(typeof(ProductoDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Authorize(Policy = "RequireAdminRole")]
    public async Task<IActionResult> Create([FromBody] ProductoRequestDto dto)
    {
        logger.LogInformation("Creando nuevo producto: {Nombre}", dto.Nombre);

        var resultado = await service.CreateAsync(dto);

        return resultado.Match(
            onSuccess: producto => CreatedAtAction(nameof(GetById), new { id = producto.Id }, producto),
            onFailure: error => error switch
            {
                ValidationError ve => BadRequest(new { message = ve.Message, errors = ve.ValidationErrors }),
                NotFoundError => NotFound(new { message = error.Message }),
                ConflictError => Conflict(new { message = error.Message }),
                _ => StatusCode(500, new { message = error.Message })
            }
        );
    }

    /// <summary>
    /// Actualiza un producto existente completamente.
    /// </summary>
    /// <param name="id">Identificador único del producto a actualizar.</param>
    /// <param name="dto">Nuevos datos para el producto.</param>
    /// <returns>Los datos del producto actualizado.</returns>
    /// <remarks>
    /// <para><b>Endpoint:</b> PUT /api/productos/{id}</para>
    /// <para><b>Descripción:</b> Actualiza todos los campos de un producto existente. Si el recurso no existe, retorna 404.</para>
    /// <para><b>Autenticación:</b> Requiere JWT token con rol de Administrador.</para>
    /// <para><b>Códigos de respuesta:</b></para>
    /// <list type="table">
    /// <item><term>200 OK</term><description>Producto actualizado exitosamente.</description></item>
    /// <item><term>400 Bad Request</term><description>Datos inválidos o errores de validación.</description></item>
    /// <item><term>401 Unauthorized</term><description>Token de autenticación inválido o expirado.</description></item>
    /// <item><term>403 Forbidden</term><description>Usuario autenticado sin permisos de administrador.</description></item>
    /// <item><term>404 Not Found</term><description>No existe producto con el ID especificado.</description></item>
    /// </list>
    /// <para><b>Ejemplo de cuerpo de solicitud:</b></para>
    /// <example>
    /// ```json
    /// {
    ///   "nombre": "Laptop HP ProBook Actualizada",
    ///   "descripcion": "Nueva descripción",
    ///   "precio": 1099.99,
    ///   "stock": 30,
    ///   "categoriaId": 1
    /// }
    /// ```
    /// </example>
    /// <para><b>Ejemplo de solicitud cURL:</b></para>
    /// <example>
    /// ```bash
    /// curl -X PUT "http://localhost:5000/api/productos/1" \
    ///   -H "Content-Type: application/json" \
    ///   -H "Authorization: Bearer {token}" \
    ///   -d '{"nombre": "Laptop HP ProBook Actualizada", "descripcion": "Nueva descripción", "precio": 1099.99, "stock": 30, "categoriaId": 1}'
    /// ```
    /// </example>
    /// </remarks>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ProductoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [Authorize(Policy = "RequireAdminRole")]
    public async Task<IActionResult> Update(long id, [FromBody] ProductoRequestDto dto)
    {
        logger.LogInformation("Actualizando producto con ID: {Id}", id);

        var resultado = await service.UpdateAsync(id, dto);

        return resultado.Match(
            onSuccess: producto => Ok(producto),
            onFailure: error => error switch
            {
                NotFoundError => NotFound(new { message = error.Message }),
                ValidationError ve => BadRequest(new { message = ve.Message, errors = ve.ValidationErrors }),
                _ => StatusCode(500, new { message = error.Message })
            }
        );
    }

    /// <summary>
    /// Elimina un producto del sistema.
    /// </summary>
    /// <param name="id">Identificador único del producto a eliminar.</param>
    /// <returns>Sin contenido en caso de éxito.</returns>
    /// <remarks>
    /// <para><b>Endpoint:</b> DELETE /api/productos/{id}</para>
    /// <para><b>Descripción:</b> Elimina permanentemente un producto del sistema. Esta acción no se puede deshacer.</para>
    /// <para><b>Autenticación:</b> Requiere JWT token con rol de Administrador.</para>
    /// <para><b>Códigos de respuesta:</b></para>
    /// <list type="table">
    /// <item><term>204 No Content</term><description>Producto eliminado exitosamente.</description></item>
    /// <item><term>401 Unauthorized</term><description>Token de autenticación inválido o expirado.</description></item>
    /// <item><term>403 Forbidden</term><description>Usuario autenticado sin permisos de administrador.</description></item>
    /// <item><term>404 Not Found</term><description>No existe producto con el ID especificado.</description></item>
    /// </list>
    /// <para><b>Ejemplo de solicitud cURL:</b></para>
    /// <example>
    /// ```bash
    /// curl -X DELETE "http://localhost:5000/api/productos/1" \
    ///   -H "Authorization: Bearer {token}"
    /// ```
    /// </example>
    /// </remarks>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [Authorize(Policy = "RequireAdminRole")]
    public async Task<IActionResult> Delete(long id)
    {
        logger.LogInformation("Eliminando producto con ID: {Id}", id);

        var resultado = await service.DeleteAsync(id);

        if (resultado.IsSuccess)
            return NoContent();

        var error = resultado.Error;
        return error switch
        {
            NotFoundError => NotFound(new { message = error.Message }),
            _ => StatusCode(500, new { message = error.Message })
        };
    }

    /// <summary>
    /// Actualiza la imagen de un producto existente.
    /// </summary>
    /// <param name="id">Identificador único del producto.</param>
    /// <param name="image">Archivo de imagen a上传 (máximo 10MB).</param>
    /// <returns>Los datos del producto con la imagen actualizada.</returns>
    /// <remarks>
    /// <para><b>Endpoint:</b> PATCH /api/productos/{id}/imagen</para>
    /// <para><b>Descripción:</b> Actualiza únicamente la imagen de un producto. Soporta formatos: JPG, PNG, GIF, WEBP.</para>
    /// <para><b>Autenticación:</b> Requiere JWT token con rol de Administrador.</para>
    /// <para><b>Códigos de respuesta:</b></para>
    /// <list type="table">
    /// <item><term>200 OK</term><description>Imagen actualizada exitosamente.</description></item>
    /// <item><term>400 Bad Request</term><description>Archivo inválido, vacío, o tipo no permitido.</description></item>
    /// <item><term>401 Unauthorized</term><description>Token de autenticación inválido o expirado.</description></item>
    /// <item><term>403 Forbidden</term><description>Usuario autenticado sin permisos de administrador.</description></item>
    /// <item><term>404 Not Found</term><description>No existe producto con el ID especificado.</description></item>
    /// </list>
    /// <para><b>Tipos de imagen permitidos:</b> image/jpeg, image/png, image/gif, image/webp</para>
    /// <para><b>Tamaño máximo:</b> 10MB</para>
    /// <para><b>Ejemplo de solicitud cURL:</b></para>
    /// <example>
    /// ```bash
    /// curl -X PATCH "http://localhost:5000/api/productos/1/imagen" \
    ///   -H "Authorization: Bearer {token}" \
    ///   -F "image=@/ruta/a/imagen.jpg"
    /// ```
    /// </example>
    /// </remarks>
    [HttpPatch("{id}/imagen")]
    [ProducesResponseType(typeof(ProductoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [RequestSizeLimit(10 * 1024 * 1024)]
    [Authorize(Policy = "RequireAdminRole")]
    public async Task<IActionResult> UpdateImage(long id, IFormFile image)
    {
        logger.LogInformation("Actualizando imagen de producto con ID: {Id}", id);

        if (image is null or { Length: 0 })
        {
            return BadRequest(new { message = "Debe proporcionar un archivo de imagen" });
        }

        var allowedTypes = new[] { "image/jpeg", "image/png", "image/gif", "image/webp" };
        if (!allowedTypes.Contains(image.ContentType.ToLowerInvariant()))
        {
            return BadRequest(new { message = "Tipo de archivo no permitido. Solo se permiten: JPG, PNG, GIF, WEBP" });
        }

        var resultado = await service.UpdateImageAsync(id, image);

        return resultado.Match(
            onSuccess: producto => Ok(producto),
            onFailure: error => error switch
            {
                NotFoundError => NotFound(new { message = error.Message }),
                ValidationError ve => BadRequest(new { message = ve.Message, errors = ve.ValidationErrors }),
                _ => StatusCode(500, new { message = error.Message })
            }
        );
    }

    /// <summary>
    /// Actualiza parcialmente un producto (solo los campos proporcionados).
    /// </summary>
    /// <param name="id">Identificador único del producto a actualizar.</param>
    /// <param name="dto">Campos a actualizar del producto.</param>
    /// <returns>Los datos del producto actualizado.</returns>
    /// <remarks>
    /// <para><b>Endpoint:</b> PATCH /api/productos/{id}</para>
    /// <para><b>Descripción:</b> Actualiza únicamente los campos del producto que se proporcionen en el cuerpo de la solicitud. Los campos no incluidos permanecen sin cambios.</para>
    /// <para><b>Autenticación:</b> Requiere JWT token con rol de Administrador.</para>
    /// <para><b>Códigos de respuesta:</b></para>
    /// <list type="table">
    /// <item><term>200 OK</term><description>Producto actualizado parcialmente exitosamente.</description></item>
    /// <item><term>400 Bad Request</term><description>Datos inválidos o errores de validación.</description></item>
    /// <item><term>401 Unauthorized</term><description>Token de autenticación inválido o expirado.</description></item>
    /// <item><term>403 Forbidden</term><description>Usuario autenticado sin permisos de administrador.</description></item>
    /// <item><term>404 Not Found</term><description>No existe producto con el ID especificado.</description></item>
    /// </list>
    /// <para><b>Ejemplo de cuerpo de solicitud:</b></para>
    /// <example>
    /// ```json
    /// {
    ///   "precio": 899.99,
    ///   "stock": 40
    /// }
    /// ```
    /// </example>
    /// <para><b>Ejemplo de solicitud cURL:</b></para>
    /// <example>
    /// ```bash
    /// curl -X PATCH "http://localhost:5000/api/productos/1" \
    ///   -H "Content-Type: application/json" \
    ///   -H "Authorization: Bearer {token}" \
    ///   -d '{"precio": 899.99, "stock": 40}'
    /// ```
    /// </example>
    /// </remarks>
    [HttpPatch("{id}")]
    [ProducesResponseType(typeof(ProductoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [Authorize(Policy = "RequireAdminRole")]
    public async Task<IActionResult> UpdatePartial(long id, [FromBody] ProductoPatchDto dto)
    {
        logger.LogInformation("Actualizando parcialmente producto con ID: {Id}", id);

        var resultado = await service.UpdatePartialAsync(id, dto);

        return resultado.Match(
            onSuccess: producto => Ok(producto),
            onFailure: error => error switch
            {
                NotFoundError => NotFound(new { message = error.Message }),
                ValidationError ve => BadRequest(new { message = ve.Message, errors = ve.ValidationErrors }),
                _ => StatusCode(500, new { message = error.Message })
            }
        );
    }
}

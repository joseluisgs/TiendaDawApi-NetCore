using CSharpFunctionalExtensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TiendaApi.Apis.Dtos.Categorias;
using TiendaApi.Apis.Dtos.Common;
using TiendaApi.Apis.Errors;
using TiendaApi.Apis.Models;
using TiendaApi.Apis.Services.Categorias;
using TiendaApi.Apis.Utils.Pagination;

namespace TiendaApi.Apis.Controllers;

/// <summary>
/// Controlador REST para la gestión de categorías de productos.
/// Implementa el patrón de diseño Result para el manejo de operaciones y errores.
/// </summary>
/// <remarks>
/// <para><b>API REST:</b> Este controlador expone endpoints que siguen los principios de RESTful.</para>
/// <para><b>Métodos HTTP:</b></para>
/// <list type="table">
/// <item>
/// <term>GET</term>
/// <description>Recuperar recursos (categorías)</description>
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
/// <para>Las operaciones de lectura (GET) son públicas. Las operaciones de escritura (POST, PUT, DELETE) requieren rol de Administrador.</para>
/// </remarks>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class CategoriasController(
    ICategoriaService service,
    ILogger<CategoriasController> logger
) : ControllerBase
{

    /// <summary>
    /// Obtiene todas las categorías de forma paginada con filtros opcionales.
    /// </summary>
    /// <param name="nombre">Filtrar por nombre de categoría (opcional).</param>
    /// <param name="isDeleted">Filtrar por estado de eliminación (opcional): true para eliminadas, false para activas, null para todas.</param>
    /// <param name="page">Número de página (base 0). Por defecto: 0.</param>
    /// <param name="size">Cantidad de elementos por página. Por defecto: 10.</param>
    /// <param name="sortBy">Campo por el cual ordenar. Por defecto: "id".</param>
    /// <param name="direction">Dirección de ordenamiento: "asc" o "desc". Por defecto: "asc".</param>
    /// <returns>Resultado paginado con la lista de categorías.</returns>
    /// <remarks>
    /// <para><b>Endpoint:</b> GET /api/categorias</para>
    /// <para><b>Descripción:</b> Retorna una lista paginada de categorías con soporte para filtros y ordenamiento.</para>
    /// <para><b>Autenticación:</b> No requerida (público).</para>
    /// <para><b>Ejemplo de respuesta exitosa:</b></para>
    /// <example>
    /// ```json
    /// {
    ///   "items": [
    ///     {
    ///       "id": 1,
    ///       "nombre": "Electrónica",
    ///       "descripcion": "Productos electrónicos y gadgets",
    ///       "isDeleted": false
    ///     }
    ///   ],
    ///   "page": 0,
    ///   "size": 10,
    ///   "totalItems": 5,
    ///   "totalPages": 1
    /// }
    /// ```
    /// </example>
    /// <para><b>Ejemplo de solicitud cURL:</b></para>
    /// <example>
    /// ```bash
    /// curl -X GET "http://localhost:5000/api/categorias?nombre=Elec&page=0&size=10" \
    ///   -H "Accept: application/json"
    /// ```
    /// </example>
    /// </remarks>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<CategoriaDto>), StatusCodes.Status200OK)]
    [AllowAnonymous]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? nombre = null,
        [FromQuery] bool? isDeleted = null,
        [FromQuery] int page = 0,
        [FromQuery] int size = 10,
        [FromQuery] string sortBy = "id",
        [FromQuery] string direction = "asc")
    {
        logger.LogInformation("Obteniendo categorías paginadas - Página: {Page}, Tamaño: {Size}", page, size);

        var filter = new CategoriaFilterDto
        {
            Nombre = nombre,
            IsDeleted = isDeleted,
            Page = page,
            Size = size,
            SortBy = sortBy,
            Direction = direction
        };

        var resultado = await service.FindAllPagedAsync(filter);

        return resultado.Match(
            onSuccess: categorias =>
            {
                var linkHeader = PaginationLinksHelper.CreateLinkHeader(categorias, Request, sortBy, direction);
                if (!string.IsNullOrEmpty(linkHeader))
                    Response.Headers.Append("Link", linkHeader);
                return Ok(categorias);
            },
            onFailure: error => error switch
            {
                NotFoundError => NotFound(new { message = error.Message }),
                ValidationError => BadRequest(new { message = error.Message }),
                ConflictError => Conflict(new { message = error.Message }),
                _ => StatusCode(500, new { message = error.Message })
            }
        );
    }

    /// <summary>
    /// Obtiene una categoría específica por su identificador único.
    /// </summary>
    /// <param name="id">Identificador único de la categoría.</param>
    /// <returns>Los datos de la categoría encontrada.</returns>
    /// <remarks>
    /// <para><b>Endpoint:</b> GET /api/categorias/{id}</para>
    /// <para><b>Descripción:</b> Busca y retorna una categoría específica usando su ID.</para>
    /// <para><b>Autenticación:</b> No requerida (público).</para>
    /// <para><b>Códigos de respuesta:</b></para>
    /// <list type="table">
    /// <item><term>200 OK</term><description>Categoría encontrada exitosamente.</description></item>
    /// <item><term>404 Not Found</term><description>No existe categoría con el ID especificado.</description></item>
    /// </list>
    /// <para><b>Ejemplo de respuesta exitosa:</b></para>
    /// <example>
    /// ```json
    /// {
    ///   "id": 1,
    ///   "nombre": "Electrónica",
    ///   "descripcion": "Productos electrónicos y gadgets",
    ///   "isDeleted": false
    /// }
    /// ```
    /// </example>
    /// <para><b>Ejemplo de solicitud cURL:</b></para>
    /// <example>
    /// ```bash
    /// curl -X GET "http://localhost:5000/api/categorias/1" \
    ///   -H "Accept: application/json"
    /// ```
    /// </example>
    /// </remarks>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(CategoriaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(long id)
    {
        logger.LogInformation("Obteniendo categoría con ID: {Id}", id);

        var resultado = await service.FindByIdAsync(id);

        return resultado.Match(
            onSuccess: categoria => Ok(categoria),
            onFailure: error => error switch
            {
                NotFoundError => NotFound(new { message = error.Message }),
                _ => StatusCode(500, new { message = error.Message })
            }
        );
    }

    /// <summary>
    /// Crea una nueva categoría en el sistema.
    /// </summary>
    /// <param name="dto">Datos de la categoría a crear.</param>
    /// <returns>Los datos de la categoría creada.</returns>
    /// <remarks>
    /// <para><b>Endpoint:</b> POST /api/categorias</para>
    /// <para><b>Descripción:</b> Registra una nueva categoría en el catálogo de productos.</para>
    /// <para><b>Autenticación:</b> Requiere JWT token con rol de Administrador.</para>
    /// <para><b>Códigos de respuesta:</b></para>
    /// <list type="table">
    /// <item><term>201 Created</term><description>Categoría creada exitosamente. Incluye Location header.</description></item>
    /// <item><term>400 Bad Request</term><description>Datos inválidos o errores de validación.</description></item>
    /// <item><term>401 Unauthorized</term><description>Token de autenticación inválido o expirado.</description></item>
    /// <item><term>403 Forbidden</term><description>Usuario autenticado sin permisos de administrador.</description></item>
    /// <item><term>409 Conflict</term><description>Ya existe una categoría con el mismo nombre.</description></item>
    /// </list>
    /// <para><b>Ejemplo de cuerpo de solicitud:</b></para>
    /// <example>
    /// ```json
    /// {
    ///   "nombre": "Hogar y Jardín",
    ///   "descripcion": "Productos para el hogar y jardín"
    /// }
    /// ```
    /// </example>
    /// <para><b>Ejemplo de solicitud cURL:</b></para>
    /// <example>
    /// ```bash
    /// curl -X POST "http://localhost:5000/api/categorias" \
    ///   -H "Content-Type: application/json" \
    ///   -H "Authorization: Bearer {token}" \
    ///   -d '{"nombre": "Hogar y Jardín", "descripcion": "Productos para el hogar y jardín"}'
    /// ```
    /// </example>
    /// </remarks>
    [HttpPost]
    [Authorize(Roles = UserRoles.ADMIN)]
    [ProducesResponseType(typeof(CategoriaDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CategoriaRequestDto dto)
    {
        logger.LogInformation("Creando nueva categoría: {Nombre}", dto.Nombre);

        var resultado = await service.CreateAsync(dto);

        return resultado.Match(
            onSuccess: categoria => CreatedAtAction(nameof(GetById), new { id = categoria.Id }, categoria),
            onFailure: error => error switch
            {
                ValidationError => BadRequest(new { message = error.Message }),
                ConflictError => Conflict(new { message = error.Message }),
                _ => StatusCode(500, new { message = error.Message })
            }
        );
    }

    /// <summary>
    /// Actualiza una categoría existente completamente.
    /// </summary>
    /// <param name="id">Identificador único de la categoría a actualizar.</param>
    /// <param name="dto">Nuevos datos para la categoría.</param>
    /// <returns>Los datos de la categoría actualizada.</returns>
    /// <remarks>
    /// <para><b>Endpoint:</b> PUT /api/categorias/{id}</para>
    /// <para><b>Descripción:</b> Actualiza todos los campos de una categoría existente. Si el recurso no existe, retorna 404.</para>
    /// <para><b>Autenticación:</b> Requiere JWT token con rol de Administrador.</para>
    /// <para><b>Códigos de respuesta:</b></para>
    /// <list type="table">
    /// <item><term>200 OK</term><description>Categoría actualizada exitosamente.</description></item>
    /// <item><term>400 Bad Request</term><description>Datos inválidos o errores de validación.</description></item>
    /// <item><term>401 Unauthorized</term><description>Token de autenticación inválido o expirado.</description></item>
    /// <item><term>403 Forbidden</term><description>Usuario autenticado sin permisos de administrador.</description></item>
    /// <item><term>404 Not Found</term><description>No existe categoría con el ID especificado.</description></item>
    /// <item><term>409 Conflict</term><description>Conflicto con datos existentes (ej: nombre duplicado).</description></item>
    /// </list>
    /// <para><b>Ejemplo de cuerpo de solicitud:</b></para>
    /// <example>
    /// ```json
    /// {
    ///   "nombre": "Electrónica Actualizada",
    ///   "descripcion": "Nueva descripción para electrónicos"
    /// }
    /// ```
    /// </example>
    /// <para><b>Ejemplo de solicitud cURL:</b></para>
    /// <example>
    /// ```bash
    /// curl -X PUT "http://localhost:5000/api/categorias/1" \
    ///   -H "Content-Type: application/json" \
    ///   -H "Authorization: Bearer {token}" \
    ///   -d '{"nombre": "Electrónica Actualizada", "descripcion": "Nueva descripción"}'
    /// ```
    /// </example>
    /// </remarks>
    [HttpPut("{id}")]
    [Authorize(Roles = UserRoles.ADMIN)]
    [ProducesResponseType(typeof(CategoriaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(long id, [FromBody] CategoriaRequestDto dto)
    {
        logger.LogInformation("Actualizando categoría con ID: {Id}", id);

        var resultado = await service.UpdateAsync(id, dto);

        return resultado.Match(
            onSuccess: categoria => Ok(categoria),
            onFailure: error => error switch
            {
                NotFoundError => NotFound(new { message = error.Message }),
                ValidationError => BadRequest(new { message = error.Message }),
                ConflictError => Conflict(new { message = error.Message }),
                _ => StatusCode(500, new { message = error.Message })
            }
        );
    }

    /// <summary>
    /// Elimina una categoría del sistema.
    /// </summary>
    /// <param name="id">Identificador único de la categoría a eliminar.</param>
    /// <returns>Sin contenido en caso de éxito.</returns>
    /// <remarks>
    /// <para><b>Endpoint:</b> DELETE /api/categorias/{id}</para>
    /// <para><b>Descripción:</b> Elimina permanentemente una categoría del sistema. Esta acción no se puede deshacer.</para>
    /// <para><b>Autenticación:</b> Requiere JWT token con rol de Administrador.</para>
    /// <para><b>Códigos de respuesta:</b></para>
    /// <list type="table">
    /// <item><term>204 No Content</term><description>Categoría eliminada exitosamente.</description></item>
    /// <item><term>401 Unauthorized</term><description>Token de autenticación inválido o expirado.</description></item>
    /// <item><term>403 Forbidden</term><description>Usuario autenticado sin permisos de administrador.</description></item>
    /// <item><term>404 Not Found</term><description>No existe categoría con el ID especificado.</description></item>
    /// </list>
    /// <para><b>Ejemplo de solicitud cURL:</b></para>
    /// <example>
    /// ```bash
    /// curl -X DELETE "http://localhost:5000/api/categorias/1" \
    ///   -H "Authorization: Bearer {token}"
    /// ```
    /// </example>
    /// </remarks>
    [HttpDelete("{id}")]
    [Authorize(Roles = UserRoles.ADMIN)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(long id)
    {
        logger.LogInformation("Eliminando categoría con ID: {Id}", id);

        var resultado = await service.DeleteAsync(id);

        if (resultado.IsSuccess)
            return NoContent();

        var error = resultado.Error;
        return error switch
        {
            NotFoundError => NotFound(new { message = error.Message }),
            ValidationError => BadRequest(new { message = error.Message }),
            _ => StatusCode(500, new { message = error.Message })
        };
    }
}

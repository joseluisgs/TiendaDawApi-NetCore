using CSharpFunctionalExtensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TiendaApi.Api.Dtos.Common;
using TiendaApi.Api.Dtos.Productos;
using TiendaApi.Api.Errors;
using TiendaApi.Api.Services.Productos;
using TiendaApi.Api.Helpers.Pagination;

namespace TiendaApi.Api.Controllers;

/// <summary>
/// Controlador de API para gestión de productos.
/// Endpoints: CRUD paginado, filtrado, búsqueda por categoría.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ProductosController(
    IProductoService service,
    ILogger<ProductosController> logger
) : ControllerBase
{
    /// <summary>
    /// Obtiene todos los productos paginados con filtros opcionales.
    /// </summary>
    /// <param name="nombre">Filtrar por nombre (contiene).</param>
    /// <param name="categoria">Filtrar por nombre de categoría.</param>
    /// <param name="isDeleted">Filtrar por estado de eliminación.</param>
    /// <param name="precioMax">Filtrar por precio máximo.</param>
    /// <param name="stockMin">Filtrar por stock mínimo.</param>
    /// <param name="page">Número de página (0-indexed).</param>
    /// <param name="size">Elementos por página.</param>
    /// <param name="sortBy">Campo de ordenación (id, nombre, precio, stock).</param>
    /// <param name="direction">Dirección (asc, desc).</param>
    /// <returns>200 OK con lista paginada de productos.</returns>
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
    /// Obtiene un producto por su ID.
    /// </summary>
    /// <param name="id">ID del producto.</param>
    /// <returns>200 OK con el producto, o 404 si no existe.</returns>
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
    /// Obtiene todos los productos de una categoría.
    /// </summary>
    /// <param name="categoriaId">ID de la categoría.</param>
    /// <returns>200 OK con lista de productos, o 404 si la categoría no existe.</returns>
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
    /// <returns>201 Created con el producto creado, o 400/404/409 si hay errores.</returns>
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
    /// <param name="id">ID del producto a actualizar.</param>
    /// <param name="dto">Nuevos datos del producto.</param>
    /// <returns>200 OK con el producto actualizado, o 400/404 si hay errores.</returns>
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
    /// Elimina un producto (soft-delete).
    /// </summary>
    /// <param name="id">ID del producto a eliminar.</param>
    /// <returns>204 No Content si tiene éxito, o 404 si no existe.</returns>
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
    /// Actualiza la imagen de un producto.
    /// </summary>
    /// <param name="id">ID del producto.</param>
    /// <param name="image">Archivo de imagen (JPG, PNG, GIF, WEBP, max 10MB).</param>
    /// <returns>200 OK con el producto actualizado, o 400/404 si hay errores.</returns>
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
    /// Actualiza parcialmente un producto (campos específicos).
    /// </summary>
    /// <param name="id">ID del producto.</param>
    /// <param name="dto">Campos a actualizar.</param>
    /// <returns>200 OK con el producto actualizado, o 400/404 si hay errores.</returns>
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

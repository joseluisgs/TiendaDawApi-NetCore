using CSharpFunctionalExtensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TiendaApi.Apis.Dtos.Common;
using TiendaApi.Apis.Dtos.Productos;
using TiendaApi.Apis.Errors;
using TiendaApi.Apis.Services.Productos;
using TiendaApi.Apis.Helpers.Pagination;

namespace TiendaApi.Apis.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ProductosController(
    IProductoService service,
    ILogger<ProductosController> logger
) : ControllerBase
{
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

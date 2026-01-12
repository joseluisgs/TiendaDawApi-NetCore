using CSharpFunctionalExtensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TiendaApi.Apis.Dtos.Productos;
using TiendaApi.Apis.Errors;
using TiendaApi.Apis.Services.Productos;

namespace TiendaApi.Apis.Controllers;

/// <summary>
/// Controlador de productos usando Patrón Result.
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
    /// Obtener todos los productos.
    /// GET /api/productos
    /// Returns: 200 OK
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ProductoDto>), StatusCodes.Status200OK)]
    [AllowAnonymous]
    public async Task<IActionResult> GetAll()
    {
        logger.LogInformation("Obteniendo todos los productos");

        var resultado = await service.FindAllAsync();

        return resultado.Match(
            onSuccess: productos => Ok(productos),
            onFailure: error => StatusCode(500, new { message = error.Message })
        );
    }

    /// <summary>
    /// Obtener un producto por ID.
    /// GET /api/productos/{id}
    /// Returns: 200 OK | 404 Not Found
    /// </summary>
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
            onFailure: error => error.Type switch
            {
                ErrorType.NotFound => NotFound(new { message = error.Message }),
                _ => StatusCode(500, new { message = error.Message })
            }
        );
    }

    /// <summary>
    /// Obtener productos por categoría.
    /// GET /api/productos/categoria/{categoriaId}
    /// Returns: 200 OK | 404 Not Found
    /// </summary>
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
            onFailure: error => error.Type switch
            {
                ErrorType.NotFound => NotFound(new { message = error.Message }),
                _ => StatusCode(500, new { message = error.Message })
            }
        );
    }

    /// <summary>
    /// Crear un nuevo producto.
    /// POST /api/productos
    /// Returns: 201 Created | 400 Bad Request | 401 Unauthorized | 404 Not Found
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ProductoDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Authorize(Policy = "RequireUserRole")]
    public async Task<IActionResult> Create([FromBody] ProductoRequestDto dto)
    {
        logger.LogInformation("Creando nuevo producto: {Nombre}", dto.Nombre);

        var resultado = await service.CreateAsync(dto);

        return resultado.Match(
            onSuccess: producto => CreatedAtAction(nameof(GetById), new { id = producto.Id }, producto),
            onFailure: error => error.Type switch
            {
                ErrorType.Validation => BadRequest(new { message = error.Message, errors = error.ValidationErrors }),
                ErrorType.NotFound => NotFound(new { message = error.Message }),
                ErrorType.Conflict => Conflict(new { message = error.Message }),
                _ => StatusCode(500, new { message = error.Message })
            }
        );
    }

    /// <summary>
    /// Actualizar un producto existente.
    /// PUT /api/productos/{id}
    /// Returns: 200 OK | 404 Not Found | 400 Bad Request | 401 Unauthorized
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ProductoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [Authorize(Policy = "RequireUserRole")]
    public async Task<IActionResult> Update(long id, [FromBody] ProductoRequestDto dto)
    {
        logger.LogInformation("Actualizando producto con ID: {Id}", id);

        var resultado = await service.UpdateAsync(id, dto);

        return resultado.Match(
            onSuccess: producto => Ok(producto),
            onFailure: error => error.Type switch
            {
                ErrorType.NotFound => NotFound(new { message = error.Message }),
                ErrorType.Validation => BadRequest(new { message = error.Message, errors = error.ValidationErrors }),
                _ => StatusCode(500, new { message = error.Message })
            }
        );
    }

    /// <summary>
    /// Eliminar un producto.
    /// DELETE /api/productos/{id}
    /// Returns: 204 No Content | 404 Not Found | 401 Unauthorized
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [Authorize(Policy = "RequireUserRole")]
    public async Task<IActionResult> Delete(long id)
    {
        logger.LogInformation("Eliminando producto con ID: {Id}", id);

        var resultado = await service.DeleteAsync(id);

        if (resultado.IsSuccess)
            return NoContent();

        var error = resultado.Error;
        return error.Type switch
        {
            ErrorType.NotFound => NotFound(new { message = error.Message }),
            _ => StatusCode(500, new { message = error.Message })
        };
    }
}

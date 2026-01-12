using CSharpFunctionalExtensions;
using Microsoft.AspNetCore.Mvc;
using TiendaApi.Dtos.Categorias;
using TiendaApi.Errors;
using TiendaApi.Services.Categorias;

namespace TiendaApi.Controllers;

/// <summary>
/// Controlador de categorías usando Patrón Result.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class CategoriasController(
    ICategoriaService service,
    ILogger<CategoriasController> logger
) : ControllerBase {

    /// <summary>
    /// Obtener todas las categorías.
    /// GET /api/categorias
    /// Returns: 200 OK
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<CategoriaDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll() {
        logger.LogInformation("Obteniendo todas las categorías");
        
        var resultado = await service.FindAllAsync();
        
        return resultado.Match(
            onSuccess: categorias => Ok(categorias),
            onFailure: error => StatusCode(500, new { message = error.Message })
        );
    }

    /// <summary>
    /// Obtener una categoría por ID.
    /// GET /api/categorias/{id}
    /// Returns: 200 OK | 404 Not Found
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(CategoriaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(long id) {
        logger.LogInformation("Obteniendo categoría con ID: {Id}", id);
        
        var resultado = await service.FindByIdAsync(id);
        
        return resultado.Match(
            onSuccess: categoria => Ok(categoria),
            onFailure: error => error.Type switch {
                ErrorType.NotFound => NotFound(new { message = error.Message }),
                _ => StatusCode(500, new { message = error.Message })
            }
        );
    }

    /// <summary>
    /// Crear una nueva categoría.
    /// POST /api/categorias
    /// Returns: 201 Created | 400 Bad Request | 409 Conflict
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(CategoriaDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CategoriaRequestDto dto) {
        logger.LogInformation("Creando nueva categoría: {Nombre}", dto.Nombre);
        
        var resultado = await service.CreateAsync(dto);
        
        return resultado.Match(
            onSuccess: categoria => CreatedAtAction(nameof(GetById), new { id = categoria.Id }, categoria),
            onFailure: error => error.Type switch {
                ErrorType.Validation => BadRequest(new { message = error.Message }),
                ErrorType.Conflict => Conflict(new { message = error.Message }),
                _ => StatusCode(500, new { message = error.Message })
            }
        );
    }

    /// <summary>
    /// Actualizar una categoría existente.
    /// PUT /api/categorias/{id}
    /// Returns: 200 OK | 404 Not Found | 400 Bad Request | 409 Conflict
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(CategoriaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(long id, [FromBody] CategoriaRequestDto dto) {
        logger.LogInformation("Actualizando categoría con ID: {Id}", id);
        
        var resultado = await service.UpdateAsync(id, dto);
        
        return resultado.Match(
            onSuccess: categoria => Ok(categoria),
            onFailure: error => error.Type switch {
                ErrorType.NotFound => NotFound(new { message = error.Message }),
                ErrorType.Validation => BadRequest(new { message = error.Message }),
                ErrorType.Conflict => Conflict(new { message = error.Message }),
                _ => StatusCode(500, new { message = error.Message })
            }
        );
    }

    /// <summary>
    /// Eliminar una categoría.
    /// DELETE /api/categorias/{id}
    /// Returns: 204 No Content | 404 Not Found
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(long id) {
        logger.LogInformation("Eliminando categoría con ID: {Id}", id);
        
        var resultado = await service.DeleteAsync(id);
        
        if (resultado.IsSuccess)
            return NoContent();
        
        var error = resultado.Error;
        return error.Type switch {
            ErrorType.NotFound => NotFound(new { message = error.Message }),
            _ => StatusCode(500, new { message = error.Message })
        };
    }
}

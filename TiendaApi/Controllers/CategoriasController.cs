using Microsoft.AspNetCore.Mvc;
using TiendaApi.Exceptions;
using TiendaApi.Models.DTOs;
using TiendaApi.Services;

namespace TiendaApi.Controllers;

/// <summary>
/// Controller for Categorías using TRADITIONAL EXCEPTION-BASED approach
/// 
/// This controller demonstrates the familiar Java/Spring Boot pattern:
/// - try/catch blocks in every action
/// - Service methods throw exceptions
/// - Manual exception-to-HTTP mapping
/// 
/// Java Spring Boot equivalent:
/// @RestController
/// @RequestMapping("/api/categorias")
/// public class CategoriaController { ... }
/// 
/// EDUCATIONAL NOTE: Compare this with ProductosController (Result Pattern)
/// 
/// Traditional Exception Handling:
/// - Familiar to Java developers
/// - @ExceptionHandler in Spring → GlobalExceptionHandler middleware in C#
/// - Hidden control flow (exceptions can be thrown anywhere)
/// - Performance overhead of exception stack unwinding
/// 
/// When to use this approach:
/// - When porting from Java/Spring Boot
/// - Team familiar with exceptions
/// - Simple CRUD with standard errors
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class CategoriasController : ControllerBase
{
    private readonly CategoriaService _service;
    private readonly ILogger<CategoriasController> _logger;

    public CategoriasController(CategoriaService service, ILogger<CategoriasController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// Get all categories
    /// GET /api/categorias
    /// </summary>
    /// <returns>List of categories</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<CategoriaDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            var categorias = await _service.FindAllAsync();
            return Ok(categorias);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all categorias");
            // GlobalExceptionHandler will catch this
            throw;
        }
    }

    /// <summary>
    /// Get category by ID
    /// GET /api/categorias/{id}
    /// 
    /// Java: @GetMapping("/{id}")
    /// </summary>
    /// <param name="id">Category ID</param>
    /// <returns>Category details</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(CategoriaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(long id)
    {
        try
        {
            // Service throws NotFoundException if not found
            var categoria = await _service.FindByIdAsync(id);
            return Ok(categoria);
        }
        catch (NotFoundException ex)
        {
            // Manual catch - or let GlobalExceptionHandler handle it
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting categoria {Id}", id);
            throw;
        }
    }

    /// <summary>
    /// Create new category
    /// POST /api/categorias
    /// 
    /// Java: @PostMapping
    ///       @Valid @RequestBody CategoriaRequestDto dto
    /// </summary>
    /// <param name="dto">Category data</param>
    /// <returns>Created category</returns>
    [HttpPost]
    [ProducesResponseType(typeof(CategoriaDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CategoriaRequestDto dto)
    {
        try
        {
            // Service throws ValidationException if invalid
            var created = await _service.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (ValidationException ex)
        {
            // Manual catch - or let GlobalExceptionHandler handle it
            return BadRequest(new { message = ex.Message, errors = ex.Errors });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating categoria");
            throw;
        }
    }

    /// <summary>
    /// Update existing category
    /// PUT /api/categorias/{id}
    /// 
    /// Java: @PutMapping("/{id}")
    /// </summary>
    /// <param name="id">Category ID</param>
    /// <param name="dto">Updated category data</param>
    /// <returns>Updated category</returns>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(CategoriaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update(long id, [FromBody] CategoriaRequestDto dto)
    {
        try
        {
            // Service throws NotFoundException or ValidationException
            var updated = await _service.UpdateAsync(id, dto);
            return Ok(updated);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Message, errors = ex.Errors });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating categoria {Id}", id);
            throw;
        }
    }

    /// <summary>
    /// Delete category
    /// DELETE /api/categorias/{id}
    /// 
    /// Java: @DeleteMapping("/{id}")
    /// </summary>
    /// <param name="id">Category ID</param>
    /// <returns>No content</returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(long id)
    {
        try
        {
            // Service throws NotFoundException if not found
            await _service.DeleteAsync(id);
            return NoContent();
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting categoria {Id}", id);
            throw;
        }
    }
}

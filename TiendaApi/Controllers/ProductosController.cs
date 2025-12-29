using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TiendaApi.Common;
using TiendaApi.Models.DTOs;
using TiendaApi.Services;

namespace TiendaApi.Controllers;

/// <summary>
/// Controller for Productos using MODERN RESULT PATTERN approach
/// 
/// This controller demonstrates the functional programming pattern:
/// - NO try/catch blocks needed
/// - Service methods return Result<T, AppError>
/// - Pattern matching to convert Result to HTTP response
/// 
/// Java comparison:
/// - Similar to Either<Error, Value>.fold()
/// - Like CompletableFuture.handle()
/// - Vavr's Try.fold() pattern
/// 
/// EDUCATIONAL NOTE: Compare this with CategoriasController (Exception-based)
/// 
/// Result Pattern Benefits:
/// 1. NO try/catch blocks cluttering code
/// 2. Explicit in method signature what can fail
/// 3. Type-safe error handling
/// 4. Easier to test (no exception mocking)
/// 5. Better performance (no stack unwinding)
/// 6. Functional programming style
/// 
/// When to use this approach:
/// - Complex business logic with multiple failure paths
/// - Performance-critical code
/// - Functional programming mindset
/// - Want explicit error handling
/// - Modern greenfield projects
/// 
/// PROTECTED: Requires JWT authentication for POST, PUT, DELETE operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ProductosController : ControllerBase
{
    private readonly ProductoService _service;
    private readonly ILogger<ProductosController> _logger;

    public ProductosController(ProductoService service, ILogger<ProductosController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// Get all products (public access)
    /// GET /api/productos
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ProductoDto>), StatusCodes.Status200OK)]
    [AllowAnonymous]
    public async Task<IActionResult> GetAll()
    {
        // NO try/catch needed - Result Pattern handles errors
        var resultado = await _service.FindAllAsync();
        
        // Pattern matching - clean and explicit
        return resultado.Match(
            onSuccess: productos => Ok(productos),
            onFailure: error => StatusCode(500, new { message = error.Message })
        );
    }

    /// <summary>
    /// Get product by ID (public access)
    /// GET /api/productos/{id}
    /// 
    /// NO try/catch - errors are values returned by service
    /// Pattern matching converts Result to HTTP response
    /// 
    /// Java equivalent:
    /// return service.findById(id)
    ///     .fold(
    ///         error -> ResponseEntity.status(error.toHttpStatus()).body(error),
    ///         product -> ResponseEntity.ok(product)
    ///     );
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ProductoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(long id)
    {
        var resultado = await _service.FindByIdAsync(id);
        
        // Match pattern - convert Result to IActionResult
        // Much cleaner than try/catch!
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
    /// Get products by category (public access)
    /// GET /api/productos/categoria/{categoriaId}
    /// </summary>
    [HttpGet("categoria/{categoriaId}")]
    [ProducesResponseType(typeof(IEnumerable<ProductoDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [AllowAnonymous]
    public async Task<IActionResult> GetByCategoria(long categoriaId)
    {
        var resultado = await _service.FindByCategoriaIdAsync(categoriaId);
        
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
    /// Create new product (requires authentication)
    /// POST /api/productos
    /// 
    /// NO try/catch for validation errors
    /// Service returns Result with validation errors as value
    /// 
    /// Compare with CategoriasController.Create() - no try/catch!
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ProductoDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Authorize(Policy = "RequireUserRole")]
    public async Task<IActionResult> Create([FromBody] ProductoRequestDto dto)
    {
        var resultado = await _service.CreateAsync(dto);
        
        return resultado.Match(
            onSuccess: producto => CreatedAtAction(
                nameof(GetById),
                new { id = producto.Id },
                producto
            ),
            onFailure: error => error.Type switch
            {
                ErrorType.Validation => BadRequest(new 
                { 
                    message = error.Message,
                    errors = error.ValidationErrors 
                }),
                ErrorType.NotFound => NotFound(new { message = error.Message }),
                _ => StatusCode(500, new { message = error.Message })
            }
        );
    }

    /// <summary>
    /// Update existing product (requires authentication)
    /// PUT /api/productos/{id}
    /// 
    /// Multiple failure scenarios handled cleanly with pattern matching
    /// NO try/catch cascades!
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ProductoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [Authorize(Policy = "RequireUserRole")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update(long id, [FromBody] ProductoRequestDto dto)
    {
        var resultado = await _service.UpdateAsync(id, dto);
        
        // Clean error handling - all cases explicit
        return resultado.Match(
            onSuccess: producto => Ok(producto),
            onFailure: error => error.Type switch
            {
                ErrorType.NotFound => NotFound(new { message = error.Message }),
                ErrorType.Validation => BadRequest(new 
                { 
                    message = error.Message,
                    errors = error.ValidationErrors 
                }),
                _ => StatusCode(500, new { message = error.Message })
            }
        );
    }

    /// <summary>
    /// Delete product (requires authentication)
    /// DELETE /api/productos/{id}
    /// 
    /// Result<AppError> for void operations
    /// Still type-safe and explicit!
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [Authorize(Policy = "RequireUserRole")]
    public async Task<IActionResult> Delete(long id)
    {
        var resultado = await _service.DeleteAsync(id);
        
        return resultado.Match<IActionResult>(
            onSuccess: () => NoContent(),
            onFailure: error => error.Type switch
            {
                ErrorType.NotFound => NotFound(new { message = error.Message }),
                _ => StatusCode(500, new { message = error.Message })
            }
        );
    }
}

/// <summary>
/// SUMMARY: Exception vs Result Pattern Comparison
/// 
/// ╔════════════════════════════════╦═════════════════════════════════╗
/// ║   Traditional Exceptions       ║      Result Pattern             ║
/// ╠════════════════════════════════╬═════════════════════════════════╣
/// ║ try/catch in every method      ║ No try/catch needed             ║
/// ║ throw new NotFoundException()  ║ return AppError.NotFound()      ║
/// ║ Hidden control flow            ║ Explicit error paths            ║
/// ║ Performance overhead           ║ Better performance              ║
/// ║ Familiar to Java devs          ║ Functional programming style    ║
/// ║ GlobalExceptionHandler         ║ Pattern matching in controller  ║
/// ║ @ExceptionHandler in Spring    ║ result.Match() in C#            ║
/// ╚════════════════════════════════╩═════════════════════════════════╝
/// 
/// Choose Exceptions when:
/// - Team familiar with Spring Boot
/// - Simple CRUD operations
/// - Standard HTTP errors only
/// 
/// Choose Result Pattern when:
/// - Complex business logic
/// - Multiple failure scenarios
/// - Want explicit error handling
/// - Performance critical
/// - Modern functional style
/// </summary>

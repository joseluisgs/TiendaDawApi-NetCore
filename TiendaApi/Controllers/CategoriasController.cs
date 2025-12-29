using Microsoft.AspNetCore.Mvc;
using TiendaApi.Common;
using TiendaApi.Models.DTOs;
using TiendaApi.Services;

namespace TiendaApi.Controllers;

/// <summary>
/// Controller para Categorías usando RESULT PATTERN (Railway Oriented Programming)
/// 
/// ANTES (Excepciones):
/// - try/catch en cada acción
/// - Excepciones ocultas en el servicio
/// - Manejo manual de exception-to-HTTP
/// 
/// AHORA (Result Pattern):
/// - SIN try/catch
/// - Errores explícitos en tipo de retorno: Result<T, AppError>
/// - Pattern matching con .Match() para convertir a HTTP
/// - Código más limpio y declarativo
/// 
/// Comparación con Java/Spring Boot:
/// - Java: @ExceptionHandler en @ControllerAdvice
/// - C# con excepciones: GlobalExceptionHandler middleware
/// - C# con Result: Match en el controller (explícito)
/// 
/// Ventajas Result Pattern:
/// 1. Errores visibles en la firma - no hay sorpresas
/// 2. Sin overhead de excepciones
/// 3. Pattern matching expresivo
/// 4. Más fácil testear (sin mocks de excepciones)
/// 5. Railway: encadenar operaciones con .Bind(), .Map(), .Tap()
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class CategoriasController : ControllerBase
{
    private readonly CategoriaService _service;
    private readonly ILogger<CategoriasController> _logger;

    public CategoriasController(CategoriaService service, ILogger<CategoriasController> _logger)
    {
        _service = service;
        this._logger = _logger;
    }

    /// <summary>
    /// Obtiene todas las categorías
    /// GET /api/categorias
    /// 
    /// NOTA PEDAGÓGICA:
    /// Observa que NO hay try/catch. El servicio retorna Result<T, AppError>
    /// y usamos .Match() para convertir Success/Failure a respuesta HTTP.
    /// 
    /// En Java/Spring Boot equivaldría a:
    /// @GetMapping
    /// public ResponseEntity<List<CategoriaDto>> getAll() { ... }
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<CategoriaDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var resultado = await _service.FindAllAsync();
        
        // Pattern matching: convierte Result a IActionResult
        return resultado.Match(
            onSuccess: categorias => Ok(categorias),
            onFailure: error => StatusCode(500, new { message = error.Message })
        );
    }

    /// <summary>
    /// Obtiene una categoría por ID
    /// GET /api/categorias/{id}
    /// 
    /// RAILWAY PATTERN visible:
    /// - Si la categoría existe → 200 OK con los datos
    /// - Si no existe → 404 Not Found
    /// 
    /// Sin try/catch, sin código boilerplate - solo Match
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(CategoriaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(long id)
    {
        var resultado = await _service.FindByIdAsync(id);
        
        return resultado.Match(
            onSuccess: categoria => Ok(categoria),
            onFailure: error => error.Type switch
            {
                ErrorType.NotFound => NotFound(new { message = error.Message }),
                _ => StatusCode(500, new { message = error.Message })
            }
        );
    }

    /// <summary>
    /// Crea una nueva categoría
    /// POST /api/categorias
    /// 
    /// NOTA PEDAGÓGICA - Pattern Matching avanzado:
    /// El switch expression mapea cada ErrorType a su HTTP status correspondiente:
    /// - NotFound → 404
    /// - Validation → 400
    /// - Conflict → 409
    /// - Internal → 500
    /// 
    /// Compara con try/catch tradicional - aquí es explícito y type-safe
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(CategoriaDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CategoriaRequestDto dto)
    {
        var resultado = await _service.CreateAsync(dto);
        
        return resultado.Match(
            onSuccess: categoria => CreatedAtAction(
                nameof(GetById), 
                new { id = categoria.Id }, 
                categoria
            ),
            onFailure: error => error.Type switch
            {
                ErrorType.Validation => BadRequest(new { message = error.Message }),
                ErrorType.Conflict => Conflict(new { message = error.Message }),
                _ => StatusCode(500, new { message = error.Message })
            }
        );
    }

    /// <summary>
    /// Actualiza una categoría existente
    /// PUT /api/categorias/{id}
    /// 
    /// Railway Pattern completo:
    /// - Validar → buscar → verificar duplicados → actualizar
    /// - Cualquier fallo desvía a la vía de error
    /// - El controller solo hace Match al final
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(CategoriaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(long id, [FromBody] CategoriaRequestDto dto)
    {
        var resultado = await _service.UpdateAsync(id, dto);
        
        return resultado.Match(
            onSuccess: categoria => Ok(categoria),
            onFailure: error => error.Type switch
            {
                ErrorType.NotFound => NotFound(new { message = error.Message }),
                ErrorType.Validation => BadRequest(new { message = error.Message }),
                ErrorType.Conflict => Conflict(new { message = error.Message }),
                _ => StatusCode(500, new { message = error.Message })
            }
        );
    }

    /// <summary>
    /// Elimina una categoría (soft delete)
    /// DELETE /api/categorias/{id}
    /// 
    /// NOTA: Usamos Result<Unit, AppError> para operaciones void
    /// Unit es el equivalente funcional de void
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(long id)
    {
        var resultado = await _service.DeleteAsync(id);
        
        return resultado.Match<IActionResult>(
            onSuccess: _ => NoContent(),
            onFailure: error => error.Type switch
            {
                ErrorType.NotFound => NotFound(new { message = error.Message }),
                _ => StatusCode(500, new { message = error.Message })
            }
        );
    }
}

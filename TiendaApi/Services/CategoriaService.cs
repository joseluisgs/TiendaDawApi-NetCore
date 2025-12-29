using AutoMapper;
using TiendaApi.Common;
using TiendaApi.Models.DTOs;
using TiendaApi.Models.Entities;
using TiendaApi.Repositories;

namespace TiendaApi.Services;

/// <summary>
/// Servicio para Categoria usando RESULT PATTERN (Railway Oriented Programming)
/// 
/// ANTES (Excepciones): throw new NotFoundException()
/// AHORA (Result): return Result.Failure(AppError.NotFound(...))
/// 
/// Ventajas del Result Pattern:
/// 1. Errores explícitos en la firma del método (Task<Result<T, AppError>>)
/// 2. Sin overhead de excepciones (no stack unwinding)
/// 3. Más fácil de testear (sin try/catch)
/// 4. Encadenamiento funcional con .Bind(), .Map(), .Tap()
/// 5. Type-safe: el compilador garantiza que se manejen los errores
/// 
/// Comparación con Java/Spring Boot:
/// - Java: Optional<T> + custom Either<Error, Value>
/// - Spring: @ControllerAdvice maneja excepciones → aquí el controller hace Match
/// - Vavr library: Either<L, R> es muy similar a Result<T, E>
/// </summary>
public class CategoriaService
{
    private readonly ICategoriaRepository _repository;
    private readonly IMapper _mapper;
    private readonly ILogger<CategoriaService> _logger;

    public CategoriaService(
        ICategoriaRepository repository,
        IMapper mapper,
        ILogger<CategoriaService> logger)
    {
        _repository = repository;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>
    /// Obtiene todas las categorías
    /// 
    /// NOTA: Este método no puede fallar realmente (retorna lista vacía si no hay datos),
    /// pero usamos Result por consistencia con los demás métodos del servicio.
    /// En Java sería: public List<CategoriaDto> findAll() - nunca lanza excepciones
    /// </summary>
    public async Task<Result<IEnumerable<CategoriaDto>, AppError>> FindAllAsync()
    {
        _logger.LogInformation("Buscando todas las categorías");
        var categorias = await _repository.FindAllAsync();
        var dtos = _mapper.Map<IEnumerable<CategoriaDto>>(categorias);
        return Result<IEnumerable<CategoriaDto>, AppError>.Success(dtos);
    }

    /// <summary>
    /// Busca una categoría por ID
    /// 
    /// Railway Pattern en acción:
    /// - Vía Éxito: Categoría encontrada → mapea a DTO → retorna Success
    /// - Vía Fracaso: No encontrada → retorna Failure con AppError.NotFound
    /// 
    /// NO lanza excepciones - el error está en el tipo de retorno
    /// </summary>
    public async Task<Result<CategoriaDto, AppError>> FindByIdAsync(long id)
    {
        _logger.LogInformation("Buscando categoría con id: {Id}", id);
        
        var categoria = await _repository.FindByIdAsync(id);
        
        if (categoria == null)
        {
            _logger.LogWarning("Categoría con id {Id} no encontrada", id);
            return Result<CategoriaDto, AppError>.Failure(
                AppError.NotFound($"Categoría con ID {id} no encontrada")
            );
        }
        
        var dto = _mapper.Map<CategoriaDto>(categoria);
        return Result<CategoriaDto, AppError>.Success(dto);
    }

    /// <summary>
    /// Crea una nueva categoría
    /// 
    /// Railway Oriented Programming - encadenamiento de validaciones:
    /// 1. ValidateNombre → si falla, todo el pipeline falla
    /// 2. CheckNombreDuplicado → si existe, falla
    /// 3. SaveCategoria → guarda en BD
    /// 4. Map → convierte entidad a DTO
    /// 5. Tap → logging de éxito
    /// 
    /// Cada paso puede desviar a la "vía del fracaso", pero una vez allí,
    /// los pasos siguientes se omiten automáticamente.
    /// </summary>
    public async Task<Result<CategoriaDto, AppError>> CreateAsync(CategoriaRequestDto dto)
    {
        _logger.LogInformation("Creando categoría: {Nombre}", dto.Nombre);
        
        // Validar el nombre
        var validationResult = ValidateNombre(dto.Nombre);
        if (validationResult.IsFailure)
        {
            return Result<CategoriaDto, AppError>.Failure(validationResult.Error);
        }
        
        // Verificar duplicados
        var duplicateCheck = await CheckNombreDuplicado(dto.Nombre);
        if (duplicateCheck.IsFailure)
        {
            return Result<CategoriaDto, AppError>.Failure(duplicateCheck.Error);
        }
        
        // Crear y guardar
        var categoria = _mapper.Map<Categoria>(dto);
        var saved = await _repository.SaveAsync(categoria);
        
        _logger.LogInformation("Categoría creada con id: {Id}", saved.Id);
        var result = _mapper.Map<CategoriaDto>(saved);
        return Result<CategoriaDto, AppError>.Success(result);
    }

    /// <summary>
    /// Actualiza una categoría existente
    /// 
    /// Railway Pattern: validar → buscar → verificar duplicados → actualizar
    /// </summary>
    public async Task<Result<CategoriaDto, AppError>> UpdateAsync(long id, CategoriaRequestDto dto)
    {
        _logger.LogInformation("Actualizando categoría con id: {Id}", id);
        
        // Validar nombre
        var validationResult = ValidateNombre(dto.Nombre);
        if (validationResult.IsFailure)
        {
            return Result<CategoriaDto, AppError>.Failure(validationResult.Error);
        }
        
        // Buscar categoría existente
        var categoria = await _repository.FindByIdAsync(id);
        if (categoria == null)
        {
            _logger.LogWarning("Categoría con id {Id} no encontrada para actualizar", id);
            return Result<CategoriaDto, AppError>.Failure(
                AppError.NotFound($"Categoría con ID {id} no encontrada")
            );
        }
        
        // Verificar duplicados (excluyendo el ID actual)
        var duplicateCheck = await CheckNombreDuplicado(dto.Nombre, id);
        if (duplicateCheck.IsFailure)
        {
            return Result<CategoriaDto, AppError>.Failure(duplicateCheck.Error);
        }
        
        // Actualizar
        categoria.Nombre = dto.Nombre;
        var updated = await _repository.UpdateAsync(categoria);
        
        _logger.LogInformation("Categoría actualizada con id: {Id}", id);
        var result = _mapper.Map<CategoriaDto>(updated);
        return Result<CategoriaDto, AppError>.Success(result);
    }

    /// <summary>
    /// Elimina una categoría (soft delete)
    /// 
    /// Retorna Result<Unit, AppError> porque la operación no retorna datos,
    /// solo indica éxito o fracaso.
    /// </summary>
    public async Task<Result<Unit, AppError>> DeleteAsync(long id)
    {
        _logger.LogInformation("Eliminando categoría con id: {Id}", id);
        
        var categoria = await _repository.FindByIdAsync(id);
        if (categoria == null)
        {
            _logger.LogWarning("Categoría con id {Id} no encontrada para eliminar", id);
            return Result<Unit, AppError>.Failure(
                AppError.NotFound($"Categoría con ID {id} no encontrada")
            );
        }
        
        await _repository.DeleteAsync(id);
        _logger.LogInformation("Categoría eliminada con id: {Id}", id);
        
        return Result<Unit, AppError>.Success(Unit.Value);
    }

    // ============================================================================
    // MÉTODOS DE VALIDACIÓN PRIVADOS - Retornan Result en lugar de lanzar excepciones
    // ============================================================================

    /// <summary>
    /// Valida el nombre de la categoría
    /// 
    /// ANTES: throw new ValidationException(...)
    /// AHORA: return Result.Failure(AppError.Validation(...))
    /// 
    /// Beneficio: El método es una función pura - dado el mismo input,
    /// siempre retorna el mismo output, sin efectos secundarios ocultos (excepciones).
    /// </summary>
    private Result<bool, AppError> ValidateNombre(string nombre)
    {
        if (string.IsNullOrWhiteSpace(nombre))
        {
            return Result<bool, AppError>.Failure(
                AppError.Validation("El nombre de la categoría es requerido")
            );
        }
        
        if (nombre.Length < 3)
        {
            return Result<bool, AppError>.Failure(
                AppError.Validation("El nombre debe tener al menos 3 caracteres")
            );
        }
        
        if (nombre.Length > 100)
        {
            return Result<bool, AppError>.Failure(
                AppError.Validation("El nombre no puede exceder 100 caracteres")
            );
        }
        
        return Result<bool, AppError>.Success(true);
    }

    /// <summary>
    /// Verifica si existe una categoría con el mismo nombre
    /// 
    /// NOTA PEDAGÓGICA:
    /// Este método es async porque consulta la BD, pero retorna Result.
    /// Muestra cómo combinar async/await con Result Pattern.
    /// </summary>
    private async Task<Result<bool, AppError>> CheckNombreDuplicado(string nombre, long? excludeId = null)
    {
        var exists = await _repository.ExistsByNombreAsync(nombre, excludeId);
        
        if (exists)
        {
            return Result<bool, AppError>.Failure(
                AppError.Conflict($"Ya existe una categoría con el nombre '{nombre}'")
            );
        }
        
        return Result<bool, AppError>.Success(true);
    }
}

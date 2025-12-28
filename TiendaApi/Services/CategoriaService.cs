using AutoMapper;
using TiendaApi.Exceptions;
using TiendaApi.Models.DTOs;
using TiendaApi.Models.Entities;
using TiendaApi.Repositories;

namespace TiendaApi.Services;

/// <summary>
/// Service for Categoria using TRADITIONAL EXCEPTION-BASED approach
/// 
/// This service demonstrates the familiar Java/Spring Boot pattern:
/// - throw new NotFoundException()
/// - throw new ValidationException()
/// - Exceptions caught by GlobalExceptionHandler middleware
/// 
/// Java Spring Boot equivalent:
/// @Service class with methods that throw exceptions
/// @ControllerAdvice catches and maps to HTTP responses
/// 
/// EDUCATIONAL NOTE: Compare this with ProductoService (Result Pattern)
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
    /// Get all categories
    /// Java: public List<CategoriaDto> findAll()
    /// </summary>
    public async Task<IEnumerable<CategoriaDto>> FindAllAsync()
    {
        _logger.LogInformation("Finding all categorias");
        var categorias = await _repository.FindAllAsync();
        return _mapper.Map<IEnumerable<CategoriaDto>>(categorias);
    }

    /// <summary>
    /// Find categoria by ID
    /// Java: public CategoriaDto findById(Long id) throws NotFoundException
    /// 
    /// THROWS NotFoundException if not found - traditional approach
    /// </summary>
    public async Task<CategoriaDto> FindByIdAsync(long id)
    {
        _logger.LogInformation("Finding categoria with id: {Id}", id);
        
        var categoria = await _repository.FindByIdAsync(id);
        
        if (categoria == null)
        {
            _logger.LogWarning("Categoria with id {Id} not found", id);
            throw new NotFoundException($"Categoría con ID {id} no encontrada");
        }
        
        return _mapper.Map<CategoriaDto>(categoria);
    }

    /// <summary>
    /// Create new categoria
    /// Java: public CategoriaDto create(CategoriaRequestDto dto) throws ValidationException
    /// 
    /// THROWS ValidationException if validation fails
    /// </summary>
    public async Task<CategoriaDto> CreateAsync(CategoriaRequestDto dto)
    {
        _logger.LogInformation("Creating categoria: {Nombre}", dto.Nombre);
        
        // Validation - throws exception on error
        await ValidateNombreAsync(dto.Nombre);
        
        var categoria = _mapper.Map<Categoria>(dto);
        var saved = await _repository.SaveAsync(categoria);
        
        _logger.LogInformation("Categoria created with id: {Id}", saved.Id);
        return _mapper.Map<CategoriaDto>(saved);
    }

    /// <summary>
    /// Update existing categoria
    /// THROWS NotFoundException or ValidationException
    /// </summary>
    public async Task<CategoriaDto> UpdateAsync(long id, CategoriaRequestDto dto)
    {
        _logger.LogInformation("Updating categoria with id: {Id}", id);
        
        var categoria = await _repository.FindByIdAsync(id);
        
        if (categoria == null)
        {
            _logger.LogWarning("Categoria with id {Id} not found for update", id);
            throw new NotFoundException($"Categoría con ID {id} no encontrada");
        }
        
        // Validation - throws exception on error
        await ValidateNombreAsync(dto.Nombre, id);
        
        categoria.Nombre = dto.Nombre;
        var updated = await _repository.UpdateAsync(categoria);
        
        _logger.LogInformation("Categoria updated with id: {Id}", id);
        return _mapper.Map<CategoriaDto>(updated);
    }

    /// <summary>
    /// Delete categoria (soft delete)
    /// THROWS NotFoundException if not found
    /// </summary>
    public async Task DeleteAsync(long id)
    {
        _logger.LogInformation("Deleting categoria with id: {Id}", id);
        
        var categoria = await _repository.FindByIdAsync(id);
        
        if (categoria == null)
        {
            _logger.LogWarning("Categoria with id {Id} not found for delete", id);
            throw new NotFoundException($"Categoría con ID {id} no encontrada");
        }
        
        await _repository.DeleteAsync(id);
        _logger.LogInformation("Categoria deleted with id: {Id}", id);
    }

    /// <summary>
    /// Validation method - THROWS ValidationException on error
    /// 
    /// This is the TRADITIONAL approach:
    /// - Validation failures throw exceptions
    /// - GlobalExceptionHandler converts to 400 Bad Request
    /// 
    /// Java Spring Boot: Similar to @Valid with MethodArgumentNotValidException
    /// </summary>
    private async Task ValidateNombreAsync(string nombre, long? excludeId = null)
    {
        if (string.IsNullOrWhiteSpace(nombre))
        {
            throw new ValidationException("El nombre de la categoría es requerido");
        }
        
        if (nombre.Length < 3)
        {
            throw new ValidationException("El nombre debe tener al menos 3 caracteres");
        }
        
        if (nombre.Length > 100)
        {
            throw new ValidationException("El nombre no puede exceder 100 caracteres");
        }
        
        var exists = await _repository.ExistsByNombreAsync(nombre, excludeId);
        if (exists)
        {
            throw new ValidationException($"Ya existe una categoría con el nombre '{nombre}'");
        }
    }
}

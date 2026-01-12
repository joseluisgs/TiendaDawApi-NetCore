using CSharpFunctionalExtensions;
using TiendaApi.Dtos.Categorias;
using TiendaApi.Errors;
using TiendaApi.Mappers;
using TiendaApi.Models;
using TiendaApi.Repositories.Categorias;

namespace TiendaApi.Services.Categorias;

/// <summary>
/// Servicio de categorías usando Patrón Result.
/// Maneja la lógica de negocio: validaciones, verificación de duplicados.
/// </summary>
public class CategoriaService(
    ICategoriaRepository repository,
    ILogger<CategoriaService> logger
) : ICategoriaService {

    /// <summary>
    /// Obtiene todas las categorías.
    /// Returns: Result.Success(List) | Result.Failure nunca
    /// </summary>
    public async Task<Result<IEnumerable<CategoriaDto>, DomainError>> FindAllAsync() {
        logger.LogInformation("Buscando todas las categorías");
        var categorias = await repository.FindAllAsync();
        var dtos = categorias.ToDtoList();
        return Result.Success<IEnumerable<CategoriaDto>, DomainError>(dtos);
    }

    /// <summary>
    /// Obtiene una categoría por su ID.
    /// Returns: Result.Success(CategoriaDto) | Result.Failure(NotFound)
    /// </summary>
    public async Task<Result<CategoriaDto, DomainError>> FindByIdAsync(long id) {
        logger.LogInformation("Buscando categoría con id: {Id}", id);
        
        var categoria = await repository.FindByIdAsync(id);
        
        if (categoria == null) {
            logger.LogWarning("Categoría con id {Id} no encontrada", id);
            return Result.Failure<CategoriaDto, DomainError>(
                DomainError.NotFound($"Categoría con ID {id} no encontrada")
            );
        }
        
        var dto = categoria.ToDto();
        return Result.Success<CategoriaDto, DomainError>(dto);
    }

    /// <summary>
    /// Crea una nueva categoría.
    /// Returns: Result.Success(CategoriaDto) | Result.Failure(Validation/Conflict)
    /// </summary>
    public async Task<Result<CategoriaDto, DomainError>> CreateAsync(CategoriaRequestDto dto) {
        logger.LogInformation("Creando categoría: {Nombre}", dto.Nombre);
        
        var validationResult = ValidateNombre(dto.Nombre);
        if (validationResult.IsFailure) {
            return Result.Failure<CategoriaDto, DomainError>(validationResult.Error);
        }
        
        var duplicateCheck = await CheckNombreDuplicado(dto.Nombre);
        if (duplicateCheck.IsFailure) {
            return Result.Failure<CategoriaDto, DomainError>(duplicateCheck.Error);
        }
        
        var categoria = dto.ToEntity();
        var saved = await repository.SaveAsync(categoria);
        
        logger.LogInformation("Categoría creada con id: {Id}", saved.Id);
        var result = saved.ToDto();
        return Result.Success<CategoriaDto, DomainError>(result);
    }

    /// <summary>
    /// Actualiza una categoría existente.
    /// Returns: Result.Success(CategoriaDto) | Result.Failure(NotFound/Validation/Conflict)
    /// </summary>
    public async Task<Result<CategoriaDto, DomainError>> UpdateAsync(long id, CategoriaRequestDto dto) {
        logger.LogInformation("Actualizando categoría con id: {Id}", id);
        
        var validationResult = ValidateNombre(dto.Nombre);
        if (validationResult.IsFailure) {
            return Result.Failure<CategoriaDto, DomainError>(validationResult.Error);
        }
        
        var categoria = await repository.FindByIdAsync(id);
        if (categoria == null) {
            logger.LogWarning("Categoría con id {Id} no encontrada para actualizar", id);
            return Result.Failure<CategoriaDto, DomainError>(
                DomainError.NotFound($"Categoría con ID {id} no encontrada")
            );
        }
        
        var duplicateCheck = await CheckNombreDuplicado(dto.Nombre, id);
        if (duplicateCheck.IsFailure) {
            return Result.Failure<CategoriaDto, DomainError>(duplicateCheck.Error);
        }
        
        categoria.Nombre = dto.Nombre;
        var updated = await repository.UpdateAsync(categoria);
        
        logger.LogInformation("Categoría actualizada con id: {Id}", id);
        var result = updated.ToDto();
        return Result.Success<CategoriaDto, DomainError>(result);
    }

    /// <summary>
    /// Elimina una categoría.
    /// Returns: UnitResult.Success | UnitResult.Failure(NotFound)
    /// </summary>
    public async Task<UnitResult<DomainError>> DeleteAsync(long id) {
        logger.LogInformation("Eliminando categoría con id: {Id}", id);
        
        var categoria = await repository.FindByIdAsync(id);
        if (categoria == null) {
            logger.LogWarning("Categoría con id {Id} no encontrada para eliminar", id);
            return UnitResult.Failure<DomainError>(
                DomainError.NotFound($"Categoría con ID {id} no encontrada")
            );
        }
        
        await repository.DeleteAsync(id);
        logger.LogInformation("Categoría eliminada con id: {Id}", id);
        
        return UnitResult.Success<DomainError>();
    }

    /// <summary>
    /// Valida el nombre de la categoría.
    /// Returns: Result.Success(true) | Result.Failure(Validation)
    /// </summary>
    private Result<bool, DomainError> ValidateNombre(string nombre) {
        if (string.IsNullOrWhiteSpace(nombre)) {
            return Result.Failure<bool, DomainError>(
                DomainError.Validation("El nombre de la categoría es requerido")
            );
        }
        
        if (nombre.Length < 3) {
            return Result.Failure<bool, DomainError>(
                DomainError.Validation("El nombre debe tener al menos 3 caracteres")
            );
        }
        
        if (nombre.Length > 100) {
            return Result.Failure<bool, DomainError>(
                DomainError.Validation("El nombre no puede exceder 100 caracteres")
            );
        }
        
        return Result.Success<bool, DomainError>(true);
    }

    /// <summary>
    /// Verifica si el nombre ya existe en otra categoría.
    /// Returns: Result.Success(true) | Result.Failure(Conflict)
    /// </summary>
    private async Task<Result<bool, DomainError>> CheckNombreDuplicado(string nombre, long? excludeId = null) {
        var exists = await repository.ExistsByNombreAsync(nombre, excludeId);
        
        if (exists) {
            return Result.Failure<bool, DomainError>(
                DomainError.Conflict($"Ya existe una categoría con el nombre '{nombre}'")
            );
        }
        
        return Result.Success<bool, DomainError>(true);
    }
}

using CSharpFunctionalExtensions;
using FluentValidation;
using TiendaApi.Apis.Dtos.Categorias;
using TiendaApi.Apis.Dtos.Common;
using TiendaApi.Apis.Errors;
using TiendaApi.Apis.Errors.Categorias;
using TiendaApi.Apis.Mappers;
using TiendaApi.Apis.Models;
using TiendaApi.Apis.Repositories.Categorias;
using TiendaApi.Apis.Services.Cache;
using TiendaApi.Apis.Validators.Categorias;

namespace TiendaApi.Apis.Services.Categorias;

/// <summary>
/// Servicio de categorías usando Patrón Result.
/// Maneja la lógica de negocio: validaciones, verificación de duplicados.
/// </summary>
public class CategoriaService(
    ICategoriaRepository repository,
    ILogger<CategoriaService> logger,
    IValidator<CategoriaRequestDto> categoriaValidator,
    ICacheService cacheService,
    IConfiguration configuration
) : ICategoriaService
{

    /// <summary>
    /// Obtiene todas las categorías.
    /// Devuelve: Result.Success(List) | Result.Failure nunca
    /// </summary>
    public async Task<Result<IEnumerable<CategoriaDto>, DomainError>> FindAllAsync()
    {
        logger.LogInformation("Buscando todas las categorías");

        const string cacheKey = "categorias:all";
        var cachedCategorias = await cacheService.GetAsync<IEnumerable<CategoriaDto>>(cacheKey);

        if (cachedCategorias is not null)
        {
            logger.LogInformation("Devolviendo categorías desde caché");
            return Result.Success<IEnumerable<CategoriaDto>, DomainError>(cachedCategorias);
        }

        var categorias = await repository.FindAllAsync();
        var dtos = categorias.ToDtoList();

        var cacheTTL = TimeSpan.FromMinutes(
            int.Parse(configuration["Cache:CategoriaCacheTTLMinutes"] ?? "10"));
        await cacheService.SetAsync(cacheKey, dtos, cacheTTL);

        return Result.Success<IEnumerable<CategoriaDto>, DomainError>(dtos);
    }

    /// <summary>
    /// Obtiene categorías paginadas con filtros.
    /// Devuelve: Result.Success(PagedResult) | Result.Failure nunca
    /// </summary>
    public async Task<Result<PagedResult<CategoriaDto>, DomainError>> FindAllPagedAsync(CategoriaFilterDto filter)
    {
        logger.LogInformation("Obteniendo categorías paginadas - Página: {Page}, Tamaño: {Size}", filter.Page, filter.Size);

        var (categorias, totalCount) = await repository.FindAllPagedAsync(filter);
        var dtos = categorias.ToDtoList();

        var pagedResult = new PagedResult<CategoriaDto>
        {
            Items = dtos,
            TotalCount = totalCount,
            Page = filter.Page + 1,
            PageSize = filter.Size
        };

        return Result.Success<PagedResult<CategoriaDto>, DomainError>(pagedResult);
    }

    /// <summary>
    /// Obtiene una categoría por su ID.
    /// Devuelve: Result.Success(CategoriaDto) | Result.Failure(NotFound)
    /// </summary>
    public async Task<Result<CategoriaDto, DomainError>> FindByIdAsync(long id)
    {
        logger.LogInformation("Buscando categoría con id: {Id}", id);

        var cacheKey = $"categorias:{id}";
        var cachedCategoria = await cacheService.GetAsync<CategoriaDto>(cacheKey);

        if (cachedCategoria is not null)
        {
            logger.LogInformation("Devolviendo categoría desde caché: {Id}", id);
            return Result.Success<CategoriaDto, DomainError>(cachedCategoria);
        }

        var categoria = await repository.FindByIdAsync(id);

        if (categoria is null)
        {
            logger.LogWarning("Categoría con id {Id} no encontrada", id);
            return Result.Failure<CategoriaDto, DomainError>(
                CategoriaError.NotFound(id)
            );
        }

        var dto = categoria.ToDto();

        var cacheTTL = TimeSpan.FromMinutes(
            int.Parse(configuration["Cache:CategoriaCacheTTLMinutes"] ?? "10"));
        await cacheService.SetAsync(cacheKey, dto, cacheTTL);

        return Result.Success<CategoriaDto, DomainError>(dto);
    }

    /// <summary>
    /// Crea una nueva categoría.
    /// Devuelve: Result.Success(CategoriaDto) | Result.Failure(Validation/Conflict)
    /// </summary>
    public async Task<Result<CategoriaDto, DomainError>> CreateAsync(CategoriaRequestDto dto)
    {
        logger.LogInformation("Creando categoría: {Nombre}", dto.Nombre);

        var validationResult = await ValidateCategoriaAsync(dto);
        if (validationResult.IsFailure)
        {
            return Result.Failure<CategoriaDto, DomainError>(validationResult.Error);
        }

        var duplicateCheck = await CheckNombreDuplicado(dto.Nombre);
        if (duplicateCheck.IsFailure)
        {
            return Result.Failure<CategoriaDto, DomainError>(duplicateCheck.Error);
        }

        var categoria = dto.ToEntity();
        var saved = await repository.SaveAsync(categoria);

        logger.LogInformation("Categoría creada con id: {Id}", saved.Id);
        var result = saved.ToDto();

        _ = Task.Run(async () =>
        {
            try
            {
                await cacheService.RemoveAsync("categorias:all");
                await cacheService.RemoveAsync($"categorias:{saved.Id}");
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, $"Cache invalidation error: Key=categorias:all,categorias:{saved.Id}");
            }
        });

        return Result.Success<CategoriaDto, DomainError>(result);
    }

    /// <summary>
    /// Actualiza una categoría existente.
    /// Devuelve: Result.Success(CategoriaDto) | Result.Failure(NotFound/Validation/Conflict)
    /// </summary>
    public async Task<Result<CategoriaDto, DomainError>> UpdateAsync(long id, CategoriaRequestDto dto)
    {
        logger.LogInformation("Actualizando categoría con id: {Id}", id);

        var validationResult = await ValidateCategoriaAsync(dto);
        if (validationResult.IsFailure)
        {
            return Result.Failure<CategoriaDto, DomainError>(validationResult.Error);
        }

        var categoria = await repository.FindByIdAsync(id);
        if (categoria is null)
        {
            logger.LogWarning("Categoría con id {Id} no encontrada para actualizar", id);
            return Result.Failure<CategoriaDto, DomainError>(
                CategoriaError.NotFound(id)
            );
        }

        var duplicateCheck = await CheckNombreDuplicado(dto.Nombre, id);
        if (duplicateCheck.IsFailure)
        {
            return Result.Failure<CategoriaDto, DomainError>(duplicateCheck.Error);
        }

        categoria.Nombre = dto.Nombre;
        var updated = await repository.UpdateAsync(categoria);

        logger.LogInformation("Categoría actualizada con id: {Id}", id);
        var result = updated.ToDto();

        _ = Task.Run(async () =>
        {
            try
            {
                await cacheService.RemoveAsync("categorias:all");
                await cacheService.RemoveAsync($"categorias:{id}");
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, $"Cache invalidation error: Key=categorias:all,categorias:{id}");
            }
        });

        return Result.Success<CategoriaDto, DomainError>(result);
    }

    /// <summary>
    /// Elimina una categoría.
    /// Devuelve: UnitResult.Success | UnitResult.Failure(NotFound)
    /// </summary>
    public async Task<UnitResult<DomainError>> DeleteAsync(long id)
    {
        logger.LogInformation("Eliminando categoría con id: {Id}", id);

        var categoria = await repository.FindByIdAsync(id);
        if (categoria is null)
        {
            logger.LogWarning("Categoría con id {Id} no encontrada para eliminar", id);
            return UnitResult.Failure<DomainError>(
                CategoriaError.NotFound(id)
            );
        }

        await repository.DeleteAsync(id);
        logger.LogInformation("Categoría eliminada con id: {Id}", id);

        _ = Task.Run(async () =>
        {
            try
            {
                await cacheService.RemoveAsync("categorias:all");
                await cacheService.RemoveAsync($"categorias:{id}");
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, $"Cache invalidation error: Key=categorias:all,categorias:{id}");
            }
        });

        return UnitResult.Success<DomainError>();
    }

    /// <summary>
    /// Valida la categoría usando FluentValidation.
    /// Devuelve: UnitResult.Success | UnitResult.Failure(Validation)
    /// </summary>
    private async Task<UnitResult<DomainError>> ValidateCategoriaAsync(CategoriaRequestDto dto)
    {
        var validationResult = await categoriaValidator.ValidateAsync(dto);

        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.ErrorMessage).ToArray()
                );

            return UnitResult.Failure<DomainError>(
                CategoriaError.ValidacionConCampos(errors)
            );
        }

        return UnitResult.Success<DomainError>();
    }

    /// <summary>
    /// Verifica si el nombre ya existe en otra categoría.
    /// Devuelve: Result.Success(true) | Result.Failure(Conflict)
    /// </summary>
    private async Task<Result<bool, DomainError>> CheckNombreDuplicado(string nombre, long? excludeId = null)
    {
        var exists = await repository.ExistsByNombreAsync(nombre, excludeId);

        if (exists)
        {
            return Result.Failure<bool, DomainError>(
                CategoriaError.NombreDuplicado(nombre)
            );
        }

        return Result.Success<bool, DomainError>(true);
    }
}

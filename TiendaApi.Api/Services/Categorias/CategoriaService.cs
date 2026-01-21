using CSharpFunctionalExtensions;
using FluentValidation;
using TiendaApi.Api.Dtos.Categorias;
using TiendaApi.Api.Dtos.Common;
using TiendaApi.Api.Errors;
using TiendaApi.Api.Errors.Categorias;
using TiendaApi.Api.Mappers;
using TiendaApi.Api.Models;
using TiendaApi.Api.Repositories.Categorias;
using TiendaApi.Api.Services.Cache;
using TiendaApi.Api.Validators.Categorias;

namespace TiendaApi.Api.Services.Categorias;

/// <summary>
/// Implementación del servicio de categorías.
/// </summary>
public class CategoriaService(
    ICategoriaRepository repository,
    ILogger<CategoriaService> logger,
    IValidator<CategoriaRequestDto> categoriaValidator,
    ICacheService cacheService,
    IConfiguration configuration
) : ICategoriaService
{
    private readonly TimeSpan _cacheTTL = TimeSpan.FromMinutes(
        int.Parse(configuration["Cache:CategoriaCacheTTLMinutes"] ?? "10"));

    /// <inheritdoc/>
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

        return Result.Success<IEnumerable<CategoriaDto>, DomainError>(dtos)
            .Tap(_ => AñadirCacheCategoria(cacheKey, dtos));
    }

    /// <inheritdoc/>
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

    /// <inheritdoc/>
    public async Task<Result<CategoriaDto, DomainError>> FindByIdAsync(long id)
    {
        logger.LogInformation("Buscando categoría: {Id}", id);

        var cacheKey = $"categorias:{id}";
        var cachedCategoria = await cacheService.GetAsync<CategoriaDto>(cacheKey);

        if (cachedCategoria is not null)
            return Result.Success<CategoriaDto, DomainError>(cachedCategoria);

        var categoria = await repository.FindByIdAsync(id);

        if (categoria is null)
            return Result.Failure<CategoriaDto, DomainError>(CategoriaError.NotFound(id));

        var dto = categoria.ToDto();
        return Result.Success<CategoriaDto, DomainError>(dto)
            .Tap(_ => AñadirCacheCategoria(cacheKey, dto));
    }

    /// <inheritdoc/>
    public async Task<Result<CategoriaDto, DomainError>> CreateAsync(CategoriaRequestDto dto)
    {
        logger.LogInformation("Creando categoría: {Nombre}", dto.Nombre);

        var validationResult = await ValidateCategoriaAsync(dto);
        if (validationResult.IsFailure)
            return Result.Failure<CategoriaDto, DomainError>(validationResult.Error);

        var duplicateCheck = await CheckNombreDuplicado(dto.Nombre, null);
        if (duplicateCheck.IsFailure)
            return Result.Failure<CategoriaDto, DomainError>(duplicateCheck.Error);

        var saved = await repository.SaveAsync(dto.ToEntity());
        var result = saved.ToDto();

        return Result.Success<CategoriaDto, DomainError>(result)
            .Tap(_ =>
            {
                logger.LogInformation("Categoría creada: {Id}", saved.Id);
                InvalidarCacheCategoria("categorias:all", $"categorias:{result.Id}");
            });
    }

    /// <inheritdoc/>
    public async Task<Result<CategoriaDto, DomainError>> UpdateAsync(long id, CategoriaRequestDto dto)
    {
        logger.LogInformation("Actualizando categoría: {Id}", id);

        var validationResult = await ValidateCategoriaAsync(dto);
        if (validationResult.IsFailure)
            return Result.Failure<CategoriaDto, DomainError>(validationResult.Error);

        var categoria = await repository.FindByIdAsync(id);
        if (categoria is null)
            return Result.Failure<CategoriaDto, DomainError>(CategoriaError.NotFound(id));

        var duplicateCheck = await CheckNombreDuplicado(dto.Nombre, id);
        if (duplicateCheck.IsFailure)
            return Result.Failure<CategoriaDto, DomainError>(duplicateCheck.Error);

        categoria.Nombre = dto.Nombre;
        var updated = await repository.UpdateAsync(categoria);
        var result = updated.ToDto();

        return Result.Success<CategoriaDto, DomainError>(result)
            .Tap(_ =>
            {
                logger.LogInformation("Categoría actualizada: {Id}", id);
                InvalidarCacheCategoria("categorias:all", $"categorias:{id}");
            });
    }

    /// <inheritdoc/>
    public async Task<UnitResult<DomainError>> DeleteAsync(long id)
    {
        logger.LogInformation("Eliminando categoría: {Id}", id);

        var categoria = await repository.FindByIdAsync(id);
        if (categoria is null)
            return UnitResult.Failure<DomainError>(CategoriaError.NotFound(id));

        await repository.DeleteAsync(id);
        logger.LogInformation("Categoría eliminada: {Id}", id);

        InvalidarCacheCategoria("categorias:all", $"categorias:{id}");

        return UnitResult.Success<DomainError>();
    }

    private void AñadirCacheCategoria<T>(string key, T value)
    {
        _ = Task.Run(async () =>
        {
            try { await cacheService.SetAsync(key, value, _cacheTTL); }
            catch (Exception ex) { logger.LogWarning(ex, "Error adding to cache: Key={Key}", key); }
        });
    }

    private void InvalidarCacheCategoria(params string[] keys)
    {
        _ = Task.Run(async () =>
        {
            foreach (var key in keys)
            {
                try { await cacheService.RemoveAsync(key); }
                catch (Exception ex) { logger.LogWarning(ex, "Cache invalidation error: Key={Key}", key); }
            }
        });
    }

    private async Task<UnitResult<DomainError>> ValidateCategoriaAsync(CategoriaRequestDto dto)
    {
        var validationResult = await categoriaValidator.ValidateAsync(dto);

        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

            return UnitResult.Failure<DomainError>(CategoriaError.ValidacionConCampos(errors));
        }

        return UnitResult.Success<DomainError>();
    }

    private async Task<Result<bool, DomainError>> CheckNombreDuplicado(string nombre, long? excludeId = null)
    {
        var exists = await repository.ExistsByNombreAsync(nombre, excludeId);

        if (exists)
            return Result.Failure<bool, DomainError>(CategoriaError.NombreDuplicado(nombre));

        return Result.Success<bool, DomainError>(true);
    }
}

using CSharpFunctionalExtensions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;
using TiendaApi.Api.Dtos.Common;
using TiendaApi.Api.Dtos.Proveedores;
using TiendaApi.Api.Errors;
using TiendaApi.Api.Errors.Proveedores;
using TiendaApi.Api.Mappers;
using TiendaApi.Api.Models;
using TiendaApi.Api.Repositories.Proveedores;
using TiendaApi.Api.Services.Cache;

namespace TiendaApi.Api.Services.Proveedores;

/// <summary>
///     Implementación del servicio de proveedores.
///     La operación de creación separa las dos comprobaciones que pueden fallar:
///     formato del código (validación, 400) y existencia del código o del nombre (409).
///     Si el código llega vacío lo genera el servidor; como la generación es aleatoria,
///     una colisión se detecta por el índice único y se devuelve como conflicto.
/// </summary>
public class ProveedorService(
    IProveedorRepository repository,
    ILogger<ProveedorService> logger,
    IValidator<ProveedorRequestDto> proveedorValidator,
    ICacheService cacheService,
    IConfiguration configuration,
    IOutputCacheStore outputCacheStore
) : IProveedorService {
    private readonly TimeSpan _cacheTTL = TimeSpan.FromMinutes(
        int.Parse(configuration["Cache:ProveedorCacheTTLMinutes"] ?? "10"));

    /// <inheritdoc />
    public async Task<Result<IEnumerable<ProveedorDto>, DomainError>> FindAllAsync() {
        logger.LogInformation("Buscando todos los proveedores");

        const string cacheKey = "proveedores:all";
        IEnumerable<ProveedorDto>? cached = await cacheService.GetAsync<IEnumerable<ProveedorDto>>(cacheKey);

        if (cached is not null) {
            logger.LogInformation("Devolviendo proveedores desde caché");
            return Result.Success<IEnumerable<ProveedorDto>, DomainError>(cached);
        }

        IEnumerable<Proveedor> proveedores = await repository.FindAllAsync();
        IEnumerable<ProveedorDto> dtos = proveedores.ToDtoList();

        return Result.Success<IEnumerable<ProveedorDto>, DomainError>(dtos)
            .Tap(_ => AñadirCache(cacheKey, dtos));
    }

    /// <inheritdoc />
    public async Task<Result<PagedResult<ProveedorDto>, DomainError>> FindAllPagedAsync(ProveedorFilterDto filter) {
        logger.LogInformation("Obteniendo proveedores paginados - Página: {Page}, Tamaño: {Size}", filter.Page,
            filter.Size);

        (IEnumerable<Proveedor> proveedores, int totalCount) = await repository.FindAllPagedAsync(filter);
        IEnumerable<ProveedorDto> dtos = proveedores.ToDtoList();

        var pagedResult = new PagedResult<ProveedorDto> {
            Items = dtos,
            TotalCount = totalCount,
            Page = filter.Page + 1,
            PageSize = filter.Size
        };

        return Result.Success<PagedResult<ProveedorDto>, DomainError>(pagedResult);
    }

    /// <inheritdoc />
    public async Task<Result<ProveedorDto, DomainError>> FindByIdAsync(string codigo) {
        logger.LogInformation("Buscando proveedor: {Codigo}", codigo);

        string cacheKey = $"proveedores:{codigo}";
        ProveedorDto? cached = await cacheService.GetAsync<ProveedorDto>(cacheKey);

        if (cached is not null) {
            return Result.Success<ProveedorDto, DomainError>(cached);
        }

        Proveedor? proveedor = await repository.FindByIdAsync(codigo);

        if (proveedor is null) {
            return Result.Failure<ProveedorDto, DomainError>(ProveedorError.NotFound(codigo));
        }

        ProveedorDto dto = proveedor.ToDto();
        return Result.Success<ProveedorDto, DomainError>(dto)
            .Tap(_ => AñadirCache(cacheKey, dto));
    }

    /// <inheritdoc />
    public async Task<Result<ProveedorDto, DomainError>> CreateAsync(ProveedorRequestDto dto) {
        logger.LogInformation("Creando proveedor: {Nombre}", dto.Nombre);

        UnitResult<DomainError> validationResult = await ValidateAsync(dto);
        if (validationResult.IsFailure) {
            return Result.Failure<ProveedorDto, DomainError>(validationResult.Error);
        }

        Result<bool, DomainError> duplicateCheck = await CheckDuplicados(dto, null);
        if (duplicateCheck.IsFailure) {
            return Result.Failure<ProveedorDto, DomainError>(duplicateCheck.Error);
        }

        try {
            Proveedor saved = await repository.CreateAsync(dto.ToEntity());
            ProveedorDto result = saved.ToDto();

            await InvalidarCacheAsync($"proveedores:{result.Codigo}");

            return Result.Success<ProveedorDto, DomainError>(result);
        } catch (DbUpdateException ex) {
            // Colisión del índice único (nombre o código aleatorio ya existente).
            logger.LogWarning(ex, "Conflicto de unicidad al crear proveedor: {Nombre}", dto.Nombre);
            return Result.Failure<ProveedorDto, DomainError>(ProveedorError.CodigoDuplicado(dto.Codigo ?? dto.Nombre));
        }
    }

    /// <inheritdoc />
    public async Task<Result<ProveedorDto, DomainError>> UpdateAsync(string codigo, ProveedorRequestDto dto) {
        logger.LogInformation("Actualizando proveedor: {Codigo}", codigo);

        UnitResult<DomainError> validationResult = await ValidateAsync(dto);
        if (validationResult.IsFailure) {
            return Result.Failure<ProveedorDto, DomainError>(validationResult.Error);
        }

        Proveedor? proveedor = await repository.FindByIdAsync(codigo);
        if (proveedor is null) {
            return Result.Failure<ProveedorDto, DomainError>(ProveedorError.NotFound(codigo));
        }

        Result<bool, DomainError> duplicateCheck = await CheckDuplicados(dto, codigo);
        if (duplicateCheck.IsFailure) {
            return Result.Failure<ProveedorDto, DomainError>(duplicateCheck.Error);
        }

        dto.UpdateEntity(proveedor);
        Proveedor updated = await repository.UpdateAsync(proveedor);
        ProveedorDto result = updated.ToDto();

        await InvalidarCacheAsync($"proveedores:{codigo}");

        return Result.Success<ProveedorDto, DomainError>(result);
    }

    /// <inheritdoc />
    public async Task<UnitResult<DomainError>> DeleteAsync(string codigo) {
        logger.LogInformation("Eliminando proveedor: {Codigo}", codigo);

        Proveedor? proveedor = await repository.FindByIdAsync(codigo);
        if (proveedor is null) {
            return UnitResult.Failure<DomainError>(ProveedorError.NotFound(codigo));
        }

        int productosCount = await repository.CountProductosAsync(codigo);
        if (productosCount > 0) {
            return UnitResult.Failure<DomainError>(ProveedorError.TieneProductos(codigo, productosCount));
        }

        await repository.DeleteAsync(codigo);
        logger.LogInformation("Proveedor eliminado: {Codigo}", codigo);

        await InvalidarCacheAsync($"proveedores:{codigo}");

        return UnitResult.Success<DomainError>();
    }

    private void AñadirCache<T>(string key, T value) {
        _ = Task.Run(async () => {
            try { await cacheService.SetAsync(key, value, _cacheTTL); } catch (Exception ex) {
                logger.LogWarning(ex, "Error adding to cache: Key={Key}", key);
            }
        });
    }

    private async Task InvalidarCacheAsync(params string[] keys) {
        foreach (string key in keys) {
            try { await cacheService.RemoveAsync(key); } catch (Exception ex) {
                logger.LogWarning(ex, "Cache invalidation error: Key={Key}", key);
            }
        }

        try {
            // Los DTO de producto llevan proveedorNombre: invalidar también el tag de productos.
            await outputCacheStore.EvictByTagAsync("proveedores", CancellationToken.None);
            await outputCacheStore.EvictByTagAsync("productos", CancellationToken.None);
        } catch (Exception ex) {
            logger.LogWarning(ex, "Output cache invalidation error: Tag=proveedores");
        }
    }

    private async Task<UnitResult<DomainError>> ValidateAsync(ProveedorRequestDto dto) {
        ValidationResult validationResult = await proveedorValidator.ValidateAsync(dto);

        if (!validationResult.IsValid) {
            var errors = validationResult.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

            return UnitResult.Failure<DomainError>(ProveedorError.ValidacionConCampos(errors));
        }

        return UnitResult.Success<DomainError>();
    }

    private async Task<Result<bool, DomainError>> CheckDuplicados(ProveedorRequestDto dto, string? excludeCodigo) {
        if (!string.IsNullOrWhiteSpace(dto.Codigo) && dto.Codigo != excludeCodigo) {
            bool codigoExiste = await repository.ExistsByIdAsync(dto.Codigo);
            if (codigoExiste) {
                return Result.Failure<bool, DomainError>(ProveedorError.CodigoDuplicado(dto.Codigo));
            }
        }

        bool nombreExiste = await repository.ExistsByNombreAsync(dto.Nombre, excludeCodigo);
        if (nombreExiste) {
            return Result.Failure<bool, DomainError>(ProveedorError.NombreDuplicado(dto.Nombre));
        }

        return Result.Success<bool, DomainError>(true);
    }
}

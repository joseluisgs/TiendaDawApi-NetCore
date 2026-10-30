using CSharpFunctionalExtensions;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.SignalR;
using TiendaApi.Api.Dtos.Common;
using TiendaApi.Api.Dtos.Productos;
using TiendaApi.Api.Errors;
using TiendaApi.Api.Errors.Productos;
using TiendaApi.Api.GraphQL.Events;
using TiendaApi.Api.GraphQL.Publishers;
using TiendaApi.Api.Mappers;
using TiendaApi.Api.Models;
using TiendaApi.Api.Realtime.Productos;
using TiendaApi.Api.Repositories.Categorias;
using TiendaApi.Api.Repositories.Historial;
using TiendaApi.Api.Repositories.Productos;
using TiendaApi.Api.Services.Cache;
using TiendaApi.Api.Services.Email;
using TiendaApi.Api.Services.Storage;
using TiendaApi.Api.Validators.Productos;

namespace TiendaApi.Api.Services.Productos;

/// <summary>
/// Servicio de productos usando Patrón Result.
/// </summary>
public class ProductoService(
IProductoRepository productoRepository,
IHistorialRepository historialRepository,
ICategoriaRepository categoriaRepository,
ILogger<ProductoService> logger,
ICacheService cacheService,
IConfiguration configuration,
IValidator<ProductoRequestDto> productoValidator,
IStorageService storageService,
IOutputCacheStore outputCacheStore,
IProductoEffects effects
) : IProductoService
{
    private readonly TimeSpan _cacheTTL = TimeSpan.FromMinutes(
        int.Parse(configuration["Cache:ProductoCacheTTLMinutes"] ?? "10"));

    /// <summary>
    /// Obtener todos los productos con patrón cache-aside.
    /// Devuelve: Result.Success(List) | Result.Failure nunca
    /// </summary>
    /// <remarks>
    /// ⚠️ NOTA PARA EL ALUMNO: Este método es un EJEMPLO DIDÁCTICO de cache-aside.
    /// En la práctica, NADIE lo llama — el controller siempre usa <see cref="FindAllPagedAsync"/>.
    /// Este código está aquí para enseñar el patrón. En producción, ELIMINAR este método
    /// porque:
    /// 1. Nunca se ejecuta (dead code).
    /// 2. Cachea "productos:all" que NUNCA se invalida (solo TTL).
    /// 3. Si alguien lo llamara, vería datos stale hasta 10 minutos tras cada escritura.
    /// </remarks>
    public async Task<Result<IEnumerable<ProductoDto>, DomainError>> FindAllAsync()
    {
        logger.LogInformation("Obteniendo todos los productos");

        const string cacheKey = "productos:all";
        var cachedProductos = await cacheService.GetAsync<IEnumerable<ProductoDto>>(cacheKey);

        if (cachedProductos is not null)
        {
            logger.LogInformation("Devolviendo productos desde caché");
            return Result.Success<IEnumerable<ProductoDto>, DomainError>(cachedProductos);
        }

        var productos = await productoRepository.FindAllAsync();
        var dtos = productos.ToDtoList();

        return Result.Success<IEnumerable<ProductoDto>, DomainError>(dtos)
            .Tap(_ => AñadirCacheProducto(cacheKey, dtos));
    }

    /// <summary>
    /// Obtener productos paginados con filtros.
    /// Devuelve: Result.Success(PagedResult) | Result.Failure nunca
    /// </summary>
    /// <remarks>
    /// 🎓 NOTA PARA EL ALUMNO: Este método NO cachea, a diferencia de <see cref="FindAllAsync"/>.
    /// ¿Por qué? Porque la paginación tiene INFINITAS combinaciones de filtros:
    /// ?page=1&amp;size=10&amp;categoria=X, ?page=2&amp;size=20&amp;categoria=Y, etc.
    /// Cada combinación sería una clave de cache distinta que:
    /// 1. Llenaría la memoria con claves que casi nunca se reusarían.
    /// 2. Sería imposible invalidar eficientemente (habría que adivinar qué combinaciones existen).
    /// La OutputCache en el controller (tag "productos") sí cachea la respuesta HTTP
    /// con TTL corto (60s) y se invalida por tag al escribir. Es suficiente para este caso.
    /// </remarks>
    public async Task<Result<PagedResult<ProductoDto>, DomainError>> FindAllPagedAsync(ProductoFilterDto filter)
    {
        logger.LogInformation("Obteniendo productos paginados - Página: {Page}, Tamaño: {Size}", filter.Page, filter.Size);

        var (productos, totalCount) = await productoRepository.FindAllPagedAsync(filter);
        var dtos = productos.ToDtoList();

        var pagedResult = new PagedResult<ProductoDto>
        {
            Items = dtos,
            TotalCount = totalCount,
            Page = filter.Page + 1,
            PageSize = filter.Size
        };

        return Result.Success<PagedResult<ProductoDto>, DomainError>(pagedResult);
    }

    /// <summary>
    /// Obtener un producto por ID con patrón cache-aside.
    /// Devuelve: Result.Success(ProductoDto) | Result.Failure(NotFound)
    /// </summary>
    public async Task<Result<ProductoDto, DomainError>> FindByIdAsync(long id)
    {
        logger.LogInformation("Obteniendo producto con ID: {Id}", id);

        var cacheKey = $"productos:{id}";
        var cachedProducto = await cacheService.GetAsync<ProductoDto>(cacheKey);

        if (cachedProducto is not null)
        {
            logger.LogInformation("Devolviendo producto desde caché: {Id}", id);
            return Result.Success<ProductoDto, DomainError>(cachedProducto);
        }

        var producto = await productoRepository.FindByIdAsync(id);

        if (producto is null)
        {
            logger.LogWarning("Producto con ID {Id} no encontrado", id);
            return Result.Failure<ProductoDto, DomainError>(
                ProductoError.NotFound(id)
            );
        }

        var dto = producto.ToDto();

        return Result.Success<ProductoDto, DomainError>(dto)
            .Tap(_ => AñadirCacheProducto(cacheKey, dto));
    }

    /// <summary>
    /// Obtener productos por categoría.
    /// Devuelve: Result.Success(List) | Result.Failure(NotFound)
    /// </summary>
    public async Task<Result<IEnumerable<ProductoDto>, DomainError>> FindByCategoriaIdAsync(long categoriaId)
    {
        logger.LogInformation("Obteniendo productos para categoría: {CategoriaId}", categoriaId);

        var categoria = await categoriaRepository.FindByIdAsync(categoriaId);
        if (categoria is null)
        {
            return Result.Failure<IEnumerable<ProductoDto>, DomainError>(
                ProductoError.CategoriaNoEncontrada(categoriaId)
            );
        }

        var productos = await productoRepository.FindByCategoriaIdAsync(categoriaId);
        var dtos = productos.ToDtoList();

        return Result.Success<IEnumerable<ProductoDto>, DomainError>(dtos);
    }

    /// <summary>
    /// Crear un nuevo producto.
    /// Devuelve: Result.Success(ProductoDto) | Result.Failure(Validation/NotFound)
    /// </summary>
    /// <remarks>
    /// NOTA PARA EL ALUMNO: Esta validación manual es OPTATIVA.
    /// Con AddFluentValidationAutoValidation() en el pipeline, la validación
    /// se ejecuta automáticamente ANTES de llegar al controller (respuesta 400 automática).
    /// Si se usa validación automática, esta llamada puede supprimirse y el service
    /// asumir que el DTO ya fue validado. Se deja aquí con fines didácticos para
    /// mostrar cómo validar manualmente en la capa de servicio cuando se requiera.
    /// </remarks>
    public async Task<Result<ProductoDto, DomainError>> CreateAsync(ProductoRequestDto dto)
    {
        logger.LogInformation("Creando producto: {Nombre}", dto.Nombre);

        var validationResult = await ValidateProductoAsync(dto);
        if (validationResult.IsFailure)
        {
            return Result.Failure<ProductoDto, DomainError>(validationResult.Error);
        }

        // ROP: Guardar -> Mapear -> Efectos (log, cache, websocket, email)
        var saved = await productoRepository.SaveAsync(dto.ToEntity());
        var resultDto = saved.ToDto();

        // Punto de precio de NACIMIENTO (Q-A): el precio inicial tambien es un
        // punto de la serie; sin el, un producto que nunca se actualiza tendria
        // el historial vacio para siempre.
        await RegistrarPrecioEnCassandra(saved);

        // Invalidar OutputCache tras crear (el listado paginado cambió)
        await InvalidarCacheProductoAsync();

        return Result.Success<ProductoDto, DomainError>(resultDto)
            .Tap(dto =>
            {
                logger.LogInformation("Producto creado con ID: {Id}", dto.Id);
                
                effects.NotificarWebSocketProductoCreado(dto);
                effects.NotificarSignalRProductoCreado(dto);
                effects.EnviarEmailProductoCreado(saved);
                effects.EventoSuscripcionProductoCreado(dto);
            });
    }

    /// <summary>
    /// Actualizar un producto existente.
    /// Devuelve: Result.Success(ProductoDto) | Result.Failure(NotFound/Validation)
    /// </summary>
    /// <remarks>
    /// NOTA PARA EL ALUMNO: Esta validación manual es OPTATIVA.
    /// Ver comentario en CreateAsync para más detalles.
    /// </remarks>
    public async Task<Result<ProductoDto, DomainError>> UpdateAsync(long id, ProductoRequestDto dto)
    {
        logger.LogInformation("Actualizando producto con ID: {Id}", id);

        var producto = await productoRepository.FindByIdAsync(id);

        if (producto is null)
        {
            logger.LogWarning("Producto con ID {Id} no encontrado para actualizar", id);
            return Result.Failure<ProductoDto, DomainError>(
                ProductoError.NotFound(id)
            );
        }

        var validationResult = await ValidateProductoAsync(dto);
        if (validationResult.IsFailure)
        {
            return Result.Failure<ProductoDto, DomainError>(validationResult.Error);
        }

        var precioAnterior = producto.Precio;   // capturado ANTES de tocar el producto

        producto.Nombre = dto.Nombre;
        producto.Descripcion = dto.Descripcion;
        producto.Precio = dto.Precio;
        producto.Stock = dto.Stock;
        producto.Imagen = dto.Imagen;
        producto.CategoriaId = dto.CategoriaId;

        var updated = await productoRepository.UpdateAsync(producto);
        var resultDto = updated.ToDto();

        // El historial registra CAMBIOS de precio, no eventos de edicion:
        // solo si el precio vario (un update de solo stock/imagen no genera
        // punto, y el informe Q-C solo cuenta cambios reales).
        if (precioAnterior != updated.Precio)
            await RegistrarPrecioEnCassandra(updated);

        await InvalidarCacheProductoAsync($"productos:{id}");

        return Result.Success<ProductoDto, DomainError>(resultDto)
            .Tap(_ =>
            {
                logger.LogInformation("Producto actualizado con ID: {Id}", id);
                effects.NotificarWebSocketProductoActualizado(resultDto);
                effects.NotificarSignalRProductoActualizado(resultDto);
                effects.EventoSuscripcionProductoActualizado(resultDto);
                effects.EventoSuscripcionStockBajo(resultDto, 10);
            });
    }

    /// <summary>
    /// Eliminar un producto.
    /// Devuelve: UnitResult.Success | UnitResult.Failure(NotFound)
    /// </summary>
    public async Task<UnitResult<DomainError>> DeleteAsync(long id)
    {
        logger.LogInformation("Eliminando producto con ID: {Id}", id);

        var producto = await productoRepository.FindByIdAsync(id);

        if (producto is null)
        {
            logger.LogWarning("Producto con ID {Id} no encontrado para eliminar", id);
            return UnitResult.Failure<DomainError>(
                ProductoError.NotFound(id)
            );
        }

        if (producto.IsLocalImage())
        {
            var deleteResult = await storageService.DeleteFileAsync(producto.Imagen!);
            if (deleteResult.IsFailure)
            {
                logger.LogWarning("Error eliminando imagen local del producto {Id}: {Error}", id, deleteResult.Error.Message);
            }
        }

        await productoRepository.DeleteAsync(id);
        logger.LogInformation("Producto eliminado con ID: {Id}", id);

        await InvalidarCacheProductoAsync($"productos:{id}");
        effects.NotificarWebSocketProductoEliminado(id);
        effects.NotificarSignalRProductoEliminado(id);
        effects.EventoSuscripcionProductoEliminado(id);

        return UnitResult.Success<DomainError>();
    }

    /// <summary>
    /// Actualizar la imagen de un producto.
    /// Devuelve: Result.Success(ProductoDto) | Result.Failure(NotFound/Validation)
    /// </summary>
    public async Task<Result<ProductoDto, DomainError>> UpdateImageAsync(long id, IFormFile image)
    {
        logger.LogInformation("Actualizando imagen de producto con ID: {Id}", id);

        var producto = await productoRepository.FindByIdAsync(id);

        if (producto is null)
        {
            logger.LogWarning("Producto con ID {Id} no encontrado para actualizar imagen", id);
            return Result.Failure<ProductoDto, DomainError>(
                ProductoError.NotFound(id)
            );
        }

        var saveResult = await storageService.SaveFileAsync(image, "productos");
        if (saveResult.IsFailure)
        {
            logger.LogWarning("Error guardando imagen para producto {Id}: {Error}", id, saveResult.Error.Message);
            return Result.Failure<ProductoDto, DomainError>(saveResult.Error);
        }

        if (producto.IsLocalImage())
        {
            await storageService.DeleteFileAsync(producto.Imagen!);
        }

        producto.Imagen = saveResult.Value;

        var updated = await productoRepository.UpdateAsync(producto);
        var resultDto = updated.ToDto();

        await InvalidarCacheProductoAsync($"productos:{id}");

        return Result.Success<ProductoDto, DomainError>(resultDto)
            .Tap(_ =>
            {
                logger.LogInformation("Imagen actualizada para producto con ID: {Id}", id);
                effects.NotificarWebSocketProductoActualizado(resultDto);
                effects.EventoSuscripcionProductoActualizado(resultDto);
            });
    }

    /// <summary>
    /// Actualizar parcialmente un producto (solo campos proporcionados).
    /// Devuelve: Result.Success(ProductoDto) | Result.Failure(NotFound/Validation)
    /// </summary>
    public async Task<Result<ProductoDto, DomainError>> UpdatePartialAsync(long id, ProductoPatchDto dto)
    {
        logger.LogInformation("Actualizando parcialmente producto con ID: {Id}", id);

        var producto = await productoRepository.FindByIdAsync(id);

        if (producto is null)
        {
            logger.LogWarning("Producto con ID {Id} no encontrado para actualizar parcialmente", id);
            return Result.Failure<ProductoDto, DomainError>(
                ProductoError.NotFound(id)
            );
        }

        var precioAnterior = producto.Precio;   // capturado ANTES de aplicar el patch

        AplicarPatch(producto, dto);

        var updated = await productoRepository.UpdateAsync(producto);
        var resultDto = updated.ToDto();

        // Solo si el precio vario: un PATCH de solo stock/imagen no es un
        // cambio de precio (ver UpdateAsync).
        if (precioAnterior != updated.Precio)
            await RegistrarPrecioEnCassandra(updated);

        await InvalidarCacheProductoAsync($"productos:{id}");

        return Result.Success<ProductoDto, DomainError>(resultDto)
            .Tap(_ =>
            {
                logger.LogInformation("Producto actualizado parcialmente con ID: {Id}", id);
                effects.NotificarWebSocketProductoActualizado(resultDto);
                effects.EventoSuscripcionProductoActualizado(resultDto);
                if (dto.Stock.HasValue)
                    effects.EventoSuscripcionStockBajo(resultDto, 10);
            });
    }

    /// <summary>
    /// Aplica el patch condicional: cada campo solo se toca si el DTO trae valor
    /// (la semantica de PATCH). Metodo puro sin estado: extraido de
    /// UpdatePartialAsync para dejar el flujo de ese metodo lineal.
    /// </summary>
    private static void AplicarPatch(Producto producto, ProductoPatchDto dto)
    {
        if (!string.IsNullOrWhiteSpace(dto.Nombre))
            producto.Nombre = dto.Nombre;

        if (!string.IsNullOrWhiteSpace(dto.Descripcion))
            producto.Descripcion = dto.Descripcion;

        if (dto.Precio.HasValue && dto.Precio.Value > 0)
            producto.Precio = dto.Precio.Value;

        if (dto.Stock.HasValue)
            producto.Stock = dto.Stock.Value;

        if (!string.IsNullOrWhiteSpace(dto.Imagen))
            producto.Imagen = dto.Imagen;

        if (dto.CategoriaId.HasValue && dto.CategoriaId.Value > 0)
            producto.CategoriaId = dto.CategoriaId.Value;
    }

    /// <summary>
    /// 🎓 Escribe el precio actualizado en las dos tablas de serie (Q-A histórico +
    /// Q-C informe del día) DESPUÉS de persistir en PostgreSQL y con await: el
    /// orden importa, quien acaba de hacer el PATCH encuentra el punto ya escrito
    /// si consulta la serie seguido. Equivalente al PrecioHistoricoHandler del
    /// proyecto CQRS: misma escritura, otro cableado.
    /// </summary>
    private async Task RegistrarPrecioEnCassandra(Producto producto)
    {
        try
        {
            await historialRepository.RegistrarPrecioAsync(producto.Id, (double)producto.Precio);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex,
                "Cassandra: no se pudo registrar el precio del producto {Id} (modo degradado)", producto.Id);
        }
    }

    #region Métodos Privados - Cache

    /// <summary>
    /// Añade un elemento a la caché de forma asíncrona (fire &amp; forget).
    /// </summary>
    private void AñadirCacheProducto<T>(string key, T value)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await cacheService.SetAsync(key, value, _cacheTTL);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Error adding to cache: Key={Key}", key);
            }
        });
    }

    /// <summary>
    /// Invalida las claves de caché especificadas de forma asíncrona (fire &amp; forget).
    /// También invalida la tag de OutputCache "productos" (en memoria: síncrono,
    /// para que el siguiente GET no sirva datos obsoletos).
    /// </summary>
    private async Task InvalidarCacheProductoAsync(params string[] keys)
    {
        foreach (var key in keys)
        {
            try { await cacheService.RemoveAsync(key); }
            catch (Exception ex) { logger.LogWarning(ex, "Cache invalidation error: Key={Key}", key); }
        }

        try { await outputCacheStore.EvictByTagAsync("productos", CancellationToken.None); }
        catch (Exception ex) { logger.LogWarning(ex, "Output cache invalidation error: Tag=productos"); }
    }

    #endregion




    #region Métodos Privados - Validación

    /// <summary>
    /// Valida los datos de un producto usando FluentValidation.
    /// Devuelve: UnitResult.Success | UnitResult.Failure(Validation/NotFound)
    /// </summary>
    private async Task<UnitResult<DomainError>> ValidateProductoAsync(ProductoRequestDto dto)
    {
        var validationResult = await productoValidator.ValidateAsync(dto);

        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.ErrorMessage).ToArray()
                );

            return UnitResult.Failure<DomainError>(
                ProductoError.ValidacionConCampos(errors)
            );
        }

        var categoriaExists = await categoriaRepository.FindByIdAsync(dto.CategoriaId);
        if (categoriaExists is null)
        {
            return UnitResult.Failure<DomainError>(
                ProductoError.ValidacionConCampos(new Dictionary<string, string[]>
                {
                    { "CategoriaId", new[] { $"La categoría con ID {dto.CategoriaId} no fue encontrada" } }
                })
            );
        }

        return UnitResult.Success<DomainError>();
    }

    #endregion

}

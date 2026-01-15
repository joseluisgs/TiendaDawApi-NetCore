using CSharpFunctionalExtensions;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using TiendaApi.Apis.Dtos.Common;
using TiendaApi.Apis.Dtos.Productos;
using TiendaApi.Apis.Errors;
using TiendaApi.Apis.Errors.Productos;
using TiendaApi.Apis.Mappers;
using TiendaApi.Apis.Models;
using TiendaApi.Apis.Repositories.Categorias;
using TiendaApi.Apis.Repositories.Productos;
using TiendaApi.Apis.Services.Cache;
using TiendaApi.Apis.Services.Email;
using TiendaApi.Apis.Services.Storage;
using TiendaApi.Apis.Validators.Productos;
using TiendaApi.Apis.WebSockets.Productos;

namespace TiendaApi.Apis.Services.Productos;

/// <summary>
/// Servicio de productos usando Patrón Result.
/// Las operaciones de caché, WebSocket y email se ejecutan en Task.Run (fire & forget)
/// para no bloquear el hilo principal. Esto es especialmente importante si:
/// - La caché está en Redis (latencia de red)
/// - WebSocket tarda en enviar la notificación
/// - El email falla o tarda en encolarse
/// Si cualquiera de estas operaciones falla, se registra un warning pero no afecta a la respuesta.
/// </summary>
public class ProductoService(
    IProductoRepository productoRepository,
    ICategoriaRepository categoriaRepository,
    ILogger<ProductoService> logger,
    ICacheService cacheService,
    ProductoWebSocketHandler webSocketHandler,
    IEmailService emailService,
    IConfiguration configuration,
    IValidator<ProductoRequestDto> productoValidator,
    IStorageService storageService
) : IProductoService
{
    private readonly TimeSpan _cacheTTL = TimeSpan.FromMinutes(
        int.Parse(configuration["Cache:ProductoCacheTTLMinutes"] ?? "10"));

    /// <summary>
    /// Obtener todos los productos con patrón cache-aside.
    /// Devuelve: Result.Success(List) | Result.Failure nunca
    /// </summary>
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

        return Result.Success<ProductoDto, DomainError>(resultDto)
            .Tap(dto =>
            {
                logger.LogInformation("Producto creado con ID: {Id}", dto.Id);
                InvalidarCacheProducto("productos:all");
                NotificarWebSocketProductoCreado(dto);
                EnviarEmailProductoCreado(saved);
            });
    }

    /// <summary>
    /// Actualizar un producto existente.
    /// Devuelve: Result.Success(ProductoDto) | Result.Failure(NotFound/Validation)
    /// </summary>
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

        producto.Nombre = dto.Nombre;
        producto.Descripcion = dto.Descripcion;
        producto.Precio = dto.Precio;
        producto.Stock = dto.Stock;
        producto.Imagen = dto.Imagen;
        producto.CategoriaId = dto.CategoriaId;

        var updated = await productoRepository.UpdateAsync(producto);
        var resultDto = updated.ToDto();

        return Result.Success<ProductoDto, DomainError>(resultDto)
            .Tap(_ =>
            {
                logger.LogInformation("Producto actualizado con ID: {Id}", id);
                InvalidarCacheProducto($"productos:{id}", "productos:all");
                NotificarWebSocketProductoActualizado(resultDto);
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

        InvalidarCacheProducto($"productos:{id}", "productos:all");
        NotificarWebSocketProductoEliminado(id);

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

        return Result.Success<ProductoDto, DomainError>(resultDto)
            .Tap(_ =>
            {
                logger.LogInformation("Imagen actualizada para producto con ID: {Id}", id);
                InvalidarCacheProducto($"productos:{id}", "productos:all");
                NotificarWebSocketProductoActualizado(resultDto);
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

        var updated = await productoRepository.UpdateAsync(producto);
        var resultDto = updated.ToDto();

        return Result.Success<ProductoDto, DomainError>(resultDto)
            .Tap(_ =>
            {
                logger.LogInformation("Producto actualizado parcialmente con ID: {Id}", id);
                InvalidarCacheProducto($"productos:{id}", "productos:all");
                NotificarWebSocketProductoActualizado(resultDto);
            });
    }

    // ========== MÉTODOS PRIVADOS - CACHE ==========

    /// <summary>
    /// Añade un elemento a la caché de forma asíncrona (fire & forget).
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
    /// Invalida las claves de caché especificadas de forma asíncrona (fire & forget).
    /// </summary>
    private void InvalidarCacheProducto(params string[] keys)
    {
        _ = Task.Run(async () =>
        {
            foreach (var key in keys)
            {
                try
                {
                    await cacheService.RemoveAsync(key);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Cache invalidation error: Key={Key}", key);
                }
            }
        });
    }

    // ========== MÉTODOS PRIVADOS - WEBSOCKET ==========

    /// <summary>
    /// Notifica vía WebSocket la creación de un producto.
    /// </summary>
    private void NotificarWebSocketProductoCreado(ProductoDto producto)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await webSocketHandler.NotifyAsync(new ProductoNotificacion(
                    ProductoNotificationType.CREATED,
                    producto.Id,
                    producto
                ));
                logger.LogDebug("Notificación WebSocket enviada tras crear producto: {ProductoId}", producto.Id);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Error en notificación WebSocket al crear producto: {ProductoId}", producto.Id);
            }
        });
    }

    /// <summary>
    /// Notifica vía WebSocket la actualización de un producto.
    /// </summary>
    private void NotificarWebSocketProductoActualizado(ProductoDto producto)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await webSocketHandler.NotifyAsync(new ProductoNotificacion(
                    ProductoNotificationType.UPDATED,
                    producto.Id,
                    producto
                ));
                logger.LogDebug("Notificación WebSocket enviada tras actualizar producto: {ProductoId}", producto.Id);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Error en notificación WebSocket al actualizar producto: {ProductoId}", producto.Id);
            }
        });
    }

    /// <summary>
    /// Notifica vía WebSocket la eliminación de un producto.
    /// </summary>
    private void NotificarWebSocketProductoEliminado(long productoId)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await webSocketHandler.NotifyAsync(new ProductoNotificacion(
                    ProductoNotificationType.DELETED,
                    productoId,
                    null
                ));
                logger.LogDebug("Notificación WebSocket enviada tras eliminar producto: {ProductoId}", productoId);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Error en notificación WebSocket al eliminar producto: {ProductoId}", productoId);
            }
        });
    }

    // ========== MÉTODOS PRIVADOS - EMAIL ==========

    /// <summary>
    /// Envía email de notificación cuando se crea un producto.
    /// </summary>
    private void EnviarEmailProductoCreado(Producto producto)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                var adminEmail = configuration["Smtp:AdminEmail"];
                if (string.IsNullOrEmpty(adminEmail)) return;

                var content = EmailTemplates.ProductoCreado(producto.Nombre, producto.Precio, producto.Stock, producto.Id);
                var body = EmailTemplates.CreateBase("Nuevo Producto Creado", content);

                var emailMessage = new EmailMessage
                {
                    To = adminEmail,
                    Subject = "🆕 Nuevo Producto en Tienda DAW",
                    Body = body,
                    IsHtml = true
                };
                await emailService.EnqueueEmailAsync(emailMessage);
                logger.LogDebug("Email de notificación encolado tras crear producto");
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Error al encolar email de notificación tras crear producto");
            }
        });
    }

    // ========== VALIDACIÓN ==========

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
                ProductoError.CategoriaNoEncontrada(dto.CategoriaId)
            );
        }

        return UnitResult.Success<DomainError>();
    }
}

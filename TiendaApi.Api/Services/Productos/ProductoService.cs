using CSharpFunctionalExtensions;
using FluentValidation;
using Microsoft.AspNetCore.Http;
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
    ICategoriaRepository categoriaRepository,
    ILogger<ProductoService> logger,
    ICacheService cacheService,
    ProductosWebSocketHandler webSocketHandler,
    IHubContext<ProductosHub> productosHubContext,
    IEmailService emailService,
    IConfiguration configuration,
    IValidator<ProductoRequestDto> productoValidator,
    IStorageService storageService,
    IEventPublisher eventPublisher
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

        return Result.Success<ProductoDto, DomainError>(resultDto)
            .Tap(dto =>
            {
                logger.LogInformation("Producto creado con ID: {Id}", dto.Id);
                InvalidarCacheProducto("productos:all");
                NotificarWebSocketProductoCreado(dto);
                NotificarSignalRProductoCreado(dto);
                EnviarEmailProductoCreado(saved);
                EventoSuscripcionProductoCreado(dto);
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
                NotificarSignalRProductoActualizado(resultDto);
                EventoSuscripcionProductoActualizado(resultDto);
                EventoSuscripcionStockBajo(resultDto, 10); // Umbral de stock bajo = 10
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
        NotificarSignalRProductoEliminado(id);
        EventoSuscripcionProductoEliminado(id);

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
                EventoSuscripcionProductoActualizado(resultDto);
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
                EventoSuscripcionProductoActualizado(resultDto);
                if (dto.Stock.HasValue)
                    EventoSuscripcionStockBajo(resultDto, 10);
            });
    }

    #region Métodos Privados - Cache

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

    #endregion

    #region Métodos Privados - WebSocket Nativo

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
                logger.LogInformation("📡 [WEBSOCKET] Notificación enviada: Producto creado ID={ProductoId}", producto.Id);
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
                logger.LogInformation("📡 [WEBSOCKET] Notificación enviada: Producto actualizado ID={ProductoId}", producto.Id);
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
                logger.LogInformation("📡 [WEBSOCKET] Notificación enviada: Producto eliminado ID={ProductoId}", productoId);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Error en notificación WebSocket al eliminar producto: {ProductoId}", productoId);
            }
        });
    }

    #endregion

    #region Métodos Privados - SignalR

    /// <summary>
    /// Notifica vía SignalR la creación de un producto.
    /// </summary>
    private void NotificarSignalRProductoCreado(ProductoDto producto)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                var payload = new
                {
                    productoId = producto.Id,
                    nombre = producto.Nombre,
                    descripcion = producto.Descripcion,
                    precio = producto.Precio,
                    stock = producto.Stock,
                    categoriaId = producto.CategoriaId,
                    categoriaNombre = producto.CategoriaNombre,
                    tipo = "PRODUCTO_CREADO",
                    timestamp = DateTime.UtcNow
                };
                await productosHubContext.Clients.All.SendAsync("ProductoCreado", payload);
                logger.LogInformation("📟 [SIGNALR] Notificación enviada: Producto creado ID={ProductoId}", producto.Id);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Error en notificación SignalR al crear producto: {ProductoId}", producto.Id);
            }
        });
    }

    /// <summary>
    /// Notifica vía SignalR la actualización de un producto.
    /// </summary>
    private void NotificarSignalRProductoActualizado(ProductoDto producto)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                var payload = new
                {
                    productoId = producto.Id,
                    nombre = producto.Nombre,
                    descripcion = producto.Descripcion,
                    precio = producto.Precio,
                    stock = producto.Stock,
                    categoriaId = producto.CategoriaId,
                    categoriaNombre = producto.CategoriaNombre,
                    tipo = "PRODUCTO_ACTUALIZADO",
                    timestamp = DateTime.UtcNow
                };
                await productosHubContext.Clients.All.SendAsync("ProductoActualizado", payload);
                logger.LogInformation("📟 [SIGNALR] Notificación enviada: Producto actualizado ID={ProductoId}", producto.Id);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Error en notificación SignalR al actualizar producto: {ProductoId}", producto.Id);
            }
        });
    }

    /// <summary>
    /// Notifica vía SignalR la eliminación de un producto.
    /// </summary>
    private void NotificarSignalRProductoEliminado(long productoId)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                var payload = new
                {
                    productoId,
                    tipo = "PRODUCTO_ELIMINADO",
                    timestamp = DateTime.UtcNow
                };
                await productosHubContext.Clients.All.SendAsync("ProductoEliminado", payload);
                logger.LogInformation("📟 [SIGNALR] Notificación enviada: Producto eliminado ID={ProductoId}", productoId);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Error en notificación SignalR al eliminar producto: {ProductoId}", productoId);
            }
        });
    }

    #endregion

    #region Métodos Privados - Email

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

    #region Métodos Privados - GraphQL Subscriptions

    /// <summary>
    /// Publica evento de GraphQL Subscription cuando se crea un producto.
    /// </summary>
    private void EventoSuscripcionProductoCreado(ProductoDto producto)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await eventPublisher.PublishAsync("onProductoCreado", new ProductoCreadoEvent
                {
                    ProductoId = producto.Id,
                    Nombre = producto.Nombre,
                    Precio = producto.Precio,
                    Stock = producto.Stock,
                    CreatedAt = DateTime.UtcNow
                });
                logger.LogInformation("🔄 [GRAPHQL] Evento Subscription enviado: Producto creado ID={ProductoId}", producto.Id);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Error publicando evento GraphQL Subscription al crear producto: {ProductoId}", producto.Id);
            }
        });
    }

    /// <summary>
    /// Publica evento de GraphQL Subscription cuando se actualiza un producto.
    /// </summary>
    private void EventoSuscripcionProductoActualizado(ProductoDto producto)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await eventPublisher.PublishAsync("onProductoActualizado", new ProductoActualizadoEvent
                {
                    ProductoId = producto.Id,
                    Nombre = producto.Nombre,
                    Precio = producto.Precio,
                    Stock = producto.Stock,
                    UpdatedAt = DateTime.UtcNow
                });
                logger.LogInformation("🔄 [GRAPHQL] Evento Subscription enviado: Producto actualizado ID={ProductoId}", producto.Id);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Error publicando evento GraphQL Subscription al actualizar producto: {ProductoId}", producto.Id);
            }
        });
    }

    /// <summary>
    /// Publica evento de GraphQL Subscription cuando se elimina un producto.
    /// </summary>
    private void EventoSuscripcionProductoEliminado(long productoId)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await eventPublisher.PublishAsync("onProductoEliminado", new ProductoEliminadoEvent
                {
                    ProductoId = productoId,
                    DeletedAt = DateTime.UtcNow
                });
                logger.LogInformation("🔄 [GRAPHQL] Evento Subscription enviado: Producto eliminado ID={ProductoId}", productoId);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Error publicando evento GraphQL Subscription al eliminar producto: {ProductoId}", productoId);
            }
        });
    }

    /// <summary>
    /// Publica evento de GraphQL Subscription cuando el stock está bajo.
    /// </summary>
    private void EventoSuscripcionStockBajo(ProductoDto producto, int umbralStock)
    {
        if (producto.Stock > umbralStock) return;

        _ = Task.Run(async () =>
        {
            try
            {
                await eventPublisher.PublishAsync("onStockBajo", new ProductoStockBajoEvent
                {
                    ProductoId = producto.Id,
                    Nombre = producto.Nombre,
                    StockActual = producto.Stock,
                    UmbralStock = umbralStock,
                    DetectedAt = DateTime.UtcNow
                });
                logger.LogInformation("🔄 [GRAPHQL] Evento Subscription enviado: Stock bajo ID={ProductoId}, Stock={Stock}", producto.Id, producto.Stock);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Error publicando evento GraphQL Subscription de stock bajo: {ProductoId}", producto.Id);
            }
        });
    }

    #endregion
}

using CSharpFunctionalExtensions;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using TiendaApi.Apis.Dtos.Productos;
using TiendaApi.Apis.Errors;
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

    /// <summary>
    /// Obtener todos los productos con patrón cache-aside.
    /// Returns: Result.Success(List) | Result.Failure nunca
    /// </summary>
    public async Task<Result<IEnumerable<ProductoDto>, DomainError>> FindAllAsync()
    {
        logger.LogInformation("Obteniendo todos los productos");

        const string cacheKey = "productos:all";
        var cachedProductos = await cacheService.GetAsync<IEnumerable<ProductoDto>>(cacheKey);

        if (cachedProductos != null)
        {
            logger.LogInformation("Devolviendo productos desde caché");
            return Result.Success<IEnumerable<ProductoDto>, DomainError>(cachedProductos);
        }

        var productos = await productoRepository.FindAllAsync();
        var dtos = productos.ToDtoList();

        var cacheTTL = TimeSpan.FromMinutes(
            int.Parse(configuration["Cache:ProductoCacheTTLMinutes"] ?? "10"));
        await cacheService.SetAsync(cacheKey, dtos, cacheTTL);

        return Result.Success<IEnumerable<ProductoDto>, DomainError>(dtos);
    }

    /// <summary>
    /// Obtener un producto por ID con patrón cache-aside.
    /// Returns: Result.Success(ProductoDto) | Result.Failure(NotFound)
    /// </summary>
    public async Task<Result<ProductoDto, DomainError>> FindByIdAsync(long id)
    {
        logger.LogInformation("Obteniendo producto con ID: {Id}", id);

        var cacheKey = $"productos:{id}";
        var cachedProducto = await cacheService.GetAsync<ProductoDto>(cacheKey);

        if (cachedProducto != null)
        {
            logger.LogInformation("Devolviendo producto desde caché: {Id}", id);
            return Result.Success<ProductoDto, DomainError>(cachedProducto);
        }

        var producto = await productoRepository.FindByIdAsync(id);

        if (producto == null)
        {
            logger.LogWarning("Producto con ID {Id} no encontrado", id);
            return Result.Failure<ProductoDto, DomainError>(
                DomainError.NotFound($"Producto con ID {id} no encontrado")
            );
        }

        var dto = producto.ToDto();

        var cacheTTL = TimeSpan.FromMinutes(
            int.Parse(configuration["Cache:ProductoCacheTTLMinutes"] ?? "10"));
        await cacheService.SetAsync(cacheKey, dto, cacheTTL);

        return Result.Success<ProductoDto, DomainError>(dto);
    }

    /// <summary>
    /// Obtener productos por categoría.
    /// Returns: Result.Success(List) | Result.Failure(NotFound)
    /// </summary>
    public async Task<Result<IEnumerable<ProductoDto>, DomainError>> FindByCategoriaIdAsync(long categoriaId)
    {
        logger.LogInformation("Obteniendo productos para categoría: {CategoriaId}", categoriaId);

        var categoria = await categoriaRepository.FindByIdAsync(categoriaId);
        if (categoria == null)
        {
            return Result.Failure<IEnumerable<ProductoDto>, DomainError>(
                DomainError.NotFound($"Categoría con ID {categoriaId} no encontrada")
            );
        }

        var productos = await productoRepository.FindByCategoriaIdAsync(categoriaId);
        var dtos = productos.ToDtoList();

        return Result.Success<IEnumerable<ProductoDto>, DomainError>(dtos);
    }

    /// <summary>
    /// Crear un nuevo producto.
    /// Returns: Result.Success(ProductoDto) | Result.Failure(Validation/NotFound)
    /// </summary>
    public async Task<Result<ProductoDto, DomainError>> CreateAsync(ProductoRequestDto dto)
    {
        logger.LogInformation("Creando producto: {Nombre}", dto.Nombre);

        var validationResult = await ValidateProductoAsync(dto);
        if (validationResult.IsFailure)
        {
            return Result.Failure<ProductoDto, DomainError>(validationResult.Error);
        }

        var producto = dto.ToEntity();
        var saved = await productoRepository.SaveAsync(producto);

        logger.LogInformation("Producto creado con ID: {Id}", saved.Id);

        var resultDto = saved.ToDto();

        _ = Task.Run(async () =>
        {
            try
            {
                await cacheService.RemoveAsync("productos:all");
                logger.LogDebug("Caché invalidada tras crear producto");
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Error al invalidar caché tras crear producto");
            }
        });

        _ = Task.Run(async () => await NotificarWebSocketProductoCreado(resultDto));

        _ = Task.Run(async () =>
        {
            try
            {
                var adminEmail = configuration["Smtp:AdminEmail"];
                if (!string.IsNullOrEmpty(adminEmail))
                {
                    var emailMessage = new EmailMessage
                    {
                        To = adminEmail,
                        Subject = "Nuevo Producto Creado",
                        Body = $@"<h2>Nuevo Producto Creado</h2>
                            <p><strong>ID:</strong> {saved.Id}</p>
                            <p><strong>Nombre:</strong> {saved.Nombre}</p>
                            <p><strong>Precio:</strong> ${saved.Precio}</p>
                            <p><strong>Stock:</strong> {saved.Stock}</p>",
                        IsHtml = true
                    };
                    await emailService.EnqueueEmailAsync(emailMessage);
                    logger.LogDebug("Email de notificación encolado tras crear producto");
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Error al encolar email de notificación");
            }
        });

        return Result.Success<ProductoDto, DomainError>(resultDto);
    }

    /// <summary>
    /// Actualizar un producto existente.
    /// Returns: Result.Success(ProductoDto) | Result.Failure(NotFound/Validation)
    /// </summary>
    public async Task<Result<ProductoDto, DomainError>> UpdateAsync(long id, ProductoRequestDto dto)
    {
        logger.LogInformation("Actualizando producto con ID: {Id}", id);

        var producto = await productoRepository.FindByIdAsync(id);

        if (producto == null)
        {
            logger.LogWarning("Producto con ID {Id} no encontrado para actualizar", id);
            return Result.Failure<ProductoDto, DomainError>(
                DomainError.NotFound($"Producto con ID {id} no encontrado")
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

        logger.LogInformation("Producto actualizado con ID: {Id}", id);

        var resultDto = updated.ToDto();

        _ = Task.Run(async () =>
        {
            try
            {
                await cacheService.RemoveAsync($"productos:{id}");
                await cacheService.RemoveAsync("productos:all");
                logger.LogDebug("Caché invalidada tras actualizar producto");
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Error al invalidar caché tras actualizar producto");
            }
        });

        _ = Task.Run(async () => await NotificarWebSocketProductoActualizado(resultDto));

        return Result.Success<ProductoDto, DomainError>(resultDto);
    }

    /// <summary>
    /// Eliminar un producto.
    /// Returns: UnitResult.Success | UnitResult.Failure(NotFound)
    /// </summary>
    public async Task<UnitResult<DomainError>> DeleteAsync(long id)
    {
        logger.LogInformation("Eliminando producto con ID: {Id}", id);

        var producto = await productoRepository.FindByIdAsync(id);

        if (producto == null)
        {
            logger.LogWarning("Producto con ID {Id} no encontrado para eliminar", id);
            return UnitResult.Failure<DomainError>(
                DomainError.NotFound($"Producto con ID {id} no encontrado")
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

        _ = Task.Run(async () =>
        {
            try
            {
                await cacheService.RemoveAsync($"productos:{id}");
                await cacheService.RemoveAsync("productos:all");
                logger.LogDebug("Caché invalidada tras eliminar producto");
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Error al invalidar caché tras eliminar producto");
            }
        });

        _ = Task.Run(async () => await NotificarWebSocketProductoEliminado(id));

        return UnitResult.Success<DomainError>();
    }

    /// <summary>
    /// Actualizar la imagen de un producto.
    /// Returns: Result.Success(ProductoDto) | Result.Failure(NotFound/Validation)
    /// </summary>
    public async Task<Result<ProductoDto, DomainError>> UpdateImageAsync(long id, IFormFile image)
    {
        logger.LogInformation("Actualizando imagen de producto con ID: {Id}", id);

        var producto = await productoRepository.FindByIdAsync(id);

        if (producto == null)
        {
            logger.LogWarning("Producto con ID {Id} no encontrado para actualizar imagen", id);
            return Result.Failure<ProductoDto, DomainError>(
                DomainError.NotFound($"Producto con ID {id} no encontrado")
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
        producto.UpdatedAt = DateTime.UtcNow;

        var updated = await productoRepository.UpdateAsync(producto);

        logger.LogInformation("Imagen actualizada para producto con ID: {Id}", id);

        var resultDto = updated.ToDto();

        _ = Task.Run(async () =>
        {
            try
            {
                await cacheService.RemoveAsync($"productos:{id}");
                await cacheService.RemoveAsync("productos:all");
                logger.LogDebug("Caché invalidada tras actualizar imagen de producto");
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Error al invalidar caché tras actualizar imagen de producto");
            }
        });

        _ = Task.Run(async () => await NotificarWebSocketProductoActualizado(resultDto));

        return Result.Success<ProductoDto, DomainError>(resultDto);
    }

    /// <summary>
    /// Notifica vía WebSocket la creación de un producto.
    /// </summary>
    private async Task NotificarWebSocketProductoCreado(ProductoDto producto)
    {
        try
        {
            await webSocketHandler.NotifyProductoCreatedAsync(producto);
            logger.LogDebug("Notificación WebSocket enviada tras crear producto: {ProductoId}", producto.Id);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Error en notificación WebSocket al crear producto: {ProductoId}", producto.Id);
        }
    }

    /// <summary>
    /// Notifica vía WebSocket la actualización de un producto.
    /// </summary>
    private async Task NotificarWebSocketProductoActualizado(ProductoDto producto)
    {
        try
        {
            await webSocketHandler.NotifyProductoUpdatedAsync(producto);
            logger.LogDebug("Notificación WebSocket enviada tras actualizar producto: {ProductoId}", producto.Id);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Error en notificación WebSocket al actualizar producto: {ProductoId}", producto.Id);
        }
    }

    /// <summary>
    /// Notifica vía WebSocket la eliminación de un producto.
    /// </summary>
    private async Task NotificarWebSocketProductoEliminado(long productoId)
    {
        try
        {
            await webSocketHandler.NotifyProductoDeletedAsync(productoId);
            logger.LogDebug("Notificación WebSocket enviada tras eliminar producto: {ProductoId}", productoId);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Error en notificación WebSocket al eliminar producto: {ProductoId}", productoId);
        }
    }

    /// <summary>
    /// Valida los datos de un producto usando FluentValidation.
    /// Returns: UnitResult.Success | UnitResult.Failure(Validation/NotFound)
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
                DomainError.Validation("Errores de validación", errors)
            );
        }

        var categoriaExists = await categoriaRepository.FindByIdAsync(dto.CategoriaId);
        if (categoriaExists == null)
        {
            return UnitResult.Failure<DomainError>(
                DomainError.NotFound($"Categoría con ID {dto.CategoriaId} no encontrada")
            );
        }

        return UnitResult.Success<DomainError>();
    }
}

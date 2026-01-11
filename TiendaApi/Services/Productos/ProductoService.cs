using CSharpFunctionalExtensions;
using TiendaApi.Dtos.Productos;
using TiendaApi.Errors;
using TiendaApi.Mappers;
using TiendaApi.Models;
using TiendaApi.Repositories.Categorias;
using TiendaApi.Repositories.Productos;
using TiendaApi.Services.Cache;
using TiendaApi.Services.Email;
using TiendaApi.WebSockets.Productos;

namespace TiendaApi.Services.Productos;

/// <summary>
/// Servicio de productos usando Patrón Result.
/// </summary>
public class ProductoService : IProductoService
{
    private readonly IProductoRepository _productoRepository;
    private readonly ICategoriaRepository _categoriaRepository;
    private readonly ILogger<ProductoService> _logger;
    private readonly ICacheService _cacheService;
    private readonly ProductoWebSocketHandler _webSocketHandler;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;

    public ProductoService(
        IProductoRepository productoRepository,
        ICategoriaRepository categoriaRepository,
        ILogger<ProductoService> logger,
        ICacheService cacheService,
        ProductoWebSocketHandler webSocketHandler,
        IEmailService emailService,
        IConfiguration configuration)
    {
        _productoRepository = productoRepository;
        _categoriaRepository = categoriaRepository;
        _logger = logger;
        _cacheService = cacheService;
        _webSocketHandler = webSocketHandler;
        _emailService = emailService;
        _configuration = configuration;
    }

    /// <summary>
    /// Obtener todos los productos con patrón cache-aside.
    /// Returns: Result.Success(List) | Result.Failure nunca
    /// </summary>
    public async Task<Result<IEnumerable<ProductoDto>, DomainError>> FindAllAsync()
    {
        _logger.LogInformation("Obteniendo todos los productos");
        
        const string cacheKey = "productos:all";
        var cachedProductos = await _cacheService.GetAsync<IEnumerable<ProductoDto>>(cacheKey);
        
        if (cachedProductos != null)
        {
            _logger.LogInformation("Devolviendo productos desde caché");
            return Result.Success<IEnumerable<ProductoDto>, DomainError>(cachedProductos);
        }
        
        var productos = await _productoRepository.FindAllAsync();
        var dtos = productos.ToDtoList();
        
        var cacheTTL = TimeSpan.FromMinutes(
            int.Parse(_configuration["Cache:ProductoCacheTTLMinutes"] ?? "10"));
        await _cacheService.SetAsync(cacheKey, dtos, cacheTTL);
        
        return Result.Success<IEnumerable<ProductoDto>, DomainError>(dtos);
    }

    /// <summary>
    /// Obtener un producto por ID con patrón cache-aside.
    /// Returns: Result.Success(ProductoDto) | Result.Failure(NotFound)
    /// </summary>
    public async Task<Result<ProductoDto, DomainError>> FindByIdAsync(long id)
    {
        _logger.LogInformation("Obteniendo producto con ID: {Id}", id);
        
        var cacheKey = $"productos:{id}";
        var cachedProducto = await _cacheService.GetAsync<ProductoDto>(cacheKey);
        
        if (cachedProducto != null)
        {
            _logger.LogInformation("Devolviendo producto desde caché: {Id}", id);
            return Result.Success<ProductoDto, DomainError>(cachedProducto);
        }
        
        var producto = await _productoRepository.FindByIdAsync(id);
        
        if (producto == null)
        {
            _logger.LogWarning("Producto con ID {Id} no encontrado", id);
            return Result.Failure<ProductoDto, DomainError>(
                DomainError.NotFound($"Producto con ID {id} no encontrado")
            );
        }
        
        var dto = producto.ToDto();
        
        var cacheTTL = TimeSpan.FromMinutes(
            int.Parse(_configuration["Cache:ProductoCacheTTLMinutes"] ?? "10"));
        await _cacheService.SetAsync(cacheKey, dto, cacheTTL);
        
        return Result.Success<ProductoDto, DomainError>(dto);
    }

    /// <summary>
    /// Obtener productos por categoría.
    /// Returns: Result.Success(List) | Result.Failure(NotFound)
    /// </summary>
    public async Task<Result<IEnumerable<ProductoDto>, DomainError>> FindByCategoriaIdAsync(long categoriaId)
    {
        _logger.LogInformation("Obteniendo productos para categoría: {CategoriaId}", categoriaId);
        
        var categoria = await _categoriaRepository.FindByIdAsync(categoriaId);
        if (categoria == null)
        {
            return Result.Failure<IEnumerable<ProductoDto>, DomainError>(
                DomainError.NotFound($"Categoría con ID {categoriaId} no encontrada")
            );
        }
        
        var productos = await _productoRepository.FindByCategoriaIdAsync(categoriaId);
        var dtos = productos.ToDtoList();
        
        return Result.Success<IEnumerable<ProductoDto>, DomainError>(dtos);
    }

    /// <summary>
    /// Crear un nuevo producto.
    /// Returns: Result.Success(ProductoDto) | Result.Failure(Validation/NotFound)
    /// </summary>
    public async Task<Result<ProductoDto, DomainError>> CreateAsync(ProductoRequestDto dto)
    {
        _logger.LogInformation("Creando producto: {Nombre}", dto.Nombre);
        
        var validationResult = await ValidateProductoAsync(dto);
        if (validationResult.IsFailure)
        {
            return Result.Failure<ProductoDto, DomainError>(validationResult.Error);
        }
        
        var producto = dto.ToEntity();
        var saved = await _productoRepository.SaveAsync(producto);
        
        _logger.LogInformation("Producto creado con ID: {Id}", saved.Id);
        
        var resultDto = saved.ToDto();
        
        _ = Task.Run(async () =>
        {
            try
            {
                await _cacheService.RemoveAsync("productos:all");
                _logger.LogDebug("Caché invalidada tras crear producto");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error al invalidar caché tras crear producto");
            }
        });
        
        _ = Task.Run(async () => await NotificarWebSocketProductoCreado(resultDto));
        
        _ = Task.Run(async () =>
        {
            try
            {
                var adminEmail = _configuration["Smtp:AdminEmail"];
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
                    await _emailService.EnqueueEmailAsync(emailMessage);
                    _logger.LogDebug("Email de notificación encolado tras crear producto");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error al encolar email de notificación");
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
        _logger.LogInformation("Actualizando producto con ID: {Id}", id);
        
        var producto = await _productoRepository.FindByIdAsync(id);
        
        if (producto == null)
        {
            _logger.LogWarning("Producto con ID {Id} no encontrado para actualizar", id);
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
        
        var updated = await _productoRepository.UpdateAsync(producto);
        
        _logger.LogInformation("Producto actualizado con ID: {Id}", id);
        
        var resultDto = updated.ToDto();
        
        _ = Task.Run(async () =>
        {
            try
            {
                await _cacheService.RemoveAsync($"productos:{id}");
                await _cacheService.RemoveAsync("productos:all");
                _logger.LogDebug("Caché invalidada tras actualizar producto");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error al invalidar caché tras actualizar producto");
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
        _logger.LogInformation("Eliminando producto con ID: {Id}", id);
        
        var producto = await _productoRepository.FindByIdAsync(id);
        
        if (producto == null)
        {
            _logger.LogWarning("Producto con ID {Id} no encontrado para eliminar", id);
            return UnitResult.Failure<DomainError>(
                DomainError.NotFound($"Producto con ID {id} no encontrado")
            );
        }
        
        await _productoRepository.DeleteAsync(id);
        _logger.LogInformation("Producto eliminado con ID: {Id}", id);
        
        _ = Task.Run(async () =>
        {
            try
            {
                await _cacheService.RemoveAsync($"productos:{id}");
                await _cacheService.RemoveAsync("productos:all");
                _logger.LogDebug("Caché invalidada tras eliminar producto");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error al invalidar caché tras eliminar producto");
            }
        });
        
        _ = Task.Run(async () => await NotificarWebSocketProductoEliminado(id));
        
        return UnitResult.Success<DomainError>();
    }

    /// <summary>
    /// Notifica vía WebSocket la creación de un producto.
    /// </summary>
    private async Task NotificarWebSocketProductoCreado(ProductoDto producto)
    {
        try
        {
            await _webSocketHandler.NotifyProductoCreatedAsync(producto);
            _logger.LogDebug("Notificación WebSocket enviada tras crear producto: {ProductoId}", producto.Id);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error en notificación WebSocket al crear producto: {ProductoId}", producto.Id);
        }
    }

    /// <summary>
    /// Notifica vía WebSocket la actualización de un producto.
    /// </summary>
    private async Task NotificarWebSocketProductoActualizado(ProductoDto producto)
    {
        try
        {
            await _webSocketHandler.NotifyProductoUpdatedAsync(producto);
            _logger.LogDebug("Notificación WebSocket enviada tras actualizar producto: {ProductoId}", producto.Id);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error en notificación WebSocket al actualizar producto: {ProductoId}", producto.Id);
        }
    }

    /// <summary>
    /// Notifica vía WebSocket la eliminación de un producto.
    /// </summary>
    private async Task NotificarWebSocketProductoEliminado(long productoId)
    {
        try
        {
            await _webSocketHandler.NotifyProductoDeletedAsync(productoId);
            _logger.LogDebug("Notificación WebSocket enviada tras eliminar producto: {ProductoId}", productoId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error en notificación WebSocket al eliminar producto: {ProductoId}", productoId);
        }
    }

    /// <summary>
    /// Valida los datos de un producto.
    /// Returns: UnitResult.Success | UnitResult.Failure(Validation/NotFound)
    /// </summary>
    private async Task<UnitResult<DomainError>> ValidateProductoAsync(ProductoRequestDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Nombre))
        {
            return UnitResult.Failure<DomainError>(
                DomainError.Validation("El nombre del producto es requerido")
            );
        }
        
        if (dto.Nombre.Length < 3)
        {
            return UnitResult.Failure<DomainError>(
                DomainError.Validation("El nombre debe tener al menos 3 caracteres")
            );
        }
        
        if (dto.Nombre.Length > 200)
        {
            return UnitResult.Failure<DomainError>(
                DomainError.Validation("El nombre no puede exceder 200 caracteres")
            );
        }
        
        if (dto.Precio <= 0)
        {
            return UnitResult.Failure<DomainError>(
                DomainError.Validation("El precio debe ser mayor que 0")
            );
        }
        
        if (dto.Stock < 0)
        {
            return UnitResult.Failure<DomainError>(
                DomainError.Validation("El stock no puede ser negativo")
            );
        }
        
        var categoriaExists = await _categoriaRepository.FindByIdAsync(dto.CategoriaId);
        if (categoriaExists == null)
        {
            return UnitResult.Failure<DomainError>(
                DomainError.NotFound($"Categoría con ID {dto.CategoriaId} no encontrada")
            );
        }
        
        return UnitResult.Success<DomainError>();
    }
}

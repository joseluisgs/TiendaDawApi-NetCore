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
/// Service for Producto using MODERN RESULT PATTERN approach
/// 
/// This service demonstrates the functional programming pattern:
/// - return Result.Success() or DomainError.NotFound()
/// - NO exceptions for business logic errors
/// - Explicit error handling in controller
/// 
/// Java comparison:
/// - Similar to Either<Error, Value> from Vavr
/// - Like Optional<T> but with error information
/// - CompletableFuture<Either<Error, T>> pattern
/// 
/// EDUCATIONAL NOTE: Compare this with CategoriaService (Exception-based)
/// 
/// Benefits of Result Pattern:
/// 1. Type-safe error handling
/// 2. No hidden control flow (exceptions)
/// 3. Easier to test (no try/catch needed)
/// 4. Explicit in method signatures what can fail
/// 5. Better performance (no stack unwinding)
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
    /// Get all products with cache-aside pattern
    /// Returns Success with list - doesn't fail
    /// Java: Either.right(List<ProductoDto>)
    /// </summary>
    public async Task<Result<IEnumerable<ProductoDto>, DomainError>> FindAllAsync()
    {
        _logger.LogInformation("Finding all productos");
        
        // Try cache first (cache-aside pattern)
        const string cacheKey = "productos:all";
        var cachedProductos = await _cacheService.GetAsync<IEnumerable<ProductoDto>>(cacheKey);
        
        if (cachedProductos != null)
        {
            _logger.LogInformation("Returning productos from cache");
            return Result.Success<IEnumerable<ProductoDto>, DomainError>(cachedProductos);
        }
        
        // Cache miss - read from database
        var productos = await _productoRepository.FindAllAsync();
        var dtos = productos.ToDtoList();
        
        // Update cache with TTL
        var cacheTTL = TimeSpan.FromMinutes(
            int.Parse(_configuration["Cache:ProductoCacheTTLMinutes"] ?? "10"));
        await _cacheService.SetAsync(cacheKey, dtos, cacheTTL);
        
        return Result.Success<IEnumerable<ProductoDto>, DomainError>(dtos);
    }

    /// <summary>
    /// Find product by ID with cache-aside pattern
    /// RETURNS Result - Success with ProductoDto OR Failure with DomainError
    /// 
    /// Java equivalent:
    /// Either<DomainError, ProductoDto> findById(Long id)
    /// 
    /// NO EXCEPTIONS thrown - error is returned as value
    /// </summary>
    public async Task<Result<ProductoDto, DomainError>> FindByIdAsync(long id)
    {
        _logger.LogInformation("Finding producto with id: {Id}", id);
        
        // Try cache first (cache-aside pattern)
        var cacheKey = $"productos:{id}";
        var cachedProducto = await _cacheService.GetAsync<ProductoDto>(cacheKey);
        
        if (cachedProducto != null)
        {
            _logger.LogInformation("Returning producto from cache: {Id}", id);
            return Result.Success<ProductoDto, DomainError>(cachedProducto);
        }
        
        // Cache miss - read from database
        var producto = await _productoRepository.FindByIdAsync(id);
        
        if (producto == null)
        {
            _logger.LogWarning("Producto with id {Id} not found", id);
            return Result.Failure<ProductoDto, DomainError>(
                DomainError.NotFound($"Producto con ID {id} no encontrado")
            );
        }
        
        var dto = producto.ToDto();
        
        // Update cache with TTL
        var cacheTTL = TimeSpan.FromMinutes(
            int.Parse(_configuration["Cache:ProductoCacheTTLMinutes"] ?? "10"));
        await _cacheService.SetAsync(cacheKey, dto, cacheTTL);
        
        return Result.Success<ProductoDto, DomainError>(dto);
    }

    /// <summary>
    /// Find products by category
    /// </summary>
    public async Task<Result<IEnumerable<ProductoDto>, DomainError>> FindByCategoriaIdAsync(long categoriaId)
    {
        _logger.LogInformation("Finding productos for categoria: {CategoriaId}", categoriaId);
        
        // Verify category exists
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
    /// Create new product
    /// RETURNS Result - no exceptions
    /// 
    /// Validation failures return DomainError.Validation
    /// Business rule violations return DomainError.BusinessRule
    /// 
    /// Java: Either<DomainError, ProductoDto> create(ProductoRequestDto dto)
    /// </summary>
    public async Task<Result<ProductoDto, DomainError>> CreateAsync(ProductoRequestDto dto)
    {
        _logger.LogInformation("Creating producto: {Nombre}", dto.Nombre);
        
        // Validation using Result Pattern
        var validationResult = await ValidateProductoAsync(dto);
        if (validationResult.IsFailure)
        {
            return Result.Failure<ProductoDto, DomainError>(validationResult.Error);
        }
        
        var producto = dto.ToEntity();
        var saved = await _productoRepository.SaveAsync(producto);
        
        _logger.LogInformation("Producto created with id: {Id}", saved.Id);
        
        var resultDto = saved.ToDto();
        
        // Invalidate cache (fire-and-forget)
        _ = Task.Run(async () =>
        {
            try
            {
                await _cacheService.RemoveAsync("productos:all");
                _logger.LogDebug("Cache invalidated after producto creation");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to invalidate cache after producto creation");
            }
        });
        
        // Notificar via WebSocket (side-effect - fire-and-forget)
        _ = Task.Run(async () => await NotificarWebSocketProductoCreado(resultDto));
        
        // Queue email notification (fire-and-forget)
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
                        Body = $@"
                            <h2>Nuevo Producto Creado</h2>
                            <p><strong>ID:</strong> {saved.Id}</p>
                            <p><strong>Nombre:</strong> {saved.Nombre}</p>
                            <p><strong>Precio:</strong> ${saved.Precio}</p>
                            <p><strong>Stock:</strong> {saved.Stock}</p>
                            <p><strong>Fecha:</strong> {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC</p>
                        ",
                        IsHtml = true
                    };
                    await _emailService.EnqueueEmailAsync(emailMessage);
                    _logger.LogDebug("Email notification queued for producto creation");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to queue email notification for producto creation");
            }
        });
        
        return Result.Success<ProductoDto, DomainError>(resultDto);
    }

    /// <summary>
    /// Update existing product
    /// RETURNS Result - no exceptions
    /// </summary>
    public async Task<Result<ProductoDto, DomainError>> UpdateAsync(long id, ProductoRequestDto dto)
    {
        _logger.LogInformation("Updating producto with id: {Id}", id);
        
        var producto = await _productoRepository.FindByIdAsync(id);
        
        if (producto == null)
        {
            _logger.LogWarning("Producto with id {Id} not found for update", id);
            return Result.Failure<ProductoDto, DomainError>(
                DomainError.NotFound($"Producto con ID {id} no encontrado")
            );
        }
        
        // Validation using Result Pattern
        var validationResult = await ValidateProductoAsync(dto);
        if (validationResult.IsFailure)
        {
            return Result.Failure<ProductoDto, DomainError>(validationResult.Error);
        }
        
        // Update fields
        producto.Nombre = dto.Nombre;
        producto.Descripcion = dto.Descripcion;
        producto.Precio = dto.Precio;
        producto.Stock = dto.Stock;
        producto.Imagen = dto.Imagen;
        producto.CategoriaId = dto.CategoriaId;
        
        var updated = await _productoRepository.UpdateAsync(producto);
        
        _logger.LogInformation("Producto updated with id: {Id}", id);
        
        var resultDto = updated.ToDto();
        
        // Invalidate cache (fire-and-forget)
        _ = Task.Run(async () =>
        {
            try
            {
                await _cacheService.RemoveAsync($"productos:{id}");
                await _cacheService.RemoveAsync("productos:all");
                _logger.LogDebug("Cache invalidated after producto update");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to invalidate cache after producto update");
            }
        });
        
        // Notificar via WebSocket (side-effect - fire-and-forget)
        _ = Task.Run(async () => await NotificarWebSocketProductoActualizado(resultDto));
        
        return Result.Success<ProductoDto, DomainError>(resultDto);
    }

    /// <summary>
    /// Delete product (soft delete)
    /// RETURNS UnitResult<DomainError> - void operation with potential error
    /// </summary>
    public async Task<UnitResult<DomainError>> DeleteAsync(long id)
    {
        _logger.LogInformation("Deleting producto with id: {Id}", id);
        
        var producto = await _productoRepository.FindByIdAsync(id);
        
        if (producto == null)
        {
            _logger.LogWarning("Producto with id {Id} not found for delete", id);
            return UnitResult.Failure<DomainError>(
                DomainError.NotFound($"Producto con ID {id} no encontrado")
            );
        }
        
        var productoNombre = producto.Nombre;
        
        await _productoRepository.DeleteAsync(id);
        _logger.LogInformation("Producto deleted with id: {Id}", id);
        
        // Invalidate cache (fire-and-forget)
        _ = Task.Run(async () =>
        {
            try
            {
                await _cacheService.RemoveAsync($"productos:{id}");
                await _cacheService.RemoveAsync("productos:all");
                _logger.LogDebug("Cache invalidated after producto deletion");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to invalidate cache after producto deletion");
            }
        });
        
        // Notificar via WebSocket (side-effect - fire-and-forget)
        _ = Task.Run(async () => await NotificarWebSocketProductoEliminado(id));
        
        return UnitResult.Success<DomainError>();
    }

    #region Private Helper Methods

    /// <summary>
    /// Notifica via WebSocket la creación de un producto
    /// Side-effect que NO debe fallar la operación principal
    /// </summary>
    private async Task NotificarWebSocketProductoCreado(ProductoDto producto)
    {
        try
        {
            await _webSocketHandler.NotifyProductoCreatedAsync(producto);
            _logger.LogDebug("WebSocket notification sent for producto creation: {ProductoId}", producto.Id);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed WebSocket notification for producto: {ProductoId}", producto.Id);
        }
    }

    /// <summary>
    /// Notifica via WebSocket la actualización de un producto
    /// Side-effect que NO debe fallar la operación principal
    /// </summary>
    private async Task NotificarWebSocketProductoActualizado(ProductoDto producto)
    {
        try
        {
            await _webSocketHandler.NotifyProductoUpdatedAsync(producto);
            _logger.LogDebug("WebSocket notification sent for producto update: {ProductoId}", producto.Id);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed WebSocket notification for producto update: {ProductoId}", producto.Id);
        }
    }

    /// <summary>
    /// Notifica via WebSocket la eliminación de un producto
    /// Side-effect que NO debe fallar la operación principal
    /// </summary>
    private async Task NotificarWebSocketProductoEliminado(long productoId)
    {
        try
        {
            await _webSocketHandler.NotifyProductoDeletedAsync(productoId);
            _logger.LogDebug("WebSocket notification sent for producto deletion: {ProductoId}", productoId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed WebSocket notification for producto deletion: {ProductoId}", productoId);
        }
    }

    #endregion

    /// <summary>
    /// Validation method using Result Pattern
    /// 
    /// RETURNS UnitResult<DomainError> instead of throwing exceptions
    /// 
    /// This is the MODERN approach:
    /// - Validation failures are returned as UnitResult
    /// - No exceptions thrown
    /// - Controller can handle gracefully
    /// 
    /// Java: Either<DomainError, Unit> validate(ProductoRequestDto dto)
    /// </summary>
    private async Task<UnitResult<DomainError>> ValidateProductoAsync(ProductoRequestDto dto)
    {
        // Validate nombre
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
        
        // Validate precio
        if (dto.Precio <= 0)
        {
            return UnitResult.Failure<DomainError>(
                DomainError.Validation("El precio debe ser mayor que 0")
            );
        }
        
        // Validate stock
        if (dto.Stock < 0)
        {
            return UnitResult.Failure<DomainError>(
                DomainError.Validation("El stock no puede ser negativo")
            );
        }
        
        // Validate categoria exists
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

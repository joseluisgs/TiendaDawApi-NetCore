using AutoMapper;
using TiendaApi.Common;
using TiendaApi.Models.DTOs;
using TiendaApi.Models.Entities;
using TiendaApi.Repositories;
using TiendaApi.Services.Cache;
using TiendaApi.Services.Email;

namespace TiendaApi.Services.Pedidos;

/// <summary>
/// Service for Pedidos using Result Pattern
/// Handles business logic: stock verification, reservation, MongoDB storage, notifications
/// </summary>
public class PedidosService : IPedidosService
{
    private readonly IPedidosRepository _pedidosRepository;
    private readonly IProductoRepository _productoRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<PedidosService> _logger;
    private readonly ICacheService _cacheService;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;

    public PedidosService(
        IPedidosRepository pedidosRepository,
        IProductoRepository productoRepository,
        IMapper mapper,
        ILogger<PedidosService> logger,
        ICacheService cacheService,
        IEmailService emailService,
        IConfiguration configuration)
    {
        _pedidosRepository = pedidosRepository;
        _productoRepository = productoRepository;
        _mapper = mapper;
        _logger = logger;
        _cacheService = cacheService;
        _emailService = emailService;
        _configuration = configuration;
    }

    public async Task<Result<IEnumerable<PedidoDto>, AppError>> FindAllAsync()
    {
        _logger.LogInformation("Finding all pedidos");
        
        var pedidos = await _pedidosRepository.FindAllAsync();
        var dtos = _mapper.Map<IEnumerable<PedidoDto>>(pedidos);
        
        return Result<IEnumerable<PedidoDto>, AppError>.Success(dtos);
    }

    public async Task<Result<IEnumerable<PedidoDto>, AppError>> FindByUserIdAsync(long userId)
    {
        _logger.LogInformation("Finding pedidos for user: {UserId}", userId);
        
        // Try cache first
        var cacheKey = $"pedidos:user:{userId}";
        var cachedPedidos = await _cacheService.GetAsync<IEnumerable<PedidoDto>>(cacheKey);
        
        if (cachedPedidos != null)
        {
            _logger.LogInformation("Returning pedidos from cache for user: {UserId}", userId);
            return Result<IEnumerable<PedidoDto>, AppError>.Success(cachedPedidos);
        }
        
        var pedidos = await _pedidosRepository.FindByUserIdAsync(userId);
        var dtos = _mapper.Map<IEnumerable<PedidoDto>>(pedidos);
        
        // Cache with TTL
        var cacheTTL = TimeSpan.FromMinutes(5);
        await _cacheService.SetAsync(cacheKey, dtos, cacheTTL);
        
        return Result<IEnumerable<PedidoDto>, AppError>.Success(dtos);
    }

    public async Task<Result<PedidoDto, AppError>> FindByIdAsync(string id)
    {
        _logger.LogInformation("Finding pedido: {Id}", id);
        
        // Try cache first
        var cacheKey = $"pedidos:{id}";
        var cachedPedido = await _cacheService.GetAsync<PedidoDto>(cacheKey);
        
        if (cachedPedido != null)
        {
            _logger.LogInformation("Returning pedido from cache: {Id}", id);
            return Result<PedidoDto, AppError>.Success(cachedPedido);
        }
        
        var pedido = await _pedidosRepository.FindByIdAsync(id);
        
        if (pedido == null)
        {
            _logger.LogWarning("Pedido not found: {Id}", id);
            return Result<PedidoDto, AppError>.Failure(
                AppError.NotFound($"Pedido con ID {id} no encontrado")
            );
        }
        
        var dto = _mapper.Map<PedidoDto>(pedido);
        
        // Cache with TTL
        var cacheTTL = TimeSpan.FromMinutes(5);
        await _cacheService.SetAsync(cacheKey, dto, cacheTTL);
        
        return Result<PedidoDto, AppError>.Success(dto);
    }

    public async Task<Result<PedidoDto, AppError>> CreateAsync(long userId, PedidoRequestDto dto)
    {
        _logger.LogInformation("Creating pedido for user: {UserId} with {ItemCount} items", userId, dto.Items.Count);
        
        // Validate items
        if (dto.Items == null || !dto.Items.Any())
        {
            return Result<PedidoDto, AppError>.Failure(
                AppError.Validation("El pedido debe contener al menos un producto")
            );
        }
        
        var pedidoItems = new List<PedidoItem>();
        var productosToUpdate = new List<Producto>();
        decimal total = 0;
        
        // Verify products and calculate totals
        foreach (var itemDto in dto.Items)
        {
            if (itemDto.Cantidad <= 0)
            {
                return Result<PedidoDto, AppError>.Failure(
                    AppError.Validation($"La cantidad debe ser mayor que 0 para el producto {itemDto.ProductoId}")
                );
            }
            
            var producto = await _productoRepository.FindByIdAsync(itemDto.ProductoId);
            
            if (producto == null)
            {
                return Result<PedidoDto, AppError>.Failure(
                    AppError.NotFound($"Producto con ID {itemDto.ProductoId} no encontrado")
                );
            }
            
            if (producto.Stock < itemDto.Cantidad)
            {
                return Result<PedidoDto, AppError>.Failure(
                    AppError.BusinessRule($"Stock insuficiente para el producto {producto.Nombre}. Disponible: {producto.Stock}, Solicitado: {itemDto.Cantidad}")
                );
            }
            
            var subtotal = producto.Precio * itemDto.Cantidad;
            total += subtotal;
            
            pedidoItems.Add(new PedidoItem
            {
                ProductoId = producto.Id,
                NombreProducto = producto.Nombre,
                Cantidad = itemDto.Cantidad,
                Precio = producto.Precio,
                Subtotal = subtotal
            });
            
            // Prepare product for stock update
            producto.Stock -= itemDto.Cantidad;
            productosToUpdate.Add(producto);
        }
        
        // Reserve stock in PostgreSQL (transaction-like behavior)
        try
        {
            foreach (var producto in productosToUpdate)
            {
                await _productoRepository.UpdateAsync(producto);
                _logger.LogDebug("Stock reserved for producto: {ProductoId}, new stock: {Stock}", producto.Id, producto.Stock);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reserve stock for pedido");
            return Result<PedidoDto, AppError>.Failure(
                AppError.Internal("Error al reservar el stock de productos")
            );
        }
        
        // Create pedido document in MongoDB
        var pedido = new Pedido
        {
            UserId = userId,
            Items = pedidoItems,
            Total = total,
            Estado = PedidoEstado.PENDIENTE,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        try
        {
            var savedPedido = await _pedidosRepository.SaveAsync(pedido);
            _logger.LogInformation("Pedido created: {Id} for user: {UserId}, total: {Total}", savedPedido.Id, userId, total);
            
            var resultDto = _mapper.Map<PedidoDto>(savedPedido);
            
            // Invalidate cache (fire-and-forget)
            _ = Task.Run(async () =>
            {
                try
                {
                    await _cacheService.RemoveAsync($"pedidos:user:{userId}");
                    _logger.LogDebug("Cache invalidated for user pedidos: {UserId}", userId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to invalidate cache after pedido creation");
                }
            });
            
            // Cache the newly created pedido (fire-and-forget)
            _ = Task.Run(async () =>
            {
                try
                {
                    var cacheTTL = TimeSpan.FromMinutes(5);
                    await _cacheService.SetAsync($"pedidos:{savedPedido.Id}", resultDto, cacheTTL);
                    _logger.LogDebug("Cached new pedido: {Id}", savedPedido.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to cache new pedido");
                }
            });
            
            // Send confirmation email to user (fire-and-forget)
            _ = Task.Run(async () =>
            {
                try
                {
                    var adminEmail = _configuration["Smtp:AdminEmail"];
                    if (!string.IsNullOrEmpty(adminEmail))
                    {
                        var itemsHtml = string.Join("", pedidoItems.Select(i => 
                            $"<li>{i.NombreProducto} - Cantidad: {i.Cantidad} - Precio: ${i.Precio:F2} - Subtotal: ${i.Subtotal:F2}</li>"));
                        
                        var emailMessage = new EmailMessage
                        {
                            To = adminEmail,
                            Subject = $"Nuevo Pedido #{savedPedido.Id}",
                            Body = $@"
                                <h2>Nuevo Pedido Recibido</h2>
                                <p><strong>ID Pedido:</strong> {savedPedido.Id}</p>
                                <p><strong>Usuario ID:</strong> {userId}</p>
                                <p><strong>Estado:</strong> {savedPedido.Estado}</p>
                                <p><strong>Total:</strong> ${total:F2}</p>
                                <h3>Items:</h3>
                                <ul>{itemsHtml}</ul>
                                <p><strong>Fecha:</strong> {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC</p>
                            ",
                            IsHtml = true
                        };
                        await _emailService.EnqueueEmailAsync(emailMessage);
                        _logger.LogDebug("Email notification queued for pedido creation");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to queue email notification for pedido creation");
                }
            });
            
            return Result<PedidoDto, AppError>.Success(resultDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save pedido to MongoDB, attempting stock compensation");
            
            // Compensation: Restore stock if MongoDB save fails
            _ = Task.Run(async () =>
            {
                try
                {
                    foreach (var producto in productosToUpdate)
                    {
                        producto.Stock += pedidoItems.First(i => i.ProductoId == producto.Id).Cantidad;
                        await _productoRepository.UpdateAsync(producto);
                        _logger.LogInformation("Stock restored for producto: {ProductoId} after pedido save failure", producto.Id);
                    }
                }
                catch (Exception compensationEx)
                {
                    _logger.LogError(compensationEx, "CRITICAL: Failed to restore stock after pedido save failure");
                }
            });
            
            return Result<PedidoDto, AppError>.Failure(
                AppError.Internal("Error al crear el pedido")
            );
        }
    }

    public async Task<Result<PedidoDto, AppError>> UpdateEstadoAsync(string id, string nuevoEstado)
    {
        _logger.LogInformation("Updating pedido estado: {Id} to {Estado}", id, nuevoEstado);
        
        // Validate estado
        var validEstados = new[] { PedidoEstado.PENDIENTE, PedidoEstado.PROCESANDO, PedidoEstado.ENVIADO, PedidoEstado.ENTREGADO, PedidoEstado.CANCELADO };
        if (!validEstados.Contains(nuevoEstado))
        {
            return Result<PedidoDto, AppError>.Failure(
                AppError.Validation($"Estado inválido. Valores permitidos: {string.Join(", ", validEstados)}")
            );
        }
        
        var pedido = await _pedidosRepository.FindByIdAsync(id);
        
        if (pedido == null)
        {
            _logger.LogWarning("Pedido not found: {Id}", id);
            return Result<PedidoDto, AppError>.Failure(
                AppError.NotFound($"Pedido con ID {id} no encontrado")
            );
        }
        
        var estadoAnterior = pedido.Estado;
        pedido.Estado = nuevoEstado;
        
        var updated = await _pedidosRepository.UpdateAsync(pedido);
        _logger.LogInformation("Pedido estado updated: {Id}, from {OldEstado} to {NewEstado}", id, estadoAnterior, nuevoEstado);
        
        var resultDto = _mapper.Map<PedidoDto>(updated);
        
        // Invalidate cache (fire-and-forget)
        _ = Task.Run(async () =>
        {
            try
            {
                await _cacheService.RemoveAsync($"pedidos:{id}");
                await _cacheService.RemoveAsync($"pedidos:user:{pedido.UserId}");
                _logger.LogDebug("Cache invalidated after pedido estado update");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to invalidate cache after pedido estado update");
            }
        });
        
        // Send status change notification email (fire-and-forget)
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
                        Subject = $"Pedido #{id} - Cambio de Estado",
                        Body = $@"
                            <h2>Cambio de Estado de Pedido</h2>
                            <p><strong>ID Pedido:</strong> {id}</p>
                            <p><strong>Usuario ID:</strong> {pedido.UserId}</p>
                            <p><strong>Estado Anterior:</strong> {estadoAnterior}</p>
                            <p><strong>Estado Nuevo:</strong> {nuevoEstado}</p>
                            <p><strong>Total:</strong> ${pedido.Total:F2}</p>
                            <p><strong>Fecha Actualización:</strong> {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC</p>
                        ",
                        IsHtml = true
                    };
                    await _emailService.EnqueueEmailAsync(emailMessage);
                    _logger.LogDebug("Email notification queued for pedido estado change");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to queue email notification for pedido estado change");
            }
        });
        
        return Result<PedidoDto, AppError>.Success(resultDto);
    }
}

using CSharpFunctionalExtensions;
using TiendaApi.Dtos.Pedidos;
using TiendaApi.Errors;
using TiendaApi.Mappers;
using TiendaApi.Models;
using TiendaApi.Repositories.Pedidos;
using TiendaApi.Repositories.Productos;
using TiendaApi.Services.Cache;
using TiendaApi.Services.Email;
using TiendaApi.WebSockets.Pedidos;

namespace TiendaApi.Services.Pedidos;

/// <summary>
/// Servicio de pedidos usando Patrón Result.
/// Maneja la lógica de negocio: verificación de stock, reservas, almacenamiento MongoDB, notificaciones.
/// </summary>
public class PedidosService(
    IPedidosRepository pedidosRepository,
    IProductoRepository productoRepository,
    ILogger<PedidosService> logger,
    ICacheService cacheService,
    IEmailService emailService,
    IConfiguration configuration,
    PedidoWebSocketHandler webSocketHandler
) : IPedidosService {

    /// <summary>
    /// Obtiene todos los pedidos.
    /// Returns: Result.Success(List) | Result.Failure nunca
    /// </summary>
    public async Task<Result<IEnumerable<PedidoDto>, DomainError>> FindAllAsync() {
        logger.LogInformation("Obteniendo todos los pedidos");
        
        var pedidos = await pedidosRepository.FindAllAsync();
        var dtos = pedidos.ToDtoList();
        
        return Result.Success<IEnumerable<PedidoDto>, DomainError>(dtos);
    }

    /// <summary>
    /// Obtiene los pedidos de un usuario con caché.
    /// Returns: Result.Success(List) | Result.Failure nunca
    /// </summary>
    public async Task<Result<IEnumerable<PedidoDto>, DomainError>> FindByUserIdAsync(long userId) {
        logger.LogInformation("Obteniendo pedidos del usuario: {UserId}", userId);
        
        var cacheKey = $"pedidos:user:{userId}";
        var cachedPedidos = await cacheService.GetAsync<IEnumerable<PedidoDto>>(cacheKey);
        
        if (cachedPedidos != null) {
            logger.LogInformation("Devolviendo pedidos desde caché para usuario: {UserId}", userId);
            return Result.Success<IEnumerable<PedidoDto>, DomainError>(cachedPedidos);
        }
        
        var pedidos = await pedidosRepository.FindByUserIdAsync(userId);
        var dtos = pedidos.ToDtoList();
        
        var cacheTTL = TimeSpan.FromMinutes(5);
        await cacheService.SetAsync(cacheKey, dtos, cacheTTL);
        
        return Result.Success<IEnumerable<PedidoDto>, DomainError>(dtos);
    }

    /// <summary>
    /// Obtiene un pedido por su ID con caché.
    /// Returns: Result.Success(PedidoDto) | Result.Failure(NotFound)
    /// </summary>
    public async Task<Result<PedidoDto, DomainError>> FindByIdAsync(string id) {
        logger.LogInformation("Obteniendo pedido: {Id}", id);
        
        var cacheKey = $"pedidos:{id}";
        var cachedPedido = await cacheService.GetAsync<PedidoDto>(cacheKey);
        
        if (cachedPedido != null) {
            logger.LogInformation("Devolviendo pedido desde caché: {Id}", id);
            return Result.Success<PedidoDto, DomainError>(cachedPedido);
        }
        
        var pedido = await pedidosRepository.FindByIdAsync(id);
        
        if (pedido == null) {
            logger.LogWarning("Pedido no encontrado: {Id}", id);
            return Result.Failure<PedidoDto, DomainError>(
                DomainError.NotFound($"Pedido con ID {id} no encontrado")
            );
        }
        
        var dto = pedido.ToDto();
        
        var cacheTTL = TimeSpan.FromMinutes(5);
        await cacheService.SetAsync(cacheKey, dto, cacheTTL);
        
        return Result.Success<PedidoDto, DomainError>(dto);
    }

    /// <summary>
    /// Crea un nuevo pedido con verificación y reserva de stock.
    /// Returns: Result.Success(PedidoDto) | Result.Failure(Validation/NotFound/BusinessRule/Internal)
    /// </summary>
    public async Task<Result<PedidoDto, DomainError>> CreateAsync(long userId, PedidoRequestDto dto) {
        logger.LogInformation("Creando pedido para usuario: {UserId} con {ItemCount} items", userId, dto.Items.Count);
        
        if (dto.Items == null || !dto.Items.Any()) {
            return Result.Failure<PedidoDto, DomainError>(
                DomainError.Validation("El pedido debe contener al menos un producto")
            );
        }
        
        var pedidoItems = new List<PedidoItem>();
        var productosToUpdate = new List<Producto>();
        decimal total = 0;
        
        foreach (var itemDto in dto.Items) {
            if (itemDto.Cantidad <= 0) {
                return Result.Failure<PedidoDto, DomainError>(
                    DomainError.Validation($"La cantidad debe ser mayor que 0 para el producto {itemDto.ProductoId}")
                );
            }
            
            var producto = await productoRepository.FindByIdAsync(itemDto.ProductoId);
            
            if (producto == null) {
                return Result.Failure<PedidoDto, DomainError>(
                    DomainError.NotFound($"Producto con ID {itemDto.ProductoId} no encontrado")
                );
            }
            
            if (producto.Stock < itemDto.Cantidad) {
                return Result.Failure<PedidoDto, DomainError>(
                    DomainError.BusinessRule($"Stock insuficiente para el producto {producto.Nombre}. Disponible: {producto.Stock}, Solicitado: {itemDto.Cantidad}")
                );
            }
            
            var subtotal = producto.Precio * itemDto.Cantidad;
            total += subtotal;
            
            pedidoItems.Add(new PedidoItem {
                ProductoId = producto.Id,
                NombreProducto = producto.Nombre,
                Cantidad = itemDto.Cantidad,
                Precio = producto.Precio,
                Subtotal = subtotal
            });
            
            producto.Stock -= itemDto.Cantidad;
            productosToUpdate.Add(producto);
        }
        
        try {
            foreach (var producto in productosToUpdate) {
                await productoRepository.UpdateAsync(producto);
                logger.LogDebug("Stock reservado para producto: {ProductoId}, nuevo stock: {Stock}", producto.Id, producto.Stock);
            }
        }
        catch (Exception ex) {
            logger.LogError(ex, "Error al reservar stock para pedido");
            return Result.Failure<PedidoDto, DomainError>(
                DomainError.Internal("Error al reservar el stock de productos")
            );
        }
        
        var pedido = new Pedido {
            UserId = userId,
            Items = pedidoItems,
            Total = total,
            Estado = PedidoEstado.PENDIENTE,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        try {
            var savedPedido = await pedidosRepository.SaveAsync(pedido);
            logger.LogInformation("Pedido creado: {Id} para usuario: {UserId}, total: {Total}", savedPedido.Id, userId, total);
            
            var resultDto = savedPedido.ToDto();
            
            _ = Task.Run(async () =>
            {
                try {
                    await cacheService.RemoveAsync($"pedidos:user:{userId}");
                    logger.LogDebug("Caché invalidada para pedidos del usuario: {UserId}", userId);
                }
                catch (Exception ex) {
                    logger.LogWarning(ex, "Error al invalidar caché tras crear pedido");
                }
            });
            
            _ = Task.Run(async () =>
            {
                try {
                    var cacheTTL = TimeSpan.FromMinutes(5);
                    await cacheService.SetAsync($"pedidos:{savedPedido.Id}", resultDto, cacheTTL);
                    logger.LogDebug("Pedido cacheado: {Id}", savedPedido.Id);
                }
                catch (Exception ex) {
                    logger.LogWarning(ex, "Error al cachear nuevo pedido");
                }
            });
            
            _ = Task.Run(async () =>
            {
                try {
                    var adminEmail = configuration["Smtp:AdminEmail"];
                    if (!string.IsNullOrEmpty(adminEmail)) {
                        var itemsHtml = string.Join("", pedidoItems.Select(i => 
                            $"<li>{i.NombreProducto} - Cantidad: {i.Cantidad} - Precio: ${i.Precio:F2} - Subtotal: ${i.Subtotal:F2}</li>"));
                        
                        var emailMessage = new EmailMessage {
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
                        await emailService.EnqueueEmailAsync(emailMessage);
                        logger.LogDebug("Email de notificación encolado tras crear pedido");
                    }
                }
                catch (Exception ex) {
                    logger.LogWarning(ex, "Error al encolar email de notificación tras crear pedido");
                }
            });
            
            if (!string.IsNullOrEmpty(savedPedido.Id)) {
                var pedidoId = savedPedido.Id;
                _ = Task.Run(async () => await NotificarWebSocketPedidoCreado(pedidoId, userId, resultDto));
            }
            
            return Result.Success<PedidoDto, DomainError>(resultDto);
        }
        catch (Exception ex) {
            logger.LogError(ex, "Error al guardar pedido en MongoDB, compensando stock");
            
            _ = Task.Run(async () =>
            {
                try {
                    foreach (var producto in productosToUpdate) {
                        producto.Stock += pedidoItems.First(i => i.ProductoId == producto.Id).Cantidad;
                        await productoRepository.UpdateAsync(producto);
                        logger.LogInformation("Stock restaurado para producto: {ProductoId} tras error al guardar pedido", producto.Id);
                    }
                }
                catch (Exception compensationEx) {
                    logger.LogError(compensationEx, "CRÍTICO: Error al restaurar stock tras error al guardar pedido");
                }
            });
            
            return Result.Failure<PedidoDto, DomainError>(
                DomainError.Internal("Error al crear el pedido")
            );
        }
    }

    /// <summary>
    /// Actualiza el estado de un pedido.
    /// Returns: Result.Success(PedidoDto) | Result.Failure(NotFound/Validation)
    /// </summary>
    public async Task<Result<PedidoDto, DomainError>> UpdateEstadoAsync(string id, string nuevoEstado) {
        logger.LogInformation("Actualizando estado del pedido: {Id} a {Estado}", id, nuevoEstado);
        
        var validEstados = new[] { PedidoEstado.PENDIENTE, PedidoEstado.PROCESANDO, PedidoEstado.ENVIADO, PedidoEstado.ENTREGADO, PedidoEstado.CANCELADO };
        if (!validEstados.Contains(nuevoEstado)) {
            return Result.Failure<PedidoDto, DomainError>(
                DomainError.Validation($"Estado inválido. Valores permitidos: {string.Join(", ", validEstados)}")
            );
        }
        
        var pedido = await pedidosRepository.FindByIdAsync(id);
        
        if (pedido == null) {
            logger.LogWarning("Pedido no encontrado: {Id}", id);
            return Result.Failure<PedidoDto, DomainError>(
                DomainError.NotFound($"Pedido con ID {id} no encontrado")
            );
        }
        
        var estadoAnterior = pedido.Estado;
        pedido.Estado = nuevoEstado;
        
        var updated = await pedidosRepository.UpdateAsync(pedido);
        logger.LogInformation("Estado del pedido actualizado: {Id}, de {OldEstado} a {NewEstado}", id, estadoAnterior, nuevoEstado);
        
        var resultDto = updated.ToDto();
        
        _ = Task.Run(async () =>
        {
            try {
                await cacheService.RemoveAsync($"pedidos:{id}");
                await cacheService.RemoveAsync($"pedidos:user:{pedido.UserId}");
                logger.LogDebug("Caché invalidada tras actualizar estado del pedido");
            }
            catch (Exception ex) {
                logger.LogWarning(ex, "Error al invalidar caché tras actualizar estado del pedido");
            }
        });
        
        _ = Task.Run(async () =>
        {
            try {
                var adminEmail = configuration["Smtp:AdminEmail"];
                if (!string.IsNullOrEmpty(adminEmail)) {
                    var emailMessage = new EmailMessage {
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
                    await emailService.EnqueueEmailAsync(emailMessage);
                    logger.LogDebug("Email de notificación encolado tras cambio de estado del pedido");
                }
            }
            catch (Exception ex) {
                logger.LogWarning(ex, "Error al encolar email de notificación tras cambio de estado");
            }
        });
        
        return Result.Success<PedidoDto, DomainError>(resultDto);
    }

    /// <summary>
    /// Notifica vía WebSocket la creación de un pedido.
    /// Efecto secundario que no debe fallar la operación principal.
    /// </summary>
    private async Task NotificarWebSocketPedidoCreado(string pedidoId, long userId, PedidoDto pedido) {
        try {
            await webSocketHandler.NotifyPedidoCreatedAsync(pedidoId, userId, pedido);
            logger.LogDebug("Notificación WebSocket enviada para pedido: {PedidoId}", pedidoId);
        }
        catch (Exception ex) {
            logger.LogWarning(ex, "Error en notificación WebSocket para pedido: {PedidoId}", pedidoId);
        }
    }
}

using CSharpFunctionalExtensions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using TiendaApi.Apis.Dtos.Common;
using TiendaApi.Apis.Dtos.Pedidos;
using TiendaApi.Apis.Errors;
using TiendaApi.Apis.Mappers;
using TiendaApi.Apis.Models;
using TiendaApi.Apis.Repositories.Pedidos;
using TiendaApi.Apis.Repositories.Productos;
using TiendaApi.Apis.Services.Cache;
using TiendaApi.Apis.Services.Email;
using TiendaApi.Apis.Validators.Pedidos;
using TiendaApi.Apis.WebSockets.Pedidos;

namespace TiendaApi.Apis.Services.Pedidos;

/// <summary>
/// Servicio de pedidos usando Patrón Result.
/// Implementa el enfoque híbrido: Serializable + Retry para garantizar integridad en operaciones críticas.
/// </summary>
public class PedidosService(
    IPedidosRepository pedidosRepository,
    IProductoRepository productoRepository,
    ILogger<PedidosService> logger,
    ICacheService cacheService,
    IEmailService emailService,
    IConfiguration configuration,
    PedidoWebSocketHandler webSocketHandler,
    IValidator<PedidoRequestDto> pedidoValidator,
    IValidator<PedidoItemRequestDto> pedidoItemValidator
) : IPedidosService
{

    private const int MaxRetries = 3;

    /// <summary>
    /// Obtiene todos los pedidos.
    /// Devuelve: Result.Success(List) | Result.Failure nunca
    /// </summary>
    public async Task<Result<IEnumerable<PedidoDto>, DomainError>> FindAllAsync()
    {
        logger.LogInformation("Obteniendo todos los pedidos");

        var pedidos = await pedidosRepository.FindAllAsync();
        var dtos = pedidos.ToDtoList();

        return Result.Success<IEnumerable<PedidoDto>, DomainError>(dtos);
    }

    /// <summary>
    /// Obtiene los pedidos de un usuario con caché.
    /// Devuelve: Result.Success(List) | Result.Failure nunca
    /// </summary>
    public async Task<Result<IEnumerable<PedidoDto>, DomainError>> FindByUserIdAsync(long userId)
    {
        logger.LogInformation("Obteniendo pedidos del usuario: {UserId}", userId);

        var cacheKey = $"pedidos:user:{userId}";
        var cachedPedidos = await cacheService.GetAsync<IEnumerable<PedidoDto>>(cacheKey);

        if (cachedPedidos is not null)
        {
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
    /// Obtiene los pedidos paginados de un usuario.
    /// Devuelve: Result.Success(PagedResult) | Result.Failure nunca
    /// </summary>
    public async Task<Result<PagedResult<PedidoDto>, DomainError>> FindByUserIdPagedAsync(long userId, int page, int size)
    {
        logger.LogInformation("Obteniendo pedidos paginados del usuario: {UserId}, Página: {Page}, Tamaño: {Size}", userId, page, size);

        var (pedidos, totalCount) = await pedidosRepository.FindByUserIdPagedAsync(userId, page, size);
        var dtos = pedidos.ToDtoList();

        var pagedResult = new PagedResult<PedidoDto>
        {
            Items = dtos,
            TotalCount = totalCount,
            Page = page + 1,
            PageSize = size
        };

        return Result.Success<PagedResult<PedidoDto>, DomainError>(pagedResult);
    }

    /// <summary>
    /// Obtiene un pedido por su ID con caché.
    /// Devuelve: Result.Success(PedidoDto) | Result.Failure(NotFound)
    /// </summary>
    public async Task<Result<PedidoDto, DomainError>> FindByIdAsync(string id)
    {
        logger.LogInformation("Obteniendo pedido: {Id}", id);

        var cacheKey = $"pedidos:{id}";
        var cachedPedido = await cacheService.GetAsync<PedidoDto>(cacheKey);

        if (cachedPedido is not null)
        {
            logger.LogInformation("Devolviendo pedido desde caché: {Id}", id);
            return Result.Success<PedidoDto, DomainError>(cachedPedido);
        }

        var pedido = await pedidosRepository.FindByIdAsync(id);

        if (pedido == null)
        {
            logger.LogWarning("Pedido no encontrado: {Id}", id);
            return Result.Failure<PedidoDto, DomainError>(
                Errors.Pedidos.PedidoError.NotFound(id)
            );
        }

        var dto = pedido.ToDto();

        var cacheTTL = TimeSpan.FromMinutes(5);
        await cacheService.SetAsync(cacheKey, dto, cacheTTL);

        return Result.Success<PedidoDto, DomainError>(dto);
    }

    /// <summary>
    /// Crea un nuevo pedido con verificación de stock usando enfoque híbrido: Serializable + Retry.
    /// Este método implementa control de concurrencia optimista con reintentos automáticos
    /// en caso de errores de serialización de PostgreSQL (código 40001).
    /// Devuelve: Result.Success(PedidoDto) | Result.Failure(Validation/NotFound/BusinessRule/Conflict/Internal)
    /// </summary>
    public async Task<Result<PedidoDto, DomainError>> CreateAsync(long userId, PedidoRequestDto dto)
    {
        logger.LogInformation("Creando pedido para usuario: {UserId} con {ItemCount} items", userId, dto.Items.Count);

        var validationResult = await ValidatePedidoAsync(dto);
        if (validationResult.IsFailure)
        {
            return Result.Failure<PedidoDto, DomainError>(validationResult.Error);
        }

        for (int attempt = 1; attempt <= MaxRetries; attempt++)
        {
            try
            {
                return await CreateWithSerializableTransactionAsync(userId, dto);
            }
            catch (SerializationFailureException)
            {
                if (attempt == MaxRetries)
                {
                    logger.LogWarning(
                        "Maximos reintentos alcanzados por conflicto de serializacion para usuario {UserId}",
                        userId);
                    return Result.Failure<PedidoDto, DomainError>(
                        Errors.Pedidos.PedidoError.PedidoAdquirido(string.Empty)
                    );
                }

                var delayMs = 50 * attempt;
                logger.LogDebug(
                    "Reintento {Attempt}/{MaxRetries} tras error de serializacion para usuario {UserId}, delay: {Delay}ms",
                    attempt, MaxRetries, userId, delayMs);

                await Task.Delay(delayMs);
            }
            catch (NpgsqlException ex) when (IsSerializationFailureMessage(ex.Message))
            {
                if (attempt == MaxRetries)
                {
                    logger.LogWarning(
                        "Maximos reintentos alcanzados por conflicto de serializacion para usuario {UserId}",
                        userId);
                    return Result.Failure<PedidoDto, DomainError>(
                        Errors.Pedidos.PedidoError.PedidoAdquirido(string.Empty)
                    );
                }

                var delayMs = 50 * attempt;
                logger.LogDebug(
                    "Reintento {Attempt}/{MaxRetries} tras error de serializacion para usuario {UserId}, delay: {Delay}ms",
                    attempt, MaxRetries, userId, delayMs);

                await Task.Delay(delayMs);
            }
        }

        return Result.Failure<PedidoDto, DomainError>(
            Errors.Pedidos.PedidoError.ErrorProcesando()
        );
    }

    /// <summary>
    /// Crea el pedido dentro de una transacción Serializable.
    /// Si ocurre un error de serialización (40001), lanza SerializationFailureException.
    /// </summary>
    private async Task<Result<PedidoDto, DomainError>> CreateWithSerializableTransactionAsync(
        long userId,
        PedidoRequestDto dto)
    {
        await using var transaction = await productoRepository.BeginTransactionAsync(
            System.Data.IsolationLevel.Serializable);

        try
        {
            var pedidoItems = new List<PedidoItem>();
            decimal total = 0;

            foreach (var itemDto in dto.Items)
            {
                var itemValidation = await ValidatePedidoItemAsync(itemDto);
                if (itemValidation.IsFailure)
                {
                    await transaction.RollbackAsync();
                    return Result.Failure<PedidoDto, DomainError>(itemValidation.Error);
                }

                var producto = await productoRepository.FindByIdAsync(itemDto.ProductoId);

                if (producto is null)
                {
                    await transaction.RollbackAsync();
                    return Result.Failure<PedidoDto, DomainError>(
                        Errors.Pedidos.PedidoError.ProductoNoEncontrado(itemDto.ProductoId)
                    );
                }

                if (producto.Stock < itemDto.Cantidad)
                {
                    await transaction.RollbackAsync();
                    return Result.Failure<PedidoDto, DomainError>(
                        Errors.Pedidos.PedidoError.StockInsuficiente(producto.Nombre, producto.Stock, itemDto.Cantidad)
                    );
                }

                producto.Stock -= itemDto.Cantidad;
                producto.UpdatedAt = DateTime.UtcNow;

                await productoRepository.UpdateAsync(producto);

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

                logger.LogDebug("Stock decrementado para producto: {ProductoId}, cantidad: {Cantidad}",
                    producto.Id, itemDto.Cantidad);
            }

            var pedido = new Pedido
            {
                UserId = userId,
                Items = pedidoItems,
                Total = total,
                Estado = PedidoEstado.PENDIENTE,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var savedPedido = await pedidosRepository.SaveAsync(pedido);

            await transaction.CommitAsync();

            logger.LogInformation("Pedido creado: {Id} para usuario: {UserId}, total: {Total}",
                savedPedido.Id, userId, total);

            var resultDto = savedPedido.ToDto();

            _ = Task.Run(async () => await NotificarPedidoCreadoAsync(userId, resultDto, pedidoItems, total));

            return Result.Success<PedidoDto, DomainError>(resultDto);
        }
        catch (DbUpdateException ex) when (IsSerializationFailure(ex))
        {
            await transaction.RollbackAsync();
            logger.LogWarning(ex, "Error de serializacion PostgreSQL (40001) al crear pedido para usuario {UserId}", userId);
            throw new SerializationFailureException("Conflicto de serializacion, reintentar", ex);
        }
        catch (NpgsqlException ex) when (IsSerializationFailureMessage(ex.Message))
        {
            await transaction.RollbackAsync();
            logger.LogWarning(ex, "Error de serializacion PostgreSQL (40001) al crear pedido para usuario {UserId}", userId);
            throw new SerializationFailureException("Conflicto de serializacion, reintentar", ex);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            logger.LogError(ex, "Error al crear pedido para usuario {UserId}", userId);
            throw;
        }
    }

    /// <summary>
    /// Determina si la excepción es un error de serialización de PostgreSQL (código 40001) desde DbUpdateException.
    /// </summary>
    private bool IsSerializationFailure(DbUpdateException ex)
    {
        return ex.InnerException is NpgsqlException npgsqlEx &&
               IsSerializationFailureMessage(npgsqlEx.Message);
    }

    /// <summary>
    /// Determina si el mensaje de excepción indica un error de serialización de PostgreSQL (código 40001).
    /// </summary>
    private bool IsSerializationFailureMessage(string message)
    {
        return message.Contains("40001") ||
               message.Contains("serialization") ||
               message.Contains("serializacion");
    }

    /// <summary>
    /// Actualiza el estado de un pedido.
    /// Devuelve: Result.Success(PedidoDto) | Result.Failure(NotFound/Validation)
    /// </summary>
    public async Task<Result<PedidoDto, DomainError>> UpdateEstadoAsync(string id, string nuevoEstado)
    {
        logger.LogInformation("Actualizando estado del pedido: {Id} a {Estado}", id, nuevoEstado);

        var validEstados = new[] { PedidoEstado.PENDIENTE, PedidoEstado.PROCESANDO, PedidoEstado.ENVIADO, PedidoEstado.ENTREGADO, PedidoEstado.CANCELADO };
        if (!validEstados.Contains(nuevoEstado))
        {
            return Result.Failure<PedidoDto, DomainError>(
                Errors.Pedidos.PedidoError.EstadoInvalido(nuevoEstado, validEstados)
            );
        }

        var pedido = await pedidosRepository.FindByIdAsync(id);

        if (pedido == null)
        {
            logger.LogWarning("Pedido no encontrado: {Id}", id);
            return Result.Failure<PedidoDto, DomainError>(
                Errors.Pedidos.PedidoError.NotFound(id)
            );
        }

        var estadoAnterior = pedido.Estado;
        pedido.Estado = nuevoEstado;

        var updated = await pedidosRepository.UpdateAsync(pedido);
        logger.LogInformation("Estado del pedido actualizado: {Id}, de {OldEstado} a {NewEstado}", id, estadoAnterior, nuevoEstado);

        var resultDto = updated.ToDto();

        _ = Task.Run(async () =>
        {
            try
            {
                await cacheService.RemoveAsync($"pedidos:{id}");
                await cacheService.RemoveAsync($"pedidos:user:{pedido.UserId}");
                logger.LogDebug("Caché invalidada tras actualizar estado del pedido");
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Error al invalidar caché tras actualizar estado del pedido");
            }
        });

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
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Error al encolar email de notificación tras cambio de estado");
            }
        });

        return Result.Success<PedidoDto, DomainError>(resultDto);
    }

    /// <summary>
    /// Notifica la creación del pedido vía WebSocket, email y caché.
    /// Efectos secundarios que no deben fallar la operación principal.
    /// </summary>
    private async Task NotificarPedidoCreadoAsync(long userId, PedidoDto resultDto, List<PedidoItem> pedidoItems, decimal total)
    {
        try
        {
            await cacheService.RemoveAsync($"pedidos:user:{userId}");
            logger.LogDebug("Caché invalidada para pedidos del usuario: {UserId}", userId);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Error al invalidar caché tras crear pedido");
        }

        try
        {
            var cacheTTL = TimeSpan.FromMinutes(5);
            await cacheService.SetAsync($"pedidos:{resultDto.Id}", resultDto, cacheTTL);
            logger.LogDebug("Pedido cacheado: {Id}", resultDto.Id);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Error al cachear nuevo pedido");
        }

        try
        {
            var adminEmail = configuration["Smtp:AdminEmail"];
            if (!string.IsNullOrEmpty(adminEmail))
            {
                var itemsHtml = string.Join("", pedidoItems.Select(i =>
                    $"<li>{i.NombreProducto} - Cantidad: {i.Cantidad} - Precio: ${i.Precio:F2} - Subtotal: ${i.Subtotal:F2}</li>"));

                var emailMessage = new EmailMessage
                {
                    To = adminEmail,
                    Subject = $"Nuevo Pedido #{resultDto.Id}",
                    Body = $@"
                        <h2>Nuevo Pedido Recibido</h2>
                        <p><strong>ID Pedido:</strong> {resultDto.Id}</p>
                        <p><strong>Usuario ID:</strong> {userId}</p>
                        <p><strong>Estado:</strong> {resultDto.Estado}</p>
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
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Error al encolar email de notificación tras crear pedido");
        }

        if (!string.IsNullOrEmpty(resultDto.Id))
        {
            try
            {
                await webSocketHandler.NotifyPedidoCreatedAsync(resultDto.Id, userId, resultDto);
                logger.LogDebug("Notificación WebSocket enviada para pedido: {PedidoId}", resultDto.Id);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Error en notificación WebSocket para pedido: {PedidoId}", resultDto.Id);
            }
        }
    }

    /// <summary>
    /// Valida el pedido usando FluentValidation.
    /// Devuelve: UnitResult.Success | UnitResult.Failure(Validation)
    /// </summary>
    private async Task<UnitResult<DomainError>> ValidatePedidoAsync(PedidoRequestDto dto)
    {
        var validationResult = await pedidoValidator.ValidateAsync(dto);

        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.ErrorMessage).ToArray()
                );

            return UnitResult.Failure<DomainError>(
                Errors.Pedidos.PedidoError.ValidacionConCampos(errors)
            );
        }

        return UnitResult.Success<DomainError>();
    }

    /// <summary>
    /// Valida un item de pedido usando FluentValidation.
    /// Devuelve: UnitResult.Success | UnitResult.Failure(Validation)
    /// </summary>
    private async Task<UnitResult<DomainError>> ValidatePedidoItemAsync(PedidoItemRequestDto dto)
    {
        var validationResult = await pedidoItemValidator.ValidateAsync(dto);

        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.ErrorMessage).ToArray()
                );

            return UnitResult.Failure<DomainError>(
                Errors.Pedidos.PedidoError.ValidacionConCampos(errors)
            );
        }

        return UnitResult.Success<DomainError>();
    }

    /// <summary>
    /// Actualiza un pedido (el usuario puede actualizar sus propios pedidos).
    /// Devuelve: Result.Success(PedidoDto) | Result.Failure(NotFound/Validation/Forbidden)
    /// </summary>
    public async Task<Result<PedidoDto, DomainError>> UpdateAsync(string id, long userId, UpdatePedidoDto dto)
    {
        logger.LogInformation("Actualizando pedido con ID: {Id} por usuario: {UserId}", id, userId);

        var pedido = await pedidosRepository.FindByIdAsync(id);

        if (pedido is null)
        {
            logger.LogWarning("Pedido no encontrado: {Id}", id);
            return Result.Failure<PedidoDto, DomainError>(
                Errors.Pedidos.PedidoError.NotFound(id)
            );
        }

        if (pedido.UserId != userId)
        {
            logger.LogWarning("Usuario {UserId} intentó actualizar pedido {Id} que no le pertenece", userId, id);
            return Result.Failure<PedidoDto, DomainError>(
                Errors.Pedidos.PedidoError.NoPropietario(userId, id)
            );
        }

        if (dto.Estado != null && !string.IsNullOrWhiteSpace(dto.Estado))
            pedido.Estado = dto.Estado;

        if (dto.DireccionEnvio != null && !string.IsNullOrWhiteSpace(dto.DireccionEnvio))
            pedido.DireccionEnvio = dto.DireccionEnvio;

        pedido.UpdatedAt = DateTime.UtcNow;

        var updated = await pedidosRepository.UpdateAsync(pedido);

        logger.LogInformation("Pedido {Id} actualizado por usuario {UserId}", id, userId);

        var resultDto = updated.ToDto();

        _ = Task.Run(async () =>
        {
            try
            {
                await cacheService.RemoveAsync($"pedidos:{id}");
                await cacheService.RemoveAsync($"pedidos:user:{userId}");
                await cacheService.RemoveAsync("pedidos:all");
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Error al invalidar caché tras actualizar pedido");
            }
        });

        _ = Task.Run(async () => await webSocketHandler.NotifyPedidoEstadoUpdatedAsync(id, userId, pedido.Estado ?? "", resultDto));

        return Result.Success<PedidoDto, DomainError>(resultDto);
    }

    /// <summary>
    /// Elimina un pedido (el usuario puede eliminar sus propios pedidos).
    /// Devuelve: UnitResult.Success | UnitResult.Failure(NotFound/Forbidden)
    /// </summary>
    public async Task<UnitResult<DomainError>> DeleteAsync(string id, long userId)
    {
        logger.LogInformation("Eliminando pedido con ID: {Id} por usuario: {UserId}", id, userId);

        var pedido = await pedidosRepository.FindByIdAsync(id);

        if (pedido is null)
        {
            logger.LogWarning("Pedido con ID {Id} no encontrado para eliminar", id);
            return UnitResult.Failure<DomainError>(
                Errors.Pedidos.PedidoError.NotFound(id)
            );
        }

        if (pedido.UserId != userId)
        {
            logger.LogWarning("Usuario {UserId} intentó eliminar pedido {Id} que no le pertenece", userId, id);
            return UnitResult.Failure<DomainError>(
                Errors.Pedidos.PedidoError.NoPropietario(userId, id)
            );
        }

        pedido.IsDeleted = true;
        pedido.UpdatedAt = DateTime.UtcNow;

        await pedidosRepository.UpdateAsync(pedido);

        logger.LogInformation("Pedido {Id} eliminado lógicamente por usuario {UserId}", id, userId);

        _ = Task.Run(async () =>
        {
            try
            {
                await cacheService.RemoveAsync($"pedidos:{id}");
                await cacheService.RemoveAsync($"pedidos:user:{userId}");
                await cacheService.RemoveAsync("pedidos:all");
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Error al invalidar caché tras eliminar pedido");
            }
        });

        return UnitResult.Success<DomainError>();
    }
}

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
/// Las operaciones de caché, WebSocket y email se ejecutan en Task.Run (fire & forget)
/// para no bloquear el hilo principal. Esto es especialmente importante si:
/// - La caché está en Redis (latencia de red)
/// - WebSocket tarda en enviar la notificación
/// - El email falla o tarda en encolarse
/// Si cualquiera de estas operaciones falla, se registra un warning pero no afecta a la respuesta.
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
    private readonly TimeSpan _cacheTTL = TimeSpan.FromMinutes(5);

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

        _ = Task.Run(() => AñadirCachePedido(cacheKey, dtos));

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

        _ = Task.Run(() => AñadirCachePedido(cacheKey, dto));

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
    /// 
    /// ¿Qué es Serializable?
    /// Es el nivel de aislamiento más estricto de PostgreSQL. Garantiza que las transacciones
    /// concurrentes se ejecuten como si fueran secuenciales. Si dos transacciones intentan
    /// modificar los mismos datos simultáneamente, PostgreSQL aborta una con error 40001.
    /// 
    /// Ejemplo del problema que solve:
    /// - Transacción A lee producto X (stock: 5)
    /// - Transacción B lee producto X (stock: 5)
    /// - Transacción A decrementa a 4 y guarda
    /// - Transacción B decrementa a 4 y guarda ❌ (debería ser 3, tenemos race condition)
    /// 
    /// Con Serializable, PostgreSQL aborta B para que se reintente con el valor actualizado.
    /// 
    /// Retry Logic:
    /// Si ocurre error 40001, CreateAsync() reintenta hasta 3 veces con delay exponencial.
    /// Esto es especialmente importante en operaciones de inventario donde necesitamos
    /// garantizar que no vendemos más stock del disponible.
    /// 
    /// ¿Por qué no bloqueamos con locks?
    /// Los locks degradan el rendimiento en escenarios de alta concurrencia.
    /// Serializable + Retry es más escalable y mantiene la integridad sin bloquear.
    /// </summary>
    private async Task<Result<PedidoDto, DomainError>> CreateWithSerializableTransactionAsync(
        long userId,
        PedidoRequestDto dto)
    {
        // Iniciar transacción con nivel Serializable
        // Esto fuerza a PostgreSQL a abortar conflictos en lugar de permitir data inconsistente
        await using var transaction = await productoRepository.BeginTransactionAsync(
            System.Data.IsolationLevel.Serializable);

        try
        {
            var pedidoItems = new List<PedidoItem>();
            decimal total = 0;

            // Por cada item del pedido, validamos y decrementamos stock
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

                // Decrementar stock dentro de la transacción
                // Si otra transacción intenta leer este producto, obtendrá el valor actualizado
                // o recibirá un error de serialización si hay conflicto
                producto.Stock -= itemDto.Cantidad;

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
                Estado = PedidoEstado.PENDIENTE
            };

            var savedPedido = await pedidosRepository.SaveAsync(pedido);

            // Commit confirma todos los cambios atomicamente
            await transaction.CommitAsync();

            logger.LogInformation("Pedido creado: {Id} para usuario: {UserId}, total: {Total}",
                savedPedido.Id, userId, total);

            var resultDto = savedPedido.ToDto();

            // Notificaciones asíncronas (no bloquean la respuesta)
            _ = Task.Run(() => NotificarPedidoCreado(userId, resultDto, pedidoItems, total));

            return Result.Success<PedidoDto, DomainError>(resultDto);
        }
        catch (DbUpdateException ex) when (IsSerializationFailure(ex))
        {
            // Error de serialización PostgreSQL (40001)
            // Rollback automático y retry desde CreateAsync()
            await transaction.RollbackAsync();
            logger.LogWarning(ex, "Error de serialización PostgreSQL (40001) al crear pedido para usuario {UserId}. Se reintentará automáticamente.", userId);
            return Result.Failure<PedidoDto, DomainError>(Errors.Pedidos.PedidoError.ErrorProcesando());
        }
        catch (NpgsqlException ex) when (IsSerializationFailureMessage(ex.Message))
        {
            // Error de serialización desde Npgsql (variante del mismo error)
            // Rollback automático y retry desde CreateAsync()
            await transaction.RollbackAsync();
            logger.LogWarning(ex, "Error de serialización PostgreSQL (40001) al crear pedido para usuario {UserId}. Se reintentará automáticamente.", userId);
            return Result.Failure<PedidoDto, DomainError>(Errors.Pedidos.PedidoError.ErrorProcesando());
        }
        catch (Exception ex)
        {
            // Error inesperado - rollback y re-lanzar
            await transaction.RollbackAsync();
            logger.LogError(ex, "Error inesperado al crear pedido para usuario {UserId}", userId);
            throw;
        }
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

        _ = Task.Run(() => InvalidarCachePedido($"pedidos:{id}", $"pedidos:user:{pedido.UserId}"));
        _ = Task.Run(() => EnviarEmailPedidoEstadoActualizado(pedido.Id.ToString(), estadoAnterior, nuevoEstado, pedido.Total, pedido.UserId));

        return Result.Success<PedidoDto, DomainError>(resultDto);
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

        var updated = await pedidosRepository.UpdateAsync(pedido);

        logger.LogInformation("Pedido {Id} actualizado por usuario {UserId}", id, userId);

        var resultDto = updated.ToDto();

        _ = Task.Run(() => InvalidarCachePedido($"pedidos:{id}", $"pedidos:user:{userId}", "pedidos:all"));
        _ = Task.Run(() => NotificarWebSocketPedidoActualizado(id, userId, pedido.Estado ?? "", resultDto));

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

        await pedidosRepository.UpdateAsync(pedido);

        logger.LogInformation("Pedido {Id} eliminado lógicamente por usuario {UserId}", id, userId);

        _ = Task.Run(() => InvalidarCachePedido($"pedidos:{id}", $"pedidos:user:{userId}", "pedidos:all"));

        return UnitResult.Success<DomainError>();
    }

    // ========== MÉTODOS PRIVADOS - CACHE ==========

    /// <summary>
    /// Añade un elemento a la caché de forma asíncrona (fire & forget).
    /// </summary>
    private void AñadirCachePedido<T>(string key, T value)
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
    private void InvalidarCachePedido(params string[] keys)
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
    /// Notifica vía WebSocket la creación de un pedido.
    /// </summary>
    private void NotificarWebSocketPedidoCreado(string pedidoId, long userId, string estado)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await webSocketHandler.NotifyAsync(new PedidoNotificacion(
                    PedidoNotificationType.CREATED,
                    pedidoId,
                    userId,
                    estado,
                    null
                ));
                logger.LogDebug("Notificación WebSocket enviada para pedido: {PedidoId}", pedidoId);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Error en notificación WebSocket para pedido: {PedidoId}", pedidoId);
            }
        });
    }

    /// <summary>
    /// Notifica vía WebSocket la actualización de un pedido.
    /// </summary>
    private void NotificarWebSocketPedidoActualizado(string pedidoId, long userId, string estado, PedidoDto pedido)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await webSocketHandler.NotifyAsync(new PedidoNotificacion(
                    PedidoNotificationType.ESTADO_UPDATED,
                    pedidoId,
                    userId,
                    estado,
                    pedido
                ));
                logger.LogDebug("Notificación WebSocket enviada para pedido: {PedidoId}", pedidoId);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Error en notificación WebSocket para pedido: {PedidoId}", pedidoId);
            }
        });
    }

    // ========== MÉTODOS PRIVADOS - EMAIL ==========

    /// <summary>
    /// Envía email de notificación cuando se crea un pedido.
    /// </summary>
    private void EnviarEmailPedidoCreado(string pedidoId, decimal total, int itemCount, long userId)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                var adminEmail = configuration["Smtp:AdminEmail"];
                if (string.IsNullOrEmpty(adminEmail)) return;

                var content = EmailTemplates.PedidoCreado(pedidoId, total, itemCount);
                var body = EmailTemplates.CreateBase("Nuevo Pedido Recibido", content);

                var emailMessage = new EmailMessage
                {
                    To = adminEmail,
                    Subject = $"🛒 Nuevo Pedido #{pedidoId}",
                    Body = body,
                    IsHtml = true
                };
                await emailService.EnqueueEmailAsync(emailMessage);
                logger.LogDebug("Email de notificación encolado tras crear pedido");
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Error al encolar email de notificación tras crear pedido");
            }
        });
    }

    /// <summary>
    /// Envía email de notificación cuando se actualiza el estado de un pedido.
    /// </summary>
    private void EnviarEmailPedidoEstadoActualizado(string pedidoId, string estadoAnterior, string nuevoEstado, decimal total, long userId)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                var adminEmail = configuration["Smtp:AdminEmail"];
                if (string.IsNullOrEmpty(adminEmail)) return;

                var content = EmailTemplates.PedidoEstadoActualizado(pedidoId, estadoAnterior, nuevoEstado, total);
                var body = EmailTemplates.CreateBase("Cambio de Estado de Pedido", content);

                var emailMessage = new EmailMessage
                {
                    To = adminEmail,
                    Subject = $"📦 Pedido #{pedidoId} - {nuevoEstado}",
                    Body = body,
                    IsHtml = true
                };
                await emailService.EnqueueEmailAsync(emailMessage);
                logger.LogDebug("Email de notificación encolado tras cambio de estado del pedido");
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Error al encolar email de notificación tras cambio de estado");
            }
        });
    }

    // ========== NOTIFICACIONES COMPUESTAS ==========

    /// <summary>
    /// Notifica la creación del pedido (cache + email + WebSocket).
    /// </summary>
    private void NotificarPedidoCreado(long userId, PedidoDto pedido, List<PedidoItem> pedidoItems, decimal total)
    {
        _ = Task.Run(async () =>
        {
            // Cache
            try
            {
                await cacheService.RemoveAsync($"pedidos:user:{userId}");
            }
            catch (Exception ex) { logger.LogWarning(ex, "Cache invalidation error: Key=pedidos:user:{UserId}", userId); }

            // Añadir a caché
            try
            {
                await cacheService.SetAsync($"pedidos:{pedido.Id}", pedido, _cacheTTL);
            }
            catch (Exception ex) { logger.LogWarning(ex, "Error caching new pedido: {PedidoId}", pedido.Id); }

            // Email
            EnviarEmailPedidoCreado(pedido.Id, total, pedidoItems.Count, userId);

            // WebSocket
            if (!string.IsNullOrEmpty(pedido.Id))
            {
                try
                {
                    await webSocketHandler.NotifyAsync(new PedidoNotificacion(
                        PedidoNotificationType.CREATED,
                        pedido.Id,
                        userId,
                        pedido.Estado ?? "",
                        pedido
                    ));
                    logger.LogDebug("Notificación WebSocket enviada para pedido: {PedidoId}", pedido.Id);
                }
                catch (Exception ex) { logger.LogWarning(ex, "Error WebSocket notification for pedido: {PedidoId}", pedido.Id); }
            }
        });
    }

    // ========== UTILIDADES ==========

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

    // ========== VALIDACIÓN ==========

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
}

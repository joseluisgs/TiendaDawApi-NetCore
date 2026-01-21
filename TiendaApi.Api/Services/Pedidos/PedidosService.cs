using CSharpFunctionalExtensions;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using TiendaApi.Api.Dtos.Common;
using TiendaApi.Api.Dtos.Pedidos;
using TiendaApi.Api.Errors;
using TiendaApi.Api.Errors.Pedidos;
using TiendaApi.Api.Exceptions;
using TiendaApi.Api.Mappers;
using TiendaApi.Api.Models;
using TiendaApi.Api.Realtime.Common;
using TiendaApi.Api.Realtime.Pedidos;
using TiendaApi.Api.Repositories.Pedidos;
using TiendaApi.Api.Repositories.Productos;
using TiendaApi.Api.Services.Cache;
using TiendaApi.Api.Services.Email;
using TiendaApi.Api.Validators.Pedidos;

namespace TiendaApi.Api.Services.Pedidos;

/// <summary>
    /// Servicio de pedidos que implementa el patrón Service Layer.
    /// </summary>
    public class PedidosService(
    IPedidosRepository pedidosRepository,
    IProductoRepository productoRepository,
    ILogger<PedidosService> logger,
    ICacheService cacheService,
    IEmailService emailService,
    IConfiguration configuration,
    PedidosWebSocketHandler webSocketHandler,
    IHubContext<PedidosHub> pedidosHubContext,
    IValidator<PedidoRequestDto> pedidoValidator,
    IValidator<PedidoItemRequestDto> pedidoItemValidator
) : IPedidosService
{
    private const int MaxRetries = 3;
    private readonly TimeSpan _cacheTTL = TimeSpan.FromMinutes(5);

    #region ========== MÉTODOS PARA ADMINISTRADORES ==========

    /// <summary>
    /// Obtiene todos los pedidos del sistema (solo administradores).
    /// </summary>
    public async Task<Result<IEnumerable<PedidoDto>, DomainError>> FindAllAsync()
    {
        logger.LogInformation("Obteniendo todos los pedidos");

        var pedidos = await pedidosRepository.FindAllAsync();
        var dtos = pedidos.ToDtoList();

        return Result.Success<IEnumerable<PedidoDto>, DomainError>(dtos);
    }

    /// <summary>
    /// Obtiene los pedidos del sistema de forma paginada (solo administradores).
    /// </summary>
    public async Task<Result<PagedResult<PedidoDto>, DomainError>> FindAllPagedAsync(int page, int size)
    {
        logger.LogInformation("Obteniendo pedidos paginados. Página: {Page}, Tamaño: {Size}", page, size);

        var pedidos = await pedidosRepository.FindAllAsync();
        var pedidosList = pedidos.ToList();

        var totalCount = pedidosList.Count;
        var pagedPedidos = pedidosList.Skip(page * size).Take(size);

        var pagedResult = new PagedResult<PedidoDto>
        {
            Items = pagedPedidos.ToDtoList(),
            TotalCount = totalCount,
            Page = page + 1,
            PageSize = size
        };

        return Result.Success<PagedResult<PedidoDto>, DomainError>(pagedResult);
    }

    /// <summary>
    /// Busca un pedido por su ID (solo administradores).
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
                PedidoError.NotFound(id)
            );
        }

        var dto = pedido.ToDto();

        return Result.Success<PedidoDto, DomainError>(dto)
            .Tap(_ => AñadirCachePedido(cacheKey, dto));
    }

    /// <summary>
    /// Actualiza un pedido (solo administradores).
    /// Los administradores pueden actualizar cualquier pedido sin restricciones de propiedad.
    /// Envía WebSocket al cliente y Email al admin.
    /// </summary>
    public async Task<Result<PedidoDto, DomainError>> UpdateAdminAsync(string id, UpdatePedidoDto dto)
    {
        logger.LogInformation("Administrador actualizando pedido: {Id}", id);

        var pedido = await pedidosRepository.FindByIdAsync(id);

        if (pedido is null)
        {
            logger.LogWarning("Pedido no encontrado: {Id}", id);
            return Result.Failure<PedidoDto, DomainError>(
                PedidoError.NotFound(id)
            );
        }

        if (dto.Estado != null && !string.IsNullOrWhiteSpace(dto.Estado))
            pedido.Estado = dto.Estado;

        if (dto.DireccionEnvio != null && !string.IsNullOrWhiteSpace(dto.DireccionEnvio))
            pedido.DireccionEnvio = dto.DireccionEnvio;

        var updated = await pedidosRepository.UpdateAsync(pedido);
        var resultDto = updated.ToDto();

        return Result.Success<PedidoDto, DomainError>(resultDto)
            .Tap(_ =>
            {
                logger.LogInformation("Pedido {Id} actualizado por administrador", id);
                InvalidarCachePedido($"pedidos:{id}", $"pedidos:user:{pedido.UserId}", "pedidos:all");
                NotificarWebSocketPedidoActualizado(id, pedido.UserId, pedido.Estado ?? "", resultDto);
                NotificarSignalRPedidoActualizado(id, pedido.UserId, pedido.Estado ?? "", resultDto);
                EnviarEmailPedidoActualizadoAdmin(pedido.Id.ToString(), pedido.Estado ?? "", pedido.Total, pedido.UserId);
            });
    }

    /// <summary>
    /// Elimina un pedido (solo administradores).
    /// Envía Email al admin.
    /// </summary>
    public async Task<UnitResult<DomainError>> DeleteAdminAsync(string id)
    {
        logger.LogInformation("Administrador eliminando pedido: {Id}", id);

        var pedido = await pedidosRepository.FindByIdAsync(id);

        if (pedido is null)
        {
            logger.LogWarning("Pedido no encontrado: {Id}", id);
            return UnitResult.Failure<DomainError>(
                PedidoError.NotFound(id)
            );
        }

        pedido.IsDeleted = true;
        await pedidosRepository.UpdateAsync(pedido);

        logger.LogInformation("Pedido {Id} eliminado lógicamente por administrador", id);

        InvalidarCachePedido($"pedidos:{id}", $"pedidos:user:{pedido.UserId}", "pedidos:all");

        NotificarSignalRPedidoEliminado(id, pedido.UserId, pedido.Estado ?? "");
        EnviarEmailPedidoEliminadoAdmin(pedido.Id.ToString(), pedido.Total, pedido.UserId);

        return UnitResult.Success<DomainError>();
    }

    /// <summary>
    /// Actualiza el estado de un pedido (solo administradores).
    /// </summary>
    public async Task<Result<PedidoDto, DomainError>> UpdateEstadoAsync(string id, string nuevoEstado)
    {
        logger.LogInformation("Actualizando estado del pedido: {Id} a {Estado}", id, nuevoEstado);

        var validEstados = new[] { PedidoEstado.PENDIENTE, PedidoEstado.PROCESANDO, PedidoEstado.ENVIADO, PedidoEstado.ENTREGADO, PedidoEstado.CANCELADO };
        if (!validEstados.Contains(nuevoEstado))
        {
            return Result.Failure<PedidoDto, DomainError>(
                PedidoError.EstadoInvalido(nuevoEstado, validEstados)
            );
        }

        var pedido = await pedidosRepository.FindByIdAsync(id);

        if (pedido == null)
        {
            logger.LogWarning("Pedido no encontrado: {Id}", id);
            return Result.Failure<PedidoDto, DomainError>(
                PedidoError.NotFound(id)
            );
        }

        var estadoAnterior = pedido.Estado;
        pedido.Estado = nuevoEstado;

        var updated = await pedidosRepository.UpdateAsync(pedido);
        var resultDto = updated.ToDto();

        return Result.Success<PedidoDto, DomainError>(resultDto)
            .Tap(_ =>
            {
                logger.LogInformation("Estado del pedido actualizado: {Id}, de {OldEstado} a {NewEstado}", id, estadoAnterior, nuevoEstado);
                InvalidarCachePedido($"pedidos:{id}", $"pedidos:user:{pedido.UserId}");
                NotificarWebSocketPedidoActualizado(id, pedido.UserId, nuevoEstado, resultDto);
                NotificarSignalRPedidoActualizado(id, pedido.UserId, nuevoEstado, resultDto);
                EnviarEmailPedidoEstadoActualizado(pedido.Id.ToString(), estadoAnterior, nuevoEstado, pedido.Total, pedido.UserId);
            });
    }

    #endregion

    #region ========== MÉTODOS PARA USUARIOS (MIS PEDIDOS) ==========

    /// <summary>
    /// Obtiene todos los pedidos del usuario autenticado (sin paginación).
    /// </summary>
    public async Task<Result<IEnumerable<PedidoDto>, DomainError>> FindByUserIdAsync(long userId)
    {
        logger.LogInformation("Obteniendo todos los pedidos del usuario: {UserId}", userId);

        var pedidos = await pedidosRepository.FindByUserIdAsync(userId);
        var dtos = pedidos.ToDtoList();

        return Result.Success<IEnumerable<PedidoDto>, DomainError>(dtos);
    }

    /// <summary>
    /// Obtiene los pedidos del usuario autenticado de forma paginada.
    /// </summary>
    public async Task<Result<PagedResult<PedidoDto>, DomainError>> FindMyPedidosAsync(long userId, int page, int size)
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
    /// Busca un pedido propio por su ID.
    /// Valida que el pedido pertenezca al usuario solicitante.
    /// </summary>
    public async Task<Result<PedidoDto, DomainError>> FindMyPedidoAsync(string id, long userId)
    {
        logger.LogInformation("Usuario {UserId} solicitando pedido: {Id}", userId, id);

        var pedido = await pedidosRepository.FindByIdAsync(id);

        if (pedido == null)
        {
            logger.LogWarning("Pedido no encontrado: {Id}", id);
            return Result.Failure<PedidoDto, DomainError>(
                PedidoError.NotFound(id)
            );
        }

        if (pedido.UserId != userId)
        {
            logger.LogWarning("Usuario {UserId} intentó acceder a pedido {Id} que no le pertenece", userId, id);
            return Result.Failure<PedidoDto, DomainError>(
                PedidoError.NoPropietario(userId, id)
            );
        }

        var dto = pedido.ToDto();
        return Result.Success<PedidoDto, DomainError>(dto);
    }

    /// <summary>
    /// Crea un nuevo pedido para el usuario autenticado.
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
                        PedidoError.PedidoAdquirido(string.Empty)
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
                        PedidoError.PedidoAdquirido(string.Empty)
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
            PedidoError.ErrorProcesando()
        );
    }

    /// <summary>
    /// Actualiza un pedido propio.
    /// Solo permite modificar pedidos en estado PENDIENTE.
    /// Envía WebSocket al cliente.
    /// </summary>
    public async Task<Result<PedidoDto, DomainError>> UpdateMyPedidoAsync(string id, long userId, UpdatePedidoDto dto)
    {
        logger.LogInformation("Usuario {UserId} actualizando pedido: {Id}", userId, id);

        var pedido = await pedidosRepository.FindByIdAsync(id);

        if (pedido is null)
        {
            logger.LogWarning("Pedido no encontrado: {Id}", id);
            return Result.Failure<PedidoDto, DomainError>(
                PedidoError.NotFound(id)
            );
        }

        if (pedido.UserId != userId)
        {
            logger.LogWarning("Usuario {UserId} intentó actualizar pedido {Id} que no le pertenece", userId, id);
            return Result.Failure<PedidoDto, DomainError>(
                PedidoError.NoPropietario(userId, id)
            );
        }

        if (pedido.Estado != PedidoEstado.PENDIENTE)
        {
            logger.LogWarning("Usuario {UserId} intentó actualizar pedido {Id} en estado {Estado}", userId, id, pedido.Estado);
            return Result.Failure<PedidoDto, DomainError>(
                PedidoError.Validacion($"No se puede actualizar un pedido en estado {pedido.Estado}. Solo se permiten pedidos en estado PENDIENTE.")
            );
        }

        if (dto.DireccionEnvio != null && !string.IsNullOrWhiteSpace(dto.DireccionEnvio))
            pedido.DireccionEnvio = dto.DireccionEnvio;

        var updated = await pedidosRepository.UpdateAsync(pedido);
        var resultDto = updated.ToDto();

        return Result.Success<PedidoDto, DomainError>(resultDto)
            .Tap(_ =>
            {
                logger.LogInformation("Pedido {Id} actualizado por usuario {UserId}", id, userId);
                InvalidarCachePedido($"pedidos:{id}", $"pedidos:user:{userId}");
                NotificarWebSocketPedidoActualizado(id, userId, pedido.Estado ?? "", resultDto);
            });
    }

    /// <summary>
    /// Cancela y elimina un pedido propio.
    /// Solo permite eliminar pedidos en estado PENDIENTE.
    /// Envía Email al admin.
    /// </summary>
    public async Task<UnitResult<DomainError>> DeleteMyPedidoAsync(string id, long userId)
    {
        logger.LogInformation("Usuario {UserId} eliminando pedido: {Id}", userId, id);

        var pedido = await pedidosRepository.FindByIdAsync(id);

        if (pedido is null)
        {
            logger.LogWarning("Pedido no encontrado: {Id}", id);
            return UnitResult.Failure<DomainError>(
                PedidoError.NotFound(id)
            );
        }

        if (pedido.UserId != userId)
        {
            logger.LogWarning("Usuario {UserId} intentó eliminar pedido {Id} que no le pertenece", userId, id);
            return UnitResult.Failure<DomainError>(
                PedidoError.NoPropietario(userId, id)
            );
        }

        if (pedido.Estado != PedidoEstado.PENDIENTE)
        {
            logger.LogWarning("Usuario {UserId} intentó eliminar pedido {Id} en estado {Estado}", userId, id, pedido.Estado);
            return UnitResult.Failure<DomainError>(
                PedidoError.Validacion($"No se puede eliminar un pedido en estado {pedido.Estado}. Solo se permiten pedidos en estado PENDIENTE.")
            );
        }

        pedido.IsDeleted = true;
        await pedidosRepository.UpdateAsync(pedido);
        logger.LogInformation("Pedido {Id} eliminado lógicamente por usuario {UserId}", id, userId);

        InvalidarCachePedido($"pedidos:{id}", $"pedidos:user:{userId}");

        EnviarEmailPedidoEliminadoAdmin(pedido.Id.ToString(), pedido.Total, pedido.UserId);

        return UnitResult.Success<DomainError>();
    }

    #endregion

    #region ========== MÉTODOS PRIVADOS - TRANSACCIÓN ==========

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
                if (producto == null)
                {
                    await transaction.RollbackAsync();
                    return Result.Failure<PedidoDto, DomainError>(
                        PedidoError.ProductoNoEncontrado(itemDto.ProductoId)
                    );
                }

                if (producto.Stock < itemDto.Cantidad)
                {
                    await transaction.RollbackAsync();
                    return Result.Failure<PedidoDto, DomainError>(
                        PedidoError.StockInsuficiente(producto.Nombre, producto.Stock, itemDto.Cantidad)
                    );
                }

                producto.Stock -= itemDto.Cantidad;
                await productoRepository.UpdateAsync(producto);

                var item = new PedidoItem
                {
                    ProductoId = itemDto.ProductoId,
                    NombreProducto = producto.Nombre,
                    Cantidad = itemDto.Cantidad,
                    Precio = producto.Precio,
                    Subtotal = producto.Precio * itemDto.Cantidad
                };
                pedidoItems.Add(item);
                total += item.Subtotal;
            }

            var pedido = new Pedido
            {
                UserId = userId,
                Destinatario = dto.Destinatario?.ToEntity(),
                Items = pedidoItems,
                Total = total,
                Estado = PedidoEstado.PENDIENTE,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var pedidoGuardado = await pedidosRepository.SaveAsync(pedido);

            await transaction.CommitAsync();

            logger.LogInformation(
                "Pedido creado exitosamente para usuario {UserId}. Total: {Total}, Items: {ItemCount}",
                userId, total, pedidoItems.Count);

            var dtoResult = pedidoGuardado.ToDto();

            return Result.Success<PedidoDto, DomainError>(dtoResult)
                .Tap(_ =>
                {
                    NotificarWebSocketPedidoCreado(pedidoGuardado.Id.ToString(), userId, PedidoEstado.PENDIENTE);
                    NotificarSignalRPedidoCreado(pedidoGuardado.Id.ToString(), userId, PedidoEstado.PENDIENTE, dtoResult);
                    EnviarEmailPedidoCreado(pedidoGuardado.Id.ToString(), total, pedidoItems.Count, userId);
                    InvalidarCachePedido($"pedidos:user:{userId}");
                });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            logger.LogError(ex, "Error al crear pedido para usuario {UserId}", userId);
            throw;
        }
    }

    #endregion

    #region ========== MÉTODOS PRIVADOS - CACHE ==========

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

    #endregion

    #region ========== MÉTODOS PRIVADOS - WEBSOCKET ==========

    /// <summary>
    /// Notifica vía WebSocket la creación de un pedido.
    /// Envía notificación al usuario que creó el pedido y a todos los administradores.
    /// </summary>
    /// <param name="pedidoId">Identificador del pedido creado.</param>
    /// <param name="userId">ID del usuario que creó el pedido.</param>
    /// <param name="estado">Estado inicial del pedido.</param>
    private void NotificarWebSocketPedidoCreado(string pedidoId, long userId, string estado)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await webSocketHandler.NotifyUserAndAdminsAsync(userId, new PedidoNotificacion(
                    PedidoNotificationType.CREADO,
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
    /// Notifica vía WebSocket la actualización del estado de un pedido.
    /// Envía notificación al usuario afectado y a todos los administradores.
    /// </summary>
    /// <param name="pedidoId">Identificador del pedido.</param>
    /// <param name="userId">ID del usuario que realizó el pedido.</param>
    /// <param name="estado">Nuevo estado del pedido.</param>
    /// <param name="pedido">Datos actualizados del pedido.</param>
    private void NotificarWebSocketPedidoActualizado(string pedidoId, long userId, string estado, PedidoDto pedido)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await webSocketHandler.NotifyUserAndAdminsAsync(userId, new PedidoNotificacion(
                    PedidoNotificationType.ESTADO_ACTUALIZADO,
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

    #endregion

    #region ========== MÉTODOS PRIVADOS - SIGNALR ==========

    private void NotificarSignalRPedidoCreado(string pedidoId, long userId, string estado, PedidoDto pedido)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                var payload = new
                {
                    pedidoId,
                    userId,
                    estado,
                    tipo = "PEDIDO_CREADO",
                    total = pedido.Total,
                    itemsCount = pedido.Items?.Count ?? 0,
                    timestamp = DateTime.UtcNow
                };
                await pedidosHubContext.Clients.All.SendAsync("PedidoCreado", payload);
                logger.LogDebug("Notificación SignalR enviada para pedido creado: {PedidoId}", pedidoId);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Error en notificación SignalR para pedido: {PedidoId}", pedidoId);
            }
        });
    }

    private void NotificarSignalRPedidoActualizado(string pedidoId, long userId, string estado, PedidoDto pedido)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                var payload = new
                {
                    pedidoId,
                    userId,
                    estado,
                    tipo = "PEDIDO_ACTUALIZADO",
                    total = pedido.Total,
                    timestamp = DateTime.UtcNow
                };
                await pedidosHubContext.Clients.All.SendAsync("PedidoActualizado", payload);
                logger.LogDebug("Notificación SignalR enviada para pedido actualizado: {PedidoId}", pedidoId);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Error en notificación SignalR para pedido: {PedidoId}", pedidoId);
            }
        });
    }

    private void NotificarSignalRPedidoEliminado(string pedidoId, long userId, string estado)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                var payload = new
                {
                    pedidoId,
                    userId,
                    estado,
                    tipo = "PEDIDO_ELIMINADO",
                    timestamp = DateTime.UtcNow
                };
                await pedidosHubContext.Clients.All.SendAsync("PedidoEliminado", payload);
                logger.LogDebug("Notificación SignalR enviada para pedido eliminado: {PedidoId}", pedidoId);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Error en notificación SignalR para pedido: {PedidoId}", pedidoId);
            }
        });
    }

    #endregion

    #region ========== MÉTODOS PRIVADOS - EMAIL ==========

    private void EnviarEmailPedidoCreado(string pedidoId, decimal total, int itemCount, long userId)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                var adminEmail = configuration["Smtp:AdminEmail"];
                if (string.IsNullOrEmpty(adminEmail)) return;

                var content = EmailTemplates.PedidoCreado(pedidoId, total, itemCount, userId);
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

    private void EnviarEmailPedidoEstadoActualizado(string pedidoId, string estadoAnterior, string nuevoEstado, decimal total, long userId)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                var adminEmail = configuration["Smtp:AdminEmail"];
                if (string.IsNullOrEmpty(adminEmail)) return;

                var content = EmailTemplates.PedidoEstadoActualizado(pedidoId, estadoAnterior, nuevoEstado, total, userId);
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

    private void EnviarEmailPedidoActualizadoAdmin(string pedidoId, string estado, decimal total, long userId)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                var adminEmail = configuration["Smtp:AdminEmail"];
                if (string.IsNullOrEmpty(adminEmail)) return;

                var content = EmailTemplates.PedidoActualizadoAdmin(pedidoId, estado, total, userId);
                var body = EmailTemplates.CreateBase("Pedido Actualizado por Administrador", content);

                var emailMessage = new EmailMessage
                {
                    To = adminEmail,
                    Subject = $"✏️ Pedido #{pedidoId} Actualizado",
                    Body = body,
                    IsHtml = true
                };
                await emailService.EnqueueEmailAsync(emailMessage);
                logger.LogDebug("Email de notificación encolado tras actualización de pedido por admin");
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Error al encolar email de notificación tras actualización por admin");
            }
        });
    }

    private void EnviarEmailPedidoEliminadoAdmin(string pedidoId, decimal total, long userId)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                var adminEmail = configuration["Smtp:AdminEmail"];
                if (string.IsNullOrEmpty(adminEmail)) return;

                var content = EmailTemplates.PedidoEliminadoAdmin(pedidoId, total, userId);
                var body = EmailTemplates.CreateBase("Pedido Eliminado", content);

                var emailMessage = new EmailMessage
                {
                    To = adminEmail,
                    Subject = $"🗑️ Pedido #{pedidoId} Eliminado",
                    Body = body,
                    IsHtml = true
                };
                await emailService.EnqueueEmailAsync(emailMessage);
                logger.LogDebug("Email de notificación encolado tras eliminación de pedido por admin");
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Error al encolar email de notificación tras eliminación por admin");
            }
        });
    }

    #endregion

    #region ========== UTILIDADES ==========

    private bool IsSerializationFailure(DbUpdateException ex)
    {
        return ex.InnerException is NpgsqlException npgsqlEx &&
               IsSerializationFailureMessage(npgsqlEx.Message);
    }

    private bool IsSerializationFailureMessage(string message)
    {
        return message.Contains("40001") ||
               message.Contains("serialization") ||
               message.Contains("serializacion");
    }

    #endregion

    #region ========== VALIDACIÓN ==========

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
                PedidoError.ValidacionConCampos(errors)
            );
        }

        return UnitResult.Success<DomainError>();
    }

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
                PedidoError.ValidacionConCampos(errors)
            );
        }

        return UnitResult.Success<DomainError>();
    }

    #endregion
}

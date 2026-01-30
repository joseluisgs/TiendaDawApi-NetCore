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
using TiendaApi.Api.Services.Storage;
using TiendaApi.Api.Validators.Pedidos;

namespace TiendaApi.Api.Services.Pedidos;

/// <inheritdoc cref="IPedidosService" />
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

    /// <inheritdoc cref="IPedidosService.FindAllAsync" />
    public async Task<Result<IEnumerable<PedidoDto>, DomainError>> FindAllAsync()
    {
        logger.LogInformation("Obteniendo todos los pedidos");

        var pedidos = await pedidosRepository.FindAllAsync();
        var dtos = pedidos.ToDtoList();

        return Result.Success<IEnumerable<PedidoDto>, DomainError>(dtos);
    }

    /// <inheritdoc cref="IPedidosService.FindAllPagedAsync(int, int)" />
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

    /// <inheritdoc cref="IPedidosService.FindByIdAsync(string)" />
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

    /// <inheritdoc cref="IPedidosService.UpdateAdminAsync(string, UpdatePedidoDto)" />
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

    /// <inheritdoc cref="IPedidosService.DeleteAdminAsync(string)" />
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

    /// <inheritdoc cref="IPedidosService.UpdateEstadoAsync(string, string)" />
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

    /// <inheritdoc cref="IPedidosService.FindByUserIdAsync(long)" />
    public async Task<Result<IEnumerable<PedidoDto>, DomainError>> FindByUserIdAsync(long userId)
    {
        logger.LogInformation("Obteniendo todos los pedidos del usuario: {UserId}", userId);

        var pedidos = await pedidosRepository.FindByUserIdAsync(userId);
        var dtos = pedidos.ToDtoList();

        return Result.Success<IEnumerable<PedidoDto>, DomainError>(dtos);
    }

    /// <inheritdoc cref="IPedidosService.FindMyPedidosAsync(long, int, int)" />
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

    /// <inheritdoc cref="IPedidosService.FindMyPedidoAsync(string, long)" />
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

    /// <inheritdoc cref="IPedidosService.CreateAsync(long, PedidoRequestDto)" />
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
                if (attempt == MaxRetries) return Result.Failure<PedidoDto, DomainError>(PedidoError.PedidoAdquirido(string.Empty));
                await Task.Delay(50 * attempt);
            }
            catch (NpgsqlException ex) when (IsSerializationFailureMessage(ex.Message))
            {
                if (attempt == MaxRetries) return Result.Failure<PedidoDto, DomainError>(PedidoError.PedidoAdquirido(string.Empty));
                await Task.Delay(50 * attempt);
            }
        }

        return Result.Failure<PedidoDto, DomainError>(PedidoError.ErrorProcesando());
    }

    /// <inheritdoc cref="IPedidosService.UpdateMyPedidoAsync(string, long, UpdatePedidoDto)" />
    public async Task<Result<PedidoDto, DomainError>> UpdateMyPedidoAsync(string id, long userId, UpdatePedidoDto dto)
    {
        logger.LogInformation("Usuario {UserId} actualizando pedido: {Id}", userId, id);

        var pedido = await pedidosRepository.FindByIdAsync(id);

        if (pedido is null) return Result.Failure<PedidoDto, DomainError>(PedidoError.NotFound(id));

        if (pedido.UserId != userId) return Result.Failure<PedidoDto, DomainError>(PedidoError.NoPropietario(userId, id));

        if (pedido.Estado != PedidoEstado.PENDIENTE)
            return Result.Failure<PedidoDto, DomainError>(PedidoError.Validacion($"No se puede actualizar un pedido en estado {pedido.Estado}. Solo se permiten pedidos en estado PENDIENTE."));

        if (dto.DireccionEnvio != null && !string.IsNullOrWhiteSpace(dto.DireccionEnvio))
            pedido.DireccionEnvio = dto.DireccionEnvio;

        var updated = await pedidosRepository.UpdateAsync(pedido);
        var resultDto = updated.ToDto();

        return Result.Success<PedidoDto, DomainError>(resultDto)
            .Tap(_ =>
            {
                InvalidarCachePedido($"pedidos:{id}", $"pedidos:user:{userId}");
                NotificarWebSocketPedidoActualizado(id, userId, pedido.Estado ?? "", resultDto);
            });
    }

    /// <inheritdoc cref="IPedidosService.DeleteMyPedidoAsync(string, long)" />
    public async Task<UnitResult<DomainError>> DeleteMyPedidoAsync(string id, long userId)
    {
        logger.LogInformation("Usuario {UserId} eliminando pedido: {Id}", userId, id);

        var pedido = await pedidosRepository.FindByIdAsync(id);

        if (pedido is null) return UnitResult.Failure<DomainError>(PedidoError.NotFound(id));

        if (pedido.UserId != userId) return UnitResult.Failure<DomainError>(PedidoError.NoPropietario(userId, id));

        if (pedido.Estado != PedidoEstado.PENDIENTE)
            return UnitResult.Failure<DomainError>(PedidoError.Validacion($"No se puede eliminar un pedido en estado {pedido.Estado}. Solo se permiten pedidos en estado PENDIENTE."));

        pedido.IsDeleted = true;
        await pedidosRepository.UpdateAsync(pedido);
        InvalidarCachePedido($"pedidos:{id}", $"pedidos:user:{userId}");
        EnviarEmailPedidoEliminadoAdmin(pedido.Id.ToString(), pedido.Total, pedido.UserId);

        return UnitResult.Success<DomainError>();
    }

    #endregion

    #region ========== MÉTODOS PRIVADOS ==========

    private async Task<Result<PedidoDto, DomainError>> CreateWithSerializableTransactionAsync(long userId, PedidoRequestDto dto)
    {
        await using var transaction = await productoRepository.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        try
        {
            var pedidoItems = new List<PedidoItem>();
            decimal total = 0;

            foreach (var itemDto in dto.Items)
            {
                var producto = await productoRepository.FindByIdAsync(itemDto.ProductoId);
                if (producto == null) { await transaction.RollbackAsync(); return Result.Failure<PedidoDto, DomainError>(PedidoError.ProductoNoEncontrado(itemDto.ProductoId)); }
                if (producto.Stock < itemDto.Cantidad) { await transaction.RollbackAsync(); return Result.Failure<PedidoDto, DomainError>(PedidoError.StockInsuficiente(producto.Nombre, producto.Stock, itemDto.Cantidad)); }

                producto.Stock -= itemDto.Cantidad;
                await productoRepository.UpdateAsync(producto);

                var item = new PedidoItem { ProductoId = itemDto.ProductoId, NombreProducto = producto.Nombre, Cantidad = itemDto.Cantidad, Precio = producto.Precio, Subtotal = producto.Precio * itemDto.Cantidad };
                pedidoItems.Add(item);
                total += item.Subtotal;
            }

            var pedido = new Pedido { UserId = userId, Destinatario = dto.Destinatario?.ToEntity(), Items = pedidoItems, Total = total, Estado = PedidoEstado.PENDIENTE, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
            var pedidoGuardado = await pedidosRepository.SaveAsync(pedido);
            await transaction.CommitAsync();

            var dtoResult = pedidoGuardado.ToDto();
            NotificarWebSocketPedidoCreado(pedidoGuardado.Id.ToString(), userId, PedidoEstado.PENDIENTE);
            NotificarSignalRPedidoCreado(pedidoGuardado.Id.ToString(), userId, PedidoEstado.PENDIENTE, dtoResult);
            EnviarEmailPedidoCreado(pedidoGuardado.Id.ToString(), total, pedidoItems.Count, userId);
            InvalidarCachePedido($"pedidos:user:{userId}");

            return Result.Success<PedidoDto, DomainError>(dtoResult);
        }
        catch (Exception ex) { await transaction.RollbackAsync(); logger.LogError(ex, "Error al crear pedido"); throw; }
    }

    private void AñadirCachePedido<T>(string key, T value)
    {
        _ = Task.Run(async () => { try { await cacheService.SetAsync(key, value, _cacheTTL); } catch (Exception ex) { logger.LogWarning(ex, "Cache error"); } });
    }

    private void InvalidarCachePedido(params string[] keys)
    {
        _ = Task.Run(async () => { foreach (var key in keys) { try { await cacheService.RemoveAsync(key); } catch (Exception ex) { logger.LogWarning(ex, "Cache error"); } } });
    }

    private void NotificarWebSocketPedidoCreado(string pedidoId, long userId, string estado)
    {
        _ = Task.Run(async () => { try { await webSocketHandler.NotifyUserAndAdminsAsync(userId, new PedidoNotificacion(PedidoNotificationType.CREADO, pedidoId, userId, estado, null)); } catch (Exception ex) { logger.LogWarning(ex, "WS error"); } });
    }

    private void NotificarWebSocketPedidoActualizado(string pedidoId, long userId, string estado, PedidoDto pedido)
    {
        _ = Task.Run(async () => { try { await webSocketHandler.NotifyUserAndAdminsAsync(userId, new PedidoNotificacion(PedidoNotificationType.ESTADO_ACTUALIZADO, pedidoId, userId, estado, pedido)); } catch (Exception ex) { logger.LogWarning(ex, "WS error"); } });
    }

    private void NotificarSignalRPedidoCreado(string pedidoId, long userId, string estado, PedidoDto pedido)
    {
        _ = Task.Run(async () => { try { await pedidosHubContext.Clients.All.SendAsync("PedidoCreado", new { pedidoId, userId, estado, total = pedido.Total }); } catch (Exception ex) { logger.LogWarning(ex, "SignalR error"); } });
    }

    private void NotificarSignalRPedidoActualizado(string pedidoId, long userId, string estado, PedidoDto pedido)
    {
        _ = Task.Run(async () => { try { await pedidosHubContext.Clients.All.SendAsync("PedidoActualizado", new { pedidoId, userId, estado, total = pedido.Total }); } catch (Exception ex) { logger.LogWarning(ex, "SignalR error"); } });
    }

    private void NotificarSignalRPedidoEliminado(string pedidoId, long userId, string estado)
    {
        _ = Task.Run(async () => { try { await pedidosHubContext.Clients.All.SendAsync("PedidoEliminado", new { pedidoId, userId, estado }); } catch (Exception ex) { logger.LogWarning(ex, "SignalR error"); } });
    }

    private void EnviarEmailPedidoCreado(string pedidoId, decimal total, int itemCount, long userId)
    {
        _ = Task.Run(async () => { try { var adminEmail = configuration["Smtp:AdminEmail"]; if (string.IsNullOrEmpty(adminEmail)) return; var body = EmailTemplates.PedidoCreado(pedidoId, total, itemCount, userId); await emailService.EnqueueEmailAsync(new EmailMessage { To = adminEmail, Subject = "Nuevo Pedido", Body = body, IsHtml = true }); } catch (Exception ex) { logger.LogWarning(ex, "Email error"); } });
    }

    private void EnviarEmailPedidoEstadoActualizado(string pedidoId, string estadoAnterior, string nuevoEstado, decimal total, long userId)
    {
        _ = Task.Run(async () => { try { var adminEmail = configuration["Smtp:AdminEmail"]; if (string.IsNullOrEmpty(adminEmail)) return; var body = EmailTemplates.PedidoEstadoActualizado(pedidoId, estadoAnterior, nuevoEstado, total, userId); await emailService.EnqueueEmailAsync(new EmailMessage { To = adminEmail, Subject = "Estado Pedido", Body = body, IsHtml = true }); } catch (Exception ex) { logger.LogWarning(ex, "Email error"); } });
    }

    private void EnviarEmailPedidoActualizadoAdmin(string pedidoId, string estado, decimal total, long userId)
    {
        _ = Task.Run(async () => { try { var adminEmail = configuration["Smtp:AdminEmail"]; if (string.IsNullOrEmpty(adminEmail)) return; var body = EmailTemplates.PedidoActualizadoAdmin(pedidoId, estado, total, userId); await emailService.EnqueueEmailAsync(new EmailMessage { To = adminEmail, Subject = "Pedido Actualizado", Body = body, IsHtml = true }); } catch (Exception ex) { logger.LogWarning(ex, "Email error"); } });
    }

    private void EnviarEmailPedidoEliminadoAdmin(string pedidoId, decimal total, long userId)
    {
        _ = Task.Run(async () => { try { var adminEmail = configuration["Smtp:AdminEmail"]; if (string.IsNullOrEmpty(adminEmail)) return; var body = EmailTemplates.PedidoEliminadoAdmin(pedidoId, total, userId); await emailService.EnqueueEmailAsync(new EmailMessage { To = adminEmail, Subject = "Pedido Eliminado", Body = body, IsHtml = true }); } catch (Exception ex) { logger.LogWarning(ex, "Email error"); } });
    }

    private bool IsSerializationFailureMessage(string message) => message.Contains("40001") || message.Contains("serialization") || message.Contains("serializacion");

    private async Task<UnitResult<DomainError>> ValidatePedidoAsync(PedidoRequestDto dto)
    {
        var result = await pedidoValidator.ValidateAsync(dto);
        if (!result.IsValid) return UnitResult.Failure<DomainError>(PedidoError.ValidacionConCampos(result.Errors.GroupBy(e => e.PropertyName).ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray())));
        return UnitResult.Success<DomainError>();
    }

    private async Task<UnitResult<DomainError>> ValidatePedidoItemAsync(PedidoItemRequestDto dto)
    {
        var result = await pedidoItemValidator.ValidateAsync(dto);
        if (!result.IsValid) return UnitResult.Failure<DomainError>(PedidoError.ValidacionConCampos(result.Errors.GroupBy(e => e.PropertyName).ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray())));
        return UnitResult.Success<DomainError>();
    }

    #endregion
}
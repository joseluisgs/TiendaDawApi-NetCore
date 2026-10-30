using Microsoft.EntityFrameworkCore;
using MongoDB.Bson;
using TiendaApi.Api.Data;
using TiendaApi.Api.Models;

namespace TiendaApi.Api.Repositories.Pedidos;

/// <summary>
/// Implementación del repositorio de pedidos con MongoDB EF Core.
/// </summary>
public class PedidosEfCoreRepository(
    TiendaMongoContext context,
    ILogger<PedidosEfCoreRepository> logger
) : IPedidosRepository
{
    /// <inheritdoc/>
    public async Task<IEnumerable<Pedido>> FindAllAsync()
    {
        logger.LogDebug("Buscando todos los pedidos");
        // AsNoTracking: estos listados solo se proyectan a DTO y jamas se
        // persisten; sin el, el change tracker retendria todas las filas del
        // listado hasta acabar el request (memoria y CPU de mas).
        return await context.Pedidos
            .AsNoTracking()
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    /// <inheritdoc/>
    public async Task<(IEnumerable<Pedido> Items, int TotalCount)> FindAllPagedAsync(int page, int size)
    {
        logger.LogDebug("Buscando pedidos paginados. Página: {Page}, Tamaño: {Size}", page, size);
        // AsNoTracking: listado de solo lectura (ver FindAllAsync).
        var query = context.Pedidos.AsNoTracking().OrderByDescending(p => p.CreatedAt);

        var totalCount = await query.CountAsync();
        var items = await query.Skip(page * size).Take(size).ToListAsync();
        return (items, totalCount);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Pedido>> FindByUserIdAsync(long userId)
    {
        logger.LogDebug("Buscando pedidos para usuario: {UserId}", userId);
        return await context.Pedidos
            .AsNoTracking()
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    /// <inheritdoc/>
    public async Task<(IEnumerable<Pedido> Items, int TotalCount)> FindByUserIdPagedAsync(long userId, int page, int size)
    {
        logger.LogDebug("Buscando pedidos paginados para usuario: {UserId}", userId);
        // AsNoTracking: listado de solo lectura (ver FindAllAsync).
        var query = context.Pedidos
            .AsNoTracking()
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.CreatedAt);

        var totalCount = await query.CountAsync();
        var items = await query.Skip(page * size).Take(size).ToListAsync();
        return (items, totalCount);
    }

    /// <inheritdoc/>
    public async Task<Pedido?> FindByIdAsync(string id)
    {
        logger.LogDebug("Buscando pedido: {Id}", id);
        return await context.Pedidos
            .FirstOrDefaultAsync(p => p.Id == ObjectId.Parse(id));
    }

    /// <inheritdoc/>
    public async Task<Pedido> SaveAsync(Pedido pedido)
    {
        logger.LogDebug("Guardando nuevo pedido");
        context.Pedidos.Add(pedido);
        await context.SaveChangesAsync();
        logger.LogInformation("Pedido guardado: {Id}", pedido.Id);
        return pedido;
    }

    /// <inheritdoc/>
    public async Task<Pedido> UpdateAsync(Pedido pedido)
    {
        logger.LogDebug("Actualizando pedido: {Id}", pedido.Id);
        context.Pedidos.Update(pedido);
        await context.SaveChangesAsync();
        logger.LogInformation("Pedido actualizado: {Id}", pedido.Id);
        return pedido;
    }
}

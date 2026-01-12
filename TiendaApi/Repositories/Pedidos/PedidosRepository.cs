using Microsoft.EntityFrameworkCore;
using TiendaApi.Data;
using TiendaApi.Models;

namespace TiendaApi.Repositories.Pedidos;

/// <summary>
/// Implementación del repositorio de pedidos usando MongoDB EF Core.
/// </summary>
public class PedidosRepository(
    TiendaDbContext context,
    ILogger<PedidosRepository> logger
) : IPedidosRepository {

    /// <summary>
    /// Obtiene todos los pedidos ordenados por fecha de creación descendente.
    /// </summary>
    /// <returns>Colección de todos los pedidos.</returns>
    public async Task<IEnumerable<Pedido>> FindAllAsync() {
        logger.LogDebug("Buscando todos los pedidos");
        
        return await context.Pedidos
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    /// <summary>
    /// Obtiene pedidos por identificador de usuario.
    /// </summary>
    /// <param name="userId">Identificador del usuario.</param>
    /// <returns>Colección de pedidos del usuario ordenados por fecha.</returns>
    public async Task<IEnumerable<Pedido>> FindByUserIdAsync(long userId) {
        logger.LogDebug("Buscando pedidos para el usuario: {UserId}", userId);
        
        return await context.Pedidos
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    /// <summary>
    /// Obtiene un pedido por su identificador.
    /// </summary>
    /// <param name="id">Identificador del pedido.</param>
    /// <returns>El pedido encontrado o null.</returns>
    public async Task<Pedido?> FindByIdAsync(string id) {
        logger.LogDebug("Buscando pedido por id: {Id}", id);
        
        return await context.Pedidos
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    /// <summary>
    /// Guarda un nuevo pedido.
    /// </summary>
    /// <param name="pedido">Pedido a guardar.</param>
    /// <returns>El pedido guardado con identificador asignado.</returns>
    public async Task<Pedido> SaveAsync(Pedido pedido) {
        logger.LogDebug("Guardando nuevo pedido");
        pedido.CreatedAt = DateTime.UtcNow;
        pedido.UpdatedAt = DateTime.UtcNow;
        
        context.Pedidos.Add(pedido);
        await context.SaveChangesAsync();
        
        logger.LogInformation("Pedido guardado con id: {Id}", pedido.Id);
        
        return pedido;
    }

    /// <summary>
    /// Actualiza un pedido existente.
    /// </summary>
    /// <param name="pedido">Pedido con datos actualizados.</param>
    /// <returns>El pedido actualizado.</returns>
    public async Task<Pedido> UpdateAsync(Pedido pedido) {
        logger.LogDebug("Actualizando pedido: {Id}", pedido.Id);
        pedido.UpdatedAt = DateTime.UtcNow;
        
        context.Pedidos.Update(pedido);
        await context.SaveChangesAsync();
        
        logger.LogInformation("Pedido actualizado: {Id}", pedido.Id);
        
        return pedido;
    }
}

using MongoDB.Bson;
using MongoDB.Driver;
using TiendaApi.Api.Models;

namespace TiendaApi.Api.Repositories.Pedidos;

/// <summary>
/// Implementación del repositorio de pedidos con MongoDB Driver nativo.
/// </summary>
public class PedidosNativeRepository(
    IMongoDatabase database,
    ILogger<PedidosNativeRepository> logger
) : IPedidosRepository
{
    private readonly IMongoCollection<Pedido> _collection = database.GetCollection<Pedido>("pedidos");

    /// <inheritdoc/>
    public async Task<IEnumerable<Pedido>> FindAllAsync()
    {
        logger.LogDebug("Buscando todos los pedidos");
        return await _collection
            .Find(_ => true)
            .SortByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Pedido>> FindByUserIdAsync(long userId)
    {
        logger.LogDebug("Buscando pedidos para usuario: {UserId}", userId);
        return await _collection
            .Find(p => p.UserId == userId)
            .SortByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    /// <inheritdoc/>
    public async Task<(IEnumerable<Pedido> Items, int TotalCount)> FindByUserIdPagedAsync(long userId, int page, int size)
    {
        logger.LogDebug("Buscando pedidos paginados para usuario: {UserId}", userId);
        var filter = Builders<Pedido>.Filter.Eq(p => p.UserId, userId);
        var totalCount = await _collection.CountDocumentsAsync(filter);

        var items = await _collection
            .Find(filter)
            .SortByDescending(p => p.CreatedAt)
            .Skip(page * size)
            .Limit(size)
            .ToListAsync();

        return (items, (int)totalCount);
    }

    /// <inheritdoc/>
    public async Task<Pedido?> FindByIdAsync(string id)
    {
        logger.LogDebug("Buscando pedido: {Id}", id);
        try
        {
            var objectId = ObjectId.Parse(id);
            return await _collection.Find(p => p.Id == objectId).FirstOrDefaultAsync();
        }
        catch (FormatException)
        {
            logger.LogWarning("ID de pedido inválido: {Id}", id);
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task<Pedido> SaveAsync(Pedido pedido)
    {
        logger.LogDebug("Guardando nuevo pedido");
        await _collection.InsertOneAsync(pedido);
        logger.LogInformation("Pedido guardado: {Id}", pedido.Id);
        return pedido;
    }

    /// <inheritdoc/>
    public async Task<Pedido> UpdateAsync(Pedido pedido)
    {
        logger.LogDebug("Actualizando pedido: {Id}", pedido.Id);
        await _collection.ReplaceOneAsync(p => p.Id == pedido.Id, pedido);
        logger.LogInformation("Pedido actualizado: {Id}", pedido.Id);
        return pedido;
    }
}

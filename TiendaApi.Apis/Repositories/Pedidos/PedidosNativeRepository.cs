using MongoDB.Bson;
using MongoDB.Driver;
using TiendaApi.Apis.Models;

namespace TiendaApi.Apis.Repositories.Pedidos;

/// <summary>
/// Implementación del repositorio de pedidos usando MongoDB Driver nativo.
/// 
/// ✅ Esta es la implementación RECOMENDADA y FUNCIONAL.
/// Usa MongoDB.Driver directamente, sin el wrapper de Entity Framework Core.
/// </summary>
public class PedidosNativeRepository(
    IMongoDatabase database,
    ILogger<PedidosNativeRepository> logger
) : IPedidosRepository
{
    private readonly IMongoCollection<Pedido> _collection = database.GetCollection<Pedido>("pedidos");

    /// <summary>
    /// Obtiene todos los pedidos ordenados por fecha de creación descendente.
    /// </summary>
    /// <returns>Colección de todos los pedidos.</returns>
    public async Task<IEnumerable<Pedido>> FindAllAsync()
    {
        logger.LogDebug("Buscando todos los pedidos (MongoDB Driver nativo)");
        return await _collection
            .Find(_ => true)
            .SortByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    /// <summary>
    /// Obtiene todos los pedidos de un usuario específico.
    /// </summary>
    /// <param name="userId">Identificador del usuario.</param>
    /// <returns>Colección de pedidos del usuario ordenados por fecha.</returns>
    public async Task<IEnumerable<Pedido>> FindByUserIdAsync(long userId)
    {
        logger.LogDebug("Buscando pedidos para el usuario: {UserId} (MongoDB Driver nativo)", userId);
        return await _collection
            .Find(p => p.UserId == userId)
            .SortByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    /// <summary>
    /// Obtiene pedidos de un usuario de forma paginada.
    /// </summary>
    /// <param name="userId">Identificador del usuario.</param>
    /// <param name="page">Número de página (0-based).</param>
    /// <param name="size">Cantidad de pedidos por página.</param>
    /// <returns>Tupla con pedidos de la página y total de pedidos del usuario.</returns>
    public async Task<(IEnumerable<Pedido> Items, int TotalCount)> FindByUserIdPagedAsync(long userId, int page, int size)
    {
        logger.LogDebug("Buscando pedidos paginados (MongoDB Driver nativo)");
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

    /// <summary>
    /// Busca un pedido específico por su identificador.
    /// </summary>
    /// <param name="id">Identificador del pedido.</param>
    /// <returns>El pedido encontrado o null si no existe.</returns>
    public async Task<Pedido?> FindByIdAsync(string id)
    {
        logger.LogDebug("Buscando pedido por id: {Id} (MongoDB Driver nativo)", id);
        try
        {
            var objectId = ObjectId.Parse(id);
            return await _collection
                .Find(p => p.Id == objectId)
                .FirstOrDefaultAsync();
        }
        catch (FormatException)
        {
            logger.LogWarning("ID de pedido inválido: {Id}", id);
            return null;
        }
    }

    /// <summary>
    /// Persiste un nuevo pedido en la base de datos.
    /// </summary>
    /// <param name="pedido">Pedido a persistir.</param>
    /// <returns>El pedido guardado con datos actualizados.</returns>
    public async Task<Pedido> SaveAsync(Pedido pedido)
    {
        logger.LogDebug("Guardando nuevo pedido (MongoDB Driver nativo)");
        await _collection.InsertOneAsync(pedido);
        logger.LogInformation("Pedido guardado con id: {Id}", pedido.Id);
        return pedido;
    }

    /// <summary>
    /// Actualiza un pedido existente.
    /// </summary>
    /// <param name="pedido">Pedido con datos actualizados.</param>
    /// <returns>El pedido actualizado.</returns>
    public async Task<Pedido> UpdateAsync(Pedido pedido)
    {
        logger.LogDebug("Actualizando pedido: {Id} (MongoDB Driver nativo)", pedido.Id);
        await _collection.ReplaceOneAsync(p => p.Id == pedido.Id, pedido);
        logger.LogInformation("Pedido actualizado: {Id}", pedido.Id);
        return pedido;
    }
}

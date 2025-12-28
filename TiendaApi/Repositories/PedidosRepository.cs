using MongoDB.Driver;
using TiendaApi.Models.Entities;

namespace TiendaApi.Repositories;

/// <summary>
/// MongoDB repository implementation for Pedidos
/// Uses MongoDB.Driver for NoSQL document operations
/// </summary>
public class PedidosRepository : IPedidosRepository
{
    private readonly IMongoCollection<Pedido> _pedidos;
    private readonly ILogger<PedidosRepository> _logger;

    public PedidosRepository(IConfiguration configuration, ILogger<PedidosRepository> logger)
    {
        _logger = logger;
        
        // Get MongoDB settings from configuration
        var connectionString = configuration.GetConnectionString("MongoDB") 
            ?? configuration["MongoDbSettings:ConnectionString"]
            ?? throw new InvalidOperationException("MongoDB connection string not configured");
        
        var databaseName = configuration["MongoDbSettings:DatabaseName"] ?? "tienda";
        var collectionName = configuration["MongoDbSettings:PedidosCollection"] ?? "pedidos";
        
        var client = new MongoClient(connectionString);
        var database = client.GetDatabase(databaseName);
        _pedidos = database.GetCollection<Pedido>(collectionName);
        
        _logger.LogInformation("PedidosRepository initialized with database: {DatabaseName}, collection: {CollectionName}", 
            databaseName, collectionName);
    }

    public async Task<IEnumerable<Pedido>> FindAllAsync()
    {
        _logger.LogDebug("Finding all pedidos");
        return await _pedidos.Find(_ => true)
            .SortByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Pedido>> FindByUserIdAsync(long userId)
    {
        _logger.LogDebug("Finding pedidos for user: {UserId}", userId);
        return await _pedidos.Find(p => p.UserId == userId)
            .SortByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    public async Task<Pedido?> FindByIdAsync(string id)
    {
        _logger.LogDebug("Finding pedido by id: {Id}", id);
        return await _pedidos.Find(p => p.Id == id)
            .FirstOrDefaultAsync();
    }

    public async Task<Pedido> SaveAsync(Pedido pedido)
    {
        _logger.LogDebug("Saving new pedido");
        pedido.CreatedAt = DateTime.UtcNow;
        pedido.UpdatedAt = DateTime.UtcNow;
        
        await _pedidos.InsertOneAsync(pedido);
        _logger.LogInformation("Pedido saved with id: {Id}", pedido.Id);
        
        return pedido;
    }

    public async Task<Pedido> UpdateAsync(Pedido pedido)
    {
        _logger.LogDebug("Updating pedido: {Id}", pedido.Id);
        pedido.UpdatedAt = DateTime.UtcNow;
        
        await _pedidos.ReplaceOneAsync(p => p.Id == pedido.Id, pedido);
        _logger.LogInformation("Pedido updated: {Id}", pedido.Id);
        
        return pedido;
    }
}

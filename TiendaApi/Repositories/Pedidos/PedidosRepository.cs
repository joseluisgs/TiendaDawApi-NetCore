using MongoDB.Driver;
using TiendaApi.Models;

namespace TiendaApi.Repositories.Pedidos;

/// <summary>
/// Implementación del repositorio de pedidos usando MongoDB.
/// </summary>
public class PedidosRepository : IPedidosRepository
{
    private readonly IMongoCollection<Pedido> _pedidos;
    private readonly ILogger<PedidosRepository> _logger;

    public PedidosRepository(IConfiguration configuration, ILogger<PedidosRepository> logger)
    {
        _logger = logger;
        
        var connectionString = configuration.GetConnectionString("MongoDB") 
            ?? configuration["MongoDbSettings:ConnectionString"]
            ?? throw new InvalidOperationException("Cadena de conexión de MongoDB no configurada");
        
        var databaseName = configuration["MongoDbSettings:DatabaseName"] ?? "tienda";
        var collectionName = configuration["MongoDbSettings:PedidosCollection"] ?? "pedidos";
        
        var client = new MongoClient(connectionString);
        var database = client.GetDatabase(databaseName);
        _pedidos = database.GetCollection<Pedido>(collectionName);
        
        _logger.LogInformation("PedidosRepository inicializado con base de datos: {DatabaseName}, colección: {CollectionName}", 
            databaseName, collectionName);
    }

    /// <summary>
    /// Obtiene todos los pedidos ordenados por fecha de creación descendente.
    /// </summary>
    /// <returns>Colección de todos los pedidos.</returns>
    public async Task<IEnumerable<Pedido>> FindAllAsync()
    {
        _logger.LogDebug("Buscando todos los pedidos");
        return await _pedidos.Find(_ => true)
            .SortByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    /// <summary>
    /// Obtiene pedidos por identificador de usuario.
    /// </summary>
    /// <param name="userId">Identificador del usuario.</param>
    /// <returns>Colección de pedidos del usuario ordenados por fecha.</returns>
    public async Task<IEnumerable<Pedido>> FindByUserIdAsync(long userId)
    {
        _logger.LogDebug("Buscando pedidos para el usuario: {UserId}", userId);
        return await _pedidos.Find(p => p.UserId == userId)
            .SortByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    /// <summary>
    /// Obtiene un pedido por su identificador.
    /// </summary>
    /// <param name="id">Identificador del pedido.</param>
    /// <returns>El pedido encontrado o null.</returns>
    public async Task<Pedido?> FindByIdAsync(string id)
    {
        _logger.LogDebug("Buscando pedido por id: {Id}", id);
        return await _pedidos.Find(p => p.Id == id)
            .FirstOrDefaultAsync();
    }

    /// <summary>
    /// Guarda un nuevo pedido.
    /// </summary>
    /// <param name="pedido">Pedido a guardar.</param>
    /// <returns>El pedido guardado con identificador asignado.</returns>
    public async Task<Pedido> SaveAsync(Pedido pedido)
    {
        _logger.LogDebug("Guardando nuevo pedido");
        pedido.CreatedAt = DateTime.UtcNow;
        pedido.UpdatedAt = DateTime.UtcNow;
        
        await _pedidos.InsertOneAsync(pedido);
        _logger.LogInformation("Pedido guardado con id: {Id}", pedido.Id);
        
        return pedido;
    }

    /// <summary>
    /// Actualiza un pedido existente.
    /// </summary>
    /// <param name="pedido">Pedido con datos actualizados.</param>
    /// <returns>El pedido actualizado.</returns>
    public async Task<Pedido> UpdateAsync(Pedido pedido)
    {
        _logger.LogDebug("Actualizando pedido: {Id}", pedido.Id);
        pedido.UpdatedAt = DateTime.UtcNow;
        
        await _pedidos.ReplaceOneAsync(p => p.Id == pedido.Id, pedido);
        _logger.LogInformation("Pedido actualizado: {Id}", pedido.Id);
        
        return pedido;
    }
}

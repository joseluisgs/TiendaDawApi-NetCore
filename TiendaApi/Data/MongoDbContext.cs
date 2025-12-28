using MongoDB.Driver;

namespace TiendaApi.Data;

/// <summary>
/// MongoDB database context
/// 
/// Java Spring Boot equivalent: @Configuration with MongoTemplate
/// Provides access to MongoDB collections for Pedidos
/// </summary>
public class MongoDbContext
{
    private readonly IMongoDatabase _database;

    public MongoDbContext(string connectionString, string databaseName)
    {
        var client = new MongoClient(connectionString);
        _database = client.GetDatabase(databaseName);
    }

    public IMongoCollection<T> GetCollection<T>(string collectionName)
    {
        return _database.GetCollection<T>(collectionName);
    }
}

/// <summary>
/// MongoDB configuration settings
/// </summary>
public class MongoDbSettings
{
    public string ConnectionString { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = "tienda";
    public string PedidosCollection { get; set; } = "pedidos";
}

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using Serilog;
using TiendaApi.Api.Data;

namespace TiendaApi.Api.Infrastructures;

/// <summary>
/// Configuración de bases de datos (PostgreSQL + MongoDB).
/// </summary>
public static class DatabaseConfig
{
    /// <summary>
    /// Configura PostgreSQL y MongoDB según configuración.
    /// </summary>
    /// <param name="services">Colección de servicios.</param>
    /// <param name="configuration">Configuración de la app.</param>
    /// <returns>IServiceCollection para encadenar.</returns>
    public static IServiceCollection AddDatabases(this IServiceCollection services, IConfiguration configuration)
    {
        Log.Information("Configurando PostgreSQL...");
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Host=localhost;Database=tienda;Username=admin;Password=admin123";

        services.AddDbContext<TiendaDbContext>(options => options.UseNpgsql(connectionString));

        var mongoImpl = configuration["Pedidos:RepositoryType"] ?? "MongoDbNative";

        if (mongoImpl == "MongoDbNative")
        {
            Log.Information("Configurando MongoDB (Native)...");
            var mongoConnectionString = configuration["MongoDbSettings:ConnectionString"]
                ?? "mongodb://admin:admin123@localhost:27017/tienda?authSource=admin";
            var mongoDatabaseName = configuration["MongoDbSettings:DatabaseName"] ?? "tienda";

            services.AddSingleton<IMongoClient>(sp => new MongoClient(mongoConnectionString));
            services.AddSingleton(sp =>
            {
                var client = sp.GetRequiredService<IMongoClient>();
                return client.GetDatabase(mongoDatabaseName);
            });
            
            services.AddSingleton(sp =>
            {
                var database = sp.GetRequiredService<IMongoDatabase>();
                return database.GetCollection<Models.Pedido>("pedidos");
            });
        }
        else
        {
            Log.Information("Configurando MongoDB (EfCore) [bug EF-272]");
            var mongoConnectionString = configuration["MongoDbSettings:ConnectionString"]
                ?? "mongodb://admin:admin123@localhost:27017/tienda?authSource=admin";
            var mongoDatabaseName = configuration["MongoDbSettings:DatabaseName"] ?? "tienda";

            services.AddDbContext<TiendaMongoContext>(options =>
                options.UseMongoDB(mongoConnectionString, mongoDatabaseName));
        }

        Log.Information("Registrando seeders...");
        if (mongoImpl == "MongoDbNative")
        {
            services.AddScoped<Data.Seed.Mongo.MongoDbSeeder>();
        }
        else
        {
            services.AddScoped<Data.Seed.Mongo.MongoDbEfCoreSeeder>();
        }
        services.AddScoped<Data.Seed.Sql.SqlSeeder>();

        return services;
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using Serilog;
using TiendaApi.Api.Data;

namespace TiendaApi.Api.Infrastructures;

/// <summary>
/// Proporciona métodos de extensión para la configuración de persistencia políglota (PostgreSQL y MongoDB).
/// </summary>
public static class DatabaseConfig
{
    /// <summary>
    /// Registra y configura los contextos de base de datos relacional y documental.
    /// </summary>
    /// <param name="services">Contenedor de inyección de dependencias.</param>
    /// <param name="configuration">Acceso a los archivos 'appsettings'.</param>
    /// <returns>La colección de servicios para encadenamiento fluido.</returns>
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
        services.AddScoped<Data.Seed.Mongo.MongoDbSeeder>();
        services.AddScoped<Data.Seed.Sql.SqlSeeder>();

        return services;
    }
}
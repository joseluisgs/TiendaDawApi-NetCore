using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using TiendaApi.Apis.Data;

namespace TiendaApi.Apis.Infrastructures;

/// <summary>
/// Extensiones de configuración de bases de datos.
/// </summary>
public static class DatabaseConfig
{
    /// <summary>
    /// Configura PostgreSQL y MongoDB.
    /// PostgreSQL: datos maestros (usuarios, productos, categorías).
    /// MongoDB: documentos transaccionales (pedidos con items embebidos).
    /// </summary>
    public static IServiceCollection AddDatabases(this IServiceCollection services, IConfiguration configuration)
    {
        Log.Information("🗄️ Configurando PostgreSQL...");
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Host=localhost;Database=tienda;Username=admin;Password=admin123";

        Log.Debug("🔗 Cadena de conexión: {ConnectionString}", connectionString.Split(';')[0] + "...");
        services.AddDbContext<TiendaDbContext>(options => options.UseNpgsql(connectionString));

        Log.Information("🗄️ Configurando MongoDB...");
        var mongoConnectionString = configuration["MongoDbSettings:ConnectionString"]
            ?? "mongodb://admin:admin123@localhost:27017/tienda?authSource=admin";
        var mongoDatabaseName = configuration["MongoDbSettings:DatabaseName"] ?? "tienda";

        services.AddDbContext<TiendaMongoContext>(options =>
            options.UseMongoDB(mongoConnectionString, mongoDatabaseName));

        Log.Information("🌱 Registrando seeders...");
        services.AddScoped<Data.Seed.Mongo.MongoDbSeeder>();
        services.AddScoped<Data.Seed.Sql.SqlSeeder>();

        return services;
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Serilog;
using TiendaApi.Api.Data;
using TiendaApi.Api.Data.Seed.Mongo;

using TiendaApi.Api.Data.Seed.Sql;

namespace TiendaApi.Api.Infrastructures;

/// <summary>
/// Extension methods para inicialización de base de datos.
/// </summary>
public static class DatabaseInitializationExtensions
{
    /// <summary>
    /// Inicializa la base de datos PostgreSQL y MongoDB.
    /// Desarrollo: Elimina y recrea la BD, siembra datos.
    /// Producción: Solo crea tablas si no existen.
    /// </summary>
    public static async Task InitializeDatabaseAsync(this WebApplication app, bool isDevelopment)
    {
        Log.Information("🗄️ Inicializando base de datos...");

        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TiendaDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        if (isDevelopment)
        {
            logger.LogWarning("🗄️ [DESARROLLO] Eliminando y recreando base de datos...");
            context.Database.EnsureDeleted();
            context.Database.EnsureCreated();
            
            // Seed PostgreSQL
            var sqlSeeder = scope.ServiceProvider.GetRequiredService<SqlSeeder>();
            await sqlSeeder.SeedAsync();
            
            logger.LogInformation("✅ Base de datos recreada con datos semilla");
        }
        else
        {
            context.Database.EnsureCreated();
            logger.LogInformation("✅ Base de datos verificada (tablas creadas si no existían)");
        }

        // Seed MongoDB solo en desarrollo
        if (isDevelopment)
        {
            var mongoImpl = configuration["Pedidos:RepositoryType"] ?? "MongoDbNative";
            
            if (mongoImpl == "MongoDbNative")
            {
                var mongoSeeder = scope.ServiceProvider.GetService<Data.Seed.Mongo.MongoDbSeeder>();
                if (mongoSeeder != null)
                {
                    Log.Information("🌱 Sembrando datos de pedidos en MongoDB (Native)...");
                    await mongoSeeder.SeedAsync();
                    Log.Information("✅ Datos de pedidos sembrados");
                }
            }
            else
            {
                var mongoSeeder = scope.ServiceProvider.GetService<Data.Seed.Mongo.MongoDbEfCoreSeeder>();
                if (mongoSeeder != null)
                {
                    Log.Information("🌱 Sembrando datos de pedidos en MongoDB (EfCore)...");
                    await mongoSeeder.SeedAsync();
                    Log.Information("✅ Datos de pedidos sembrados");
                }
            }
        }
    }
}

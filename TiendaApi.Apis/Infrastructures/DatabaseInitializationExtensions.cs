using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using TiendaApi.Apis.Data;
using TiendaApi.Apis.Data.Seed.Mongo;

namespace TiendaApi.Apis.Infrastructures;

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

        if (isDevelopment)
        {
            logger.LogWarning("🗄️ [DESARROLLO] Eliminando y recreando base de datos...");
            context.Database.EnsureDeleted();
            context.Database.EnsureCreated();
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
            var mongoSeeder = scope.ServiceProvider.GetService<MongoDbSeeder>();
            if (mongoSeeder != null)
            {
                Log.Information("🌱 Sembrando datos de pedidos en MongoDB...");
                await mongoSeeder.SeedAsync();
                Log.Information("✅ Datos de pedidos sembrados");
            }
        }
    }
}

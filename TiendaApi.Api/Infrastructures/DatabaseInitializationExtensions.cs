using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
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
    /// Producción: Aplica las migraciones de EF Core pendientes (solo lo pendiente,
    /// sin perder datos de BDs vivas).
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
            // Producción: EF Core Migrations (8.4/8.5) — Migrate() aplica solo lo
            // pendiente, por lo que una BD viva actualiza sin perder datos.
            await ApplyPendingMigrationsAsync(context, logger);
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

    /// <summary>
    /// Aplica las migraciones pendientes en producción.
    /// <para>
    /// <b>Baseline (8.5):</b> las BDs creadas antes de esta fase con <c>EnsureCreated</c>
    /// ya tienen las tablas pero NO la tabla <c>__EFMigrationsHistory</c>. Si
    /// <c>Migrate()</c> intentase ejecutar <c>InitialCreate</c> fallaría por "tabla ya
    /// existe". Por eso, si el esquema ya existe y no hay historial, marcamos
    /// <c>InitialCreate</c> como aplicada (sin ejecutarla) y <c>Migrate()</c> se
    /// limitará a las migraciones futuras (p. ej. <c>AddOptimizationIndexes</c>,
    /// que solo crea índices y no toca datos).
    /// </para>
    /// </summary>
    /// <param name="context">DbContext a migrar.</param>
    /// <param name="logger">Logger para dejar constancia del baseline.</param>
    private static async Task ApplyPendingMigrationsAsync(TiendaDbContext context, Microsoft.Extensions.Logging.ILogger logger)
    {
        var pending = (await context.Database.GetPendingMigrationsAsync()).ToList();

        if (pending.Count > 0 && await CategoriasTableExistsAsync(context))
        {
            var initial = pending.FirstOrDefault(m => m.EndsWith("_InitialCreate", StringComparison.Ordinal));
            if (initial is not null)
            {
                await context.Database.ExecuteSqlRawAsync(
                    """
                    CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
                        "MigrationId" character varying(150) NOT NULL,
                        "ProductVersion" character varying(32) NOT NULL,
                        CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
                    );
                    """);
                await context.Database.ExecuteSqlAsync(
                    $"INSERT INTO \"__EFMigrationsHistory\" (\"MigrationId\", \"ProductVersion\") VALUES ({initial}, {ProductInfo.GetVersion()}) ON CONFLICT DO NOTHING;");
                logger.LogWarning(
                    "🗄️ [PRODUCCIÓN] BD preexistente sin historial: '{Initial}' marcada como aplicada (baseline, sin ejecutar)",
                    initial);
            }
        }

        await context.Database.MigrateAsync();
        logger.LogInformation(
            "✅ Migraciones aplicadas — pendientes antes: [{Pending}]",
            pending.Count == 0 ? "ninguna" : string.Join(", ", pending));
    }

    /// <summary>
    /// Indica si la tabla del esquema de la API (<c>categorias</c>) ya existe en la BD.
    /// Se usa para distinguir una BD de esta aplicación de una BD ajena o vacía.
    /// </summary>
    private static async Task<bool> CategoriasTableExistsAsync(TiendaDbContext context)
    {
        var connection = context.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText =
            "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'categorias'";
        var result = await command.ExecuteScalarAsync();
        return result is long count && count > 0;
    }
}

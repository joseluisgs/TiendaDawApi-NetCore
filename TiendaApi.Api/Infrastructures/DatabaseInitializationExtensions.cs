using System.Data;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using MongoDB.Driver;
using Serilog;
using TiendaApi.Api.Data;
using TiendaApi.Api.Data.Seed.Mongo;
using TiendaApi.Api.Data.Seed.Sql;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace TiendaApi.Api.Infrastructures;

/// <summary>
///     Extension methods para inicialización de base de datos.
/// </summary>
public static class DatabaseInitializationExtensions {
    /// <summary>
    ///     Inicializa la base de datos PostgreSQL y MongoDB.
    ///     Desarrollo: Elimina y recrea la BD, siembra datos.
    ///     Producción: Aplica las migraciones de EF Core pendientes (solo lo pendiente,
    ///     sin perder datos de BDs vivas).
    /// </summary>
    public static async Task InitializeDatabaseAsync(this WebApplication app, bool isDevelopment) {
        Log.Information("🗄️ Inicializando base de datos...");

        using IServiceScope scope = app.Services.CreateScope();
        TiendaDbContext context = scope.ServiceProvider.GetRequiredService<TiendaDbContext>();
        ILogger<Program> logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        IConfiguration configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        if (isDevelopment) {
            logger.LogWarning("🗄️ [DESARROLLO] Eliminando y recreando base de datos...");
            context.Database.EnsureDeleted();
            context.Database.EnsureCreated();

            // 🎓 Mongo con el MISMO patrón que PostgreSQL: en desarrollo se
            // elimina la BD y las siembras de más abajo (read-model de productos
            // y pedidos) la vuelven a poblar desde cero. Sin este paso,
            // MongoDbSeeder solo siembra si está vacío y la BD acumulaba
            // pedidos entre corridas de las suites E2E.
            try {
                string mongoDbName = configuration["MongoDbSettings:DatabaseName"] ?? "tienda";
                IMongoClient mongoClient = scope.ServiceProvider.GetRequiredService<IMongoClient>();
                await mongoClient.DropDatabaseAsync(mongoDbName);
                logger.LogInformation("🗂️ [DESARROLLO] MongoDB '{Database}' eliminado (se siembra a continuación)",
                    mongoDbName);
            } catch (Exception ex) {
                // Mongo puede no estar disponible aún: mismo criterio que el resto
                // de siembras Mongo, no bloquea el arranque (se reintenta en cada reinicio).
                logger.LogWarning(ex, "No se pudo reiniciar MongoDB en desarrollo");
            }

            // Seed PostgreSQL
            SqlSeeder sqlSeeder = scope.ServiceProvider.GetRequiredService<SqlSeeder>();
            await sqlSeeder.SeedAsync();

            logger.LogInformation("✅ Base de datos recreada con datos semilla");
        } else {
            // Producción: EF Core Migrations — Migrate() aplica solo lo
            // pendiente, por lo que una BD viva actualiza sin perder datos.
            await ApplyPendingMigrationsAsync(context, logger);
        }

        // Seed MongoDB solo en desarrollo
        if (isDevelopment) {
            string mongoImpl = configuration["Pedidos:RepositoryType"] ?? "MongoDbNative";

            if (mongoImpl == "MongoDbNative") {
                MongoDbSeeder? mongoSeeder = scope.ServiceProvider.GetService<MongoDbSeeder>();
                if (mongoSeeder != null) {
                    Log.Information("🌱 Sembrando datos de pedidos en MongoDB (Native)...");
                    await mongoSeeder.SeedAsync();
                    Log.Information("✅ Datos de pedidos sembrados");
                }
            } else {
                MongoDbEfCoreSeeder? mongoSeeder = scope.ServiceProvider.GetService<MongoDbEfCoreSeeder>();
                if (mongoSeeder != null) {
                    Log.Information("🌱 Sembrando datos de pedidos en MongoDB (EfCore)...");
                    await mongoSeeder.SeedAsync();
                    Log.Information("✅ Datos de pedidos sembrados");
                }
            }
        }
    }

    /// <summary>
    ///     Aplica las migraciones pendientes en producción.
    ///     <para>
    ///         <b>Baseline de migraciones:</b> las BDs ya creadas con <c>EnsureCreated</c>
    ///         tienen las tablas pero NO la tabla <c>__EFMigrationsHistory</c>. Si
    ///         <c>Migrate()</c> intentase ejecutar <c>InitialCreate</c> fallaría por "tabla ya
    ///         existe". Por eso, si el esquema ya existe y no hay historial, marcamos
    ///         <c>InitialCreate</c> como aplicada (sin ejecutarla) y <c>Migrate()</c> se
    ///         limitará a las migraciones futuras (p. ej. <c>AddOptimizationIndexes</c>,
    ///         que solo crea índices y no toca datos).
    ///     </para>
    /// </summary>
    /// <param name="context">DbContext a migrar.</param>
    /// <param name="logger">Logger para dejar constancia del baseline.</param>
    private static async Task ApplyPendingMigrationsAsync(TiendaDbContext context, ILogger logger) {
        var pending = (await context.Database.GetPendingMigrationsAsync()).ToList();

        if (pending.Count > 0 && await CategoriasTableExistsAsync(context)) {
            string? initial = pending.FirstOrDefault(m => m.EndsWith("_InitialCreate", StringComparison.Ordinal));
            if (initial is not null) {
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
    ///     Indica si la tabla del esquema de la API (<c>categorias</c>) ya existe en la BD.
    ///     Se usa para distinguir una BD de esta aplicación de una BD ajena o vacía.
    /// </summary>
    private static async Task<bool> CategoriasTableExistsAsync(TiendaDbContext context) {
        // 🎓 La conexión la gestiona el DbContext (scoped); no disposear aquí.
        // El warning del analizador es falso positivo en este caso.
        DbConnection connection = context.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open) {
            await connection.OpenAsync();
        }

        await using DbCommand command = connection.CreateCommand();
        command.CommandText =
            "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'categorias'";
        object? result = await command.ExecuteScalarAsync();
        return result is long count && count > 0;
    }
}

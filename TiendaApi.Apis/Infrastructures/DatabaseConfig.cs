using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using Serilog;
using TiendaApi.Apis.Data;

namespace TiendaApi.Apis.Infrastructures;

/// <summary>
/// Extensiones de configuración de bases de datos.
/// 
/// <para>
/// Este archivo demuestra DOS formas de acceder a MongoDB:
/// <list type="bullet">
///   <item><b>Entity Framework Core:</b> ORM de Microsoft, familiar para quienes usan PostgreSQL</item>
///   <item><b>MongoDB Driver Nativo:</b> Driver oficial de MongoDB, más performante</item>
/// </list>
/// </para>
/// 
/// <para>
/// La elección se realiza mediante configuration["Pedidos:RepositoryType"]:
/// <list type="bullet">
///   <item><b>"MongoDbNative":</b> Usa driver nativo (RECOMENDADO, funcional)</item>
///   <item><b>"MongoDbEfCore":</b> Usa Entity Framework Core (tiene bug con EF Core 10)</item>
/// </list>
/// </para>
/// </summary>
public static class DatabaseConfig
{
    /// <summary>
    /// Configura todas las bases de datos del proyecto.
    /// 
    /// <para><b>PostgreSQL:</b> Datos maestros (usuarios, productos, categorías)</para>
    /// <para><b>MongoDB:</b> Documentos transaccionales (pedidos con items embebidos)</para>
    /// </summary>
    /// 
    /// <param name="services">Colección de servicios de DI</param>
    /// <param name="configuration">Configuración de la aplicación</param>
    /// <returns>La colección de servicios para encadenar llamadas</returns>
    public static IServiceCollection AddDatabases(this IServiceCollection services, IConfiguration configuration)
    {
        // ============================================
        // POSTGRESQL (igual para todas las configuraciones)
        // ============================================
        // PostgreSQL almacena datos estructurados: usuarios, productos, categorías.
        // Usamos Entity Framework Core con el provider de PostgreSQL (Npgsql).
        // Esta configuración NO cambia según Pedidos:RepositoryType.
        // ============================================
        
        Log.Information("🗄️ Configurando PostgreSQL...");
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Host=localhost;Database=tienda;Username=admin;Password=admin123";

        Log.Debug("🔗 Cadena de conexión: {ConnectionString}", connectionString.Split(';')[0] + "...");
        services.AddDbContext<TiendaDbContext>(options => options.UseNpgsql(connectionString));

        // ============================================
        // MONGODB (depende de Pedidos:RepositoryType)
        // ============================================
        // Hay dos formas de acceder a MongoDB:
        //
        // 1. MongoDbEfCore (Entity Framework Core)
        //    - Ventajas: Sintaxis similar a PostgreSQL, usa LINQ
        //    - Desventajas: Bug conocido con EF Core 10 (MongoDB Jira: EF-272)
        //
        // 2. MongoDbNative (MongoDB Driver nativo)
        //    - Ventajas: Más rápido, más control, sin bugs de EF
        //    - Desventajas: Sintaxis diferente (Builders API)
        // ============================================
        
        var mongoImpl = configuration["Pedidos:RepositoryType"] ?? "MongoDbNative";

        if (mongoImpl == "MongoDbNative")
        {
            // MongoDB Driver nativo (recomendado)
            Log.Information("🗄️ Configurando MongoDB (Native)...");
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
            // MongoDB EF Core (tiene bug conocido con EF Core 10)
            Log.Information("🗄️ Configurando MongoDB (EfCore) [⚠️ bug EF-272]");
            var mongoConnectionString = configuration["MongoDbSettings:ConnectionString"]
                ?? "mongodb://admin:admin123@localhost:27017/tienda?authSource=admin";
            var mongoDatabaseName = configuration["MongoDbSettings:DatabaseName"] ?? "tienda";

            services.AddDbContext<TiendaMongoContext>(options =>
                options.UseMongoDB(mongoConnectionString, mongoDatabaseName));
        }

        // ============================================
        // SEEDERS (datos iniciales)
        // ============================================
        // Los seedersinsertan datos de ejemplo en las bases de datos.
        // Solo se ejecutan en desarrollo o cuando no hay datos.
        // ============================================
        
        Log.Information("🌱 Registrando seeders...");
        services.AddScoped<Data.Seed.Mongo.MongoDbSeeder>();
        services.AddScoped<Data.Seed.Sql.SqlSeeder>();

        return services;
    }
}

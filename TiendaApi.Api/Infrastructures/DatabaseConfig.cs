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
    /// <param name="environment">Entorno (producción exige configuración explícita).</param>
    /// <returns>IServiceCollection para encadenar.</returns>
    public static IServiceCollection AddDatabases(this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment environment)
    {
        Log.Information("Configurando PostgreSQL...");

        // 🎓 Fail-fast: en producción, si falta la configuración, la app NO debe arrancar.
        var connectionString = ResolverCadenaObligatoria(
            configuration.GetConnectionString("DefaultConnection"),
            "Host=localhost;Database=tienda;Username=admin;Password=admin123",
            "ConnectionStrings:DefaultConnection",
            environment);

        services.AddDbContext<TiendaDbContext>(options => options.UseNpgsql(connectionString));

        var mongoImpl = configuration["Pedidos:RepositoryType"] ?? "MongoDbNative";

        if (mongoImpl == "MongoDbNative")
        {
            Log.Information("Configurando MongoDB (Native)...");
            var mongoConnectionString = ResolverCadenaObligatoria(
                configuration["MongoDbSettings:ConnectionString"],
                "mongodb://admin:admin123@localhost:27017/tienda?authSource=admin",
                "MongoDbSettings:ConnectionString",
                environment);
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
            var mongoConnectionString = ResolverCadenaObligatoria(
                configuration["MongoDbSettings:ConnectionString"],
                "mongodb://admin:admin123@localhost:27017/tienda?authSource=admin",
                "MongoDbSettings:ConnectionString",
                environment);
            var mongoDatabaseName = configuration["MongoDbSettings:DatabaseName"] ?? "tienda";

            services.AddDbContext<TiendaMongoContext>(options =>
                options.UseMongoDB(mongoConnectionString, mongoDatabaseName));

            // Cliente compartido (lo usan los health checks para el ping)
            services.AddSingleton<IMongoClient>(_ => new MongoClient(mongoConnectionString));
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
        services.AddScoped<Data.Seed.Cassandra.CassandraSeeder>();

        return services;
    }

    /// <summary>
    /// Resuelve una cadena de conexión obligatoria sin repetir el mismo
    /// bloque if/else en cada almacén: en desarrollo cae al default local con
    /// warning; en producción, si falta, lanza (fail-fast: la app NO arranca
    /// con credenciales por defecto).
    /// </summary>
    /// <param name="valor">Valor de configuración (puede venir nulo/vacío).</param>
    /// <param name="fallbackDesarrollo">Default local solo para Development.</param>
    /// <param name="claveConfig">Nombre de la clave, para los mensajes (mismos textos que antes).</param>
    /// <param name="environment">Entorno actual.</param>
    private static string ResolverCadenaObligatoria(
        string? valor, string fallbackDesarrollo, string claveConfig, IWebHostEnvironment environment)
    {
        if (!string.IsNullOrEmpty(valor))
            return valor;

        if (environment.IsDevelopment())
        {
            Log.Warning("⚠️ Usando credenciales por defecto de desarrollo ({Clave} no definida)", claveConfig);
            return fallbackDesarrollo;
        }

        throw new InvalidOperationException(
            $"{claveConfig} no está definida. " +
            "En producción es obligatorio configurarla (appsettings.json o variables de entorno).");
    }
}

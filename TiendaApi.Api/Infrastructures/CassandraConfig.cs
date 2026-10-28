using Cassandra;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using ISession = Cassandra.ISession;

namespace TiendaApi.Api.Infrastructures;

/// <summary>
/// Configuración de la conexión a Cassandra.
/// </summary>
public static class CassandraConfig
{
    /// <summary>
    /// Registra el cluster y la sesión de Cassandra en el contenedor de dependencias.
    ///
    /// 🎓 El <see cref="ISession"/> es thread-safe y multiplexa las peticiones:
    /// se crea una vez y vive todo el proceso (singleton). No hay "pool de contexts"
    /// como en EF Core, y por eso no existe el equivalente al DbContext descartado.
    /// La conexión real es perezosa: el primer statement es el que descubre al nodo,
    /// de ahí el reintento del InitializeSchemaAsync.
    /// </summary>
    /// <param name="services">Colección de servicios.</param>
    /// <param name="configuration">Configuración de la app.</param>
    /// <returns>IServiceCollection para encadenar.</returns>
    public static IServiceCollection AddCassandra(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var settings = configuration
            .GetSection(CassandraSettings.SectionName)
            .Get<CassandraSettings>() ?? new CassandraSettings();

        Log.Information("Conectando a Cassandra: {Hosts}:{Port} (keyspace {Keyspace})",
            string.Join(",", settings.Hosts), settings.Port, settings.Keyspace);

        var cluster = Cluster.Builder()
            .AddContactPoints(settings.Hosts)
            .WithPort(settings.Port)
            .Build();

        var session = cluster.Connect();

        services.AddSingleton(settings);
        services.AddSingleton<ISession>(session);

        return services;
    }
}

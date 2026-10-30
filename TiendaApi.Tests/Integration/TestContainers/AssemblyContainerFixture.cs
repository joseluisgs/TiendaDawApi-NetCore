using Cassandra;
using MongoDB.Driver;
using Npgsql;
using NUnit.Framework;
using Testcontainers.Cassandra;
using Testcontainers.MongoDb;
using Testcontainers.PostgreSql;
using TiendaApi.Api.Infrastructures;

namespace TiendaApi.Tests.Integration.TestContainers;

/// <summary>
/// Contenedores compartidos por assembly: un único PostgreSQL y un único MongoDB
/// para todos los tests de integración (en lugar de arrancar un par por clase).
///
/// El aislamiento entre clases se consigue con base de datos propia por clase
/// dentro de esos contenedores (véase <see cref="CreatePostgresDatabaseAsync"/>
/// y <see cref="DropMongoDatabase"/>).
///
/// Al estar declarada como <c>[SetUpFixture]</c> en este namespace, gobierna
/// todos los tests de <c>TiendaApi.Tests.Integration.TestContainers.*</c>.
/// </summary>
[SetUpFixture]
public sealed class AssemblyContainerFixture
{
    private static PostgreSqlContainer? _postgres;
    private static MongoDbContainer? _mongo;

    // Cassandra se arranca SOLO bajo demanda (GetCassandraSessionAsync): es una
    // JVM y tarda ~1 min, coste que no deben pagar los tests que no usan series.
    // NUnit1032: SÍ se disposea en el [OneTimeTearDown] DisposeAsync de este
    // fixture; el analyzer no lo empareja porque la asignacion esta en el
    // metodo lazy de arriba (mismo motivo por el que la plantilla 26 lo omite).
#pragma warning disable NUnit1032
    private static CassandraContainer? _cassandra;
    private static ISession? _cassandraSession;
#pragma warning restore NUnit1032
    private static readonly SemaphoreSlim _cassandraLock = new(1, 1);

    /// <summary>Contenedor PostgreSQL compartido (arrancado una vez por assembly).</summary>
    internal static PostgreSqlContainer Postgres =>
        _postgres ?? throw new InvalidOperationException("El contenedor PostgreSQL no está arrancado.");

    /// <summary>Contenedor MongoDB compartido (arrancado una vez por assembly).</summary>
    internal static MongoDbContainer Mongo =>
        _mongo ?? throw new InvalidOperationException("El contenedor MongoDB no está arrancado.");

    /// <summary>Cadena de conexión de MongoDB compartida entre tests.</summary>
    internal static string MongoConnectionString => Mongo.GetConnectionString();

    /// <summary>
    /// Crea (o recrea) una base de datos PostgreSQL con el nombre indicado y
    /// devuelve la cadena de conexión apuntando a ella. Usada por cada clase
    /// de tests para aislar sus datos dentro del contenedor compartido.
    /// </summary>
    internal static async Task<string> CreatePostgresDatabaseAsync(string databaseName)
    {
        var admin = new NpgsqlConnectionStringBuilder(Postgres.GetConnectionString())
        {
            Database = "postgres",
        };

        await using (var connection = new NpgsqlConnection(admin.ConnectionString))
        {
            await connection.OpenAsync();
            await using var drop = new NpgsqlCommand(
                $"DROP DATABASE IF EXISTS \"{databaseName}\" WITH (FORCE)", connection);
            await drop.ExecuteNonQueryAsync();

            await using var create = new NpgsqlCommand(
                $"CREATE DATABASE \"{databaseName}\"", connection);
            await create.ExecuteNonQueryAsync();
        }

        var target = new NpgsqlConnectionStringBuilder(Postgres.GetConnectionString())
        {
            Database = databaseName,
        };
        return target.ConnectionString;
    }

    /// <summary>Elimina una base de datos PostgreSQL creada por una clase de tests.</summary>
    internal static async Task DropPostgresDatabaseAsync(string databaseName)
    {
        var admin = new NpgsqlConnectionStringBuilder(Postgres.GetConnectionString())
        {
            Database = "postgres",
        };

        await using var connection = new NpgsqlConnection(admin.ConnectionString);
        await connection.OpenAsync();
        await using var drop = new NpgsqlCommand(
            $"DROP DATABASE IF EXISTS \"{databaseName}\" WITH (FORCE)", connection);
        await drop.ExecuteNonQueryAsync();
    }

    /// <summary>Elimina una base de datos MongoDB creada por una clase de tests.</summary>
    internal static void DropMongoDatabase(string databaseName)
    {
        new MongoClient(MongoConnectionString).DropDatabase(databaseName);
    }

    /// <summary>
    /// Sesión de Cassandra compartida por assembly, arrancada bajo demanda con el
    /// MISMO esquema de producción (<c>InitializeSchemaAsync</c>: keyspace +
    /// tablas + ChangeKeyspace sobre el contenedor efímero).
    /// </summary>
    /// <param name="keyspace">Keyspace a crear/usar (por defecto el de la tienda).</param>
    /// <returns>Sesión conectada al contenedor de tests.</returns>
    internal static async Task<ISession> GetCassandraSessionAsync(string keyspace = "tienda")
    {
        if (_cassandraSession is not null)
            return _cassandraSession;

        await _cassandraLock.WaitAsync();
        try
        {
            if (_cassandraSession is not null)
                return _cassandraSession;

            _cassandra = new CassandraBuilder(TestContainerImages.Cassandra)
                .WithEnvironment("MAX_HEAP_SIZE", "1G")
                .WithEnvironment("HEAP_NEWSIZE", "256M")
                .Build();

            await _cassandra.StartAsync();

            var cluster = Cluster.Builder()
                .AddContactPoints(_cassandra.Hostname)
                .WithPort(_cassandra.GetMappedPublicPort(9042))
                .Build();

            var session = cluster.Connect();
            await session.InitializeSchemaAsync(keyspace);

            _cassandraSession = session;
            return _cassandraSession;
        }
        finally
        {
            _cassandraLock.Release();
        }
    }

    /// <summary>Arranca los dos contenedores compartidos (una sola vez por assembly).</summary>
    [OneTimeSetUp]
    public async Task InitializeAsync()
    {
        _mongo = new MongoDbBuilder(TestContainerImages.Mongo)
            .WithPortBinding(27017, true)
            .Build();

        await _mongo.StartAsync();

        _postgres = new PostgreSqlBuilder(TestContainerImages.Postgres)
            .WithUsername("test")
            .WithPassword("test")
            .Build();

        await _postgres.StartAsync();
    }

    /// <summary>Detiene los contenedores compartidos al finalizar los tests.</summary>
    [OneTimeTearDown]
    public async Task DisposeAsync()
    {
        if (_mongo is not null)
        {
            await _mongo.DisposeAsync();
            _mongo = null;
        }

        if (_postgres is not null)
        {
            await _postgres.DisposeAsync();
            _postgres = null;
        }

        if (_cassandraSession is not null)
        {
            _cassandraSession.Dispose();
            _cassandraSession = null;
        }

        if (_cassandra is not null)
        {
            await _cassandra.DisposeAsync();
            _cassandra = null;
        }
    }
}

using MongoDB.Driver;
using Npgsql;
using NUnit.Framework;
using Testcontainers.MongoDb;
using Testcontainers.PostgreSql;

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
    }
}

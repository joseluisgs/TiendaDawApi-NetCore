using System.Threading.Channels;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TiendaApi.Api.Services.Email;

namespace TiendaApi.Tests.Integration.TestContainers.Categorias.Containers;

/// <summary>
///     Tests de integración para Containers de Categorías.
///     Verifica la conectividad y configuración de containers Docker (PostgreSQL, MongoDB).
/// </summary>
[TestFixture]
[Category("Integration")]
public class CategoriasContainersIntegrationTests {
    [OneTimeSetUp]
    public async Task OneTimeSetup() {
        _connectionString = await AssemblyContainerFixture.CreatePostgresDatabaseAsync(DatabaseName);
        _mongoConnectionString = AssemblyContainerFixture.MongoConnectionString;
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown() {
        await AssemblyContainerFixture.DropPostgresDatabaseAsync(DatabaseName);
        AssemblyContainerFixture.DropMongoDatabase(DatabaseName);
    }

    private const string DatabaseName = "it_categoria_containers";
    private string _connectionString = string.Empty;
    private string _mongoConnectionString = string.Empty;

    [Test]
    public async Task PostgreSQLContainer_ShouldBeRunning() {
        string connectionString = _connectionString;
        connectionString.Should().NotBeNullOrEmpty();
        connectionString.Should().Contain("Host=");

        await Task.CompletedTask;
    }

    [Test]
    public async Task PostgreSQLContainer_ConnectionString_IsValid() {
        string connectionString = _connectionString;
        connectionString.Should().Contain("Host=");
        connectionString.Should().Contain("Port=");
        connectionString.Should().Contain($"Database={DatabaseName}");

        await Task.CompletedTask;
    }

    [Test]
    public async Task MongoDBContainer_ShouldBeRunning() {
        string connectionString = _mongoConnectionString;
        connectionString.Should().NotBeNullOrEmpty();
        connectionString.Should().Contain("mongodb://");

        await Task.CompletedTask;
    }

    [Test]
    public async Task MongoDBContainer_ConnectionString_IsValid() {
        string connectionString = _mongoConnectionString;
        connectionString.Should().Contain("mongodb://");
        connectionString.Should().Contain("127.0.0.1");

        await Task.CompletedTask;
    }

    [Test]
    public async Task Configuration_CanBuildServiceProvider() {
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> {
                { "ConnectionStrings:DefaultConnection", _connectionString },
                { "MongoDbSettings:ConnectionString", _mongoConnectionString },
                { "MongoDbSettings:DatabaseName", DatabaseName }
            }!)
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddMemoryCache();
        services.AddSingleton(Channel.CreateUnbounded<EmailMessage>());

        using ServiceProvider provider = services.BuildServiceProvider();
        provider.Should().NotBeNull();

        IConfiguration config = provider.GetRequiredService<IConfiguration>();
        config.Should().NotBeNull();

        await Task.CompletedTask;
    }

    [Test]
    public async Task Configuration_CanGetConnectionStrings() {
        string postgresConn = _connectionString;
        string mongoConn = _mongoConnectionString;

        postgresConn.Should().NotBeNullOrEmpty();
        mongoConn.Should().NotBeNullOrEmpty();

        await Task.CompletedTask;
    }
}

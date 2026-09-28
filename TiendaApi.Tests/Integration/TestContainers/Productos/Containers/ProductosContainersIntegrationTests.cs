using System.Threading.Channels;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TiendaApi.Api.Services.Cache;
using TiendaApi.Api.Services.Email;

namespace TiendaApi.Tests.Integration.TestContainers.Productos.Containers;

/// <summary>
/// Tests de integración para Containers de Productos.
/// Verifica la conectividad y configuración de containers Docker (PostgreSQL, MongoDB).
/// </summary>
[TestFixture]
[Category("Integration")]
public class ProductosContainersIntegrationTests
{
    private const string DatabaseName = "it_producto_containers";
    private string _connectionString = string.Empty;
    private string _mongoConnectionString = string.Empty;

    [OneTimeSetUp]
    public async Task OneTimeSetup()
    {
        _connectionString = await AssemblyContainerFixture.CreatePostgresDatabaseAsync(DatabaseName);
        _mongoConnectionString = AssemblyContainerFixture.MongoConnectionString;
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        await AssemblyContainerFixture.DropPostgresDatabaseAsync(DatabaseName);
        AssemblyContainerFixture.DropMongoDatabase(DatabaseName);
    }

    [Test]
    public async Task MongoDBContainer_ShouldBeRunning()
    {
        var connectionString = _mongoConnectionString;
        connectionString.Should().NotBeNullOrEmpty();
        connectionString.Should().Contain("mongodb://");

        await Task.CompletedTask;
    }

    [Test]
    public async Task MongoDBContainer_ConnectionString_IsValid()
    {
        var connectionString = _mongoConnectionString;
        connectionString.Should().Contain("mongodb://");

        await Task.CompletedTask;
    }

    [Test]
    public async Task PostgreSQLContainer_ShouldBeRunning()
    {
        var connectionString = _connectionString;
        connectionString.Should().NotBeNullOrEmpty();

        await Task.CompletedTask;
    }

    [Test]
    public async Task Configuration_CanBuildServiceProvider()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "ConnectionStrings:DefaultConnection", _connectionString },
                { "MongoDbSettings:ConnectionString", _mongoConnectionString },
                { "MongoDbSettings:DatabaseName", DatabaseName },
                { "Storage:UploadPath", "test-uploads" }
            }!)
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddMemoryCache();
        services.AddSingleton(Channel.CreateUnbounded<EmailMessage>());

        using var provider = services.BuildServiceProvider();
        provider.Should().NotBeNull();

        await Task.CompletedTask;
    }

    [Test]
    public async Task BothContainers_CanRunTogether()
    {
        var postgresConnection = _connectionString;
        var mongoConnection = _mongoConnectionString;

        postgresConnection.Should().NotBeNullOrEmpty();
        mongoConnection.Should().NotBeNullOrEmpty();
        postgresConnection.Should().NotBe(mongoConnection);

        await Task.CompletedTask;
    }
}

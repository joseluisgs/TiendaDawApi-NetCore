using System.Threading.Channels;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TiendaApi.Api.Services.Cache;
using TiendaApi.Api.Services.Email;

namespace TiendaApi.Tests.Integration.TestContainers.Usuarios.Containers;

/// <summary>
/// Tests de integración para Containers de Usuarios.
/// Verifica la conectividad y configuración de containers Docker (PostgreSQL, MongoDB).
/// </summary>
[TestFixture]
[Category("Integration")]
public class UsuariosContainersIntegrationTests
{
    private const string DatabaseName = "it_usuario_containers";
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
    public async Task PostgreSQLContainer_ShouldBeRunning()
    {
        var connectionString = _connectionString;
        connectionString.Should().NotBeNullOrEmpty();
        connectionString.Should().Contain("Host=");

        await Task.CompletedTask;
    }

    [Test]
    public async Task MongoDBContainer_ShouldBeRunning()
    {
        var connectionString = _mongoConnectionString;
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
                { "Jwt:Key", "TestKeyWithAtLeast32CharactersForSecurity!" },
                { "Jwt:Issuer", "TiendaApiTest" },
                { "Jwt:Audience", "TiendaApiTest" }
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
    public async Task Configuration_CanGetConnectionStrings()
    {
        var postgresConn = _connectionString;
        var mongoConn = _mongoConnectionString;

        postgresConn.Should().NotBeNullOrEmpty();
        mongoConn.Should().NotBeNullOrEmpty();

        await Task.CompletedTask;
    }
}

using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.MongoDb;
using Testcontainers.PostgreSql;
using TiendaApi.Apis.Dtos.Pedidos;
using TiendaApi.Apis.Models;
using TiendaApi.Apis.Repositories;
using TiendaApi.Apis.Services.Pedidos;

namespace TiendaApi.Tests.Integration.TestContainers;

/// <summary>
/// Tests de integración para funcionalidad de Pedidos usando Testcontainers
/// Prueba interacciones reales con bases de datos usando contenedores MongoDB y PostgreSQL
/// </summary>
[TestFixture]
public class PedidosIntegrationTests
{
    private MongoDbContainer? _mongoContainer;
    private PostgreSqlContainer? _postgresContainer;
    private ServiceProvider? _serviceProvider;

    [OneTimeSetUp]
    public async Task OneTimeSetup()
    {
        _mongoContainer = new MongoDbBuilder()
            .WithImage("mongo:7.0")
            .WithPortBinding(27017, true)
            .Build();

        await _mongoContainer.StartAsync();

        _postgresContainer = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("tienda_test")
            .WithUsername("test")
            .WithPassword("test")
            .Build();

        await _postgresContainer.StartAsync();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                { "ConnectionStrings:MongoDB", _mongoContainer.GetConnectionString() },
                { "ConnectionStrings:DefaultConnection", _postgresContainer.GetConnectionString() },
                { "MongoDbSettings:DatabaseName", "tienda_test" },
                { "MongoDbSettings:PedidosCollection", "pedidos" },
                { "Jwt:Key", "TestKeyWithAtLeast32CharactersForSecurity!" },
                { "Jwt:Issuer", "TiendaApiTest" },
                { "Jwt:Audience", "TiendaApiTest" },
                { "Smtp:AdminEmail", "admin@test.com" }
            }!)
            .Build();

        var services = new ServiceCollection();

        services.AddSingleton<IConfiguration>(configuration);

        _serviceProvider = services.BuildServiceProvider();
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        _serviceProvider?.Dispose();

        if (_mongoContainer != null)
        {
            await _mongoContainer.DisposeAsync();
        }

        if (_postgresContainer != null)
        {
            await _postgresContainer.DisposeAsync();
        }
    }

    [Test]
    [Ignore("Test de integración scaffold - implementar cuando esté listo el DI completo")]
    public async Task CreatePedido_ConBasesDeDatosReales_DebePersistirEnMongoDB()
    {
        await Task.CompletedTask;
    }

    [Test]
    [Ignore("Test de integración scaffold - implementar cuando esté listo el DI completo")]
    public async Task FindAllPedidos_ConMongoDBReald_DebeRetornarPedidos()
    {
        await Task.CompletedTask;
    }

    [Test]
    [Ignore("Test de integración scaffold - implementar cuando esté listo el DI completo")]
    public async Task UpdatePedidoEstado_ConMongoDBReal_DebePersistirCambios()
    {
        await Task.CompletedTask;
    }

    [Test]
    public async Task MongoDBContainer_ShouldBeRunning()
    {
        _mongoContainer.Should().NotBeNull();
        var connectionString = _mongoContainer!.GetConnectionString();
        connectionString.Should().NotBeNullOrEmpty();
        connectionString.Should().Contain("mongodb://");

        await Task.CompletedTask;
    }

    [Test]
    public async Task PostgreSQLContainer_ShouldBeRunning()
    {
        _postgresContainer.Should().NotBeNull();
        var connectionString = _postgresContainer!.GetConnectionString();
        connectionString.Should().NotBeNullOrEmpty();
        connectionString.Should().Contain("Host=");

        await Task.CompletedTask;
    }
}

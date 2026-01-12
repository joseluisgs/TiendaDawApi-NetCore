using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.MongoDb;
using Testcontainers.PostgreSql;
using TiendaApi.Apis.Dtos.Pedidos;
using TiendaApi.Apis.Models;
using TiendaApi.Apis.Repositories;
using TiendaApi.Apis.Services.Pedidos;

namespace TiendaApi.Tests.Integration;

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
        // Start MongoDB container
        _mongoContainer = new MongoDbBuilder()
            .WithImage("mongo:7.0")
            .WithPortBinding(27017, true)
            .Build();

        await _mongoContainer.StartAsync();

        // Start PostgreSQL container
        _postgresContainer = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("tienda_test")
            .WithUsername("test")
            .WithPassword("test")
            .Build();

        await _postgresContainer.StartAsync();

        // Setup configuration with container connection strings
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

        // Setup DI container
        var services = new ServiceCollection();

        // Note: This is a scaffold for integration tests
        // Full implementation would require:
        // 1. DbContext setup with PostgreSQL container
        // 2. Repository registrations
        // 3. Service registrations with all dependencies
        // 4. AutoMapper configuration
        // 5. Cache and email service mocks

        services.AddSingleton<IConfiguration>(configuration);

        // TODO: Add full service registration when implementing
        // services.AddDbContext<TiendaDbContext>(...);
        // services.AddScoped<IPedidosRepository, PedidosRepository>();
        // services.AddScoped<IProductoRepository, ProductoRepository>();
        // services.AddAutoMapper(...);
        // etc.

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
        // This is a scaffold test demonstrating the structure
        // Actual implementation requires full DI setup

        // Arrange
        // var pedidosService = _serviceProvider!.GetRequiredService<IPedidosService>();
        // var productoRepo = _serviceProvider!.GetRequiredService<IProductoRepository>();

        // Setup test data in PostgreSQL
        // var testProducto = new Producto { ... };
        // await productoRepo.SaveAsync(testProducto);

        // var pedidoRequest = new PedidoRequestDto { ... };

        // Act
        // var result = await pedidosService.CreateAsync(1, pedidoRequest);

        // Assert
        // result.IsSuccess.Should().BeTrue();
        // Verify pedido was saved to MongoDB
        // Verify stock was updated in PostgreSQL

        await Task.CompletedTask;
    }

    [Test]
    [Ignore("Test de integración scaffold - implementar cuando esté listo el DI completo")]
    public async Task FindAllPedidos_ConMongoDBReald_DebeRetornarPedidos()
    {
        // This is a scaffold test demonstrating the structure

        // Arrange
        // var pedidosService = _serviceProvider!.GetRequiredService<IPedidosService>();

        // Act
        // var result = await pedidosService.FindAllAsync();

        // Assert
        // result.IsSuccess.Should().BeTrue();

        await Task.CompletedTask;
    }

    [Test]
    [Ignore("Test de integración scaffold - implementar cuando esté listo el DI completo")]
    public async Task UpdatePedidoEstado_ConMongoDBReal_DebePersistirCambios()
    {
        // This is a scaffold test demonstrating the structure

        // Arrange
        // Create a pedido first
        // var pedidosService = _serviceProvider!.GetRequiredService<IPedidosService>();
        // var pedidoId = "...";
        // var nuevoEstado = PedidoEstado.PROCESANDO;

        // Act
        // var result = await pedidosService.UpdateEstadoAsync(pedidoId, nuevoEstado);

        // Assert
        // result.IsSuccess.Should().BeTrue();
        // result.Value.Estado.Should().Be(nuevoEstado);

        await Task.CompletedTask;
    }

    /// <summary>
    /// Ejemplo de cómo verificar conexión MongoDB
    /// </summary>
    [Test]
    public async Task MongoDBContainer_ShouldBeRunning()
    {
        _mongoContainer.Should().NotBeNull();
        var connectionString = _mongoContainer!.GetConnectionString();
        connectionString.Should().NotBeNullOrEmpty();
        connectionString.Should().Contain("mongodb://");

        await Task.CompletedTask;
    }

    /// <summary>
    /// Ejemplo de cómo verificar conexión PostgreSQL
    /// </summary>
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

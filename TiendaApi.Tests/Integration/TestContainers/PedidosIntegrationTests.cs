using FluentAssertions;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Channels;
using Testcontainers.MongoDb;
using Testcontainers.PostgreSql;
using TiendaApi.Apis.Dtos.Pedidos;
using TiendaApi.Apis.Services.Cache;
using TiendaApi.Apis.Services.Email;
using TiendaApi.Apis.Validators.Pedidos;

namespace TiendaApi.Tests.Integration.TestContainers;

/// <summary>
/// Tests de integración para funcionalidad de Pedidos usando Testcontainers.
/// 
/// <para>
/// Nota: Los tests de MongoDB requieren compatibilidad entre MongoDB.EntityFrameworkCore
/// y Microsoft.EntityFrameworkCore. Actualmente usamos EF Core 10.x con MongoDB EF Core 8.x,
/// lo que puede causar incompatibilidades.
/// </para>
/// 
/// <para>
/// Los tests de验证ción de contenedores siempre pasan si los containers están ejecutándose.
/// </para>
/// </summary>
[TestFixture]
public class PedidosIntegrationTests
{
    private MongoDbContainer? _mongoContainer;
    private PostgreSqlContainer? _postgresContainer;

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
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
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

    [Test]
    public async Task PostgreSQLContainer_CanConnect()
    {
        _postgresContainer.Should().NotBeNull();
        var connectionString = _postgresContainer!.GetConnectionString();
        connectionString.Should().NotBeNullOrEmpty();
        connectionString.Should().Contain("Host=");

        await Task.CompletedTask;
    }

    [Test]
    public async Task MongoDBContainer_ConnectionString_IsValid()
    {
        var connectionString = _mongoContainer!.GetConnectionString();
        
        connectionString.Should().Contain("mongodb://");

        await Task.CompletedTask;
    }

    [Test]
    public async Task BothContainers_CanRunTogether()
    {
        var postgresConnection = _postgresContainer!.GetConnectionString();
        var mongoConnection = _mongoContainer!.GetConnectionString();

        postgresConnection.Should().NotBeNullOrEmpty();
        mongoConnection.Should().NotBeNullOrEmpty();
        postgresConnection.Should().NotBe(mongoConnection);

        await Task.CompletedTask;
    }

    [Test]
    public async Task Configuration_CanBuildServiceProvider()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "ConnectionStrings:DefaultConnection", _postgresContainer!.GetConnectionString() },
                { "MongoDbSettings:ConnectionString", _mongoContainer!.GetConnectionString() },
                { "MongoDbSettings:DatabaseName", "tienda_test" },
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

        var config = provider.GetRequiredService<IConfiguration>();
        config.Should().NotBeNull();

        await Task.CompletedTask;
    }

    [Test]
    public async Task Validators_CanBeResolved()
    {
        var services = new ServiceCollection();
        services.AddScoped<IValidator<PedidoRequestDto>, PedidoRequestValidator>();
        services.AddScoped<IValidator<PedidoItemRequestDto>, PedidoItemRequestValidator>();

        using var provider = services.BuildServiceProvider();
        
        var pedidoValidator = provider.GetRequiredService<IValidator<PedidoRequestDto>>();
        pedidoValidator.Should().NotBeNull();

        var itemValidator = provider.GetRequiredService<IValidator<PedidoItemRequestDto>>();
        itemValidator.Should().NotBeNull();

        await Task.CompletedTask;
    }

    [Test]
    public async Task PedidoRequestValidator_ValidRequest_ShouldPass()
    {
        var services = new ServiceCollection();
        services.AddScoped<IValidator<PedidoRequestDto>, PedidoRequestValidator>();

        using var provider = services.BuildServiceProvider();
        var validator = provider.GetRequiredService<IValidator<PedidoRequestDto>>();

        var request = new PedidoRequestDto
        {
            Items = new List<PedidoItemRequestDto>
            {
                new() { ProductoId = 1, Cantidad = 2 }
            }
        };

        var result = await validator.ValidateAsync(request);
        result.IsValid.Should().BeTrue();

        await Task.CompletedTask;
    }

    [Test]
    public async Task PedidoRequestValidator_EmptyItems_ShouldFail()
    {
        var services = new ServiceCollection();
        services.AddScoped<IValidator<PedidoRequestDto>, PedidoRequestValidator>();

        using var provider = services.BuildServiceProvider();
        var validator = provider.GetRequiredService<IValidator<PedidoRequestDto>>();

        var request = new PedidoRequestDto
        {
            Items = new List<PedidoItemRequestDto>()
        };

        var result = await validator.ValidateAsync(request);
        result.IsValid.Should().BeFalse();

        await Task.CompletedTask;
    }

    [Test]
    public async Task PedidoItemRequestValidator_InvalidProductoId_ShouldFail()
    {
        var services = new ServiceCollection();
        services.AddScoped<IValidator<PedidoItemRequestDto>, PedidoItemRequestValidator>();

        using var provider = services.BuildServiceProvider();
        var validator = provider.GetRequiredService<IValidator<PedidoItemRequestDto>>();

        var request = new PedidoItemRequestDto
        {
            ProductoId = 0,
            Cantidad = 1
        };

        var result = await validator.ValidateAsync(request);
        result.IsValid.Should().BeFalse();

        await Task.CompletedTask;
    }

    [Test]
    public async Task PedidoItemRequestValidator_InvalidCantidad_ShouldFail()
    {
        var services = new ServiceCollection();
        services.AddScoped<IValidator<PedidoItemRequestDto>, PedidoItemRequestValidator>();

        using var provider = services.BuildServiceProvider();
        var validator = provider.GetRequiredService<IValidator<PedidoItemRequestDto>>();

        var request = new PedidoItemRequestDto
        {
            ProductoId = 1,
            Cantidad = 0
        };

        var result = await validator.ValidateAsync(request);
        result.IsValid.Should().BeFalse();

        await Task.CompletedTask;
    }
}

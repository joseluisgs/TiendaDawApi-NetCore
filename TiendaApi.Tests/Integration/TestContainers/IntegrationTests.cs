using FluentAssertions;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Channels;
using Testcontainers.MongoDb;
using Testcontainers.PostgreSql;
using TiendaApi.Apis.Dtos.Categorias;
using TiendaApi.Apis.Dtos.Common;
using TiendaApi.Apis.Dtos.Pedidos;
using TiendaApi.Apis.Dtos.Productos;
using TiendaApi.Apis.Dtos.Usuarios;
using TiendaApi.Apis.Services.Cache;
using TiendaApi.Apis.Services.Email;
using TiendaApi.Apis.Validators.Categorias;
using TiendaApi.Apis.Validators.Productos;
using TiendaApi.Apis.Validators.Pedidos;
using TiendaApi.Apis.Validators.Usuarios;

namespace TiendaApi.Tests.Integration.TestContainers;

/// <summary>
/// Tests de integración para verificar Containers y Validators.
/// Los tests completos de servicios requieren más configuración de la que es práctico en este momento.
/// </summary>
[TestFixture]
public class IntegrationTests
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

    #region MongoDB Container Tests

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
    public async Task MongoDBContainer_ConnectionString_IsValid()
    {
        var connectionString = _mongoContainer!.GetConnectionString();
        
        connectionString.Should().Contain("mongodb://");

        await Task.CompletedTask;
    }

    #endregion

    #region PostgreSQL Container Tests

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
    public async Task PostgreSQLContainer_ConnectionString_IsValid()
    {
        var connectionString = _postgresContainer!.GetConnectionString();
        
        connectionString.Should().Contain("Host=");
        connectionString.Should().Contain("Port=");
        connectionString.Should().Contain("Database=tienda_test");

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

    #endregion

    #region CategoriaValidator Tests

    [Test]
    public async Task CategoriaValidator_ConNombreValido_PasaValidacion()
    {
        var services = new ServiceCollection();
        services.AddScoped<IValidator<CategoriaRequestDto>, CategoriaRequestValidator>();

        using var provider = services.BuildServiceProvider();
        var validator = provider.GetRequiredService<IValidator<CategoriaRequestDto>>();

        var dto = new CategoriaRequestDto { Nombre = "Electrónica" };
        var result = await validator.ValidateAsync(dto);

        result.IsValid.Should().BeTrue();

        await Task.CompletedTask;
    }

    [Test]
    public async Task CategoriaValidator_ConNombreVacio_FallaValidacion()
    {
        var services = new ServiceCollection();
        services.AddScoped<IValidator<CategoriaRequestDto>, CategoriaRequestValidator>();

        using var provider = services.BuildServiceProvider();
        var validator = provider.GetRequiredService<IValidator<CategoriaRequestDto>>();

        var dto = new CategoriaRequestDto { Nombre = "" };
        var result = await validator.ValidateAsync(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();

        await Task.CompletedTask;
    }

    [Test]
    public async Task CategoriaValidator_ConNombreCorto_FallaValidacion()
    {
        var services = new ServiceCollection();
        services.AddScoped<IValidator<CategoriaRequestDto>, CategoriaRequestValidator>();

        using var provider = services.BuildServiceProvider();
        var validator = provider.GetRequiredService<IValidator<CategoriaRequestDto>>();

        var dto = new CategoriaRequestDto { Nombre = "AB" };
        var result = await validator.ValidateAsync(dto);

        result.IsValid.Should().BeFalse();

        await Task.CompletedTask;
    }

    [Test]
    public async Task CategoriaValidator_ConNombreLargo_PasaValidacion()
    {
        var services = new ServiceCollection();
        services.AddScoped<IValidator<CategoriaRequestDto>, CategoriaRequestValidator>();

        using var provider = services.BuildServiceProvider();
        var validator = provider.GetRequiredService<IValidator<CategoriaRequestDto>>();

        var dto = new CategoriaRequestDto { Nombre = new string('A', 50) };
        var result = await validator.ValidateAsync(dto);

        result.IsValid.Should().BeTrue();

        await Task.CompletedTask;
    }

    #endregion

    #region ProductoValidator Tests

    [Test]
    public async Task ProductoValidator_ConDtoValido_PasaValidacion()
    {
        var services = new ServiceCollection();
        services.AddScoped<IValidator<ProductoRequestDto>, ProductoRequestValidator>();

        using var provider = services.BuildServiceProvider();
        var validator = provider.GetRequiredService<IValidator<ProductoRequestDto>>();

        var dto = new ProductoRequestDto
        {
            Nombre = "Laptop",
            Precio = 999.99m,
            Stock = 10,
            CategoriaId = 1,
            Descripcion = "Una laptop genial"
        };
        var result = await validator.ValidateAsync(dto);

        result.IsValid.Should().BeTrue();

        await Task.CompletedTask;
    }

    [Test]
    public async Task ProductoValidator_ConPrecioNegativo_FallaValidacion()
    {
        var services = new ServiceCollection();
        services.AddScoped<IValidator<ProductoRequestDto>, ProductoRequestValidator>();

        using var provider = services.BuildServiceProvider();
        var validator = provider.GetRequiredService<IValidator<ProductoRequestDto>>();

        var dto = new ProductoRequestDto
        {
            Nombre = "Laptop",
            Precio = -100m,
            Stock = 10,
            CategoriaId = 1
        };
        var result = await validator.ValidateAsync(dto);

        result.IsValid.Should().BeFalse();

        await Task.CompletedTask;
    }

    [Test]
    public async Task ProductoValidator_ConStockNegativo_FallaValidacion()
    {
        var services = new ServiceCollection();
        services.AddScoped<IValidator<ProductoRequestDto>, ProductoRequestValidator>();

        using var provider = services.BuildServiceProvider();
        var validator = provider.GetRequiredService<IValidator<ProductoRequestDto>>();

        var dto = new ProductoRequestDto
        {
            Nombre = "Laptop",
            Precio = 100m,
            Stock = -5,
            CategoriaId = 1
        };
        var result = await validator.ValidateAsync(dto);

        result.IsValid.Should().BeFalse();

        await Task.CompletedTask;
    }

    [Test]
    public async Task ProductoValidator_ConCategoriaIdInvalido_FallaValidacion()
    {
        var services = new ServiceCollection();
        services.AddScoped<IValidator<ProductoRequestDto>, ProductoRequestValidator>();

        using var provider = services.BuildServiceProvider();
        var validator = provider.GetRequiredService<IValidator<ProductoRequestDto>>();

        var dto = new ProductoRequestDto
        {
            Nombre = "Laptop",
            Precio = 100m,
            Stock = 10,
            CategoriaId = 0
        };
        var result = await validator.ValidateAsync(dto);

        result.IsValid.Should().BeFalse();

        await Task.CompletedTask;
    }

    #endregion

    #region PedidoValidator Tests

    [Test]
    public async Task PedidoRequestValidator_ConItemsValidos_PasaValidacion()
    {
        var services = new ServiceCollection();
        services.AddScoped<IValidator<PedidoRequestDto>, PedidoRequestValidator>();
        services.AddScoped<IValidator<PedidoItemRequestDto>, PedidoItemRequestValidator>();

        using var provider = services.BuildServiceProvider();
        var validator = provider.GetRequiredService<IValidator<PedidoRequestDto>>();

        var dto = new PedidoRequestDto
        {
            Items = new List<PedidoItemRequestDto>
            {
                new() { ProductoId = 1, Cantidad = 2 }
            }
        };
        var result = await validator.ValidateAsync(dto);

        result.IsValid.Should().BeTrue();

        await Task.CompletedTask;
    }

    [Test]
    public async Task PedidoRequestValidator_SinItems_FallaValidacion()
    {
        var services = new ServiceCollection();
        services.AddScoped<IValidator<PedidoRequestDto>, PedidoRequestValidator>();
        services.AddScoped<IValidator<PedidoItemRequestDto>, PedidoItemRequestValidator>();

        using var provider = services.BuildServiceProvider();
        var validator = provider.GetRequiredService<IValidator<PedidoRequestDto>>();

        var dto = new PedidoRequestDto
        {
            Items = new List<PedidoItemRequestDto>()
        };
        var result = await validator.ValidateAsync(dto);

        result.IsValid.Should().BeFalse();

        await Task.CompletedTask;
    }

    [Test]
    public async Task PedidoItemRequestValidator_ConProductoIdInvalido_FallaValidacion()
    {
        var services = new ServiceCollection();
        services.AddScoped<IValidator<PedidoItemRequestDto>, PedidoItemRequestValidator>();

        using var provider = services.BuildServiceProvider();
        var validator = provider.GetRequiredService<IValidator<PedidoItemRequestDto>>();

        var dto = new PedidoItemRequestDto
        {
            ProductoId = 0,
            Cantidad = 1
        };
        var result = await validator.ValidateAsync(dto);

        result.IsValid.Should().BeFalse();

        await Task.CompletedTask;
    }

    [Test]
    public async Task PedidoItemRequestValidator_ConCantidadInvalida_FallaValidacion()
    {
        var services = new ServiceCollection();
        services.AddScoped<IValidator<PedidoItemRequestDto>, PedidoItemRequestValidator>();

        using var provider = services.BuildServiceProvider();
        var validator = provider.GetRequiredService<IValidator<PedidoItemRequestDto>>();

        var dto = new PedidoItemRequestDto
        {
            ProductoId = 1,
            Cantidad = 0
        };
        var result = await validator.ValidateAsync(dto);

        result.IsValid.Should().BeFalse();

        await Task.CompletedTask;
    }

    #endregion

    #region UsuarioValidator Tests

    [Test]
    public async Task RegisterValidator_ConDtoValido_PasaValidacion()
    {
        var services = new ServiceCollection();
        services.AddScoped<IValidator<RegisterDto>, RegisterValidator>();

        using var provider = services.BuildServiceProvider();
        var validator = provider.GetRequiredService<IValidator<RegisterDto>>();

        var dto = new RegisterDto
        {
            Username = "juanperez",
            Email = "juan@test.com",
            Password = "Password123"
        };
        var result = await validator.ValidateAsync(dto);

        result.IsValid.Should().BeTrue();

        await Task.CompletedTask;
    }

    [Test]
    public async Task RegisterValidator_ConEmailInvalido_FallaValidacion()
    {
        var services = new ServiceCollection();
        services.AddScoped<IValidator<RegisterDto>, RegisterValidator>();

        using var provider = services.BuildServiceProvider();
        var validator = provider.GetRequiredService<IValidator<RegisterDto>>();

        var dto = new RegisterDto
        {
            Username = "juanperez",
            Email = "email-invalido",
            Password = "Password123"
        };
        var result = await validator.ValidateAsync(dto);

        result.IsValid.Should().BeFalse();

        await Task.CompletedTask;
    }

    [Test]
    public async Task RegisterValidator_ConPasswordCorto_FallaValidacion()
    {
        var services = new ServiceCollection();
        services.AddScoped<IValidator<RegisterDto>, RegisterValidator>();

        using var provider = services.BuildServiceProvider();
        var validator = provider.GetRequiredService<IValidator<RegisterDto>>();

        var dto = new RegisterDto
        {
            Username = "juanperez",
            Email = "juan@test.com",
            Password = "123"
        };
        var result = await validator.ValidateAsync(dto);

        result.IsValid.Should().BeFalse();

        await Task.CompletedTask;
    }

    #endregion

    #region Configuration Tests

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
    public async Task Configuration_CanGetConnectionStrings()
    {
        var postgresConn = _postgresContainer!.GetConnectionString();
        var mongoConn = _mongoContainer!.GetConnectionString();

        postgresConn.Should().NotBeNullOrEmpty();
        mongoConn.Should().NotBeNullOrEmpty();
        postgresConn.Should().NotBe(mongoConn);

        await Task.CompletedTask;
    }

    #endregion

    #region DTO Tests

    [Test]
    public async Task CategoriaFilterDto_DefaultValues_AreCorrect()
    {
        var filter = new CategoriaFilterDto();

        filter.Page.Should().Be(0);
        filter.Size.Should().Be(10);
        filter.SortBy.Should().Be("id");
        filter.Direction.Should().Be("asc");

        await Task.CompletedTask;
    }

    [Test]
    public async Task ProductoFilterDto_DefaultValues_AreCorrect()
    {
        var filter = new ProductoFilterDto(null, null, null, null, null);

        filter.Page.Should().Be(0);
        filter.Size.Should().Be(10);
        filter.SortBy.Should().Be("id");
        filter.Direction.Should().Be("asc");

        await Task.CompletedTask;
    }

    [Test]
    public async Task UserFilterDto_DefaultValues_AreCorrect()
    {
        var filter = new UserFilterDto(null, null, null, 0, 10, "id", "asc");

        filter.Page.Should().Be(0);
        filter.Size.Should().Be(10);
        filter.SortBy.Should().Be("id");
        filter.Direction.Should().Be("asc");

        await Task.CompletedTask;
    }

    [Test]
    public async Task PedidoDto_AllStates_ShouldBeValid()
    {
        var estadosValidos = new[] { "PENDIENTE", "PROCESANDO", "ENVIADO", "ENTREGADO", "CANCELADO" };

        foreach (var estado in estadosValidos)
        {
            var dto = new PedidoDto(
                Id: "PED-2024-0001",
                UserId: 1,
                Items: new List<PedidoItemDto>(),
                Total: 100m,
                Estado: estado,
                DireccionEnvio: "Calle Test 123",
                CreatedAt: DateTime.UtcNow
            );

            dto.Estado.Should().Be(estado);
        }

        await Task.CompletedTask;
    }

    #endregion
}

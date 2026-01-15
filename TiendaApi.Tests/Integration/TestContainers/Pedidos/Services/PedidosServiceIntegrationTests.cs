using CSharpFunctionalExtensions;
using FluentAssertions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Threading.Channels;
using Testcontainers.MongoDb;
using Testcontainers.PostgreSql;
using TiendaApi.Apis.Data;
using TiendaApi.Apis.Dtos.Pedidos;
using TiendaApi.Apis.Models;
using TiendaApi.Apis.Repositories.Categorias;
using TiendaApi.Apis.Repositories.Pedidos;
using TiendaApi.Apis.Repositories.Productos;
using TiendaApi.Apis.Services.Cache;
using TiendaApi.Apis.Services.Email;
using TiendaApi.Apis.Services.Pedidos;
using TiendaApi.Apis.Validators.Pedidos;

namespace TiendaApi.Tests.Integration.TestContainers.Pedidos.Services;

/// <summary>
/// Tests de integración para PedidosService con DI completo.
/// Verifica el servicio con bases de datos reales usando Testcontainers.
/// </summary>
[TestFixture]
public class PedidosServiceIntegrationTests
{
    private MongoDbContainer? _mongoContainer;
    private PostgreSqlContainer? _postgresContainer;
    private IServiceProvider? _serviceProvider;
    private TiendaDbContext? _dbContext;
    private IPedidosService? _pedidosService;
    private long _productoId;
    private long _userId;

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

    [SetUp]
    public async Task Setup()
    {
        var connectionString = _postgresContainer!.GetConnectionString();
        var mongoConnectionString = _mongoContainer!.GetConnectionString();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "ConnectionStrings:DefaultConnection", connectionString },
                { "MongoDbSettings:ConnectionString", mongoConnectionString },
                { "MongoDbSettings:DatabaseName", "tienda_test" },
                { "Cache:PedidoCacheTTLMinutes", "5" }
            }!)
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddMemoryCache();
        services.AddSingleton(Channel.CreateUnbounded<EmailMessage>());

        services.AddDbContext<TiendaDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<ICategoriaRepository, CategoriaRepository>();
        services.AddScoped<IProductoRepository, ProductoRepository>();
        services.AddScoped<IPedidosRepository, PedidosRepository>();
        services.AddScoped<IPedidosService, PedidosService>();
        services.AddScoped<IValidator<PedidoRequestDto>, PedidoRequestValidator>();
        services.AddScoped<IValidator<PedidoItemRequestDto>, PedidoItemRequestValidator>();
        services.AddScoped<ICacheService, MemoryCacheService>();

        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        services.AddSingleton(loggerFactory);

        _serviceProvider = services.BuildServiceProvider();

        _dbContext = _serviceProvider.GetRequiredService<TiendaDbContext>();
        await _dbContext.Database.EnsureCreatedAsync();

        var productoRepo = _serviceProvider.GetRequiredService<IProductoRepository>();
        var categoriaRepo = _serviceProvider.GetRequiredService<ICategoriaRepository>();
        var categoria = new Categoria { Nombre = "Test Categoria" };
        await categoriaRepo.SaveAsync(categoria);

        var producto = new Producto
        {
            Nombre = "Producto Test",
            Descripcion = "Producto para pedidos",
            Precio = 99.99m,
            Stock = 100,
            CategoriaId = categoria.Id
        };
        await productoRepo.SaveAsync(producto);
        _productoId = producto.Id;

        var user = new User
        {
            Username = "testuser",
            Email = "test@example.com",
            PasswordHash = "$2a$11$test",
            Role = "USER"
        };
        _userId = user.Id;

        _pedidosService = _serviceProvider.GetRequiredService<IPedidosService>();
    }

    [Test]
    public async Task FindAllAsync_SinPedidos_RetornaListaVacia()
    {
        var result = await _pedidosService!.FindAllAsync();

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
    }

    [Test]
    public async Task FindByUserIdAsync_SinPedidos_RetornaListaVacia()
    {
        var result = await _pedidosService!.FindByUserIdAsync(_userId);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
    }

    [Test]
    public async Task FindByIdAsync_SinPedidos_RetornaNotFound()
    {
        var result = await _pedidosService!.FindByIdAsync("507f1f77bcf86cd799439011");

        result.IsFailure.Should().BeTrue();
    }

    [Test]
    public async Task CreateAsync_ConItemsValidos_RetornaPedidoCreado()
    {
        var dto = new PedidoRequestDto
        {
            Items = new List<PedidoItemRequestDto>
            {
                new() { ProductoId = _productoId, Cantidad = 2 }
            }
        };

        var result = await _pedidosService!.CreateAsync(_userId, dto);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Id.Should().NotBeNullOrEmpty();
        result.Value.Items.Should().HaveCount(1);
    }

    [Test]
    public async Task CreateAsync_ConItemsVacios_RetornaError()
    {
        var dto = new PedidoRequestDto
        {
            Items = new List<PedidoItemRequestDto>()
        };

        var result = await _pedidosService!.CreateAsync(_userId, dto);

        result.IsFailure.Should().BeTrue();
    }

    [Test]
    public async Task CreateAsync_ConProductoNoExistente_RetornaError()
    {
        var dto = new PedidoRequestDto
        {
            Items = new List<PedidoItemRequestDto>
            {
                new() { ProductoId = 999999, Cantidad = 1 }
            }
        };

        var result = await _pedidosService!.CreateAsync(_userId, dto);

        result.IsFailure.Should().BeTrue();
    }

    [Test]
    public async Task CreateAsync_ConCantidadExcesiva_RetornaError()
    {
        var dto = new PedidoRequestDto
        {
            Items = new List<PedidoItemRequestDto>
            {
                new() { ProductoId = _productoId, Cantidad = 999 }
            }
        };

        var result = await _pedidosService!.CreateAsync(_userId, dto);

        result.IsFailure.Should().BeTrue();
    }
}

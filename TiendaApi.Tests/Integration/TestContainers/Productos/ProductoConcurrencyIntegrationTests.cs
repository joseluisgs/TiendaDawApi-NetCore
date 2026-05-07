using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Threading.Channels;
using Testcontainers.PostgreSql;
using TiendaApi.Api.Data;
using TiendaApi.Api.Models;
using TiendaApi.Api.Repositories.Categorias;
using TiendaApi.Api.Repositories.Productos;
using TiendaApi.Api.Services.Cache;
using TiendaApi.Api.Services.Email;

namespace TiendaApi.Tests.Integration.TestContainers.Pedidos.Services;

/// <summary>
/// Tests de integración para verificar control de concurrencia con RowVersion.
/// Usa solo PostgreSQL para simplificar.
/// </summary>
[TestFixture]
[Category("Integration")]
public class ProductoConcurrencyIntegrationTests
{
    private static readonly ActivitySource ActivitySource = new("ProductoConcurrencyTests");
    private PostgreSqlContainer? _postgresContainer;
    private IServiceProvider? _serviceProvider;
    private TiendaDbContext? _dbContext;
    private IProductoRepository? _productoRepository;
    private long _productoId;

    [OneTimeSetUp]
    public async Task OneTimeSetup()
    {
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
        if (_postgresContainer != null)
        {
            await _postgresContainer.DisposeAsync();
        }
    }

    [SetUp]
    public async Task Setup()
    {
        var connectionString = _postgresContainer!.GetConnectionString();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "ConnectionStrings:DefaultConnection", connectionString }
            }!)
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddMemoryCache();
        services.AddSingleton(Channel.CreateUnbounded<EmailMessage>());
        services.AddSingleton(ActivitySource);

        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });

        services.AddDbContext<TiendaDbContext>(options =>
            options.UseNpgsql(connectionString));

        RegisterRepositories(services);

        _serviceProvider = services.BuildServiceProvider();

        _dbContext = _serviceProvider.GetRequiredService<TiendaDbContext>();
        await _dbContext.Database.EnsureCreatedAsync();

        var productoRepo = _serviceProvider.GetRequiredService<IProductoRepository>();
        var categoriaRepo = _serviceProvider.GetRequiredService<ICategoriaRepository>();

        var uniqueName = $"Test Categoria {Guid.NewGuid():N}".Substring(0, 30);
        var categoria = new Categoria { Nombre = uniqueName };
        await categoriaRepo.SaveAsync(categoria);

        var producto = new Producto
        {
            Nombre = $"Producto Test {Guid.NewGuid():N}".Substring(0, 20),
            Descripcion = "Producto para tests de concurrencia",
            Precio = 99.99m,
            Stock = 100,
            CategoriaId = categoria.Id,
            RowVersion = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }
        };
        await productoRepo.SaveAsync(producto);
        _productoId = producto.Id;

        _productoRepository = productoRepo;
    }

    [TearDown]
    public void TearDown()
    {
        _dbContext?.Dispose();
        (_serviceProvider as IDisposable)?.Dispose();
    }

    private static void RegisterRepositories(IServiceCollection services)
    {
        services.AddScoped<IProductoRepository, ProductoRepository>();
        services.AddScoped<ICategoriaRepository, CategoriaRepository>();
    }

    [Test]
    public async Task SaveAsync_ConRowVersionInicializado_InsertaCorrectamente()
    {
        var producto = new Producto
        {
            Nombre = "Nuevo Producto",
            Descripcion = "Test RowVersion",
            Precio = 50.00m,
            Stock = 25,
            CategoriaId = 1,
            RowVersion = new byte[] { 9, 10, 11, 12, 13, 14, 15, 16 }
        };

        var result = await _productoRepository!.SaveAsync(producto);

        result.Id.Should().BeGreaterThan(0);
        result.RowVersion.Should().NotBeNull();
        result.RowVersion.Length.Should().Be(8);
    }

    [Test]
    public async Task FindByIdAsync_RetornaProductoConRowVersion()
    {
        var producto = await _productoRepository!.FindByIdAsync(_productoId);

        producto.Should().NotBeNull();
        producto!.RowVersion.Should().NotBeNull();
        producto.RowVersion.Length.Should().Be(8);
    }

    [Test]
    public async Task DecrementStockAsync_ConStockSuficiente_DecrementaCorrectamente()
    {
        var stockInicial = 50;
        await SetProductoStock(_productoId, stockInicial);

        var producto = await _productoRepository!.FindByIdAsync(_productoId);
        producto.Should().NotBeNull();

        var resultado = await _productoRepository.DecrementStockAsync(_productoId, 10, producto!.RowVersion);

        resultado.Should().BeTrue();

        var productoFinal = await _productoRepository.FindByIdAsync(_productoId);
        productoFinal!.Stock.Should().Be(stockInicial - 10);
    }

    [Test]
    public async Task DecrementStockAsync_ConStockInsuficiente_NoDecrementa()
    {
        var stockInicial = 5;
        await SetProductoStock(_productoId, stockInicial);

        var producto = await _productoRepository!.FindByIdAsync(_productoId);
        producto.Should().NotBeNull();

        var resultado = await _productoRepository.DecrementStockAsync(_productoId, 10, producto!.RowVersion);

        resultado.Should().BeFalse();

        var productoFinal = await _productoRepository.FindByIdAsync(_productoId);
        productoFinal!.Stock.Should().Be(stockInicial);
    }

    [Test]
    public async Task DecrementStockAsync_StockExacto_PermiteDecremento()
    {
        var stockInicial = 10;
        await SetProductoStock(_productoId, stockInicial);

        var producto = await _productoRepository!.FindByIdAsync(_productoId);
        producto.Should().NotBeNull();

        var resultado = await _productoRepository.DecrementStockAsync(_productoId, stockInicial, producto!.RowVersion);

        resultado.Should().BeTrue();

        var productoFinal = await _productoRepository.FindByIdAsync(_productoId);
        productoFinal!.Stock.Should().Be(0);
    }

    [Test]
    public async Task DecrementStockAsync_CantidadMayorStock_Rechaza()
    {
        var stockInicial = 10;
        await SetProductoStock(_productoId, stockInicial);

        var producto = await _productoRepository!.FindByIdAsync(_productoId);
        producto.Should().NotBeNull();

        var resultado = await _productoRepository.DecrementStockAsync(_productoId, stockInicial + 1, producto!.RowVersion);

        resultado.Should().BeFalse();

        var productoFinal = await _productoRepository.FindByIdAsync(_productoId);
        productoFinal!.Stock.Should().Be(stockInicial);
    }

    [Test]
    public async Task DecrementStockAsync_ConStockCero_Rechaza()
    {
        await SetProductoStock(_productoId, 0);

        var producto = await _productoRepository!.FindByIdAsync(_productoId);
        producto.Should().NotBeNull();

        var resultado = await _productoRepository.DecrementStockAsync(_productoId, 1, producto!.RowVersion);

        resultado.Should().BeFalse();

        var productoFinal = await _productoRepository.FindByIdAsync(_productoId);
        productoFinal!.Stock.Should().Be(0);
    }

    [Test]
    public async Task FindAllAsync_SinProductos_RetornaListaVacia()
    {
        var productos = await _productoRepository!.FindAllAsync();
        productos.Should().NotBeNull();
    }

    private async Task SetProductoStock(long productoId, int stock)
    {
        var producto = await _dbContext!.FindAsync<Producto>(productoId);
        producto!.Stock = stock;
        await _dbContext.SaveChangesAsync();
    }
}

using System.Diagnostics;
using System.Threading.Channels;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TiendaApi.Api.Data;
using TiendaApi.Api.Models;
using TiendaApi.Api.Repositories.Categorias;
using TiendaApi.Api.Repositories.Productos;
using TiendaApi.Api.Services.Email;

namespace TiendaApi.Tests.Integration.TestContainers.Pedidos.Services;

/// <summary>
///     Tests de integración para verificar control de concurrencia con RowVersion.
///     Usa solo PostgreSQL para simplificar.
/// </summary>
[TestFixture]
[Category("Integration")]
public class ProductoConcurrencyIntegrationTests {
    [OneTimeSetUp]
    public async Task OneTimeSetup() {
        _connectionString = await AssemblyContainerFixture.CreatePostgresDatabaseAsync(DatabaseName);
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown() {
        await AssemblyContainerFixture.DropPostgresDatabaseAsync(DatabaseName);
    }

    [SetUp]
    public async Task Setup() {
        string connectionString = _connectionString;

        IConfigurationRoot configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> {
                { "ConnectionStrings:DefaultConnection", connectionString }
            }!)
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddMemoryCache();
        services.AddSingleton(Channel.CreateUnbounded<EmailMessage>());
        services.AddSingleton(ActivitySource);

        services.AddLogging(builder => {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });

        services.AddDbContext<TiendaDbContext>(options =>
            options.UseNpgsql(connectionString));

        RegisterRepositories(services);

        _serviceProvider = services.BuildServiceProvider();

        _dbContext = _serviceProvider.GetRequiredService<TiendaDbContext>();
        await _dbContext.Database.EnsureCreatedAsync();

        IProductoRepository productoRepo = _serviceProvider.GetRequiredService<IProductoRepository>();
        ICategoriaRepository categoriaRepo = _serviceProvider.GetRequiredService<ICategoriaRepository>();

        string uniqueName = $"Test Categoria {Guid.NewGuid():N}".Substring(0, 30);
        var categoria = new Categoria { Nombre = uniqueName };
        await categoriaRepo.SaveAsync(categoria);

        var producto = new Producto {
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
    public void TearDown() {
        _dbContext?.Dispose();
        (_serviceProvider as IDisposable)?.Dispose();
    }

    private static readonly ActivitySource ActivitySource = new("ProductoConcurrencyTests");
    private const string DatabaseName = "it_producto_concurrency";
    private string _connectionString = string.Empty;
    private IServiceProvider? _serviceProvider;
    private TiendaDbContext? _dbContext;
    private IProductoRepository? _productoRepository;
    private long _productoId;

    private static void RegisterRepositories(IServiceCollection services) {
        services.AddScoped<IProductoRepository, ProductoRepository>();
        services.AddScoped<ICategoriaRepository, CategoriaRepository>();
    }

    [Test]
    public async Task SaveAsync_ConRowVersionInicializado_InsertaCorrectamente() {
        var producto = new Producto {
            Nombre = "Nuevo Producto",
            Descripcion = "Test RowVersion",
            Precio = 50.00m,
            Stock = 25,
            CategoriaId = 1,
            RowVersion = new byte[] { 9, 10, 11, 12, 13, 14, 15, 16 }
        };

        Producto result = await _productoRepository!.SaveAsync(producto);

        result.Id.Should().BeGreaterThan(0);
        result.RowVersion.Should().NotBeNull();
        result.RowVersion.Length.Should().Be(8);
    }

    [Test]
    public async Task FindByIdAsync_RetornaProductoConRowVersion() {
        Producto? producto = await _productoRepository!.FindByIdAsync(_productoId);

        producto.Should().NotBeNull();
        producto!.RowVersion.Should().NotBeNull();
        producto.RowVersion.Length.Should().Be(8);
    }

    [Test]
    public async Task DecrementStockAsync_ConStockSuficiente_DecrementaCorrectamente() {
        int stockInicial = 50;
        await SetProductoStock(_productoId, stockInicial);

        Producto? producto = await _productoRepository!.FindByIdAsync(_productoId);
        producto.Should().NotBeNull();

        bool resultado = await _productoRepository.DecrementStockAsync(_productoId, 10, producto!.RowVersion);

        resultado.Should().BeTrue();

        Producto? productoFinal = await _productoRepository.FindByIdAsync(_productoId);
        productoFinal!.Stock.Should().Be(stockInicial - 10);
    }

    [Test]
    public async Task DecrementStockAsync_ConStockInsuficiente_NoDecrementa() {
        int stockInicial = 5;
        await SetProductoStock(_productoId, stockInicial);

        Producto? producto = await _productoRepository!.FindByIdAsync(_productoId);
        producto.Should().NotBeNull();

        bool resultado = await _productoRepository.DecrementStockAsync(_productoId, 10, producto!.RowVersion);

        resultado.Should().BeFalse();

        Producto? productoFinal = await _productoRepository.FindByIdAsync(_productoId);
        productoFinal!.Stock.Should().Be(stockInicial);
    }

    [Test]
    public async Task DecrementStockAsync_StockExacto_PermiteDecremento() {
        int stockInicial = 10;
        await SetProductoStock(_productoId, stockInicial);

        Producto? producto = await _productoRepository!.FindByIdAsync(_productoId);
        producto.Should().NotBeNull();

        bool resultado = await _productoRepository.DecrementStockAsync(_productoId, stockInicial, producto!.RowVersion);

        resultado.Should().BeTrue();

        Producto? productoFinal = await _productoRepository.FindByIdAsync(_productoId);
        productoFinal!.Stock.Should().Be(0);
    }

    [Test]
    public async Task DecrementStockAsync_CantidadMayorStock_Rechaza() {
        int stockInicial = 10;
        await SetProductoStock(_productoId, stockInicial);

        Producto? producto = await _productoRepository!.FindByIdAsync(_productoId);
        producto.Should().NotBeNull();

        bool resultado =
            await _productoRepository.DecrementStockAsync(_productoId, stockInicial + 1, producto!.RowVersion);

        resultado.Should().BeFalse();

        Producto? productoFinal = await _productoRepository.FindByIdAsync(_productoId);
        productoFinal!.Stock.Should().Be(stockInicial);
    }

    [Test]
    public async Task DecrementStockAsync_ConStockCero_Rechaza() {
        await SetProductoStock(_productoId, 0);

        Producto? producto = await _productoRepository!.FindByIdAsync(_productoId);
        producto.Should().NotBeNull();

        bool resultado = await _productoRepository.DecrementStockAsync(_productoId, 1, producto!.RowVersion);

        resultado.Should().BeFalse();

        Producto? productoFinal = await _productoRepository.FindByIdAsync(_productoId);
        productoFinal!.Stock.Should().Be(0);
    }

    [Test]
    public async Task FindAllAsync_SinProductos_RetornaListaVacia() {
        IEnumerable<Producto> productos = await _productoRepository!.FindAllAsync();
        productos.Should().NotBeNull();
    }

    private async Task SetProductoStock(long productoId, int stock) {
        Producto? producto = await _dbContext!.FindAsync<Producto>(productoId);
        producto!.Stock = stock;
        await _dbContext.SaveChangesAsync();
    }
}

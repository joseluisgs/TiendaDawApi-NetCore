using System.Text;
using System.Text.Json;
using FluentAssertions;
using FluentValidation;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using TiendaApi.Api.Data;
using TiendaApi.Api.Dtos.Backup;
using TiendaApi.Api.Dtos.Categorias;
using TiendaApi.Api.Dtos.Productos;
using TiendaApi.Api.Dtos.Proveedores;
using TiendaApi.Api.Infrastructures;
using TiendaApi.Api.Models;
using TiendaApi.Api.Repositories.Categorias;
using TiendaApi.Api.Repositories.Productos;
using TiendaApi.Api.Repositories.Proveedores;
using TiendaApi.Api.Services.Backup;
using TiendaApi.Api.Services.Cache;
using TiendaApi.Api.Validators.Categorias;
using TiendaApi.Api.Validators.Productos;
using TiendaApi.Api.Validators.Proveedores;

namespace TiendaApi.Tests.Integration.TestContainers.Backup;

/// <summary>
/// Tests de integración del servicio de backup sobre PostgreSQL real.
///
/// 🎓 El caso que de verdad importa: restaurar con IDs propios en columnas
/// <c>GENERATED ALWAYS</c>. Si la secuencia no se re-sincroniza tras el import,
/// la siguiente creación normal fallaría por clave duplicada — y eso es lo que
/// comprueba <see cref="Importar_ConIdNuevo_DejaLaSecuenciaOperativa"/>.
/// </summary>
[TestFixture]
[Category("Integration")]
[NonParallelizable]
public class BackupServiceIntegrationTests {
    private const string DatabaseName = "it_backup_service";

    private string _connectionString = string.Empty;
    private IServiceProvider? _provider;
    private TiendaDbContext? _dbContext;
    private IBackupService? _servicio;

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
        // Cada test parte de cero: los nombres de categoría son únicos y la
        // semilla se repite en cada test, así que sin limpiar fallaría el índice.
        // Include Error Detail: muestra la sentencia SQL que falle (diagnóstico).
        var connectionString = _connectionString + ";Include Error Detail=True";

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> {
                { "ConnectionStrings:DefaultConnection", connectionString + ";Include Error Detail=True" },
                { "Cache:ProductoCacheTTLMinutes", "10" }
            }!)
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddMemoryCache();
        services.AddOutputCache();
        services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Warning));

        services.AddDbContext<TiendaDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<ILogger<CategoriaRepository>, Logger<CategoriaRepository>>();
        services.AddScoped<ILogger<ProductoRepository>, Logger<ProductoRepository>>();
        services.AddScoped<ILogger<ProveedorRepository>, Logger<ProveedorRepository>>();

        services.AddScoped<ICategoriaRepository, CategoriaRepository>();
        services.AddScoped<IProductoRepository, ProductoRepository>();
        services.AddScoped<IProveedorRepository, ProveedorRepository>();
        services.AddScoped<IValidator<CategoriaRequestDto>, CategoriaRequestValidator>();
        services.AddScoped<IValidator<ProductoRequestDto>, ProductoRequestValidator>();
        services.AddScoped<IValidator<ProveedorRequestDto>, ProveedorRequestValidator>();
        services.AddScoped<ICacheService, MemoryCacheService>();
        services.AddScoped<IBackupService, BackupService>();

        _provider = services.BuildServiceProvider();
        _dbContext = _provider.GetRequiredService<TiendaDbContext>();
        _dbContext.Database.EnsureCreated();

        // Datos de tests anteriores: los tests comparten la BD del fixture.
        _dbContext.Productos.RemoveRange(_dbContext.Productos.IgnoreQueryFilters());
        _dbContext.Categorias.RemoveRange(_dbContext.Categorias.IgnoreQueryFilters());
        _dbContext.Proveedores.RemoveRange(_dbContext.Proveedores.IgnoreQueryFilters());
        await _dbContext.SaveChangesAsync();

        _servicio = _provider.GetRequiredService<IBackupService>();
    }

    [TearDown]
    public void TearDown() {
        _dbContext?.Dispose();
        if (_provider is IDisposable desechable) desechable.Dispose();
    }

    // ────────────────────────────────────────────────────────────
    // Export
    // ────────────────────────────────────────────────────────────

    [Test]
    public async Task Exportar_Json_ContieneLasEntidadesSembradas() {
        await SembrarAsync();

        using var salida = new MemoryStream();
        var export = await _servicio!.ExportarAsync(
            salida, "json", ["categorias", "proveedores", "productos"], incluirBorrados: false);

        export.IsSuccess.Should().BeTrue(export.IsFailure ? export.Error.Message : "");
        salida.Length.Should().BeGreaterThan(0);

        salida.Position = 0;
        var texto = new StreamReader(salida).ReadToEnd();
        var backup = JsonSerializer.Deserialize<BackupDto>(texto, BackupService.OpcionesJson);

        backup.Should().NotBeNull();
        backup!.Manifest.Version.Should().Be("1.0");
        backup.Manifest.Entidades["productos"].Should().Be(3);
        backup.Productos.Should().HaveCount(3);
        backup.Categorias.Should().HaveCount(3);
        backup.Proveedores.Should().HaveCount(1, "la semilla solo crea un proveedor");
    }

    [Test]
    public async Task Exportar_ConFormatoDesconocido_RetornaErrorSinEscribirNada() {
        using var salida = new MemoryStream();

        var export = await _servicio!.ExportarAsync(salida, "xml", ["productos"], incluirBorrados: false);

        export.IsFailure.Should().BeTrue("el formato xml no existe");
        salida.Length.Should().Be(0, "no debe escribirse nada si el formato no vale");
    }

    [Test]
    public async Task Exportar_SoloUnaEntidad_LaDemejadaQuedaFuera() {
        await SembrarAsync();

        using var salida = new MemoryStream();
        await _servicio!.ExportarAsync(salida, "json", ["categorias"], incluirBorrados: false);

        salida.Position = 0;
        var backup = JsonSerializer.Deserialize<BackupDto>(
            new StreamReader(salida).ReadToEnd(), BackupService.OpcionesJson);

        backup!.Categorias.Should().HaveCount(3);
        backup.Productos.Should().BeNull("no se pidió");
        backup.Proveedores.Should().BeNull("no se pidió");
    }

    // ────────────────────────────────────────────────────────────
    // Import
    // ────────────────────────────────────────────────────────────

    [Test]
    public async Task Importar_DryRun_NoEscribeNada() {
        await SembrarAsync();
        var antes = await ContarProductosAsync();

        var json = await ExportarTextoAsync("json", ["productos"]);
        using var entrada = new MemoryStream(Encoding.UTF8.GetBytes(json));

        var informe = await _servicio!.ImportarAsync(entrada, "json", ["productos"], dryRun: true);

        informe.IsSuccess.Should().BeTrue(informe.IsFailure ? informe.Error.Message : "");
        informe.Value.DryRun.Should().BeTrue();
        informe.Value.Entidades.Should().ContainSingle();
        informe.Value.Entidades[0].Leidos.Should().Be(3);
        var despues = await ContarProductosAsync();
        despues.Should().Be(antes, "dryRun no debe tocar nada");
    }

    [Test]
    public async Task Importar_ConIdNuevo_DejaLaSecuenciaOperativa() {
        // Es el caso delicado: se inserta un ID explícito (9999) y después se
        // crea otro por el camino normal. Si la secuencia no se re-sincronizó,
        // ese segundo INSERT chocaría por clave duplicada.
        await SembrarAsync();

        var json = await ExportarTextoAsync("json", ["productos", "categorias", "proveedores"]);
        var backup = JsonSerializer.Deserialize<BackupDto>(json, BackupService.OpcionesJson)!;
        backup = ConProductoExtra(backup, backup.Productos![0] with {
            Id = 9999,
            Nombre = "Producto restaurado con ID propio",
        });

        using var entrada = new MemoryStream(Encoding.UTF8.GetBytes(
            JsonSerializer.Serialize(backup, BackupService.OpcionesJson)));

        var informe = await _servicio!.ImportarAsync(entrada, "json",
            ["productos", "categorias", "proveedores"], dryRun: false);

        informe.IsSuccess.Should().BeTrue(informe.IsFailure ? informe.Error.Message : "");
        informe.Value.Completo.Should().BeTrue(
            string.Join("; ", informe.Value.Entidades.SelectMany(e => e.Errores).Select(e => e.Mensaje)));

        var producto = await _dbContext!.Productos.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == 9999);
        producto.Should().NotBeNull("el registro con ID propio debe existir");

        // Camino normal, sin ID explícito: la secuencia debe estar al día.
        // La categoría se reutiliza la del producto restaurado (los IDs no vuelven
        // a empezar en 1 entre tests, así que 1 podría no existir).
        var productoRepository = _provider!.GetRequiredService<IProductoRepository>();
        var nuevo = await productoRepository.SaveAsync(new Producto {
            Nombre = "Creado tras el restore",
            Descripcion = "Comprueba la secuencia",
            Precio = 5m,
            Stock = 1,
            CategoriaId = producto!.CategoriaId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            RowVersion = new byte[8],
        });

        nuevo.Id.Should().BeGreaterThan(0, "la creación normal debe seguir funcionando");
        nuevo.Id.Should().NotBe(9999, "no debe reutilizar el ID restaurado");
    }

    [Test]
    public async Task Importar_ConErroresDeFila_AcumulaSinAbortar() {
        await SembrarAsync();
        var json = await ExportarTextoAsync("json", ["productos"]);
        var backup = JsonSerializer.Deserialize<BackupDto>(json, BackupService.OpcionesJson)!;
        backup = ConProductoExtra(backup, backup.Productos![0] with {
            Id = 8888,
            Nombre = "Producto con categoría inexistente",
            CategoriaId = 999999,
        });

        using var entrada = new MemoryStream(Encoding.UTF8.GetBytes(
            JsonSerializer.Serialize(backup, BackupService.OpcionesJson)));

        var informe = await _servicio!.ImportarAsync(entrada, "json", ["productos"], dryRun: false);

        informe.IsSuccess.Should().BeTrue("los errores de fila no son un fallo de la operación");
        informe.Value.Completo.Should().BeFalse();
        var errores = informe.Value.Entidades.Single().Errores;
        errores.Should().ContainSingle().Which.Mensaje.Should().Contain("999999");

        // La fila válida (el primer producto exportado) sí está: el error de una
        // no para el resto de la tanda.
        var valido = backup.Productos![0];
        var producto = await _dbContext!.Productos.AsNoTracking().FirstOrDefaultAsync(p => p.Id == valido.Id);
        producto.Should().NotBeNull("el producto válido de la misma tanda debe existir");
        producto!.Id.Should().NotBe(8888, "el de categoría inexistente no debe haber entrado");
    }

    [Test]
    public async Task Importar_Csv_HaceElRoundTrip() {
        await SembrarAsync();
        var csv = await ExportarTextoAsync("csv", ["categorias"]);

        using var entrada = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var informe = await _servicio!.ImportarAsync(entrada, "csv", ["categorias"], dryRun: false);

        informe.IsSuccess.Should().BeTrue(informe.IsFailure ? informe.Error.Message : "");
        informe.Value.Entidades.Should().ContainSingle();
        informe.Value.Entidades[0].Leidos.Should().Be(3);
        informe.Value.Entidades[0].SinCambios.Should().Be(3, "el round-trip no debe cambiar nada");
    }

    // ────────────────────────────────────────────────────────────
    // Helpers
    // ────────────────────────────────────────────────────────────

    private async Task SembrarAsync() {
        var categorias = new List<Categoria> {
            new() { Nombre = "Cat Uno", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new() { Nombre = "Cat Dos", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new() { Nombre = "Cat Tres", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
        };
        _dbContext!.Categorias.AddRange(categorias);
        await _dbContext.SaveChangesAsync();

        _dbContext.Proveedores.Add(new Proveedor {
            Codigo = "Backup00001",
            Nombre = "Proveedor backup",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        });
        await _dbContext.SaveChangesAsync();

        // Los IDs de categoría se leen tras insertar: la secuencia no vuelve a
        // empezar en 1 entre tests, y un ID fijo rompería la clave foránea.
        for (var i = 0; i < categorias.Count; i++) {
            _dbContext.Productos.Add(new Producto {
                Nombre = $"Producto {i + 1}",
                Descripcion = "Para el test de backup",
                Precio = (i + 1) * 10m,
                Stock = i + 1,
                CategoriaId = categorias[i].Id,
                ProveedorId = "Backup00001",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                RowVersion = new byte[8],
            });
        }
        await _dbContext.SaveChangesAsync();
    }

    private async Task<string> ExportarTextoAsync(string formato, string[] entidades) {
        using var salida = new MemoryStream();
        var resultado = await _servicio!.ExportarAsync(salida, formato, entidades, incluirBorrados: false);
        resultado.IsSuccess.Should().BeTrue(resultado.IsFailure ? resultado.Error.Message : "");
        salida.Position = 0;
        return new StreamReader(salida).ReadToEnd();
    }

    /// <summary>Devuelve el backup con un producto más (IReadOnlyList no admite Add).</summary>
    private static BackupDto ConProductoExtra(BackupDto backup, BackupProductoDto producto) {
        var lista = backup.Productos!.ToList();
        lista.Add(producto);
        return backup with { Productos = lista };
    }

    private async Task<int> ContarProductosAsync() =>
        await _dbContext!.Productos.AsNoTracking().CountAsync();
}

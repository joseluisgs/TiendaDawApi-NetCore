using Cassandra;
using FluentAssertions;
using TiendaApi.Api.Models;
using TiendaApi.Api.Repositories.Historial;
using Path = System.IO.Path;

namespace TiendaApi.Tests.Integration.TestContainers.Historial;

/// <summary>
/// Integración de la pila de series contra Cassandra REAL (Testcontainers,
/// el mismo esquema de producción creado por InitializeSchemaAsync). Sin mocks:
/// se verifican las 5 garantías del diseño de la pizarra.
/// </summary>
public class CassandraHistorialRepositoryTests
{
    // IDs y usuarios EXCLUSIVOS de estos tests: no pisan a los seeds ni a otros
    // tests dentro del contenedor compartido.
    private const long ProductoSerie = 9101;
    private const long ProductoDual = 9102;
    private const long UsuarioFeed = 9910;

    private static ISession _session = null!;
    private static CassandraHistorialRepository _repositorio = null!;

    [OneTimeSetUp]
    public static async Task OneTimeSetUp()
    {
        _session = await AssemblyContainerFixture.GetCassandraSessionAsync();
        _repositorio = new CassandraHistorialRepository(_session);
    }

    [SetUp]
    public async Task SetUp()
    {
        // Particiones limpias por test: idempotencia dentro de la suite.
        await _session.ExecuteAsync(new SimpleStatement(
            "DELETE FROM historial_precios_por_producto WHERE producto_id = ?", ProductoSerie));
        await _session.ExecuteAsync(new SimpleStatement(
            "DELETE FROM historial_precios_por_producto WHERE producto_id = ?", ProductoDual));
        await _session.ExecuteAsync(new SimpleStatement(
            "DELETE FROM cambios_precio_por_fecha WHERE fecha = ?", HoyUtc()));
        await _session.ExecuteAsync(new SimpleStatement(
            "DELETE FROM vistas_por_usuario WHERE usuario_id = ?", UsuarioFeed));
    }

    [Test]
    public async Task GetHistorialPrecios_ParticionVacia_DevuelveListaVacia_NoError()
    {
        var serie = await _repositorio.GetHistorialPreciosAsync(ProductoSerie, null, null, 50);

        // Una partición que nunca se escribió no es un error: es una lista vacía.
        serie.Should().BeEmpty();
    }

    [Test]
    public async Task GetFeedVistas_ParticionVacia_DevuelveListaVacia_NoError()
    {
        var feed = await _repositorio.GetFeedVistasAsync(UsuarioFeed, 20);

        feed.Should().BeEmpty();
    }

    [Test]
    public async Task RegistrarPrecio_EscribeEnLasDosTablas_QA_Y_QC()
    {
        await _repositorio.RegistrarPrecioAsync(ProductoDual, 1299.99);

        // Q-A: el punto aparece en la serie del producto.
        var serie = await _repositorio.GetHistorialPreciosAsync(ProductoDual, null, null, 10);
        serie.Should().ContainSingle(p => p.Precio == 1299.99);

        // Q-C: la MISMA escritura también cayó en el bucket del día. Dos INSERT
        // independientes SIN transacción repartida (diseño asumido en T9).
        // Materializamos UNA vez: el RowSet del driver no re-itera bien y
        // enumerarlo dos veces (HaveCount + First) dejaria la 2ª vacía.
        var filas = (await _session.ExecuteAsync(new SimpleStatement(
            "SELECT precio FROM cambios_precio_por_fecha WHERE fecha = ? AND producto_id = ?",
            HoyUtc(), ProductoDual))).ToList();
        filas.Should().HaveCount(1);
        filas[0].GetValue<double>("precio").Should().Be(1299.99);
    }

    [Test]
    public async Task GetHistorialPrecios_OrdenDescPorClustering_SinOrderBy()
    {
        // Tres puntos con fechas fijas: el orden DEBE salir solo de la tabla.
        await InsertarPuntoAsync(ProductoSerie, DateTime.UtcNow.AddDays(-30), 100.0);
        await InsertarPuntoAsync(ProductoSerie, DateTime.UtcNow.AddDays(-10), 200.0);
        await InsertarPuntoAsync(ProductoSerie, DateTime.UtcNow.AddDays(-1), 300.0);

        var serie = await _repositorio.GetHistorialPreciosAsync(ProductoSerie, null, null, 50);

        serie.Should().HaveCount(3);
        // Mas reciente primero SIN ORDER BY en el SELECT: lo garantiza la
        // CLUSTERING KEY (fecha DESC) declarada en el esquema (T8).
        serie.Select(p => p.Precio).Should().Equal(300.0, 200.0, 100.0);
        serie.Select(p => p.Fecha).Should().BeInDescendingOrder();

        // Bonus: rango sobre la clustering key (desde/hasta) = solo el punto del
        // medio, sin ALLOW FILTERING y sin tocar otras particiones.
        var desde = DateTime.UtcNow.AddDays(-20);
        var hasta = DateTime.UtcNow.AddDays(-5);
        var rango = await _repositorio.GetHistorialPreciosAsync(ProductoSerie, desde, hasta, 50);
        rango.Should().ContainSingle().Which.Precio.Should().Be(200.0);
    }

    [Test]
    public async Task RegistrarVista_DenormalizaElNombre_Y_AplicaElTtlDe180Dias()
    {
        await _repositorio.RegistrarVistaAsync(UsuarioFeed, ProductoDual, "Laptop de prueba");

        // Q-B: la fila trae el nombre COPIADO (el feed se pinta sin cruces).
        var feed = await _repositorio.GetFeedVistasAsync(UsuarioFeed, 20);
        feed.Should().ContainSingle();
        feed[0].Nombre.Should().Be("Laptop de prueba");
        feed[0].ProductoId.Should().Be(ProductoDual);

        // TTL nativo de la tabla (default_time_to_live = 180 dias): la columna no
        // primaria expira sola; recien escrita debe rondar el maximo de segundos.
        var fila = (await _session.ExecuteAsync(new SimpleStatement(
            "SELECT ttl(nombre) AS ttl FROM vistas_por_usuario WHERE usuario_id = ? LIMIT 1",
            UsuarioFeed))).First();
        var ttl = fila.GetValue<int?>("ttl");
        ttl.Should().NotBeNull("la columna del feed debe llevar TTL de tabla");
        ttl.Should().BeGreaterThan(0);
        ttl.Should().BeLessThanOrEqualTo(180 * 24 * 60 * 60);
    }

    [Test]
    public void CodigoDeLasSeries_SinAllowFiltering()
    {
        var api = EncontrarRaizDelApi();
        var carpetas = new[]
        {
            Path.Combine(api, "Repositories", "Historial"),
            Path.Combine(api, "Services", "Historial"),
            Path.Combine(api, "Data", "Seed", "Cassandra")
        };

        var ficheros = carpetas
            .Where(Directory.Exists)
            .SelectMany(d => Directory.EnumerateFiles(d, "*.cs", SearchOption.AllDirectories))
            .ToList();

        ficheros.Should().NotBeEmpty("deben existir los ficheros de series del proyecto Api");

        foreach (var fichero in ficheros)
        {
            // Anti-patron T5.2.9: ALLOW FILTERING escanea la tabla entera; el
            // diseno por particiones no debe necesitarlo jamas.
            File.ReadAllText(fichero).ToUpperInvariant().Should().NotContain("ALLOW FILTERING",
                $"el fichero {Path.GetFileName(fichero)} no debe depender de filtros en el servidor");
        }
    }

    private async Task InsertarPuntoAsync(long productoId, DateTime fecha, double precio)
        => await _session.ExecuteAsync(new SimpleStatement(
            "INSERT INTO historial_precios_por_producto (producto_id, fecha, precio) VALUES (?, ?, ?)",
            productoId, fecha, precio));

    /// <summary>
    /// LocalDate del día de hoy (UTC), al ESTILO del repositorio: el driver
    /// 3.23 no expone LocalDate.FromDateTime, se construye con los tres números.
    /// </summary>
    private static LocalDate HoyUtc()
    {
        var hoy = DateTime.UtcNow;
        return new LocalDate(hoy.Year, hoy.Month, hoy.Day);
    }

    private static string EncontrarRaizDelApi()
    {
        // Desde bin/Debug/net10.0 caminamos hacia arriba hasta el csproj del Api.
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var csproj = Path.Combine(dir.FullName, "TiendaApi.Api", "TiendaApi.csproj");
            if (File.Exists(csproj))
                return Path.Combine(dir.FullName, "TiendaApi.Api");

            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException(
            "No se localizo TiendaApi.Api caminando desde " + AppContext.BaseDirectory);
    }
}

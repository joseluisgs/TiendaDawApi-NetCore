using FluentAssertions;
using Moq;
using TiendaApi.Api.Dtos.Historial;
using TiendaApi.Api.Errors;
using TiendaApi.Api.Errors.Productos;
using TiendaApi.Api.Models;
using TiendaApi.Api.Repositories.Historial;
using TiendaApi.Api.Repositories.Productos;
using TiendaApi.Api.Services.Historial;

namespace TiendaApi.Tests.Unit.Services;

/// <summary>
/// Tests unitarios de HistorialService con los DOS repositorios mockeados
/// (PostgreSQL = existencia, Cassandra = series). La leccion clave: si
/// Cassandra falla, el servicio NO lanza — fail-open (lecturas en vacio,
/// escritura en warning), igual que el resto del cableado T10.
/// </summary>
public class HistorialServiceTests
{
    // NUnit reutiliza la instancia del fixture entre tests: los mocks se
    // rehacen en [SetUp] para que las verificaciones (Times.Never) solo
    // vean las invocaciones del test en curso.
    private Mock<IProductoRepository> _productoRepository = null!;
    private Mock<IHistorialRepository> _historialRepository = null!;
    private HistorialService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _productoRepository = new Mock<IProductoRepository>();
        _historialRepository = new Mock<IHistorialRepository>();
        _service = new HistorialService(
            _productoRepository.Object,
            _historialRepository.Object,
            Mock.Of<Microsoft.Extensions.Logging.ILogger<HistorialService>>());
    }

    // ===================== GetHistorialPreciosAsync =====================

    [Test]
    public async Task GetHistorialPrecios_ProductoNoExiste_DevuelveNotFoundDePostgres()
    {
        _productoRepository.Setup(r => r.FindByIdAsync(999)).ReturnsAsync((Producto?)null);

        var result = await _service.GetHistorialPreciosAsync(999, null, null, 50);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<NotFoundError>();
        result.Error.Message.Should().Contain("999");
        // La fuente de verdad no certifica el producto: ni se toca Cassandra.
        _historialRepository.Verify(
            h => h.GetHistorialPreciosAsync(It.IsAny<long>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<int>()),
            Times.Never);
    }

    [Test]
    public async Task GetHistorialPrecios_RangoInvertido_DevuelveValidationErrorDeDesde()
    {
        _productoRepository.Setup(r => r.FindByIdAsync(1)).ReturnsAsync(new Producto { Id = 1, Nombre = "Laptop" });

        var result = await _service.GetHistorialPreciosAsync(
            1, new DateTime(2026, 10, 10), new DateTime(2026, 9, 1), 50);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<ValidationError>();
        var validation = (ValidationError)result.Error;
        validation.ValidationErrors.Should().ContainKey("desde");
    }

    [Test]
    public async Task GetHistorialPrecios_SerieOk_DevuelveLaSerieDelRepositorio()
    {
        _productoRepository.Setup(r => r.FindByIdAsync(1)).ReturnsAsync(new Producto { Id = 1, Nombre = "Laptop" });
        var serie = new List<HistorialPrecioDto>
        {
            new(DateTime.UtcNow.AddDays(-1), 1299.99),
            new(DateTime.UtcNow.AddDays(-10), 1349.99)
        };
        _historialRepository.Setup(h => h.GetHistorialPreciosAsync(1, null, null, 50))
            .ReturnsAsync(serie);

        var result = await _service.GetHistorialPreciosAsync(1, null, null, 50);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(serie);
    }

    [Test]
    public async Task GetHistorialPrecios_LimitCero_SeAcotaA1()
    {
        _productoRepository.Setup(r => r.FindByIdAsync(1)).ReturnsAsync(new Producto { Id = 1, Nombre = "Laptop" });
        _historialRepository.Setup(h => h.GetHistorialPreciosAsync(1, null, null, 1))
            .ReturnsAsync(new List<HistorialPrecioDto>());

        await _service.GetHistorialPreciosAsync(1, null, null, 0);

        // Math.Clamp(0, 1, 200) = 1: el limit lo acota la fachada, no el controller.
        _historialRepository.Verify(h => h.GetHistorialPreciosAsync(1, null, null, 1), Times.Once);
    }

    [Test]
    public async Task GetHistorialPrecios_LimitExcesivo_SeAcotaA200()
    {
        _productoRepository.Setup(r => r.FindByIdAsync(1)).ReturnsAsync(new Producto { Id = 1, Nombre = "Laptop" });
        _historialRepository.Setup(h => h.GetHistorialPreciosAsync(1, null, null, 200))
            .ReturnsAsync(new List<HistorialPrecioDto>());

        await _service.GetHistorialPreciosAsync(1, null, null, 9999);

        _historialRepository.Verify(h => h.GetHistorialPreciosAsync(1, null, null, 200), Times.Once);
    }

    [Test]
    public async Task GetHistorialPrecios_CassandraCaida_NoLanza_Y_DevuelveVacia()
    {
        _productoRepository.Setup(r => r.FindByIdAsync(1)).ReturnsAsync(new Producto { Id = 1, Nombre = "Laptop" });
        _historialRepository.Setup(h => h.GetHistorialPreciosAsync(It.IsAny<long>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<int>()))
            .ThrowsAsync(new InvalidOperationException("No hay contact points"));

        var act = async () => await _service.GetHistorialPreciosAsync(1, null, null, 50);
        var result = await act.Should().NotThrowAsync();

        result.Which.IsSuccess.Should().BeTrue();
        result.Which.Value.Should().BeEmpty();
    }

    // ====================== RegistrarVistaAsync =======================

    [Test]
    public async Task RegistrarVista_ProductoNoExiste_DevuelveNotFoundDePostgres()
    {
        _productoRepository.Setup(r => r.FindByIdAsync(999)).ReturnsAsync((Producto?)null);

        var result = await _service.RegistrarVistaAsync(2, 999);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<NotFoundError>();
        _historialRepository.Verify(
            h => h.RegistrarVistaAsync(It.IsAny<long>(), It.IsAny<long>(), It.IsAny<string>()),
            Times.Never);
    }

    [Test]
    public async Task RegistrarVista_RegistroOk_DevuelveSuccess_Y_DenormalizaElNombre()
    {
        _productoRepository.Setup(r => r.FindByIdAsync(1))
            .ReturnsAsync(new Producto { Id = 1, Nombre = "Laptop Dell XPS 15" });
        _historialRepository.Setup(h => h.RegistrarVistaAsync(2, 1, "Laptop Dell XPS 15"))
            .Returns(Task.CompletedTask);

        var result = await _service.RegistrarVistaAsync(2, 1);

        result.IsSuccess.Should().BeTrue();
        // El nombre viaja COPIADO a la fila (el feed no hara cruces).
        _historialRepository.Verify(h => h.RegistrarVistaAsync(2, 1, "Laptop Dell XPS 15"), Times.Once);
    }

    [Test]
    public async Task RegistrarVista_CassandraCaida_NoLanza_DevuelveSuccess()
    {
        _productoRepository.Setup(r => r.FindByIdAsync(1))
            .ReturnsAsync(new Producto { Id = 1, Nombre = "Laptop" });
        _historialRepository.Setup(h => h.RegistrarVistaAsync(It.IsAny<long>(), It.IsAny<long>(), It.IsAny<string>()))
            .ThrowsAsync(new InvalidOperationException("Nodo caido"));

        var act = async () => await _service.RegistrarVistaAsync(2, 1);
        var result = await act.Should().NotThrowAsync();

        // Fail-open de escritura: el POST /vista responde 204 aunque Cassandra no este.
        result.Which.IsSuccess.Should().BeTrue();
    }

    // ======================= GetFeedVistasAsync =======================

    [Test]
    public async Task GetFeedVistas_FeedOk_DevuelveLaListaDelRepositorio()
    {
        var feed = new List<VistaFeedDto>
        {
            new(DateTime.UtcNow.AddHours(-1), 1, "Laptop Dell XPS 15"),
            new(DateTime.UtcNow.AddHours(-4), 3, "Clean Code")
        };
        _historialRepository.Setup(h => h.GetFeedVistasAsync(2, 20)).ReturnsAsync(feed);

        var result = await _service.GetFeedVistasAsync(2, 20);

        result.Should().BeEquivalentTo(feed);
    }

    [Test]
    public async Task GetFeedVistas_LimitSeAcotaA100()
    {
        _historialRepository.Setup(h => h.GetFeedVistasAsync(2, 100))
            .ReturnsAsync(new List<VistaFeedDto>());

        await _service.GetFeedVistasAsync(2, 9999);

        _historialRepository.Verify(h => h.GetFeedVistasAsync(2, 100), Times.Once);
    }

    [Test]
    public async Task GetFeedVistas_CassandraCaida_NoLanza_DevuelveVacia()
    {
        _historialRepository.Setup(h => h.GetFeedVistasAsync(It.IsAny<long>(), It.IsAny<int>()))
            .ThrowsAsync(new InvalidOperationException("Nodo caido"));

        var act = async () => await _service.GetFeedVistasAsync(2, 20);
        var result = await act.Should().NotThrowAsync();

        // Particion vacia = lista vacia; el modo degradado tambien.
        result.Which.Should().BeEmpty();
    }
}

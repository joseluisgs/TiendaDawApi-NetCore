using CSharpFunctionalExtensions;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using TiendaApi.Api.Controllers;
using TiendaApi.Api.Dtos.Estadisticas;
using TiendaApi.Api.Errors;
using TiendaApi.Api.Services.Estadisticas;

namespace TiendaApi.Tests.Unit.Controllers;

/// <summary>
/// Tests unitarios de EstadisticasController: comprueba que el controlador es
/// delgado y que reparte correctamente éxito (200) y error de dominio.
/// </summary>
[TestFixture]
public class EstadisticasControllerTests
{
    private readonly Mock<IEstadisticasService> _mockService = new();
    private readonly EstadisticasController _controller;

    public EstadisticasControllerTests()
    {
        _mockService.Setup(s => s.ResumenAsync(It.IsAny<DateTime?>(), It.IsAny<DateTime?>()))
            .ReturnsAsync(Result.Success<ResumenEstadisticasDto, DomainError>(
                new ResumenEstadisticasDto(
                    new PeriodoDto(new DateTime(2026, 1, 1), new DateTime(2026, 11, 1), 305),
                    new ResumenVentasDto(0, 0, 0, 0, 0, 0, 0),
                    new Dictionary<string, int>(),
                    new ResumenProductosDto(0, 0, 0, 0, 0, 0, 0, 0, 0),
                    new ResumenCategoriasDto(0, 0),
                    new ResumenUsuariosDto(0, 0, 0),
                    null)));

        _mockService.Setup(s => s.VentasAsync(It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<string>()))
            .ReturnsAsync(Result.Failure<VentasSerieDto, DomainError>(
                ValidationError.Create("La agrupación 'trimestre' no es válida: usa dia, semana o mes")));

        _controller = new EstadisticasController(
            _mockService.Object, Mock.Of<ILogger<EstadisticasController>>())
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() },
        };
    }

    [Test]
    public async Task Resumen_ConResultadoOk_Retorna200ConElResumen()
    {
        var resultado = await _controller.Resumen(null, null);

        // El éxito viaja en Value (la conversión implícita crea ActionResult<T> con
        // el valor dentro); Result queda vacío.
        resultado.Result.Should().BeNull("con éxito no hay resultado de error");
        resultado.Value.Should().BeOfType<ResumenEstadisticasDto>();
        _mockService.Verify(s => s.ResumenAsync(null, null), Times.Once);
    }

    [Test]
    public async Task Resumen_ConErrorDeValidacion_Retorna400()
    {
        // Se distingue por los argumentos: si se redefiniera el Setup general,
        // el test de éxito recibiría este fallo (NUnit comparte la instancia).
        var desde = new DateTime(2026, 12, 31);
        _mockService.Setup(s => s.ResumenAsync(desde, It.IsAny<DateTime?>()))
            .ReturnsAsync(Result.Failure<ResumenEstadisticasDto, DomainError>(
                ValidationError.Create("La fecha desde no puede ser posterior a hasta")));

        var resultado = await _controller.Resumen(desde, null);

        resultado.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Test]
    public async Task Ventas_ConAgrupacionInvalida_Retorna400()
    {
        var resultado = await _controller.Ventas(null, null, "trimestre");

        resultado.Result.Should().BeOfType<BadRequestObjectResult>();
        _mockService.Verify(s => s.VentasAsync(null, null, "trimestre"), Times.Once);
    }

    [Test]
    public async Task ProductosMasVendidos_PasaLosParametrosAlServicio()
    {
        _mockService.Setup(s => s.ProductosMasVendidosAsync(
                It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync(Result.Success<ProductosMasVendidosDto, DomainError>(
                new ProductosMasVendidosDto(
                    new PeriodoDto(null, null, 0), "recaudacion", [], new TotalesDto(0, 0, 0, 0))));

        await _controller.ProductosMasVendidos(null, null, orden: "recaudacion", top: 5);

        _mockService.Verify(s => s.ProductosMasVendidosAsync(null, null, "recaudacion", 5), Times.Once);
    }

    [Test]
    public async Task Precios_ConSerieNoEncontrada_Retorna404()
    {
        _mockService.Setup(s => s.PreciosAsync(It.IsAny<long>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>()))
            .ReturnsAsync(Result.Failure<PreciosEstadisticasDto, DomainError>(
                NotFoundError.FromId(999, "Producto")));

        var resultado = await _controller.Precios(999, null, null);

        resultado.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Test]
    public async Task Inventario_PasaElUmbralDeStockAlServicio()
    {
        _mockService.Setup(s => s.InventarioAsync(It.IsAny<int>()))
            .ReturnsAsync(Result.Success<InventarioDto, DomainError>(
                new InventarioDto(8, new InventarioResumenDto(0, 0, 0, 0, 0, 0, 0), [], [], [])));

        await _controller.Inventario(stockMax: 8);

        _mockService.Verify(s => s.InventarioAsync(8), Times.Once);
    }
}

using ClientBlazor.Cliente.Clients;
using ClientBlazor.Cliente.Domain.Errors;
using ClientBlazor.Cliente.DTOs.Common;
using ClientBlazor.Cliente.DTOs.Productos;
using ClientBlazor.Cliente.Services.Rest;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using Refit;
using System.Net;

namespace ClientBlazor.Tests.Services;

/// <summary>
/// Pruebas unitarias para el servicio REST principal.
/// Objetivo: Validar la orquestación de operaciones CRUD y la gestión de respuestas HTTP.
/// </summary>
[TestFixture]
public class RestServiceTests
{
    private Mock<ITiendaRestClient> _clientMock = null!;
    private ClientBlazor.Cliente.Services.Rest.RestService _restService = null!;

    /// <summary>
    /// Configura el entorno de aislamiento para probar la lógica del servicio REST.
    /// </summary>
    [SetUp]
    public void Setup()
    {
        _clientMock = new Mock<ITiendaRestClient>();
        _restService = new ClientBlazor.Cliente.Services.Rest.RestService(_clientMock.Object);
    }

    /// <summary>
    /// Comprueba que el servicio devuelva con éxito el listado de productos cuando la API responde correctamente.
    /// </summary>
    [Test]
    public async Task GetProductosAsync_Should_Return_Success_When_Api_Returns_Data()
    {
        // Arrange
        var pagedResult = new PagedResult<ProductoDto>
        {
            Items = new List<ProductoDto>(),
            TotalCount = 0,
            Page = 0,
            PageSize = 10
        };
        _clientMock.Setup(c => c.GetProductosAsync(It.IsAny<ProductoFilterDto>()))
                   .ReturnsAsync(pagedResult);

        // Act
        var result = await _restService.GetProductosAsync(new ProductoFilterDto());

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(pagedResult);
    }

    /// <summary>
    /// Verifica que el servicio devuelva un error de tipo 'NotFound' cuando la API devuelve un código 404.
    /// </summary>
    [Test]
    public async Task GetProductoByIdAsync_Should_Return_NotFound_When_Api_Returns_404()
    {
        // Arrange
        var apiException = await ApiException.Create(
            new HttpRequestMessage(),
            HttpMethod.Get,
            new HttpResponseMessage(HttpStatusCode.NotFound),
            new RefitSettings());

        _clientMock.Setup(c => c.GetProductoByIdAsync(It.IsAny<long>()))
                   .ThrowsAsync(apiException);

        // Act
        var result = await _restService.GetProductoByIdAsync(1);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(NetworkErrors.NotFound.Code);
    }

    /// <summary>
    /// Valida que la creación de un producto sea procesada correctamente por el servicio.
    /// </summary>
    [Test]
    public async Task CreateProductoAsync_Should_Return_Success_When_Created()
    {
        // Arrange
        var request = new ProductoRequestDto { Nombre = "Nuevo" };
        var expectedDto = new ProductoDto { Id = 1, Nombre = "Nuevo" };
        
        _clientMock.Setup(c => c.CreateProductoAsync(It.IsAny<ProductoRequestDto>()))
                   .ReturnsAsync(expectedDto);

        // Act
        var result = await _restService.CreateProductoAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Nombre.Should().Be("Nuevo");
        _clientMock.Verify(c => c.CreateProductoAsync(request), Times.Once);
    }

    /// <summary>
    /// Verifica el borrado de recursos a través del servicio.
    /// </summary>
    [Test]
    public async Task DeleteProductoAsync_Should_Return_Success_When_Deleted()
    {
        // Arrange
        _clientMock.Setup(c => c.DeleteProductoAsync(It.IsAny<long>()))
                   .Returns(Task.CompletedTask);

        // Act
        var result = await _restService.DeleteProductoAsync(1);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
    }

    /// <summary>
    /// Comprueba que cualquier excepción no controlada sea capturada y devuelta como un fallo de conexión.
    /// </summary>
    [Test]
    public async Task AnyMethod_Should_Return_ConnectionFailed_On_Generic_Exception()
    {
        // Arrange
        _clientMock.Setup(c => c.GetProductosAsync(It.IsAny<ProductoFilterDto>()))
                   .ThrowsAsync(new Exception("Network down"));

        // Act
        var result = await _restService.GetProductosAsync(new ProductoFilterDto());

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(NetworkErrors.ConnectionFailed.Code);
    }

    /// <summary>
    /// Valida el listado de categorías a través del servicio REST.
    /// </summary>
    [Test]
    public async Task GetCategoriasAsync_Should_Return_Success_When_Api_Returns_Data()
    {
        // Arrange
        _clientMock.Setup(c => c.GetCategoriasAsync(It.IsAny<ClientBlazor.Cliente.DTOs.Categorias.CategoriaFilterDto>()))
                   .ReturnsAsync(new PagedResult<ClientBlazor.Cliente.DTOs.Categorias.CategoriaDto> { Items = new List<ClientBlazor.Cliente.DTOs.Categorias.CategoriaDto>(), TotalCount = 0, Page = 0, PageSize = 10 });

        // Act
        var result = await _restService.GetCategoriasAsync(new ClientBlazor.Cliente.DTOs.Categorias.CategoriaFilterDto());

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    /// <summary>
    /// Valida la recuperación de una categoría individual.
    /// </summary>
    [Test]
    public async Task GetCategoriaByIdAsync_Should_Return_NotFound_When_Api_Returns_404()
    {
        // Arrange
        var apiException = await ApiException.Create(new HttpRequestMessage(), HttpMethod.Get, new HttpResponseMessage(HttpStatusCode.NotFound), new RefitSettings());
        _clientMock.Setup(c => c.GetCategoriaByIdAsync(It.IsAny<long>())).ThrowsAsync(apiException);

        // Act
        var result = await _restService.GetCategoriaByIdAsync(1);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(NetworkErrors.NotFound.Code);
    }
}
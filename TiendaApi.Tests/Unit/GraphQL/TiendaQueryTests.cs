using FluentAssertions;
using Moq;
using NUnit.Framework;
using TiendaApi.Api.Dtos.Categorias;
using TiendaApi.Api.Dtos.Common;
using TiendaApi.Api.Dtos.Productos;
using TiendaApi.Api.GraphQL.Queries;
using TiendaApi.Api.Models;
using TiendaApi.Api.Repositories.Categorias;
using TiendaApi.Api.Repositories.Productos;

namespace TiendaApi.Tests.Unit.GraphQL;

[TestFixture]
[Category("Unit")]
[Category("GraphQL")]
public class TiendaQueryTests
{
    private Mock<IProductoRepository> _productoRepoMock = null!;
    private Mock<ICategoriaRepository> _categoriaRepoMock = null!;
    private TiendaQuery _query = null!;

    [SetUp]
    public void Setup()
    {
        _productoRepoMock = new Mock<IProductoRepository>();
        _categoriaRepoMock = new Mock<ICategoriaRepository>();
        _query = new TiendaQuery();
    }

    #region GetProductos Tests

    [Test]
    public void GetProductos_RepositoryExists_ReturnsQueryable()
    {
        _productoRepoMock.Setup(r => r.FindAllAsNoTracking())
            .Returns(new List<Producto>().AsQueryable());

        var result = _query.GetProductos(_productoRepoMock.Object);

        result.Should().NotBeNull();
    }

    #endregion

    #region GetProducto Tests

    [Test]
    public async Task GetProducto_WithId_ReturnsProducto()
    {
        var productoId = 1L;
        var producto = new Producto { Id = productoId, Nombre = "Test" };

        _productoRepoMock.Setup(r => r.FindByIdAsync(productoId))
            .ReturnsAsync(producto);

        var result = await _query.GetProducto(productoId, _productoRepoMock.Object);

        result.Should().NotBeNull();
        result!.Id.Should().Be(productoId);
    }

    [Test]
    public async Task GetProducto_WithInvalidId_ReturnsNull()
    {
        _productoRepoMock.Setup(r => r.FindByIdAsync(It.IsAny<long>()))
            .ReturnsAsync((Producto?)null);

        var result = await _query.GetProducto(999, _productoRepoMock.Object);

        result.Should().BeNull();
    }

    #endregion

    #region GetCategorias Tests

    [Test]
    public void GetCategorias_RepositoryExists_ReturnsQueryable()
    {
        _categoriaRepoMock.Setup(r => r.FindAllAsNoTracking())
            .Returns(new List<Categoria>().AsQueryable());

        var result = _query.GetCategorias(_categoriaRepoMock.Object);

        result.Should().NotBeNull();
    }

    #endregion

    #region GetCategoria Tests

    [Test]
    public async Task GetCategoria_WithId_ReturnsCategoria()
    {
        var categoriaId = 1L;
        var categoria = new Categoria { Id = categoriaId, Nombre = "Test" };

        _categoriaRepoMock.Setup(r => r.FindByIdAsync(categoriaId))
            .ReturnsAsync(categoria);

        var result = await _query.GetCategoria(categoriaId, _categoriaRepoMock.Object);

        result.Should().NotBeNull();
        result!.Id.Should().Be(categoriaId);
    }

    [Test]
    public async Task GetCategoria_WithInvalidId_ReturnsNull()
    {
        _categoriaRepoMock.Setup(r => r.FindByIdAsync(It.IsAny<long>()))
            .ReturnsAsync((Categoria?)null);

        var result = await _query.GetCategoria(999, _categoriaRepoMock.Object);

        result.Should().BeNull();
    }

    #endregion

    #region GetProductosPaged Tests

    [Test]
    public async Task GetProductosPaged_WithPage1_ConvertsToZeroBasedFilter()
    {
        // Arrange: capturar el filtro que llega al repositorio
        ProductoFilterDto? capturedFilter = null;
        _productoRepoMock.Setup(r => r.FindAllPagedAsync(It.IsAny<ProductoFilterDto>()))
            .Callback<ProductoFilterDto>(f => capturedFilter = f)
            .ReturnsAsync((new List<Producto>(), 2));

        // Act: GraphQL page=1 (base 1) → el repositorio debe recibir Page=0 (base 0)
        var result = await _query.GetProductosPaged(_productoRepoMock.Object, 1, 10);

        // Assert
        result.Should().NotBeNull();
        result.Page.Should().Be(1); // GraphQL devuelve base 1
        capturedFilter.Should().NotBeNull();
        capturedFilter!.Page.Should().Be(0); // pero el filtro va en base 0
        capturedFilter.Size.Should().Be(10);
    }

    [Test]
    public async Task GetProductosPaged_WithPage5_ConvertsToFourZeroBased()
    {
        ProductoFilterDto? capturedFilter = null;
        _productoRepoMock.Setup(r => r.FindAllPagedAsync(It.IsAny<ProductoFilterDto>()))
            .Callback<ProductoFilterDto>(f => capturedFilter = f)
            .ReturnsAsync((new List<Producto>(), 50));

        var result = await _query.GetProductosPaged(_productoRepoMock.Object, 5, 10);

        result.Page.Should().Be(5);
        capturedFilter!.Page.Should().Be(4);
    }

    #endregion

    #region GetCategoriasPaged Tests

    [Test]
    public async Task GetCategoriasPaged_WithPage1_ConvertsToZeroBasedFilter()
    {
        // Arrange: capturar el filtro que llega al repositorio
        CategoriaFilterDto? capturedFilter = null;
        _categoriaRepoMock.Setup(r => r.FindAllPagedAsync(It.IsAny<CategoriaFilterDto>()))
            .Callback<CategoriaFilterDto>(f => capturedFilter = f)
            .ReturnsAsync((new List<Categoria>(), 2));

        // Act
        var result = await _query.GetCategoriasPaged(_categoriaRepoMock.Object, 1, 10);

        // Assert
        result.Should().NotBeNull();
        result.Page.Should().Be(1); // GraphQL devuelve base 1
        capturedFilter.Should().NotBeNull();
        capturedFilter!.Page.Should().Be(0); // pero el filtro va en base 0
        capturedFilter.Size.Should().Be(10);
    }

    [Test]
    public async Task GetCategoriasPaged_WithPage3_ConvertsToTwoZeroBased()
    {
        CategoriaFilterDto? capturedFilter = null;
        _categoriaRepoMock.Setup(r => r.FindAllPagedAsync(It.IsAny<CategoriaFilterDto>()))
            .Callback<CategoriaFilterDto>(f => capturedFilter = f)
            .ReturnsAsync((new List<Categoria>(), 30));

        var result = await _query.GetCategoriasPaged(_categoriaRepoMock.Object, 3, 10);

        result.Page.Should().Be(3);
        capturedFilter!.Page.Should().Be(2);
    }

    #endregion
}

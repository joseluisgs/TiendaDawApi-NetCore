using FluentAssertions;
using Moq;
using NUnit.Framework;
using TiendaApi.Api.Dtos.Categorias;
using TiendaApi.Api.Dtos.Common;
using TiendaApi.Api.Dtos.Productos;
using TiendaApi.Api.GraphQL.Queries;
using TiendaApi.Api.Repositories.Categorias;
using TiendaApi.Api.Repositories.Productos;
using TiendaApi.Api.Models;

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
    public async Task GetProductosPaged_WithPaging_ReturnsPagedResult()
    {
        var filter = new ProductoFilterDto(null, null, null, null, null, 1, 10);
        var items = new List<Producto>();
        var pagedResult = (items, 2);

        _productoRepoMock.Setup(r => r.FindAllPagedAsync(It.IsAny<ProductoFilterDto>()))
            .ReturnsAsync(pagedResult);

        var result = await _query.GetProductosPaged(_productoRepoMock.Object, 1, 10);

        result.Should().NotBeNull();
    }

    #endregion

    #region GetCategoriasPaged Tests

    [Test]
    public async Task GetCategoriasPaged_WithPaging_ReturnsPagedResult()
    {
        var filter = new CategoriaFilterDto { Page = 1, Size = 10 };
        var items = new List<Categoria>();
        var pagedResult = (items, 2);

        _categoriaRepoMock.Setup(r => r.FindAllPagedAsync(It.IsAny<CategoriaFilterDto>()))
            .ReturnsAsync(pagedResult);

        var result = await _query.GetCategoriasPaged(_categoriaRepoMock.Object, 1, 10);

        result.Should().NotBeNull();
    }

    #endregion
}
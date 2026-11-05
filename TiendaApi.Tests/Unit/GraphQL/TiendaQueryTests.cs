using System.Security.Claims;
using FluentAssertions;
using HotChocolate;
using Moq;
using NUnit.Framework;
using TiendaApi.Api.Dtos.Categorias;
using TiendaApi.Api.Dtos.Common;
using TiendaApi.Api.Dtos.Pedidos;
using TiendaApi.Api.Dtos.Productos;
using TiendaApi.Api.GraphQL.Queries;
using TiendaApi.Api.Models;
using TiendaApi.Api.Repositories.Categorias;
using TiendaApi.Api.Repositories.Pedidos;
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
    public async Task GetProductos_RepositoryExists_ReturnsList()
    {
        _productoRepoMock.Setup(r => r.FindAllAsync())
            .ReturnsAsync(new List<Producto>());

        var result = await _query.GetProductos(_productoRepoMock.Object);

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
    public async Task GetCategorias_RepositoryExists_ReturnsList()
    {
        _categoriaRepoMock.Setup(r => r.FindAllAsync())
            .ReturnsAsync(new List<Categoria>());

        var result = await _query.GetCategorias(_categoriaRepoMock.Object);

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

    #region GetPedidos / GetMisPedidos Tests

    private static Pedido UnPedido(long userId) => new()
    {
        Id = MongoDB.Bson.ObjectId.GenerateNewId(),
        UserId = userId,
        Estado = "Pendiente",
        Total = 10m,
        Items = new List<PedidoItem>(),
        CreatedAt = DateTime.UtcNow
    };

    [Test]
    public async Task GetPedidos_ReturnsAllAsDtos()
    {
        var repo = new Mock<IPedidosRepository>();
        repo.Setup(r => r.FindAllAsync())
            .ReturnsAsync(new List<Pedido> { UnPedido(1), UnPedido(2) });

        var result = await _query.GetPedidos(repo.Object);

        result.Should().HaveCount(2);
        result![0].Should().BeOfType<PedidoDto>();
        repo.Verify(r => r.FindAllAsync(), Times.Once);
    }

    [Test]
    public async Task GetMisPedidos_ConClaim_FiltraPorElUsuarioAutenticado()
    {
        // El userId SIEMPRE sale del claim del JWT: el repositorio filtra
        // por él y un usuario jamás ve pedidos ajenos por GraphQL.
        var repo = new Mock<IPedidosRepository>();
        repo.Setup(r => r.FindByUserIdAsync(7))
            .ReturnsAsync(new List<Pedido> { UnPedido(7) });

        var user = new ClaimsPrincipal(new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.NameIdentifier, "7") }, "test"));

        var result = await _query.GetMisPedidos(user, repo.Object);

        result.Should().HaveCount(1);
        result![0].UserId.Should().Be(7);
        repo.Verify(r => r.FindByUserIdAsync(7), Times.Once);
        repo.Verify(r => r.FindAllAsync(), Times.Never, "misPedidos jamás debe listar todos los pedidos");
    }

    [Test]
    public async Task GetMisPedidos_SinClaim_LanzaError()
    {
        var repo = new Mock<IPedidosRepository>();
        var sinClaims = new ClaimsPrincipal(new ClaimsIdentity()); // anónimo

        var act = () => _query.GetMisPedidos(sinClaims, repo.Object);

        await act.Should().ThrowAsync<GraphQLException>()
            .WithMessage("*NameIdentifier*");
        repo.Verify(r => r.FindByUserIdAsync(It.IsAny<long>()), Times.Never);
    }

    #endregion
}

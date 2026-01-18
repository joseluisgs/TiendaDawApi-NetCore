using FluentAssertions;
using Moq;
using NUnit.Framework;
using TiendaApi.Apis.GraphQL.Queries;
using TiendaApi.Apis.Models;
using TiendaApi.Apis.Repositories.Categorias;
using TiendaApi.Apis.Repositories.Productos;

namespace TiendaApi.Tests.Unit.GraphQL;

/// <summary>
/// Tests unitarios para TiendaQuery (GraphQL).
/// </summary>
public class TiendaQueryTests
{
    private Mock<IProductoRepository> _productoRepositoryMock = null!;
    private Mock<ICategoriaRepository> _categoriaRepositoryMock = null!;
    private TiendaQuery _query = null!;

    [SetUp]
    public void Setup()
    {
        _productoRepositoryMock = new Mock<IProductoRepository>();
        _categoriaRepositoryMock = new Mock<ICategoriaRepository>();
        _query = new TiendaQuery();
    }

    #region GetProductos Tests

    [Test]
    public void GetProductos_ConProductosExistentes_RetornaQueryable()
    {
        // Arrange
        var productos = new List<Producto>
        {
            new() { Id = 1, Nombre = "Laptop", Precio = 999.99m, Stock = 10 },
            new() { Id = 2, Nombre = "Mouse", Precio = 29.99m, Stock = 50 }
        }.AsQueryable();

        _productoRepositoryMock
            .Setup(x => x.FindAllAsNoTracking())
            .Returns(productos);

        // Act
        var result = _query.GetProductos(_productoRepositoryMock.Object);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.First().Nombre.Should().Be("Laptop");
        _productoRepositoryMock.Verify(x => x.FindAllAsNoTracking(), Times.Once);
    }

    [Test]
    public void GetProductos_SinProductos_RetornaQueryableVacio()
    {
        // Arrange
        var productos = Enumerable.Empty<Producto>().AsQueryable();

        _productoRepositoryMock
            .Setup(x => x.FindAllAsNoTracking())
            .Returns(productos);

        // Act
        var result = _query.GetProductos(_productoRepositoryMock.Object);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    #endregion

    #region GetProducto Tests

    [Test]
    public async Task GetProducto_ConIdExistente_RetornaProducto()
    {
        // Arrange
        var productoEsperado = new Producto
        {
            Id = 1,
            Nombre = "Laptop Dell",
            Descripcion = "Laptop de alto rendimiento",
            Precio = 1299.99m,
            Stock = 10
        };

        _productoRepositoryMock
            .Setup(x => x.FindByIdAsync(1))
            .ReturnsAsync(productoEsperado);

        // Act
        var result = await _query.GetProducto(1, _productoRepositoryMock.Object);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(1);
        result.Nombre.Should().Be("Laptop Dell");
        _productoRepositoryMock.Verify(x => x.FindByIdAsync(1), Times.Once);
    }

    [Test]
    public async Task GetProducto_ConIdNoExistente_RetornaNull()
    {
        // Arrange
        _productoRepositoryMock
            .Setup(x => x.FindByIdAsync(999))
            .ReturnsAsync((Producto?)null);

        // Act
        var result = await _query.GetProducto(999, _productoRepositoryMock.Object);

        // Assert
        result.Should().BeNull();
        _productoRepositoryMock.Verify(x => x.FindByIdAsync(999), Times.Once);
    }

    [TestCase(1)]
    [TestCase(100)]
    [TestCase(999999)]
    public async Task GetProducto_ConIdValido_RetornaProducto(long id)
    {
        // Arrange
        var producto = new Producto { Id = id, Nombre = $"Producto {id}", Precio = 99.99m };
        _productoRepositoryMock
            .Setup(x => x.FindByIdAsync(id))
            .ReturnsAsync(producto);

        // Act
        var result = await _query.GetProducto(id, _productoRepositoryMock.Object);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(id);
    }

    #endregion

    #region GetCategorias Tests

    [Test]
    public void GetCategorias_ConCategoriasExistentes_RetornaQueryable()
    {
        // Arrange
        var categorias = new List<Categoria>
        {
            new() { Id = 1, Nombre = "Electrónica" },
            new() { Id = 2, Nombre = "Ropa" },
            new() { Id = 3, Nombre = "Libros" }
        }.AsQueryable();

        _categoriaRepositoryMock
            .Setup(x => x.FindAllAsNoTracking())
            .Returns(categorias);

        // Act
        var result = _query.GetCategorias(_categoriaRepositoryMock.Object);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(3);
        _categoriaRepositoryMock.Verify(x => x.FindAllAsNoTracking(), Times.Once);
    }

    [Test]
    public void GetCategorias_SinCategorias_RetornaQueryableVacio()
    {
        // Arrange
        var categorias = Enumerable.Empty<Categoria>().AsQueryable();

        _categoriaRepositoryMock
            .Setup(x => x.FindAllAsNoTracking())
            .Returns(categorias);

        // Act
        var result = _query.GetCategorias(_categoriaRepositoryMock.Object);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    #endregion

    #region GetCategoria Tests

    [Test]
    public async Task GetCategoria_ConIdExistente_RetornaCategoria()
    {
        // Arrange
        var categoriaEsperada = new Categoria
        {
            Id = 1,
            Nombre = "Electrónica",
            CreatedAt = DateTime.UtcNow
        };

        _categoriaRepositoryMock
            .Setup(x => x.FindByIdAsync(1))
            .ReturnsAsync(categoriaEsperada);

        // Act
        var result = await _query.GetCategoria(1, _categoriaRepositoryMock.Object);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(1);
        result.Nombre.Should().Be("Electrónica");
        _categoriaRepositoryMock.Verify(x => x.FindByIdAsync(1), Times.Once);
    }

    [Test]
    public async Task GetCategoria_ConIdNoExistente_RetornaNull()
    {
        // Arrange
        _categoriaRepositoryMock
            .Setup(x => x.FindByIdAsync(999))
            .ReturnsAsync((Categoria?)null);

        // Act
        var result = await _query.GetCategoria(999, _categoriaRepositoryMock.Object);

        // Assert
        result.Should().BeNull();
        _categoriaRepositoryMock.Verify(x => x.FindByIdAsync(999), Times.Once);
    }

    #endregion

    #region GetProductosPaged Tests

    [Test]
    public void GetProductosPaged_ConProductos_RetornaQueryablePaginable()
    {
        // Arrange
        var productos = new List<Producto>
        {
            new() { Id = 1, Nombre = "P1", Precio = 100 },
            new() { Id = 2, Nombre = "P2", Precio = 200 },
            new() { Id = 3, Nombre = "P3", Precio = 300 }
        }.AsQueryable();

        _productoRepositoryMock
            .Setup(x => x.FindAllAsNoTracking())
            .Returns(productos);

        // Act
        var result = _query.GetProductosPaged(_productoRepositoryMock.Object);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(3);
        _productoRepositoryMock.Verify(x => x.FindAllAsNoTracking(), Times.Once);
    }

    #endregion

    #region GetCategoriasPaged Tests

    [Test]
    public void GetCategoriasPaged_ConCategorias_RetornaQueryablePaginable()
    {
        // Arrange
        var categorias = new List<Categoria>
        {
            new() { Id = 1, Nombre = "C1" },
            new() { Id = 2, Nombre = "C2" }
        }.AsQueryable();

        _categoriaRepositoryMock
            .Setup(x => x.FindAllAsNoTracking())
            .Returns(categorias);

        // Act
        var result = _query.GetCategoriasPaged(_categoriaRepositoryMock.Object);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        _categoriaRepositoryMock.Verify(x => x.FindAllAsNoTracking(), Times.Once);
    }

    #endregion

    #region Integration Tests - Multiple Queries

    [Test]
    public void GetProductosYGetCategorias_UsandoMismoRepositorio_NoCausaConflictos()
    {
        // Arrange
        var productos = new List<Producto>
        {
            new() { Id = 1, Nombre = "P1" }
        }.AsQueryable();

        var categorias = new List<Categoria>
        {
            new() { Id = 1, Nombre = "C1" }
        }.AsQueryable();

        _productoRepositoryMock.Setup(x => x.FindAllAsNoTracking()).Returns(productos);
        _categoriaRepositoryMock.Setup(x => x.FindAllAsNoTracking()).Returns(categorias);

        // Act
        var productosResult = _query.GetProductos(_productoRepositoryMock.Object);
        var categoriasResult = _query.GetCategorias(_categoriaRepositoryMock.Object);

        // Assert
        productosResult.Should().HaveCount(1);
        categoriasResult.Should().HaveCount(1);
        _productoRepositoryMock.Verify(x => x.FindAllAsNoTracking(), Times.Once);
        _categoriaRepositoryMock.Verify(x => x.FindAllAsNoTracking(), Times.Once);
    }

    #endregion
}

using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using TiendaApi.Apis.Dtos.Productos;
using TiendaApi.Apis.Errors;
using TiendaApi.Apis.Models;
using TiendaApi.Apis.Repositories.Categorias;
using TiendaApi.Apis.Repositories.Productos;
using TiendaApi.Apis.Services.Cache;
using TiendaApi.Apis.Services.Email;
using TiendaApi.Apis.Services.Productos;
using TiendaApi.Apis.WebSockets.Productos;

namespace TiendaApi.Tests.Unit.Services.Productos;

/// <summary>
/// Suite de tests para ProductoService
/// Prueba el enfoque Result Pattern para operaciones de productos
/// </summary>
public class ProductoServiceTests
{
    private Mock<IProductoRepository> _mockProductoRepo = null!;
    private Mock<ICategoriaRepository> _mockCategoriaRepo = null!;
    private Mock<ILogger<ProductoService>> _mockLogger = null!;
    private Mock<ICacheService> _mockCacheService = null!;
    private Mock<ProductoWebSocketHandler> _mockWebSocketHandler = null!;
    private Mock<IEmailService> _mockEmailService = null!;
    private Mock<IConfiguration> _mockConfiguration = null!;
    private ProductoService _service = null!;

    [SetUp]
    public void Setup()
    {
        _mockProductoRepo = new Mock<IProductoRepository>();
        _mockCategoriaRepo = new Mock<ICategoriaRepository>();
        _mockLogger = new Mock<ILogger<ProductoService>>();
        _mockCacheService = new Mock<ICacheService>();
        _mockWebSocketHandler = new Mock<ProductoWebSocketHandler>(Mock.Of<ILogger<ProductoWebSocketHandler>>());
        _mockEmailService = new Mock<IEmailService>();
        _mockConfiguration = new Mock<IConfiguration>();

        _mockConfiguration.Setup(c => c["Cache:ProductoCacheTTLMinutes"]).Returns("10");

        _service = new ProductoService(
            _mockProductoRepo.Object,
            _mockCategoriaRepo.Object,
            _mockLogger.Object,
            _mockCacheService.Object,
            _mockWebSocketHandler.Object,
            _mockEmailService.Object,
            _mockConfiguration.Object
        );
    }

    #region FindAllAsync Tests

    [Test]
    public async Task FindAllAsync_ConProductos_RetornaTodosLosProductos()
    {
        // Arrange
        var productos = new List<Producto>
        {
            new() { Id = 1, Nombre = "Product1" },
            new() { Id = 2, Nombre = "Product2" }
        };

        _mockCacheService.Setup(c => c.GetAsync<IEnumerable<ProductoDto>>(It.IsAny<string>()))
            .ReturnsAsync((IEnumerable<ProductoDto>?)null);
        _mockProductoRepo.Setup(r => r.FindAllAsync())
            .ReturnsAsync(productos);

        // Act
        var result = await _service.FindAllAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
    }

    [Test]
    public async Task FindAllAsync_ConCache_RetornaDesdeCache()
    {
        // Arrange
        var cachedDtos = new List<ProductoDto>
        {
            new() { Id = 1, Nombre = "Cached Product" }
        };

        _mockCacheService.Setup(c => c.GetAsync<IEnumerable<ProductoDto>>("productos:all"))
            .ReturnsAsync(cachedDtos);

        // Act
        var result = await _service.FindAllAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value.First().Nombre.Should().Be("Cached Product");
        _mockProductoRepo.Verify(r => r.FindAllAsync(), Times.Never);
    }

    #endregion

    #region FindByIdAsync Tests

    [Test]
    public async Task FindByIdAsync_ConIdExistente_RetornaExito()
    {
        // Arrange
        var producto = new Producto
        {
            Id = 1,
            Nombre = "Test Product",
            Categoria = new Categoria { Id = 1, Nombre = "Electronics" }
        };

        _mockCacheService.Setup(c => c.GetAsync<ProductoDto>("productos:1"))
            .ReturnsAsync((ProductoDto?)null);
        _mockProductoRepo.Setup(r => r.FindByIdAsync(1))
            .ReturnsAsync(producto);

        // Act
        var result = await _service.FindByIdAsync(1);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(1);
        result.Value.Nombre.Should().Be("Test Product");
    }

    [Test]
    public async Task FindByIdAsync_ConIdNoExistente_RetornaNoEncontrado()
    {
        // Arrange
        _mockCacheService.Setup(c => c.GetAsync<ProductoDto>("productos:999"))
            .ReturnsAsync((ProductoDto?)null);
        _mockProductoRepo.Setup(r => r.FindByIdAsync(999))
            .ReturnsAsync((Producto?)null);

        // Act
        var result = await _service.FindByIdAsync(999);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.NotFound);
    }

    #endregion

    #region FindByCategoriaIdAsync Tests

    [Test]
    public async Task FindByCategoriaIdAsync_ConCategoriaExistente_RetornaProductos()
    {
        // Arrange
        var categoria = new Categoria { Id = 1, Nombre = "Electronics" };
        var productos = new List<Producto>
        {
            new() { Id = 1, Nombre = "Laptop", CategoriaId = 1 }
        };

        _mockCategoriaRepo.Setup(r => r.FindByIdAsync(1))
            .ReturnsAsync(categoria);
        _mockProductoRepo.Setup(r => r.FindByCategoriaIdAsync(1))
            .ReturnsAsync(productos);

        // Act
        var result = await _service.FindByCategoriaIdAsync(1);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
    }

    [Test]
    public async Task FindByCategoriaIdAsync_ConCategoriaNoExistente_RetornaNoEncontrado()
    {
        // Arrange
        _mockCategoriaRepo.Setup(r => r.FindByIdAsync(999))
            .ReturnsAsync((Categoria?)null);

        // Act
        var result = await _service.FindByCategoriaIdAsync(999);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.NotFound);
    }

    #endregion

    #region CreateAsync Tests

    [Test]
    public async Task CreateAsync_ConDatosValidos_RetornaExito()
    {
        // Arrange
        var dto = new ProductoRequestDto
        {
            Nombre = "New Product",
            Descripcion = "Description",
            Precio = 99.99m,
            Stock = 10,
            CategoriaId = 1
        };

        var savedProducto = new Producto
        {
            Id = 1,
            Nombre = "New Product",
            Descripcion = "Description",
            Precio = 99.99m,
            Stock = 10,
            CategoriaId = 1
        };

        _mockCategoriaRepo.Setup(r => r.FindByIdAsync(1))
            .ReturnsAsync(new Categoria { Id = 1 });
        _mockProductoRepo.Setup(r => r.SaveAsync(It.IsAny<Producto>()))
            .ReturnsAsync(savedProducto);

        // Act
        var result = await _service.CreateAsync(dto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Nombre.Should().Be("New Product");
    }

    [Test]
    public async Task CreateAsync_ConPrecioInvalido_RetornaErrorValidacion()
    {
        // Arrange
        var dto = new ProductoRequestDto
        {
            Nombre = "Test",
            Precio = -10,
            Stock = 5,
            CategoriaId = 1
        };

        // Act
        var result = await _service.CreateAsync(dto);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
        result.Error.Message.Should().Contain("precio");
    }

    [Test]
    public async Task CreateAsync_ConStockInvalido_RetornaErrorValidacion()
    {
        // Arrange
        var dto = new ProductoRequestDto
        {
            Nombre = "Test",
            Precio = 50,
            Stock = -5,
            CategoriaId = 1
        };

        // Act
        var result = await _service.CreateAsync(dto);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
    }

    [Test]
    public async Task CreateAsync_ConCategoriaNoExistente_RetornaNoEncontrado()
    {
        // Arrange
        var dto = new ProductoRequestDto
        {
            Nombre = "Test",
            Precio = 50,
            Stock = 5,
            CategoriaId = 999
        };

        _mockCategoriaRepo.Setup(r => r.FindByIdAsync(999))
            .ReturnsAsync((Categoria?)null);

        // Act
        var result = await _service.CreateAsync(dto);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.NotFound);
    }

    #endregion

    #region UpdateAsync Tests

    [Test]
    public async Task UpdateAsync_ConDatosValidos_RetornaExito()
    {
        // Arrange
        var dto = new ProductoRequestDto
        {
            Nombre = "Updated Product",
            Descripcion = "Updated Description",
            Precio = 199.99m,
            Stock = 20,
            CategoriaId = 1
        };

        var existingProducto = new Producto { Id = 1, Nombre = "Old Product", CategoriaId = 1 };
        var updatedProducto = new Producto { Id = 1, Nombre = "Updated Product", CategoriaId = 1 };

        _mockProductoRepo.Setup(r => r.FindByIdAsync(1))
            .ReturnsAsync(existingProducto);
        _mockCategoriaRepo.Setup(r => r.FindByIdAsync(1))
            .ReturnsAsync(new Categoria { Id = 1 });
        _mockProductoRepo.Setup(r => r.UpdateAsync(It.IsAny<Producto>()))
            .ReturnsAsync(updatedProducto);

        // Act
        var result = await _service.UpdateAsync(1, dto);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Test]
    public async Task UpdateAsync_ConIdNoExistente_RetornaNoEncontrado()
    {
        // Arrange
        var dto = new ProductoRequestDto { Nombre = "Updated" };
        _mockProductoRepo.Setup(r => r.FindByIdAsync(999))
            .ReturnsAsync((Producto?)null);

        // Act
        var result = await _service.UpdateAsync(999, dto);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.NotFound);
    }

    #endregion

    #region DeleteAsync Tests

    [Test]
    public async Task DeleteAsync_ConIdExistente_RetornaExito()
    {
        // Arrange
        var producto = new Producto { Id = 1, Nombre = "To Delete" };
        _mockProductoRepo.Setup(r => r.FindByIdAsync(1))
            .ReturnsAsync(producto);

        // Act
        var result = await _service.DeleteAsync(1);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Test]
    public async Task DeleteAsync_ConIdNoExistente_RetornaNoEncontrado()
    {
        // Arrange
        _mockProductoRepo.Setup(r => r.FindByIdAsync(999))
            .ReturnsAsync((Producto?)null);

        // Act
        var result = await _service.DeleteAsync(999);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.NotFound);
    }

    #endregion
}

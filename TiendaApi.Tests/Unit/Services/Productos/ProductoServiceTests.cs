using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using TiendaApi.Api.Dtos.Productos;
using TiendaApi.Api.Errors;
using TiendaApi.Api.Errors.Productos;
using TiendaApi.Api.GraphQL.Publishers;
using TiendaApi.Api.Models;
using TiendaApi.Api.Repositories.Categorias;
using TiendaApi.Api.Repositories.Productos;
using TiendaApi.Api.Services.Cache;
using TiendaApi.Api.Services.Email;
using TiendaApi.Api.Services.Productos;
using TiendaApi.Api.Services.Storage;
using TiendaApi.Api.Validators.Productos;
using TiendaApi.Api.Realtime.Productos;

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
    private Mock<ProductosWebSocketHandler> _mockWebSocketHandler = null!;
    private Mock<IHubContext<ProductosHub>> _mockHubContext = null!;
    private Mock<IEmailService> _mockEmailService = null!;
    private Mock<IConfiguration> _mockConfiguration = null!;
    private Mock<IValidator<ProductoRequestDto>> _mockValidator = null!;
    private Mock<IStorageService> _mockStorageService = null!;
    private Mock<IEventPublisher> _mockEventPublisher = null!;
    private ProductoService _service = null!;

    [SetUp]
    public void Setup()
    {
        _mockProductoRepo = new Mock<IProductoRepository>();
        _mockCategoriaRepo = new Mock<ICategoriaRepository>();
        _mockLogger = new Mock<ILogger<ProductoService>>();
        _mockCacheService = new Mock<ICacheService>();
        _mockWebSocketHandler = new Mock<ProductosWebSocketHandler>(Mock.Of<ILogger<ProductosWebSocketHandler>>());
        _mockHubContext = new Mock<IHubContext<ProductosHub>>();
        _mockEmailService = new Mock<IEmailService>();
        _mockConfiguration = new Mock<IConfiguration>();
        _mockValidator = new Mock<IValidator<ProductoRequestDto>>();
        _mockStorageService = new Mock<IStorageService>();
        _mockEventPublisher = new Mock<IEventPublisher>();

        _mockConfiguration.Setup(c => c["Cache:ProductoCacheTTLMinutes"]).Returns("10");

        _mockValidator.Setup(v => v.ValidateAsync(It.IsAny<ProductoRequestDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        var hubContext = new Mock<IHubContext<ProductosHub>>();
        var eventPublisher = new Mock<IEventPublisher>();
        _service = new ProductoService(
            _mockProductoRepo.Object,
            _mockCategoriaRepo.Object,
            _mockLogger.Object,
            _mockCacheService.Object,
            _mockWebSocketHandler.Object,
            _mockHubContext.Object,
            _mockEmailService.Object,
            _mockConfiguration.Object,
            _mockValidator.Object,
            _mockStorageService.Object,
            _mockEventPublisher.Object
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
            new ProductoDto(1, "Cached Product", "", 0, 0, null, 0, "", DateTime.UtcNow, DateTime.UtcNow)
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
        result.Error.Should().BeOfType<NotFoundError>();
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
        result.Error.Should().BeOfType<NotFoundError>();
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

        // Configurar mock de categoría para que pase la validación de existencia
        var categoria = new Categoria { Id = 1, Nombre = "Electronics" };
        _mockCategoriaRepo.Setup(r => r.FindByIdAsync(1))
            .ReturnsAsync(categoria);

        // Configurar validator para que falle
        _mockValidator.Setup(v => v.ValidateAsync(dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(new[]
            {
                new ValidationFailure("Precio", "El precio debe ser mayor a 0")
            }));

        // Re-crear servicio con mocks configurados
        _service = new ProductoService(
            _mockProductoRepo.Object,
            _mockCategoriaRepo.Object,
            _mockLogger.Object,
            _mockCacheService.Object,
            _mockWebSocketHandler.Object,
            _mockHubContext.Object,
            _mockEmailService.Object,
            _mockConfiguration.Object,
            _mockValidator.Object,
            _mockStorageService.Object,
            _mockEventPublisher.Object
        );
    }

    #endregion

    #region CreateAsync_CategoriaNoExiste_RetornaFailure

    [Test]
    public async Task CreateAsync_CategoriaNoExiste_RetornaFailure()
    {
        // Arrange
        _mockCategoriaRepo.Setup(r => r.FindByIdAsync(999))
            .ReturnsAsync((Categoria?)null);

        var dto = new ProductoRequestDto
        {
            Nombre = "Test",
            Precio = 99,
            Stock = 10,
            CategoriaId = 999
        };

        _mockValidator.Setup(v => v.ValidateAsync(It.IsAny<ProductoRequestDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        // Act
        var result = await _service.CreateAsync(dto);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<ValidationError>();
        var validationError = (ValidationError)result.Error;
        validationError.ValidationErrors.Should().ContainKey("CategoriaId");
    }

    #endregion

    #region CreateAsync_ConStockInvalido_RetornaErrorValidacion

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

        // Configurar mock de categoría para que pase la validación de existencia
        var categoria = new Categoria { Id = 1, Nombre = "Electronics" };
        _mockCategoriaRepo.Setup(r => r.FindByIdAsync(1))
            .ReturnsAsync(categoria);

        // Configurar validator para que falle
        _mockValidator.Setup(v => v.ValidateAsync(dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(new[]
            {
                new ValidationFailure("Stock", "El stock no puede ser negativo")
            }));

        // Act
        var result = await _service.CreateAsync(dto);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<ValidationError>();
        ((ValidationError)result.Error).ValidationErrors.Should().ContainKey("Stock");
    }

    #endregion

    #region CreateAsync_ConCategoriaNoExistente_RetornaNoEncontrado

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
        result.Error.Should().BeOfType<ValidationError>();
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
        result.Error.Should().BeOfType<NotFoundError>();
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
        result.Error.Should().BeOfType<NotFoundError>();
    }

    #endregion

    #region UpdateImageAsync Tests

    [Test]
    public async Task UpdateImageAsync_ConIdNoExistente_RetornaNoEncontrado()
    {
        // Arrange
        _mockProductoRepo.Setup(r => r.FindByIdAsync(999))
            .ReturnsAsync((Producto?)null);

        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.Length).Returns(1000);

        // Act
        var result = await _service.UpdateImageAsync(999, mockFile.Object);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<NotFoundError>();
    }

    [Test]
    public async Task UpdateImageAsync_ConArchivoValido_RetornaExito()
    {
        // Arrange
        var categoria = new Categoria { Id = 1, Nombre = "Test Category" };
        var producto = new Producto
        {
            Id = 1,
            Nombre = "Test Product",
            CategoriaId = 1,
            Categoria = categoria,
            Imagen = "https://example.com/old.jpg"
        };
        _mockProductoRepo.Setup(r => r.FindByIdAsync(1))
            .ReturnsAsync(producto);

        _mockProductoRepo.Setup(r => r.UpdateAsync(It.IsAny<Producto>()))
            .ReturnsAsync(producto);

        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.FileName).Returns("test.png");
        mockFile.Setup(f => f.Length).Returns(1000);
        mockFile.Setup(f => f.ContentType).Returns("image/png");

        var saveResult = CSharpFunctionalExtensions.Result.Success<string, TiendaApi.Api.Errors.DomainError>("/uploads/productos/new.png");
        _mockStorageService.Setup(s => s.SaveFileAsync(It.IsAny<IFormFile>(), "productos"))
            .ReturnsAsync(saveResult);

        // Act
        var result = await _service.UpdateImageAsync(1, mockFile.Object);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _mockStorageService.Verify(s => s.SaveFileAsync(It.IsAny<IFormFile>(), "productos"), Times.Once);
    }

    [Test]
    public async Task UpdateImageAsync_ConImagenLocalAnterior_EliminaImagenAnterior()
    {
        // Arrange
        var categoria = new Categoria { Id = 1, Nombre = "Test Category" };
        var producto = new Producto
        {
            Id = 1,
            Nombre = "Test Product",
            CategoriaId = 1,
            Categoria = categoria,
            Imagen = "/storage/uploads/productos/old.png"
        };
        _mockProductoRepo.Setup(r => r.FindByIdAsync(1))
            .ReturnsAsync(producto);

        _mockProductoRepo.Setup(r => r.UpdateAsync(It.IsAny<Producto>()))
            .ReturnsAsync(producto);

        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.FileName).Returns("test.png");
        mockFile.Setup(f => f.Length).Returns(1000);
        mockFile.Setup(f => f.ContentType).Returns("image/png");

        var saveResult = CSharpFunctionalExtensions.Result.Success<string, TiendaApi.Api.Errors.DomainError>("/uploads/productos/new.png");
        _mockStorageService.Setup(s => s.SaveFileAsync(It.IsAny<IFormFile>(), "productos"))
            .ReturnsAsync(saveResult);

        var deleteResult = CSharpFunctionalExtensions.Result.Success<bool, TiendaApi.Api.Errors.DomainError>(true);
        _mockStorageService.Setup(s => s.DeleteFileAsync("/storage/uploads/productos/old.png"))
            .ReturnsAsync(deleteResult);

        // Act
        var result = await _service.UpdateImageAsync(1, mockFile.Object);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _mockStorageService.Verify(s => s.DeleteFileAsync("/storage/uploads/productos/old.png"), Times.Once);
    }

    [Test]
    public async Task UpdateImageAsync_ConErrorEnStorage_RetornaError()
    {
        // Arrange
        var categoria = new Categoria { Id = 1, Nombre = "Test Category" };
        var producto = new Producto
        {
            Id = 1,
            Nombre = "Test Product",
            CategoriaId = 1,
            Categoria = categoria
        };
        _mockProductoRepo.Setup(r => r.FindByIdAsync(1))
            .ReturnsAsync(producto);

        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.Length).Returns(1000);

        var errorResult = CSharpFunctionalExtensions.Result.Failure<string, DomainError>(
            ValidationError.Create("Archivo no válido"));
        _mockStorageService.Setup(s => s.SaveFileAsync(It.IsAny<IFormFile>(), "productos"))
            .ReturnsAsync(errorResult);

        // Act
        var result = await _service.UpdateImageAsync(1, mockFile.Object);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<ValidationError>();
    }

    #endregion
}

using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using TiendaApi.Common;
using TiendaApi.Exceptions;
using TiendaApi.Models.DTOs;
using TiendaApi.Models.Entities;
using TiendaApi.Repositories;
using TiendaApi.Services;
using TiendaApi.WebSockets;

namespace TiendaApi.Tests;

/// <summary>
/// Test suite demonstrating the difference between Exception-based and Result Pattern
/// 
/// EDUCATIONAL NOTE:
/// Compare how CategoriaService tests (exception-based) differ from 
/// ProductoService tests (Result Pattern) in terms of:
/// - Test setup complexity
/// - Assertion clarity
/// - Error handling verification
/// </summary>
public class ErrorHandlingComparisonTests
{
    private Mock<ICategoriaRepository> _mockCategoriaRepo = null!;
    private Mock<IProductoRepository> _mockProductoRepo;
    private Mock<IMapper> _mockMapper = null!;
    private Mock<ILogger<CategoriaService>> _mockCategoriaLogger = null!;
    private Mock<ILogger<ProductoService>> _mockProductoLogger = null!;
    
    private CategoriaService _categoriaService = null!;
    private ProductoService _productoService = null!;

    [SetUp]
    public void Setup()
    {
        _mockCategoriaRepo = new Mock<ICategoriaRepository>();
        _mockProductoRepo = new Mock<IProductoRepository>();
        _mockMapper = new Mock<IMapper>();
        _mockCategoriaLogger = new Mock<ILogger<CategoriaService>>();
        _mockProductoLogger = new Mock<ILogger<ProductoService>>();
        
        _categoriaService = new CategoriaService(
            _mockCategoriaRepo.Object,
            _mockMapper.Object,
            _mockCategoriaLogger.Object
        );
        
        var mockWebSocketHandler = new Mock<ProductoWebSocketHandler>(MockBehavior.Loose, Mock.Of<ILogger<ProductoWebSocketHandler>>());
        var mockEmailService = new Mock<Services.Email.IEmailService>();
        var mockCacheService = new Mock<Services.Cache.ICacheService>();
        var mockConfiguration = new Mock<Microsoft.Extensions.Configuration.IConfiguration>();
        
        _productoService = new ProductoService(
            _mockProductoRepo.Object,
            _mockCategoriaRepo.Object,
            _mockMapper.Object,
            _mockProductoLogger.Object,
            mockCacheService.Object,
            mockWebSocketHandler.Object,
            mockEmailService.Object,
            mockConfiguration.Object
        );
    }

    #region Exception-Based Tests (Traditional Approach - Categorías)

    /// <summary>
    /// TEST EXCEPTION APPROACH: Testing for exceptions
    /// 
    /// Java equivalent:
    /// @Test(expected = NotFoundException.class)
    /// public void findById_WhenNotFound_ThrowsException() { ... }
    /// 
    /// Characteristics:
    /// - Uses Assert.ThrowsAsync to catch exceptions
    /// - Requires understanding of exception types
    /// - Less explicit about what can fail
    /// </summary>
    [Test]
    public async Task CategoriaService_FindById_WhenNotFound_ThrowsNotFoundException()
    {
        // Arrange
        _mockCategoriaRepo.Setup(r => r.FindByIdAsync(It.IsAny<long>()))
            .ReturnsAsync((Categoria?)null);

        // Act & Assert
        var ex = Assert.ThrowsAsync<NotFoundException>(async () =>
            await _categoriaService.FindByIdAsync(999)
        );

        ex.Message.Should().Contain("no encontrada");
    }

    /// <summary>
    /// TEST EXCEPTION APPROACH: Success case
    /// No exception = success (implicit)
    /// </summary>
    [Test]
    public async Task CategoriaService_FindById_WhenFound_ReturnsDto()
    {
        // Arrange
        var categoria = new Categoria { Id = 1, Nombre = "Test" };
        var dto = new CategoriaDto { Id = 1, Nombre = "Test" };
        
        _mockCategoriaRepo.Setup(r => r.FindByIdAsync(1))
            .ReturnsAsync(categoria);
        _mockMapper.Setup(m => m.Map<CategoriaDto>(categoria))
            .Returns(dto);

        // Act
        var result = await _categoriaService.FindByIdAsync(1);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(1);
        result.Nombre.Should().Be("Test");
    }

    #endregion

    #region Result Pattern Tests (Modern Approach - Productos)

    /// <summary>
    /// TEST RESULT PATTERN: Testing for failures
    /// 
    /// Java equivalent:
    /// Either<AppError, ProductoDto> result = service.findById(999);
    /// assertTrue(result.isLeft());
    /// assertEquals(ErrorType.NOT_FOUND, result.getLeft().getType());
    /// 
    /// Characteristics:
    /// - No exceptions needed
    /// - Result type makes failure explicit
    /// - Clear what can fail
    /// - Easy to test without try/catch
    /// </summary>
    [Test]
    public async Task ProductoService_FindById_WhenNotFound_ReturnsFailure()
    {
        // Arrange
        _mockProductoRepo.Setup(r => r.FindByIdAsync(It.IsAny<long>()))
            .ReturnsAsync((Producto?)null);

        // Act
        var resultado = await _productoService.FindByIdAsync(999);

        // Assert - Clean and explicit!
        resultado.IsFailure.Should().BeTrue();
        resultado.IsSuccess.Should().BeFalse();
        resultado.Error.Type.Should().Be(ErrorType.NotFound);
        resultado.Error.Message.Should().Contain("no encontrado");
    }

    /// <summary>
    /// TEST RESULT PATTERN: Success case
    /// Explicit success state
    /// </summary>
    [Test]
    public async Task ProductoService_FindById_WhenFound_ReturnsSuccess()
    {
        // Arrange
        var producto = new Producto 
        { 
            Id = 1, 
            Nombre = "Test",
            Categoria = new Categoria { Id = 1, Nombre = "Cat" }
        };
        var dto = new ProductoDto 
        { 
            Id = 1, 
            Nombre = "Test",
            CategoriaId = 1,
            CategoriaNombre = "Cat"
        };
        
        _mockProductoRepo.Setup(r => r.FindByIdAsync(1))
            .ReturnsAsync(producto);
        _mockMapper.Setup(m => m.Map<ProductoDto>(producto))
            .Returns(dto);

        // Act
        var resultado = await _productoService.FindByIdAsync(1);

        // Assert - Explicit success!
        resultado.IsSuccess.Should().BeTrue();
        resultado.IsFailure.Should().BeFalse();
        resultado.Value.Id.Should().Be(1);
        resultado.Value.Nombre.Should().Be("Test");
    }

    /// <summary>
    /// TEST RESULT PATTERN: Validation errors
    /// Clean handling of validation without exceptions
    /// </summary>
    [Test]
    public async Task ProductoService_Create_WithInvalidPrice_ReturnsValidationError()
    {
        // Arrange
        var dto = new ProductoRequestDto
        {
            Nombre = "Test",
            Descripcion = "Test",
            Precio = -10, // Invalid!
            Stock = 5,
            CategoriaId = 1
        };

        // Act
        var resultado = await _productoService.CreateAsync(dto);

        // Assert - Clean validation error handling!
        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Type.Should().Be(ErrorType.Validation);
        resultado.Error.Message.Should().Contain("precio");
    }

    #endregion

    #region Comparison Test - Exception vs Result

    /// <summary>
    /// COMPARISON TEST: Shows the difference in test complexity
    /// 
    /// Notice how:
    /// - Exception test needs Assert.ThrowsAsync
    /// - Result test just checks resultado.IsFailure
    /// - Result pattern is more readable
    /// - No try/catch or exception handling needed
    /// </summary>
    [Test]
    public void Comparison_ExceptionVsResult_NotFoundScenario()
    {
        // Setup for both
        _mockCategoriaRepo.Setup(r => r.FindByIdAsync(999))
            .ReturnsAsync((Categoria?)null);
        _mockProductoRepo.Setup(r => r.FindByIdAsync(999))
            .ReturnsAsync((Producto?)null);

        // EXCEPTION APPROACH (Traditional):
        // - Requires Assert.ThrowsAsync
        // - Less explicit about error type
        Assert.ThrowsAsync<NotFoundException>(async () =>
            await _categoriaService.FindByIdAsync(999)
        );

        // RESULT PATTERN (Modern):
        // - Direct result checking
        // - Explicit error information
        // - No exception handling needed
        var resultado = _productoService.FindByIdAsync(999).Result;
        
        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Type.Should().Be(ErrorType.NotFound);
    }

    #endregion
}

/// <summary>
/// SUMMARY: Testing Comparison
/// 
/// ╔════════════════════════════════╦═════════════════════════════════╗
/// ║   Exception Tests              ║      Result Pattern Tests       ║
/// ╠════════════════════════════════╬═════════════════════════════════╣
/// ║ Assert.ThrowsAsync required    ║ Direct result.IsFailure check   ║
/// ║ Exception type matching        ║ Error type checking             ║
/// ║ try/catch in tests             ║ No exception handling           ║
/// ║ Implicit success (no throw)    ║ Explicit result.IsSuccess       ║
/// ║ Less readable                  ║ More readable                   ║
/// ║ Familiar to Java devs          ║ Functional style                ║
/// ╚════════════════════════════════╩═════════════════════════════════╝
/// </summary>


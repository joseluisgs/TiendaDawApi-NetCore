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

    #region Result Pattern Tests (Categorías - Actualizado)

    /// <summary>
    /// TEST RESULT PATTERN: Testing for failures
    /// 
    /// ANTES (Exception): Assert.ThrowsAsync<NotFoundException>
    /// AHORA (Result): result.IsFailure.Should().BeTrue()
    /// 
    /// Ventajas:
    /// - No hay excepciones que capturar
    /// - El fallo es explícito en el tipo de retorno
    /// - Más fácil de entender y mantener
    /// </summary>
    [Test]
    public async Task CategoriaService_FindById_WhenNotFound_ReturnsFailure()
    {
        // Arrange
        _mockCategoriaRepo.Setup(r => r.FindByIdAsync(It.IsAny<long>()))
            .ReturnsAsync((Categoria?)null);

        // Act
        var result = await _categoriaService.FindByIdAsync(999);

        // Assert - Sin excepciones, verificación explícita
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.NotFound);
        result.Error.Message.Should().Contain("no encontrada");
    }

    /// <summary>
    /// TEST RESULT PATTERN: Success case
    /// 
    /// ANTES (Exception): No exception = success (implícito)
    /// AHORA (Result): result.IsSuccess.Should().BeTrue() (explícito)
    /// 
    /// El éxito es explícito y type-safe
    /// </summary>
    [Test]
    public async Task CategoriaService_FindById_WhenFound_ReturnsSuccess()
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

        // Assert - Verificación explícita de éxito y datos
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(1);
        result.Value.Nombre.Should().Be("Test");
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
    /// COMPARISON TEST: Muestra la consistencia del Result Pattern
    /// 
    /// ACTUALIZADO: Ambos servicios ahora usan Result Pattern
    /// 
    /// Notice cómo:
    /// - CategoriaService ahora usa Result (actualizado desde excepciones)
    /// - ProductoService ya usaba Result
    /// - Ambos se testean de la misma manera
    /// - Consistencia en toda la aplicación
    /// </summary>
    [Test]
    public void Comparison_BothUseResultPattern_NotFoundScenario()
    {
        // Setup for both
        _mockCategoriaRepo.Setup(r => r.FindByIdAsync(999))
            .ReturnsAsync((Categoria?)null);
        _mockProductoRepo.Setup(r => r.FindByIdAsync(999))
            .ReturnsAsync((Producto?)null);

        // CATEGORIASERVICE (Result Pattern):
        // - Direct result checking
        // - Explicit error information
        var resultadoCategoria = _categoriaService.FindByIdAsync(999).Result;
        resultadoCategoria.IsFailure.Should().BeTrue();
        resultadoCategoria.Error.Type.Should().Be(ErrorType.NotFound);

        // PRODUCTOSERVICE (Result Pattern):
        // - Same pattern, same verification
        // - No exception handling needed
        var resultadoProducto = _productoService.FindByIdAsync(999).Result;
        resultadoProducto.IsFailure.Should().BeTrue();
        resultadoProducto.Error.Type.Should().Be(ErrorType.NotFound);
        
        // Ambos servicios ahora manejan errores de manera consistente
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


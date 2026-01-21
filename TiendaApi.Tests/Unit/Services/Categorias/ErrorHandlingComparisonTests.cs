using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using TiendaApi.Api.Dtos.Categorias;
using TiendaApi.Api.Dtos.Productos;
using TiendaApi.Api.Errors;
using TiendaApi.Api.GraphQL.Publishers;
using TiendaApi.Api.Models;
using TiendaApi.Api.Repositories.Categorias;
using TiendaApi.Api.Repositories.Productos;
using TiendaApi.Api.Services.Cache;
using TiendaApi.Api.Services.Categorias;
using TiendaApi.Api.Services.Productos;
using TiendaApi.Api.Services.Storage;
using TiendaApi.Api.Validators.Categorias;
using TiendaApi.Api.Validators.Productos;
using TiendaApi.Api.Realtime.Productos;

namespace TiendaApi.Tests.Unit.Services.Categorias;

/// <summary>
/// Suite de tests que demuestra la diferencia entre manejo basado en Excepciones y Result Pattern
///
/// NOTA EDUCATIVA:
/// Compara cómo los tests de CategoriaService (basado en excepciones) difieren de
/// los tests de ProductoService (Result Pattern) en términos de:
/// - Complejidad de setup de tests
/// - Claridad en aserciones
/// - Verificación de manejo de errores
/// </summary>
public class ErrorHandlingComparisonTests
{
    private Mock<ICategoriaRepository> _mockCategoriaRepo = null!;
    private Mock<IProductoRepository> _mockProductoRepo = null!;
    private Mock<ILogger<CategoriaService>> _mockCategoriaLogger = null!;
    private Mock<ILogger<ProductoService>> _mockProductoLogger = null!;
    private Mock<IValidator<CategoriaRequestDto>> _mockCategoriaValidator = null!;
    private Mock<IValidator<ProductoRequestDto>> _mockProductoValidator = null!;

    private CategoriaService _categoriaService = null!;
    private ProductoService _productoService = null!;

    [SetUp]
    public void Setup()
    {
        _mockCategoriaRepo = new Mock<ICategoriaRepository>();
        _mockProductoRepo = new Mock<IProductoRepository>();
        _mockCategoriaLogger = new Mock<ILogger<CategoriaService>>();
        _mockProductoLogger = new Mock<ILogger<ProductoService>>();
        _mockCategoriaValidator = new Mock<IValidator<CategoriaRequestDto>>();
        _mockProductoValidator = new Mock<IValidator<ProductoRequestDto>>();

        // Configurar validadores para que pasen por defecto
        _mockCategoriaValidator.Setup(v => v.ValidateAsync(It.IsAny<CategoriaRequestDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());
        _mockProductoValidator.Setup(v => v.ValidateAsync(It.IsAny<ProductoRequestDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        var mockCategoriaCacheService = new Mock<ICacheService>();
        var mockCategoriaConfiguration = new Mock<IConfiguration>();

        _categoriaService = new CategoriaService(
            _mockCategoriaRepo.Object,
            _mockCategoriaLogger.Object,
            _mockCategoriaValidator.Object,
            mockCategoriaCacheService.Object,
            mockCategoriaConfiguration.Object
        );

        var mockWebSocketHandler = new Mock<ProductosWebSocketHandler>(MockBehavior.Loose, Mock.Of<ILogger<ProductosWebSocketHandler>>());
        var mockHubContext = new Mock<IHubContext<ProductosHub>>();
        var mockEmailService = new Mock<TiendaApi.Api.Services.Email.IEmailService>();
        var mockCacheService = new Mock<TiendaApi.Api.Services.Cache.ICacheService>();
        var mockConfiguration = new Mock<Microsoft.Extensions.Configuration.IConfiguration>();
        var mockStorageService = new Mock<IStorageService>();
        var mockEventPublisher = new Mock<IEventPublisher>();

        _productoService = new ProductoService(
            _mockProductoRepo.Object,
            _mockCategoriaRepo.Object,
            _mockProductoLogger.Object,
            mockCacheService.Object,
            mockWebSocketHandler.Object,
            mockHubContext.Object,
            mockEmailService.Object,
            mockConfiguration.Object,
            _mockProductoValidator.Object,
            mockStorageService.Object,
            mockEventPublisher.Object
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
    public async Task CategoriaService_FindById_CuandoNoEncontrado_RetornaFallo()
    {
        // Arrange
        _mockCategoriaRepo.Setup(r => r.FindByIdAsync(It.IsAny<long>()))
            .ReturnsAsync((Categoria?)null);

        // Act
        var result = await _categoriaService.FindByIdAsync(999);

        // Assert - Sin excepciones, verificación explícita
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<NotFoundError>();
        result.Error.Message.Should().Contain("Recurso con ID");
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
    public async Task CategoriaService_FindById_CuandoEncontrado_RetornaExito()
    {
        // Arrange
        var categoria = new Categoria { Id = 1, Nombre = "Test" };

        _mockCategoriaRepo.Setup(r => r.FindByIdAsync(1))
            .ReturnsAsync(categoria);

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
    /// TEST RESULT PATTERN: Probando fallos
    /// 
    /// Características:
    /// - No se necesitan excepciones
    /// - El tipo Result hace el fallo explícito
    /// - Claro qué puede fallar
    /// - Fácil de testar sin try/catch
    /// </summary>
    [Test]
    public async Task ProductoService_FindById_CuandoNoEncontrado_RetornaFallo()
    {
        // Arrange
        _mockProductoRepo.Setup(r => r.FindByIdAsync(It.IsAny<long>()))
            .ReturnsAsync((Producto?)null);

        // Act
        var resultado = await _productoService.FindByIdAsync(999);

        // Assert - Clean and explicit!
        resultado.IsFailure.Should().BeTrue();
        resultado.IsSuccess.Should().BeFalse();
        resultado.Error.Should().BeOfType<NotFoundError>();
        resultado.Error.Message.Should().Contain("no encontrado");
    }

    /// <summary>
    /// TEST RESULT PATTERN: Caso de éxito
    /// Estado de éxito explícito
    /// </summary>
    [Test]
    public async Task ProductoService_FindById_CuandoEncontrado_RetornaExito()
    {
        // Arrange
        var producto = new Producto
        {
            Id = 1,
            Nombre = "Test",
            Categoria = new Categoria { Id = 1, Nombre = "Cat" }
        };

        _mockProductoRepo.Setup(r => r.FindByIdAsync(1))
            .ReturnsAsync(producto);

        // Act
        var resultado = await _productoService.FindByIdAsync(1);

        // Assert - Explicit success!
        resultado.IsSuccess.Should().BeTrue();
        resultado.IsFailure.Should().BeFalse();
        resultado.Value.Id.Should().Be(1);
        resultado.Value.Nombre.Should().Be("Test");
    }

    /// <summary>
    /// TEST RESULT PATTERN: Errores de validación
    /// Manejo limpio de validación sin excepciones
    /// </summary>
    [Test]
    public async Task ProductoService_Create_ConPrecioInvalido_RetornaErrorValidacion()
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

        // Configurar mock de categoría para que pase la validación de existencia
        var categoria = new Categoria { Id = 1, Nombre = "Electronics" };
        _mockCategoriaRepo.Setup(r => r.FindByIdAsync(1))
            .ReturnsAsync(categoria);

        // Configurar validator para que falle
        _mockProductoValidator.Setup(v => v.ValidateAsync(dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(new[]
            {
                new ValidationFailure("Precio", "El precio debe ser mayor a 0")
            }));

        // Re-crear servicio con mocks configurados
        var mockWebSocketHandler = new Mock<ProductosWebSocketHandler>(MockBehavior.Loose, Mock.Of<ILogger<ProductosWebSocketHandler>>());
        var mockHubContext = new Mock<IHubContext<ProductosHub>>();
        var mockEmailService = new Mock<TiendaApi.Api.Services.Email.IEmailService>();
        var mockCacheService = new Mock<TiendaApi.Api.Services.Cache.ICacheService>();
        var mockConfiguration = new Mock<Microsoft.Extensions.Configuration.IConfiguration>();
        var mockStorageService = new Mock<TiendaApi.Api.Services.Storage.IStorageService>();
        var mockEventPublisher = new Mock<IEventPublisher>();

        _productoService = new ProductoService(
            _mockProductoRepo.Object,
            _mockCategoriaRepo.Object,
            _mockProductoLogger.Object,
            mockCacheService.Object,
            mockWebSocketHandler.Object,
            mockHubContext.Object,
            mockEmailService.Object,
            mockConfiguration.Object,
            _mockProductoValidator.Object,
            mockStorageService.Object,
            mockEventPublisher.Object
        );

        // Act
        var resultado = await _productoService.CreateAsync(dto);

        // Assert - Clean validation error handling!
        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().BeOfType<ValidationError>();
        ((ValidationError)resultado.Error).ValidationErrors.Should().ContainKey("Precio");
    }

    #endregion

    #region Comparison Test - Exception vs Result

    /// <summary>
    /// TEST DE COMPARACIÓN: Muestra la consistencia del Result Pattern
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
    public void Comparison_AmbosUsanResultPattern_EscenarioNoEncontrado()
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
        resultadoCategoria.Error.Should().BeOfType<NotFoundError>();

        // PRODUCTOSERVICE (Result Pattern):
        // - Same pattern, same verification
        // - No exception handling needed
        var resultadoProducto = _productoService.FindByIdAsync(999).Result;
        resultadoProducto.IsFailure.Should().BeTrue();
        resultadoProducto.Error.Should().BeOfType<NotFoundError>();

        // Ambos servicios ahora manejan errores de manera consistente
    }

    #endregion
}

/// <summary>
/// RESUMEN: Comparación de Tests
/// 
/// ╔════════════════════════════════╦═════════════════════════════════╗
/// ║   Tests con Excepciones        ║      Tests con Result Pattern    ║
/// ╠════════════════════════════════╬═════════════════════════════════╣
/// ║ Assert.ThrowsAsync requerido   ║ Verificación directa IsFailure   ║
/// ║ Coincidencia tipo excepción    ║ Verificación tipo error          ║
/// ║ try/catch en tests             ║ Sin manejo de excepciones        ║
/// ║ Éxito implícito (no lanza)     ║ Éxito explícito IsSuccess        ║
/// ║ Menos legible                 ║ Más legible                      ║
/// ╚════════════════════════════════╩═════════════════════════════════╝
/// </summary>


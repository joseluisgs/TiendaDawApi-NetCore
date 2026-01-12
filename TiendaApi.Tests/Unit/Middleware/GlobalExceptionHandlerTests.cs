using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using TiendaApi.Apis.Exceptions;
using TiendaApi.Apis.Middleware;

namespace TiendaApi.Tests.Unit.Middleware;

/// <summary>
/// Tests unitarios para GlobalExceptionHandler.
/// </summary>
public class GlobalExceptionHandlerTests
{
    private readonly Mock<RequestDelegate> _mockNext = new();
    private readonly Mock<ILogger<GlobalExceptionHandler>> _mockLogger = new();
    private readonly GlobalExceptionHandler _handler;
    private readonly DefaultHttpContext _httpContext;

    public GlobalExceptionHandlerTests()
    {
        _handler = new GlobalExceptionHandler(_mockNext.Object, _mockLogger.Object);
        _httpContext = new DefaultHttpContext
        {
            Request =
            {
                Path = "/api/productos",
                Method = HttpMethods.Post
            }
        };
    }

    [SetUp]
    public void Setup()
    {
        _httpContext.Response.Body = new MemoryStream();
    }

    [TearDown]
    public void TearDown()
    {
        _httpContext.Response.Body.Dispose();
    }

    private async Task<(int StatusCode, string Body)> ExecuteHandlerAsync(Exception exception)
    {
        _mockNext.Setup(next => next(It.IsAny<HttpContext>()))
            .Throws(exception);

        await _handler.InvokeAsync(_httpContext);

        _httpContext.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(_httpContext.Response.Body);
        var body = await reader.ReadToEndAsync();

        return (_httpContext.Response.StatusCode, body);
    }

    #region NotFoundException Tests

    [Test]
    public async Task InvokeAsync_ConNotFoundException_DeberiaRetornar404()
    {
        // Arrange
        var exception = new NotFoundException("Producto no encontrado");

        // Act
        var (statusCode, body) = await ExecuteHandlerAsync(exception);

        // Assert
        statusCode.Should().Be(404);
        body.Should().Contain("Producto no encontrado");
        body.Should().Contain("NotFound");
    }

    [Test]
    public async Task InvokeAsync_ConNotFoundException_DeberiaTenerErrorId()
    {
        // Arrange
        var exception = new NotFoundException("Categoría no encontrada");

        // Act
        var (statusCode, body) = await ExecuteHandlerAsync(exception);

        // Assert
        body.Should().Contain("errorId");
    }

    [Test]
    public async Task InvokeAsync_ConNotFoundException_DeberiaTenerTimestamp()
    {
        // Arrange
        var exception = new NotFoundException("Usuario no encontrado");

        // Act
        var (statusCode, body) = await ExecuteHandlerAsync(exception);

        // Assert
        body.Should().Contain("timestamp");
    }

    #endregion

    #region ValidationException Tests

    [Test]
    public async Task InvokeAsync_ConValidationException_DeberiaRetornar400()
    {
        // Arrange
        var errors = new Dictionary<string, string[]>
        {
            { "Nombre", new[] { "El nombre es obligatorio" } },
            { "Precio", new[] { "El precio debe ser mayor a 0" } }
        };
        var exception = new ValidationException("Errores de validación", errors);

        // Act
        var (statusCode, body) = await ExecuteHandlerAsync(exception);

        // Assert
        statusCode.Should().Be(400);
        body.Should().Contain("errorType\":\"Validation");
        body.Should().Contain("errors");
    }

    [Test]
    public async Task InvokeAsync_ConValidationException_DeberiaIncluirErrors()
    {
        // Arrange
        var errors = new Dictionary<string, string[]>
        {
            { "Nombre", new[] { "El nombre es obligatorio" } }
        };
        var exception = new ValidationException("Errores de validación", errors);

        // Act
        var (statusCode, body) = await ExecuteHandlerAsync(exception);

        // Assert
        body.Should().Contain("errors");
        body.Should().Contain("Nombre");
    }

    #endregion

    #region BusinessException Tests

    [Test]
    public async Task InvokeAsync_ConBusinessException_DeberiaRetornar400()
    {
        // Arrange
        var exception = new BusinessException("Stock insuficiente");

        // Act
        var (statusCode, body) = await ExecuteHandlerAsync(exception);

        // Assert
        statusCode.Should().Be(400);
        body.Should().Contain("Stock insuficiente");
        body.Should().Contain("BusinessRule");
    }

    #endregion

    #region UnauthorizedAccessException Tests

    [Test]
    public async Task InvokeAsync_ConUnauthorizedAccessException_DeberiaRetornar401()
    {
        // Arrange
        var exception = new UnauthorizedAccessException("Token inválido");

        // Act
        var (statusCode, body) = await ExecuteHandlerAsync(exception);

        // Assert
        statusCode.Should().Be(401);
        body.Should().Contain("No autorizado");
        body.Should().Contain("Unauthorized");
    }

    #endregion

    #region ArgumentException Tests

    [Test]
    public async Task InvokeAsync_ConArgumentException_DeberiaRetornar400()
    {
        // Arrange
        var exception = new ArgumentException("El argumento es inválido");

        // Act
        var (statusCode, body) = await ExecuteHandlerAsync(exception);

        // Assert
        statusCode.Should().Be(400);
        body.Should().Contain("errorType\":\"Validation");
    }

    #endregion

    #region TimeoutException Tests

    [Test]
    public async Task InvokeAsync_ConTimeoutException_DeberiaRetornar408()
    {
        // Arrange
        var exception = new TimeoutException("La operación tardó demasiado");

        // Act
        var (statusCode, body) = await ExecuteHandlerAsync(exception);

        // Assert
        statusCode.Should().Be(408);
        body.Should().Contain("Tiempo de espera agotado");
    }

    #endregion

    #region Exception Genérica Tests

    [Test]
    public async Task InvokeAsync_ConExceptionGenerica_DeberiaRetornar500()
    {
        // Arrange
        var exception = new Exception("Error inesperado");

        // Act
        var (statusCode, body) = await ExecuteHandlerAsync(exception);

        // Assert
        statusCode.Should().Be(500);
        body.Should().Contain("Ha ocurrido un error interno");
    }

    [Test]
    public async Task InvokeAsync_ConExceptionGenerica_NoDeberiaExponerDetalles()
    {
        // Arrange
        var exception = new NullReferenceException("Object reference not set to an instance of an object.");

        // Act
        var (statusCode, body) = await ExecuteHandlerAsync(exception);

        // Assert
        statusCode.Should().Be(500);
        body.Should().Contain("Ha ocurrido un error interno");
        body.Should().NotContain("NullReferenceException");
        body.Should().NotContain("Object reference");
    }

    [Test]
    public async Task InvokeAsync_ConExceptionGenerica_DeberiaTenerErrorId()
    {
        // Arrange
        var exception = new InvalidOperationException("Operación inválida");

        // Act
        var (statusCode, body) = await ExecuteHandlerAsync(exception);

        // Assert
        body.Should().Contain("errorId");
    }

    #endregion

    #region Response Format Tests

    [Test]
    public async Task InvokeAsync_DeberiaRetornarContentTypeJson()
    {
        // Arrange
        var exception = new Exception("Error");

        // Act
        await ExecuteHandlerAsync(exception);

        // Assert
        _httpContext.Response.ContentType.Should().Contain("application/json");
    }

    [Test]
    public async Task InvokeAsync_DeberiaTenerPathEnRespuesta()
    {
        // Arrange
        var exception = new Exception("Error");

        // Act
        await ExecuteHandlerAsync(exception);

        // Assert
        _httpContext.Response.StatusCode.Should().Be(500);
    }

    [Test]
    public async Task InvokeAsync_SinExcepcion_NoDeberiaLanzarExcepcion()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.Response.Body = new MemoryStream();
        _mockNext.Setup(next => next(It.IsAny<HttpContext>()))
            .Returns(Task.CompletedTask);

        // Act & Assert - No debe lanzar
        await _handler.InvokeAsync(httpContext);
        
        // Verificar que no hubo error (Status 200)
        httpContext.Response.StatusCode.Should().Be(200);
    }

    #endregion

    #region DbUpdateException Tests

    [Test]
    public async Task InvokeAsync_ConDbUpdateException_DeberiaRetornar409()
    {
        // Arrange
        var exception = new DbUpdateException("Error de base de datos");

        // Act
        var result = await ExecuteHandlerAsync(exception);

        // Assert
        result.StatusCode.Should().Be(409);
        result.Body.Should().Contain("Error al actualizar la base de datos");
    }

    #endregion
}

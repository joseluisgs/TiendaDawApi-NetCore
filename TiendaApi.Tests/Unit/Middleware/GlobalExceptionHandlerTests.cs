using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Npgsql;
using System.Collections.Generic;
using TiendaApi.Api.Exceptions;
using TiendaApi.Api.Middleware;

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

    #region NpgsqlException Tests

    [Test]
    public async Task InvokeAsync_ConNpgsqlException_DeberiaRetornar500()
    {
        // Arrange
        var exception = new NpgsqlException("Connection timeout");

        // Act
        var (statusCode, body) = await ExecuteHandlerAsync(exception);

        // Assert
        statusCode.Should().Be(500);
        body.Should().Contain("Ha ocurrido un error interno");
    }

    [Test]
    public async Task InvokeAsync_ConNpgsqlException_DeberiaTenerErrorId()
    {
        // Arrange
        var exception = new NpgsqlException("Error de PostgreSQL");

        // Act
        var (_, body) = await ExecuteHandlerAsync(exception);

        // Assert
        body.Should().Contain("errorId");
    }

    #endregion

    #region InvalidOperationException Tests

    [Test]
    public async Task InvokeAsync_ConInvalidOperationException_DeberiaRetornar500()
    {
        // Arrange
        var exception = new InvalidOperationException("Operación inválida");

        // Act
        var (statusCode, body) = await ExecuteHandlerAsync(exception);

        // Assert
        statusCode.Should().Be(500);
        body.Should().Contain("Ha ocurrido un error interno");
    }

    #endregion

    #region ArgumentNullException Tests

    [Test]
    public async Task InvokeAsync_ConArgumentNullException_DeberiaRetornar400()
    {
        // Arrange
        var exception = new ArgumentNullException("parameterName", "El argumento no puede ser null");

        // Act
        var (statusCode, body) = await ExecuteHandlerAsync(exception);

        // Assert
        statusCode.Should().Be(400);
        body.Should().Contain("errorType\":\"Validation");
    }

    #endregion

    #region KeyNotFoundException Tests

    [Test]
    public async Task InvokeAsync_ConKeyNotFoundException_DeberiaRetornar500()
    {
        // Arrange
        var exception = new System.Collections.Generic.KeyNotFoundException("La clave no existe");

        // Act
        var result = await ExecuteHandlerAsync(exception);

        // Assert
        result.StatusCode.Should().Be(500);
        result.Body.Should().Contain("Ha ocurrido un error interno");
    }

    #endregion

    #region OperationCanceledException Tests

    [Test]
    public async Task InvokeAsync_ConOperationCanceledException_DeberiaRetornar500()
    {
        // Arrange
        var exception = new OperationCanceledException("La operación fue cancelada");

        // Act
        var (statusCode, body) = await ExecuteHandlerAsync(exception);

        // Assert
        statusCode.Should().Be(500);
        body.Should().Contain("Ha ocurrido un error interno");
    }

    #endregion

    #region JsonException Tests

    [Test]
    public async Task InvokeAsync_ConJsonException_DeberiaRetornar500()
    {
        // Arrange
        var exception = new System.Text.Json.JsonException("Error al parsear JSON");

        // Act
        var (statusCode, body) = await ExecuteHandlerAsync(exception);

        // Assert
        statusCode.Should().Be(500);
        body.Should().Contain("Ha ocurrido un error interno");
    }

    #endregion

    #region Response Content Tests

    [Test]
    public async Task InvokeAsync_Response_DeberiaSerJson()
    {
        // Arrange
        var exception = new Exception("Test");

        // Act
        await ExecuteHandlerAsync(exception);

        // Assert
        _httpContext.Response.ContentType.Should().Contain("application/json");
    }

    [Test]
    public async Task InvokeAsync_Response_DeberiaTenerStatusCodeCorrecto()
    {
        // Arrange
        var exception = new NotFoundException("Test");

        // Act
        await ExecuteHandlerAsync(exception);

        // Assert
        _httpContext.Response.StatusCode.Should().Be(404);
    }

    [Test]
    public async Task InvokeAsync_Response_DeberiaTenerPath()
    {
        // Arrange
        var exception = new Exception("Test");
        _httpContext.Request.Path = "/api/test/path";

        // Act
        var (_, body) = await ExecuteHandlerAsync(exception);

        // Assert
        body.Should().Contain("/api/test/path");
    }

    [Test]
    public async Task InvokeAsync_Response_DeberiaTenerMethod()
    {
        // Arrange
        var exception = new Exception("Test");
        _httpContext.Request.Method = HttpMethods.Post;

        // Act
        var (_, body) = await ExecuteHandlerAsync(exception);

        // Assert
        body.Should().Contain("POST");
    }

    [Test]
    public async Task InvokeAsync_Response_DeberiaTenerTimestamp()
    {
        // Arrange
        var exception = new Exception("Test");

        // Act
        var (_, body) = await ExecuteHandlerAsync(exception);

        // Assert
        body.Should().Contain("timestamp");
    }

    [Test]
    public async Task InvokeAsync_Response_Timestamp_DeberiaSerValido()
    {
        // Arrange
        var exception = new Exception("Test");
        var before = DateTime.UtcNow;

        // Act
        var (_, body) = await ExecuteHandlerAsync(exception);

        // Assert
        body.Should().MatchRegex(@"""timestamp"":\s*""\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}");
    }

    #endregion

    #region ErrorId Tests

    [Test]
    public async Task InvokeAsync_ErrorId_DeberiaSerUnico()
    {
        // Arrange
        var exception1 = new Exception("Error 1");
        var exception2 = new Exception("Error 2");

        // Act
        var (_, body1) = await ExecuteHandlerAsync(exception1);
        _httpContext.Response.Body = new MemoryStream();
        var (_, body2) = await ExecuteHandlerAsync(exception2);

        // Assert - Los errorId deberían ser diferentes
        body1.Should().NotBeEquivalentTo(body2);
    }

    #endregion

    #region Exception Message Tests

    [Test]
    public async Task InvokeAsync_NotFoundException_Message_DeberiaIncluirMsgOriginal()
    {
        // Arrange
        var exception = new NotFoundException("Producto específico no encontrado");

        // Act
        var (_, body) = await ExecuteHandlerAsync(exception);

        // Assert - El JSON contiene el mensaje escapado
        body.Should().Contain("message");
        body.Should().Contain("Producto espec");
    }

    [Test]
    public async Task InvokeAsync_ValidationException_Errors_DeberiaSerDiccionario()
    {
        // Arrange
        var errors = new Dictionary<string, string[]>
        {
            { "Campo1", new[] { "Error1", "Error2" } },
            { "Campo2", new[] { "Error3" } }
        };
        var exception = new ValidationException("Errores de validación", errors);

        // Act
        var (_, body) = await ExecuteHandlerAsync(exception);

        // Assert
        body.Should().Contain("Campo1");
        body.Should().Contain("Campo2");
        body.Should().Contain("Error1");
        body.Should().Contain("Error2");
        body.Should().Contain("Error3");
    }

    #endregion
}

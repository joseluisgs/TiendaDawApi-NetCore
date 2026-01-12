# 14. Unit Testing con NUnit y Moq

Los tests unitarios aseguran que cada componente funciona correctamente de forma aislada.

---

## 1. Piramide de Testing

```mermaid
pie
    title Proporcion de Tests
    "Unit Tests (70%)" : 70
    "Integration Tests (20%)" : 20
    "E2E Tests (10%)" : 10
```

---

## 2. Instalacion

```bash
dotnet add package NUnit
dotnet add package NUnit3TestAdapter
dotnet add package Moq
dotnet add package FluentAssertions
```

---

## 2. Estructura de Tests

```
TiendaApi.Tests/
├── Unit/
│   ├── Controllers/
│   │   ├── AuthControllerTests.cs
│   │   └── ProductosControllerTests.cs
│   ├── Services/
│   │   ├── CategoriaServiceTests.cs
│   │   └── ProductoServiceTests.cs
│   ├── Validators/
│   │   ├── Productos/
│   │   │   └── ProductoRequestValidatorTests.cs
│   │   ├── Categorias/
│   │   │   └── CategoriaRequestValidatorTests.cs
│   │   ├── Pedidos/
│   │   │   ├── PedidoRequestValidatorTests.cs
│   │   │   └── PedidoItemRequestValidatorTests.cs
│   │   └── Usuarios/
│   │       ├── RegisterValidatorTests.cs
│   │       └── LoginValidatorTests.cs
│   ├── Mappers/
│   │   └── ProductoMapperTests.cs
│   └── Middleware/
│       └── GlobalExceptionHandlerTests.cs
```

### Tests de Validadores

```csharp
using FluentValidation.TestHelper;
using TiendaApi.Apis.Dtos.Productos;
using TiendaApi.Apis.Validators.Productos;

namespace TiendaApi.Tests.Unit.Validators.Productos;

public class ProductoRequestValidatorTests
{
    private readonly ProductoRequestValidator _validator = new();

    [Test]
    public void CreateAsync_ConNombreVacio_DeberiaTenerError()
    {
        var dto = new ProductoRequestDto { Nombre = "", Precio = 10, Stock = 5, CategoriaId = 1 };
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.Nombre)
            .WithErrorMessage("El nombre es obligatorio");
    }

    [Test]
    public void CreateAsync_ConPrecioNegativo_DeberiaTenerError()
    {
        var dto = new ProductoRequestDto { Nombre = "Test", Precio = -10, Stock = 5, CategoriaId = 1 };
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.Precio)
            .WithErrorMessage("El precio debe ser mayor a 0");
    }

    [Test]
    public void CreateAsync_ConDtoValido_NoDeberiaTenerErrores()
    {
        var dto = new ProductoRequestDto
        {
            Nombre = "iPhone 15 Pro",
            Precio = 999.99m,
            Stock = 50,
            CategoriaId = 1,
            Imagen = "https://ejemplo.com/iphone.jpg"
        };
        var result = _validator.TestValidate(dto);
        result.ShouldNotHaveAnyValidationErrors();
    }
}
```

### Tests de Middleware

```csharp
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using TiendaApi.Apis.Exceptions;
using TiendaApi.Apis.Middleware;

namespace TiendaApi.Tests.Unit.Middleware;

public class GlobalExceptionHandlerTests
{
    private readonly Mock<RequestDelegate> _mockNext = new();
    private readonly Mock<ILogger<GlobalExceptionHandler>> _mockLogger = new();
    private readonly GlobalExceptionHandler _handler;

    [SetUp]
    public void Setup()
    {
        _handler = new GlobalExceptionHandler(_mockNext.Object, _mockLogger.Object);
    }

    [Test]
    public async Task InvokeAsync_ConNotFoundException_DeberiaRetornar404()
    {
        // Arrange
        var exception = new NotFoundException("Producto no encontrado");
        var httpContext = new DefaultHttpContext();
        httpContext.Response.Body = new MemoryStream();
        _mockNext.Setup(next => next(It.IsAny<HttpContext>())).Throws(exception);

        // Act
        await _handler.InvokeAsync(httpContext);

        // Assert
        httpContext.Response.StatusCode.Should().Be(404);
    }

    [Test]
    public async Task InvokeAsync_ConExceptionGenerica_NoDeberiaExponerDetalles()
    {
        // Arrange
        var exception = new NullReferenceException("Object reference not set");
        var httpContext = new DefaultHttpContext();
        httpContext.Response.Body = new MemoryStream();
        _mockNext.Setup(next => next(It.IsAny<HttpContext>())).Throws(exception);

        // Act
        await _handler.InvokeAsync(httpContext);

        // Assert
        httpContext.Response.StatusCode.Should().Be(500);
        httpContext.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(httpContext.Response.Body);
        var body = await reader.ReadToEndAsync();
        body.Should().Contain("Ha ocurrido un error interno");
        body.Should().NotContain("NullReferenceException");
    }
}
```

---

## 3. Test de Controlador

```csharp
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using TiendaApi.Apis.Controllers;
using TiendaApi.Apis.Dtos.Categorias;
using TiendaApi.Apis.Errors;
using TiendaApi.Apis.Services.Categorias;

namespace TiendaApi.Tests.Unit.Controllers;

public class CategoriasControllerTests
{
    private readonly Mock<ICategoriaService> _mockService;
    private readonly CategoriasController _controller;

    public CategoriasControllerTests()
    {
        _mockService = new Mock<ICategoriaService>();
        _controller = new CategoriasController(_mockService.Object);
    }

    [Test]
    public async Task GetAll_ConCategoriasExistentes_RetornaOkConLista()
    {
        // Arrange
        var categorias = new List<CategoriaDto>
        {
            new() { Id = 1, Nombre = "Electronica" },
            new() { Id = 2, Nombre = "Ropa" }
        };

        _mockService.Setup(s => s.FindAllAsync())
            .ReturnsAsync(Result.Success<IEnumerable<CategoriaDto>, DomainError>(categorias));

        // Act
        var result = await _controller.GetAll();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedCategorias = okResult.Value.Should().BeAssignableTo<IEnumerable<CategoriaDto>>().Subject;
        returnedCategorias.Should().HaveCount(2);
    }

    [Test]
    public async Task GetById_ConIdExistente_RetornaOkConCategoria()
    {
        // Arrange
        var categoria = new CategoriaDto { Id = 1, Nombre = "Electronica" };

        _mockService.Setup(s => s.FindByIdAsync(1))
            .ReturnsAsync(Result.Success<CategoriaDto, DomainError>(categoria));

        // Act
        var result = await _controller.GetById(1);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().NotBeNull();
    }

    [Test]
    public async Task GetById_ConIdNoExistente_RetornaNotFound()
    {
        // Arrange
        _mockService.Setup(s => s.FindByIdAsync(999))
            .ReturnsAsync(Result.Failure<CategoriaDto, DomainError>(
                DomainError.NotFound("Categoria no encontrada")));

        // Act
        var result = await _controller.GetById(999);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }
}
```

---

## 4. Test de Servicio

```csharp
public class CategoriaServiceTests
{
    private readonly Mock<ICategoriaRepository> _mockRepository;
    private readonly Mock<ILogger<CategoriaService>> _mockLogger;
    private readonly CategoriaService _service;

    public CategoriaServiceTests()
    {
        _mockRepository = new Mock<ICategoriaRepository>();
        _mockLogger = new Mock<ILogger<CategoriaService>>();
        _service = new CategoriaService(_mockRepository.Object, _mockLogger.Object);
    }

    [Test]
    public async Task FindById_ConIdExistente_RetornaExito()
    {
        // Arrange
        var categoria = new Categoria { Id = 1, Nombre = "Electronica" };
        _mockRepository.Setup(r => r.FindByIdAsync(1))
            .ReturnsAsync(categoria);

        // Act
        var resultado = await _service.FindByIdAsync(1);

        // Assert
        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Id.Should().Be(1);
    }

    [Test]
    public async Task FindById_ConIdNoExistente_RetornaFallo()
    {
        // Arrange
        _mockRepository.Setup(r => r.FindByIdAsync(999))
            .ReturnsAsync((Categoria?)null);

        // Act
        var resultado = await _service.FindByIdAsync(999);

        // Assert
        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Type.Should().Be(ErrorType.NotFound);
    }

    [Test]
    public async Task Create_ConNombreDuplicado_RetornaConflicto()
    {
        // Arrange
        var dto = new CategoriaRequestDto { Nombre = "Electronica" };
        _mockRepository.Setup(r => r.ExistsByNombreAsync("Electronica", null))
            .ReturnsAsync(true);

        // Act
        var resultado = await _service.CreateAsync(dto);

        // Assert
        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Type.Should().Be(ErrorType.Conflict);
    }
}
```

---

## 5. Beneficios

- **Confianza**: Cambios seguros
- **Documentacion**: Tests documentan comportamiento esperado
- **Refactorizacion**: Cambios sin miedo
- **Regresion**: Detectar errores rapido

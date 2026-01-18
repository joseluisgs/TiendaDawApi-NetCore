using CSharpFunctionalExtensions;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using TiendaApi.Apis.Dtos.Productos;
using TiendaApi.Apis.Errors;
using TiendaApi.Apis.GraphQL.Inputs;
using TiendaApi.Apis.GraphQL.Mutations;
using TiendaApi.Apis.Services.Productos;

namespace TiendaApi.Tests.Unit.GraphQL;

/// <summary>
/// Tests unitarios para ProductoMutation (GraphQL).
/// </summary>
public class ProductoMutationTests
{
    private Mock<IProductoService> _productoServiceMock = null!;
    private ProductoMutation _mutation = null!;

    [SetUp]
    public void Setup()
    {
        _productoServiceMock = new Mock<IProductoService>();
        _mutation = new ProductoMutation(_productoServiceMock.Object);
    }

    #region CreateProducto Tests

    [Test]
    public async Task CreateProducto_ConDtoValido_RetornaProductoCreado()
    {
        // Arrange
        var input = new CreateProductoInput
        {
            Nombre = "Laptop Dell",
            Descripcion = "Laptop de alto rendimiento",
            Precio = 1299.99m,
            Stock = 10,
            CategoriaId = 1
        };

        var productoCreado = new ProductoDto(
            1,
            "Laptop Dell",
            "Laptop de alto rendimiento",
            1299.99m,
            10,
            null,
            1,
            "Electrónica",
            DateTime.UtcNow,
            DateTime.UtcNow
        );

        _productoServiceMock
            .Setup(s => s.CreateAsync(It.IsAny<ProductoRequestDto>()))
            .ReturnsAsync(Result.Success<ProductoDto, DomainError>(productoCreado));

        // Act
        var result = await _mutation.CreateProducto(input, _productoServiceMock.Object);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(1);
        result.Value.Nombre.Should().Be("Laptop Dell");
        _productoServiceMock.Verify(s => s.CreateAsync(It.IsAny<ProductoRequestDto>()), Times.Once);
    }

    [Test]
    public async Task CreateProducto_ConErrorValidacion_RetornaFailure()
    {
        // Arrange
        var input = new CreateProductoInput
        {
            Nombre = "A", // Nombre muy corto
            Precio = -100, // Precio inválido
            Stock = 10,
            CategoriaId = 1
        };

        var error = new ValidationError("El nombre debe tener entre 3 y 200 caracteres", new());

        _productoServiceMock
            .Setup(s => s.CreateAsync(It.IsAny<ProductoRequestDto>()))
            .ReturnsAsync(Result.Failure<ProductoDto, DomainError>(error));

        // Act
        var result = await _mutation.CreateProducto(input, _productoServiceMock.Object);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<ValidationError>();
    }

    [Test]
    public async Task CreateProducto_ConCategoriaNoExistente_RetornaNotFound()
    {
        // Arrange
        var input = new CreateProductoInput
        {
            Nombre = "Producto Nuevo",
            Precio = 99.99m,
            Stock = 10,
            CategoriaId = 999
        };

        var error = new NotFoundError("La categoría no existe");

        _productoServiceMock
            .Setup(s => s.CreateAsync(It.IsAny<ProductoRequestDto>()))
            .ReturnsAsync(Result.Failure<ProductoDto, DomainError>(error));

        // Act
        var result = await _mutation.CreateProducto(input, _productoServiceMock.Object);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<NotFoundError>();
    }

    [Test]
    public async Task CreateProducto_ConNombreDuplicado_RetornaConflict()
    {
        // Arrange
        var input = new CreateProductoInput
        {
            Nombre = "Laptop Dell", // Ya existe
            Precio = 99.99m,
            Stock = 10,
            CategoriaId = 1
        };

        var error = new ConflictError("Ya existe un producto con ese nombre");

        _productoServiceMock
            .Setup(s => s.CreateAsync(It.IsAny<ProductoRequestDto>()))
            .ReturnsAsync(Result.Failure<ProductoDto, DomainError>(error));

        // Act
        var result = await _mutation.CreateProducto(input, _productoServiceMock.Object);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<ConflictError>();
    }

    #endregion

    #region UpdateProducto Tests

    [Test]
    public async Task UpdateProducto_ConIdExistenteYDtoValido_RetornaProductoActualizado()
    {
        // Arrange
        long productoId = 1;
        var input = new UpdateProductoInput
        {
            Precio = 1499.99m,
            Stock = 5
        };

        var productoActualizado = new ProductoDto(
            productoId,
            "Laptop Dell Actualizado",
            "Laptop actualizada",
            1499.99m,
            5,
            null,
            1,
            "Electrónica",
            DateTime.UtcNow,
            DateTime.UtcNow
        );

        var productoExistente = new ProductoDto(
            productoId,
            "Laptop Dell",
            "Laptop original",
            1299.99m,
            10,
            null,
            1,
            "Electrónica",
            DateTime.UtcNow,
            DateTime.UtcNow
        );

        _productoServiceMock
            .Setup(s => s.FindByIdAsync(productoId))
            .ReturnsAsync(Result.Success<ProductoDto, DomainError>(productoExistente));

        _productoServiceMock
            .Setup(s => s.UpdateAsync(productoId, It.IsAny<ProductoRequestDto>()))
            .ReturnsAsync(Result.Success<ProductoDto, DomainError>(productoActualizado));

        // Act
        var result = await _mutation.UpdateProducto(productoId, input, _productoServiceMock.Object);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Precio.Should().Be(1499.99m);
        result.Value.Stock.Should().Be(5);
    }

    [Test]
    public async Task UpdateProducto_ConIdNoExistente_RetornaNotFound()
    {
        // Arrange
        long productoId = 999;
        var input = new UpdateProductoInput { Precio = 99.99m };

        _productoServiceMock
            .Setup(s => s.FindByIdAsync(productoId))
            .ReturnsAsync(Result.Failure<ProductoDto, DomainError>(
                NotFoundError.FromId(productoId, "Producto")));

        // Act
        var result = await _mutation.UpdateProducto(productoId, input, _productoServiceMock.Object);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<NotFoundError>();
    }

    [Test]
    public async Task UpdateProducto_SoloPrecio_ActualizaSoloPrecio()
    {
        // Arrange
        long productoId = 1;
        var input = new UpdateProductoInput { Precio = 1999.99m };

        var productoExistente = new ProductoDto(
            productoId,
            "Producto Original",
            "Descripción",
            999.99m,
            20,
            null,
            1,
            "Electrónica",
            DateTime.UtcNow,
            DateTime.UtcNow
        );

        var productoActualizado = new ProductoDto(
            productoId,
            "Producto Original",
            "Descripción",
            1999.99m,
            20,
            null,
            1,
            "Electrónica",
            DateTime.UtcNow,
            DateTime.UtcNow
        );

        _productoServiceMock
            .Setup(s => s.FindByIdAsync(productoId))
            .ReturnsAsync(Result.Success<ProductoDto, DomainError>(productoExistente));

        _productoServiceMock
            .Setup(s => s.UpdateAsync(productoId, It.IsAny<ProductoRequestDto>()))
            .ReturnsAsync(Result.Success<ProductoDto, DomainError>(productoActualizado));

        // Act
        var result = await _mutation.UpdateProducto(productoId, input, _productoServiceMock.Object);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Precio.Should().Be(1999.99m);
    }

    #endregion

    #region DeleteProducto Tests

    [Test]
    public async Task DeleteProducto_ConIdExistente_RetornaSuccess()
    {
        // Arrange
        long productoId = 1;

        _productoServiceMock
            .Setup(s => s.DeleteAsync(productoId))
            .ReturnsAsync(UnitResult.Success<DomainError>());

        // Act
        var result = await _mutation.DeleteProducto(productoId, _productoServiceMock.Object);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _productoServiceMock.Verify(s => s.DeleteAsync(productoId), Times.Once);
    }

    [Test]
    public async Task DeleteProducto_ConIdNoExistente_RetornaNotFound()
    {
        // Arrange
        long productoId = 999;

        _productoServiceMock
            .Setup(s => s.DeleteAsync(productoId))
            .ReturnsAsync(UnitResult.Failure<DomainError>(
                NotFoundError.FromId(productoId, "Producto")));

        // Act
        var result = await _mutation.DeleteProducto(productoId, _productoServiceMock.Object);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<NotFoundError>();
    }

    [Test]
    public async Task DeleteProducto_ConProductoConPedidos_RetornaBusinessRuleError()
    {
        // Arrange
        long productoId = 1;

        _productoServiceMock
            .Setup(s => s.DeleteAsync(productoId))
            .ReturnsAsync(UnitResult.Failure<DomainError>(
                new BusinessRuleError("No se puede eliminar un producto con pedidos asociados")));

        // Act
        var result = await _mutation.DeleteProducto(productoId, _productoServiceMock.Object);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<BusinessRuleError>();
    }

    #endregion

    #region Input Mapping Tests

    [Test]
    public async Task CreateProducto_MapeaInputCorrectamente()
    {
        // Arrange
        var input = new CreateProductoInput
        {
            Nombre = "Nuevo Producto",
            Descripcion = "Descripción del producto",
            Precio = 599.99m,
            Stock = 25,
            CategoriaId = 2,
            Imagen = "https://ejemplo.com/imagen.jpg"
        };

        ProductoRequestDto? capturedDto = null;

        _productoServiceMock
            .Setup(s => s.CreateAsync(It.IsAny<ProductoRequestDto>()))
            .Callback<ProductoRequestDto>(dto => capturedDto = dto)
            .ReturnsAsync(Result.Success<ProductoDto, DomainError>(
                new ProductoDto(1, "test", "test", 1, 1, null, 1, "", DateTime.UtcNow, DateTime.UtcNow)));

        // Act
        await _mutation.CreateProducto(input, _productoServiceMock.Object);

        // Assert
        capturedDto.Should().NotBeNull();
        capturedDto!.Nombre.Should().Be("Nuevo Producto");
        capturedDto.Descripcion.Should().Be("Descripción del producto");
        capturedDto.Precio.Should().Be(599.99m);
        capturedDto.Stock.Should().Be(25);
        capturedDto.CategoriaId.Should().Be(2);
        capturedDto.Imagen.Should().Be("https://ejemplo.com/imagen.jpg");
    }

    #endregion
}

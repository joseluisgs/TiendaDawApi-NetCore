using FluentAssertions;
using Moq;
using NUnit.Framework;
using TiendaApi.Api.Dtos.Productos;
using TiendaApi.Api.Dtos.Common;
using TiendaApi.Api.Errors;
using TiendaApi.Api.GraphQL.Inputs;
using TiendaApi.Api.GraphQL.Mutations;
using TiendaApi.Api.Services.Productos;
using CSharpFunctionalExtensions;

namespace TiendaApi.Tests.Unit.GraphQL;

[TestFixture]
[Category("Unit")]
[Category("GraphQL")]
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
        var input = new CreateProductoInput
        {
            Nombre = "Nuevo Producto",
            Precio = 99.99m,
            Stock = 10,
            CategoriaId = 1
        };

        var productoCreado = new ProductoDto(
            1,
            "Nuevo Producto",
            "Descripción",
            99.99m,
            10,
            "image.jpg",
            1,
            "Electrónica",
            DateTime.UtcNow,
            DateTime.UtcNow);

        _productoServiceMock
            .Setup(s => s.CreateAsync(It.IsAny<ProductoRequestDto>()))
            .ReturnsAsync(Result.Success<ProductoDto, DomainError>(productoCreado));

        var result = await _mutation.CreateProducto(input, _productoServiceMock.Object);

        result.Should().NotBeNull();
    }

    [Test]
    public async Task CreateProducto_ConErrorValidacion_RetornaNull()
    {
        var input = new CreateProductoInput
        {
            Nombre = "",
            Precio = -100m,
            Stock = 0,
            CategoriaId = 1
        };

        _productoServiceMock
            .Setup(s => s.CreateAsync(It.IsAny<ProductoRequestDto>()))
            .ReturnsAsync(Result.Failure<ProductoDto, DomainError>(new BusinessRuleError("Error")));

        var result = await _mutation.CreateProducto(input, _productoServiceMock.Object);

        result.Should().BeNull();
    }

    #endregion

    #region UpdateProducto Tests

    [Test]
    public async Task UpdateProducto_ConProductoExistente_RetornaProductoActualizado()
    {
        long productoId = 1;
        var input = new UpdateProductoInput { Nombre = "Nuevo Nombre" };

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
            DateTime.UtcNow);

        _productoServiceMock.Setup(s => s.FindByIdAsync(productoId))
            .ReturnsAsync(Result.Success<ProductoDto, DomainError>(productoExistente));

        _productoServiceMock.Setup(s => s.UpdateAsync(productoId, It.IsAny<ProductoRequestDto>()))
            .ReturnsAsync(Result.Success<ProductoDto, DomainError>(productoExistente));

        var result = await _mutation.UpdateProducto(productoId, input, _productoServiceMock.Object);

        result.Should().NotBeNull();
    }

    [Test]
    public async Task UpdateProducto_ConProductoNoExistente_RetornaNull()
    {
        long productoId = 999;
        var input = new UpdateProductoInput { Nombre = "Nuevo Nombre" };

        _productoServiceMock.Setup(s => s.FindByIdAsync(productoId))
            .ReturnsAsync(Result.Failure<ProductoDto, DomainError>(new NotFoundError("No encontrado")));

        var result = await _mutation.UpdateProducto(productoId, input, _productoServiceMock.Object);

        result.Should().BeNull();
    }

    #endregion

    #region DeleteProducto Tests

    [Test]
    public async Task DeleteProducto_ConProductoExistente_RetornaTrue()
    {
        long productoId = 1;

        _productoServiceMock.Setup(s => s.DeleteAsync(productoId))
            .ReturnsAsync(UnitResult.Success<DomainError>());

        var result = await _mutation.DeleteProducto(productoId, _productoServiceMock.Object);

        result.Should().BeTrue();
    }

    [Test]
    public async Task DeleteProducto_ConProductoNoExistente_RetornaFalse()
    {
        long productoId = 999;

        _productoServiceMock.Setup(s => s.DeleteAsync(productoId))
            .ReturnsAsync(UnitResult.Failure<DomainError>(new NotFoundError("No encontrado")));

        var result = await _mutation.DeleteProducto(productoId, _productoServiceMock.Object);

        result.Should().BeFalse();
    }

    #endregion
}
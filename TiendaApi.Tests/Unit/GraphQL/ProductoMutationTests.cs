using FluentAssertions;
using Moq;
using NUnit.Framework;
using TiendaApi.Api.Dtos.Productos;
using TiendaApi.Api.GraphQL.Inputs;
using TiendaApi.Api.GraphQL.Mutations;
using TiendaApi.Api.Services.Productos;

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
            DateTime.UtcNow);

        _productoServiceMock
            .Setup(s => s.CreateAsync(It.IsAny<ProductoRequestDto>()))
            .ReturnsAsync(productoCreado);

        var result = await _mutation.CreateProducto(input, _productoServiceMock.Object);

        result.Should().NotBeNull();
        result!.Id.Should().Be(1);
        result.Nombre.Should().Be("Laptop Dell");
        _productoServiceMock.Verify(s => s.CreateAsync(It.IsAny<ProductoRequestDto>()), Times.Once);
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
            .ReturnsAsync((ProductoDto?)null);

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

        var productoActualizado = new ProductoDto(
            productoId,
            "Nuevo Nombre",
            "Descripción",
            999.99m,
            20,
            null,
            1,
            "Electrónica",
            DateTime.UtcNow,
            DateTime.UtcNow);

        _productoServiceMock.Setup(s => s.FindByIdAsync(productoId))
            .ReturnsAsync(productoExistente);

        _productoServiceMock.Setup(s => s.UpdateAsync(productoId, It.IsAny<ProductoRequestDto>()))
            .ReturnsAsync(productoActualizado);

        var result = await _mutation.UpdateProducto(productoId, input, _productoServiceMock.Object);

        result.Should().NotBeNull();
        result!.Nombre.Should().Be("Nuevo Nombre");
    }

    [Test]
    public async Task UpdateProducto_ConProductoNoExistente_RetornaNull()
    {
        long productoId = 999;
        var input = new UpdateProductoInput { Nombre = "Nuevo Nombre" };

        _productoServiceMock.Setup(s => s.FindByIdAsync(productoId))
            .ReturnsAsync((ProductoDto?)null);

        var result = await _mutation.UpdateProducto(productoId, input, _productoServiceMock.Object);

        result.Should().BeNull();
    }

    [Test]
    public async Task UpdatePokemon_SoloPrecio_ActualizaSoloPrecio()
    {
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
            DateTime.UtcNow);

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
            DateTime.UtcNow);

        _productoServiceMock.Setup(s => s.FindByIdAsync(productoId))
            .ReturnsAsync(productoExistente);

        _productoServiceMock.Setup(s => s.UpdateAsync(productoId, It.IsAny<ProductoRequestDto>()))
            .ReturnsAsync(productoActualizado);

        var result = await _mutation.UpdateProducto(productoId, input, _productoServiceMock.Object);

        result.Should().NotBeNull();
        result!.Precio.Should().Be(1999.99m);
    }

    #endregion

    #region DeleteProducto Tests

    [Test]
    public async Task DeleteProducto_ConProductoExistente_RetornaTrue()
    {
        long productoId = 1;

        _productoServiceMock.Setup(s => s.DeleteAsync(productoId))
            .ReturnsAsync(true);

        var result = await _mutation.DeleteProducto(productoId, _productoServiceMock.Object);

        result.Should().BeTrue();
    }

    [Test]
    public async Task DeleteProducto_ConProductoNoExistente_RetornaFalse()
    {
        long productoId = 999;

        _productoServiceMock.Setup(s => s.DeleteAsync(productoId))
            .ReturnsAsync(false);

        var result = await _mutation.DeleteProducto(productoId, _productoServiceMock.Object);

        result.Should().BeFalse();
    }

    #endregion
}
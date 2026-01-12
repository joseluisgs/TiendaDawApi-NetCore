using CSharpFunctionalExtensions;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using TiendaApi.Controllers;
using TiendaApi.Dtos.Productos;
using TiendaApi.Errors;
using TiendaApi.Services.Productos;

namespace TiendaApi.Tests.Unit.Controllers;

public class ProductosControllerTests
{
    private readonly Mock<IProductoService> _mockService;
    private readonly ProductosController _controller;

    public ProductosControllerTests()
    {
        _mockService = new Mock<IProductoService>();
        var mockLogger = new Mock<ILogger<ProductosController>>();
        _controller = new ProductosController(_mockService.Object, mockLogger.Object);
    }

    #region GetAll Tests

    /// <summary>
    /// Dado que existen productos, cuando se obtienen todos, entonces retorna 200 OK con la lista.
    /// Returns: 200 OK con lista de productos
    /// </summary>
    [Test]
    public async Task GetAll_ConProductosExistentes_RetornaOkConLista()
    {
        var productos = new List<ProductoDto>
        {
            new ProductoDto { Id = 1, Nombre = "Laptop", Precio = 999.99m, Stock = 10 },
            new ProductoDto { Id = 2, Nombre = "Mouse", Precio = 29.99m, Stock = 50 }
        };

        _mockService.Setup(s => s.FindAllAsync())
            .ReturnsAsync(Result.Success<IEnumerable<ProductoDto>, DomainError>(productos));

        var result = await _controller.GetAll();

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedProductos = okResult.Value.Should().BeAssignableTo<IEnumerable<ProductoDto>>().Subject;
        returnedProductos.Should().HaveCount(2);
    }

    /// <summary>
    /// Dado que no existen productos, cuando se obtienen todos, entonces retorna 200 OK con lista vacía.
    /// Returns: 200 OK con lista vacía
    /// </summary>
    [Test]
    public async Task GetAll_SinProductos_RetornaOkConListaVacia()
    {
        _mockService.Setup(s => s.FindAllAsync())
            .ReturnsAsync(Result.Success<IEnumerable<ProductoDto>, DomainError>(new List<ProductoDto>()));

        var result = await _controller.GetAll();

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedProductos = okResult.Value.Should().BeAssignableTo<IEnumerable<ProductoDto>>().Subject;
        returnedProductos.Should().BeEmpty();
    }

    #endregion

    #region GetById Tests

    /// <summary>
    /// Dado que existe un producto, cuando se obtiene por ID, entonces retorna 200 OK con el producto.
    /// Returns: 200 OK con producto encontrado
    /// </summary>
    [Test]
    public async Task GetById_ConIdExistente_RetornaOkConProducto()
    {
        var producto = new ProductoDto { Id = 1, Nombre = "Laptop", Precio = 999.99m, Stock = 10 };

        _mockService.Setup(s => s.FindByIdAsync(1))
            .ReturnsAsync(Result.Success<ProductoDto, DomainError>(producto));

        var result = await _controller.GetById(1);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedProducto = okResult.Value.Should().BeAssignableTo<ProductoDto>().Subject;
        returnedProducto.Id.Should().Be(1);
        returnedProducto.Nombre.Should().Be("Laptop");
    }

    /// <summary>
    /// Dado que no existe un producto, cuando se obtiene por ID, entonces retorna 404 Not Found.
    /// Returns: 404 Not Found
    /// </summary>
    [Test]
    public async Task GetById_ConIdNoExistente_RetornaNotFound()
    {
        var error = DomainError.NotFound("Producto no encontrado");

        _mockService.Setup(s => s.FindByIdAsync(999))
            .ReturnsAsync(Result.Failure<ProductoDto, DomainError>(error));

        var result = await _controller.GetById(999);

        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFoundResult.Value.Should().NotBeNull();
    }

    #endregion

    #region GetByCategoria Tests

    /// <summary>
    /// Dado que existen productos de una categoría, cuando se obtienen, entonces retorna 200 OK con la lista.
    /// Returns: 200 OK con lista de productos
    /// </summary>
    [Test]
    public async Task GetByCategoria_ConCategoriaExistente_RetornaOkConLista()
    {
        var productos = new List<ProductoDto>
        {
            new ProductoDto { Id = 1, Nombre = "Laptop", Precio = 999.99m, CategoriaId = 1 }
        };

        _mockService.Setup(s => s.FindByCategoriaIdAsync(1))
            .ReturnsAsync(Result.Success<IEnumerable<ProductoDto>, DomainError>(productos));

        var result = await _controller.GetByCategoria(1);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedProductos = okResult.Value.Should().BeAssignableTo<IEnumerable<ProductoDto>>().Subject;
        returnedProductos.Should().HaveCount(1);
    }

    /// <summary>
    /// Dado que no existe la categoría, cuando se obtienen productos, entonces retorna 404 Not Found.
    /// Returns: 404 Not Found
    /// </summary>
    [Test]
    public async Task GetByCategoria_ConCategoriaNoExistente_RetornaNotFound()
    {
        var error = DomainError.NotFound("Categoría no encontrada");

        _mockService.Setup(s => s.FindByCategoriaIdAsync(999))
            .ReturnsAsync(Result.Failure<IEnumerable<ProductoDto>, DomainError>(error));

        var result = await _controller.GetByCategoria(999);

        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFoundResult.Value.Should().NotBeNull();
    }

    #endregion

    #region Create Tests

    /// <summary>
    /// Dado un DTO válido, cuando se crea un producto, entonces retorna 201 Created con el producto.
    /// Returns: 201 Created con producto creado
    /// </summary>
    [Test]
    public async Task Create_ConDtoValido_RetornaCreatedConProducto()
    {
        var requestDto = new ProductoRequestDto
        {
            Nombre = "Nuevo Producto",
            Descripcion = "Descripción",
            Precio = 99.99m,
            Stock = 10,
            CategoriaId = 1
        };
        var productoDto = new ProductoDto
        {
            Id = 1,
            Nombre = "Nuevo Producto",
            Descripcion = "Descripción",
            Precio = 99.99m,
            Stock = 10,
            CategoriaId = 1
        };

        _mockService.Setup(s => s.CreateAsync(requestDto))
            .ReturnsAsync(Result.Success<ProductoDto, DomainError>(productoDto));

        var result = await _controller.Create(requestDto);

        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.ActionName.Should().Be(nameof(ProductosController.GetById));
        createdResult.RouteValues.Should().ContainKey("id");
        var returnedProducto = createdResult.Value.Should().BeAssignableTo<ProductoDto>().Subject;
        returnedProducto.Nombre.Should().Be("Nuevo Producto");
    }

    /// <summary>
    /// Dado un DTO con precio negativo, cuando se crea un producto, entonces retorna 400 Bad Request.
    /// Returns: 400 Bad Request
    /// </summary>
    [Test]
    public async Task Create_ConPrecioNegativo_RetornaBadRequest()
    {
        var requestDto = new ProductoRequestDto
        {
            Nombre = "Producto",
            Precio = -10m,
            Stock = 10
        };
        var error = DomainError.Validation("El precio debe ser mayor a 0");

        _mockService.Setup(s => s.CreateAsync(requestDto))
            .ReturnsAsync(Result.Failure<ProductoDto, DomainError>(error));

        var result = await _controller.Create(requestDto);

        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.Value.Should().NotBeNull();
    }

    /// <summary>
    /// Dado un DTO con categoría inexistente, cuando se crea un producto, entonces retorna 404 Not Found.
    /// Returns: 404 Not Found
    /// </summary>
    [Test]
    public async Task Create_ConCategoriaNoExistente_RetornaNotFound()
    {
        var requestDto = new ProductoRequestDto
        {
            Nombre = "Producto",
            Precio = 99.99m,
            Stock = 10,
            CategoriaId = 999
        };
        var error = DomainError.NotFound("La categoría especificada no existe");

        _mockService.Setup(s => s.CreateAsync(requestDto))
            .ReturnsAsync(Result.Failure<ProductoDto, DomainError>(error));

        var result = await _controller.Create(requestDto);

        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFoundResult.Value.Should().NotBeNull();
    }

    #endregion

    #region Update Tests

    /// <summary>
    /// Dado un ID válido y DTO válido, cuando se actualiza, entonces retorna 200 OK con el producto actualizado.
    /// Returns: 200 OK con producto actualizado
    /// </summary>
    [Test]
    public async Task Update_ConIdValido_RetornaOkConProductoActualizado()
    {
        var id = 1L;
        var requestDto = new ProductoRequestDto
        {
            Nombre = "Producto Actualizado",
            Precio = 149.99m,
            Stock = 20
        };
        var productoDto = new ProductoDto
        {
            Id = 1,
            Nombre = "Producto Actualizado",
            Precio = 149.99m,
            Stock = 20
        };

        _mockService.Setup(s => s.UpdateAsync(id, requestDto))
            .ReturnsAsync(Result.Success<ProductoDto, DomainError>(productoDto));

        var result = await _controller.Update(id, requestDto);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedProducto = okResult.Value.Should().BeAssignableTo<ProductoDto>().Subject;
        returnedProducto.Nombre.Should().Be("Producto Actualizado");
    }

    /// <summary>
    /// Dado un ID no existente, cuando se actualiza, entonces retorna 404 Not Found.
    /// Returns: 404 Not Found
    /// </summary>
    [Test]
    public async Task Update_ConIdNoExistente_RetornaNotFound()
    {
        var id = 999L;
        var requestDto = new ProductoRequestDto { Nombre = "Actualizado", Precio = 99.99m, Stock = 10 };
        var error = DomainError.NotFound("Producto no encontrado");

        _mockService.Setup(s => s.UpdateAsync(id, requestDto))
            .ReturnsAsync(Result.Failure<ProductoDto, DomainError>(error));

        var result = await _controller.Update(id, requestDto);

        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFoundResult.Value.Should().NotBeNull();
    }

    /// <summary>
    /// Dado un DTO con stock negativo, cuando se actualiza, entonces retorna 400 Bad Request.
    /// Returns: 400 Bad Request
    /// </summary>
    [Test]
    public async Task Update_ConStockNegativo_RetornaBadRequest()
    {
        var id = 1L;
        var requestDto = new ProductoRequestDto { Nombre = "Producto", Precio = 99.99m, Stock = -5 };
        var error = DomainError.Validation("El stock no puede ser negativo");

        _mockService.Setup(s => s.UpdateAsync(id, requestDto))
            .ReturnsAsync(Result.Failure<ProductoDto, DomainError>(error));

        var result = await _controller.Update(id, requestDto);

        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.Value.Should().NotBeNull();
    }

    #endregion

    #region Delete Tests

    /// <summary>
    /// Dado un ID existente, cuando se elimina, entonces retorna 204 No Content.
    /// Returns: 204 No Content
    /// </summary>
    [Test]
    public async Task Delete_ConIdExistente_RetornaNoContent()
    {
        _mockService.Setup(s => s.DeleteAsync(1))
            .ReturnsAsync(UnitResult.Success<DomainError>());

        var result = await _controller.Delete(1);

        result.Should().BeOfType<NoContentResult>();
    }

    /// <summary>
    /// Dado un ID no existente, cuando se elimina, entonces retorna 404 Not Found.
    /// Returns: 404 Not Found
    /// </summary>
    [Test]
    public async Task Delete_ConIdNoExistente_RetornaNotFound()
    {
        var error = DomainError.NotFound("Producto no encontrado");

        _mockService.Setup(s => s.DeleteAsync(999))
            .ReturnsAsync(UnitResult.Failure<DomainError>(error));

        var result = await _controller.Delete(999);

        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFoundResult.Value.Should().NotBeNull();
    }

    #endregion
}

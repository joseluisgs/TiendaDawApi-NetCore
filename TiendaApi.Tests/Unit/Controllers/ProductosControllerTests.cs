using System.Reflection;
using CSharpFunctionalExtensions;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using TiendaApi.Api.Controllers;
using TiendaApi.Api.Dtos.Common;
using TiendaApi.Api.Dtos.Productos;
using TiendaApi.Api.Errors;
using TiendaApi.Api.Services.Historial;
using TiendaApi.Api.Services.Productos;

namespace TiendaApi.Tests.Unit.Controllers;

public class ProductosControllerTests {
    private readonly ProductosController _controller;
    private readonly Mock<IProductoService> _mockService;

    public ProductosControllerTests() {
        _mockService = new Mock<IProductoService>();
        var mockLogger = new Mock<ILogger<ProductosController>>();
        // IHistorialService mockeado: estos tests no tocan el historial/vista (lo
        // cubren los tests unit e integration del historial).
        _controller = new ProductosController(_mockService.Object, Mock.Of<IHistorialService>(), mockLogger.Object) {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
    }

    #region GetAll Tests

    /// <summary>
    ///     Dado que existen productos, cuando se obtienen todos paginados, entonces retorna 200 OK con la lista paginada.
    ///     Returns: 200 OK con lista de productos paginada
    /// </summary>
    [Test]
    public async Task GetAll_ConProductosExistentes_RetornaOkConLista() {
        var productos = new List<ProductoDto> {
            new(1, "Laptop", "", 999.99m, 10, "", new CategoriaResumenDto(1, "Electrónica"), null, DateTime.UtcNow,
                DateTime.UtcNow),
            new(2, "Mouse", "", 29.99m, 50, "", new CategoriaResumenDto(1, "Electrónica"), null, DateTime.UtcNow,
                DateTime.UtcNow)
        };
        var pagedResult = new PagedResult<ProductoDto> {
            Items = productos,
            TotalCount = 2,
            Page = 1,
            PageSize = 10
        };

        _mockService.Setup(s => s.FindAllPagedAsync(It.IsAny<ProductoFilterDto>()))
            .ReturnsAsync(Result.Success<PagedResult<ProductoDto>, DomainError>(pagedResult));

        ActionResult<PagedResult<ProductoDto>> result = await _controller.GetAll();

        PagedResult<ProductoDto>? okResult = result.Value;
        PagedResult<ProductoDto> returnedProductos =
            okResult.Should().BeAssignableTo<PagedResult<ProductoDto>>().Subject;
        returnedProductos.Items.Should().HaveCount(2);
    }

    /// <summary>
    ///     Dado que no existen productos, cuando se obtienen todos paginados, entonces retorna 200 OK con lista vacía.
    ///     Returns: 200 OK con lista vacía
    /// </summary>
    [Test]
    public async Task GetAll_SinProductos_RetornaOkConListaVacia() {
        var pagedResult = new PagedResult<ProductoDto> {
            Items = new List<ProductoDto>(),
            TotalCount = 0,
            Page = 1,
            PageSize = 10
        };

        _mockService.Setup(s => s.FindAllPagedAsync(It.IsAny<ProductoFilterDto>()))
            .ReturnsAsync(Result.Success<PagedResult<ProductoDto>, DomainError>(pagedResult));

        ActionResult<PagedResult<ProductoDto>> result = await _controller.GetAll();

        PagedResult<ProductoDto>? okResult = result.Value;
        PagedResult<ProductoDto> returnedProductos =
            okResult.Should().BeAssignableTo<PagedResult<ProductoDto>>().Subject;
        returnedProductos.Items.Should().BeEmpty();
    }

    /// <summary>
    ///     Dado un filtro por categoría, cuando se obtienen productos, entonces retorna solo los de esa categoría.
    ///     Returns: 200 OK con lista filtrada
    /// </summary>
    [Test]
    public async Task GetAll_ConFiltroCategoria_RetornaListaFiltrada() {
        var productos = new List<ProductoDto> {
            new(1, "Laptop", "", 999.99m, 10, "", new CategoriaResumenDto(1, "Electrónica"), null, DateTime.UtcNow,
                DateTime.UtcNow)
        };
        var pagedResult = new PagedResult<ProductoDto> {
            Items = productos,
            TotalCount = 1,
            Page = 1,
            PageSize = 10
        };

        _mockService.Setup(s => s.FindAllPagedAsync(It.IsAny<ProductoFilterDto>()))
            .ReturnsAsync(Result.Success<PagedResult<ProductoDto>, DomainError>(pagedResult));

        ActionResult<PagedResult<ProductoDto>> result = await _controller.GetAll(categoria: "Electrónica");

        PagedResult<ProductoDto>? okResult = result.Value;
        PagedResult<ProductoDto> returnedProductos =
            okResult.Should().BeAssignableTo<PagedResult<ProductoDto>>().Subject;
        returnedProductos.Items.Should().HaveCount(1);
    }

    /// <summary>
    ///     Dado un filtro por precio máximo, cuando se obtienen productos, entonces retorna los que cumplen el filtro.
    ///     Returns: 200 OK con lista filtrada
    /// </summary>
    [Test]
    public async Task GetAll_ConFiltroPrecioMax_RetornaListaFiltrada() {
        var productos = new List<ProductoDto> {
            new(1, "Mouse", "", 29.99m, 50, "", new CategoriaResumenDto(1, ""), null, DateTime.UtcNow, DateTime.UtcNow)
        };
        var pagedResult = new PagedResult<ProductoDto> {
            Items = productos,
            TotalCount = 1,
            Page = 1,
            PageSize = 10
        };

        _mockService.Setup(s => s.FindAllPagedAsync(It.IsAny<ProductoFilterDto>()))
            .ReturnsAsync(Result.Success<PagedResult<ProductoDto>, DomainError>(pagedResult));

        ActionResult<PagedResult<ProductoDto>> result = await _controller.GetAll(precioMax: 100);

        PagedResult<ProductoDto>? okResult = result.Value;
        PagedResult<ProductoDto> returnedProductos =
            okResult.Should().BeAssignableTo<PagedResult<ProductoDto>>().Subject;
        returnedProductos.Items.Should().HaveCount(1);
    }

    /// <summary>
    ///     Dado un filtro por stock mínimo, cuando se obtienen productos, entonces retorna los que cumplen el filtro.
    ///     Returns: 200 OK con lista filtrada
    /// </summary>
    [Test]
    public async Task GetAll_ConFiltroStockMin_RetornaListaFiltrada() {
        var productos = new List<ProductoDto> {
            new(1, "Laptop", "", 999.99m, 10, "", new CategoriaResumenDto(1, "Electrónica"), null, DateTime.UtcNow,
                DateTime.UtcNow)
        };
        var pagedResult = new PagedResult<ProductoDto> {
            Items = productos,
            TotalCount = 1,
            Page = 1,
            PageSize = 10
        };

        _mockService.Setup(s => s.FindAllPagedAsync(It.IsAny<ProductoFilterDto>()))
            .ReturnsAsync(Result.Success<PagedResult<ProductoDto>, DomainError>(pagedResult));

        ActionResult<PagedResult<ProductoDto>> result = await _controller.GetAll(stockMin: 5);

        PagedResult<ProductoDto>? okResult = result.Value;
        PagedResult<ProductoDto> returnedProductos =
            okResult.Should().BeAssignableTo<PagedResult<ProductoDto>>().Subject;
        returnedProductos.Items.Should().HaveCount(1);
    }

    /// <summary>
    ///     Dado parámetros de paginación, cuando se obtienen productos, entonces retorna la página correcta.
    ///     Returns: 200 OK con página específica
    /// </summary>
    [Test]
    public async Task GetAll_ConPaginacion_RetornaPaginaCorrecta() {
        var productos = new List<ProductoDto> {
            new(3, "Teclado", "", 49.99m, 30, "", new CategoriaResumenDto(1, ""), null, DateTime.UtcNow,
                DateTime.UtcNow)
        };
        var pagedResult = new PagedResult<ProductoDto> {
            Items = productos,
            TotalCount = 15,
            Page = 2,
            PageSize = 5
        };

        _mockService.Setup(s => s.FindAllPagedAsync(It.IsAny<ProductoFilterDto>()))
            .ReturnsAsync(Result.Success<PagedResult<ProductoDto>, DomainError>(pagedResult));

        ActionResult<PagedResult<ProductoDto>> result = await _controller.GetAll(page: 1, size: 5);

        PagedResult<ProductoDto>? okResult = result.Value;
        PagedResult<ProductoDto> returnedProductos =
            okResult.Should().BeAssignableTo<PagedResult<ProductoDto>>().Subject;
        returnedProductos.Page.Should().Be(2);
        returnedProductos.PageSize.Should().Be(5);
        returnedProductos.TotalCount.Should().Be(15);
        returnedProductos.TotalPages.Should().Be(3);
    }

    #endregion

    #region GetById Tests

    /// <summary>
    ///     Dado que existe un producto, cuando se obtiene por ID, entonces retorna 200 OK con el producto.
    ///     Returns: 200 OK con producto encontrado
    /// </summary>
    [Test]
    public async Task GetById_ConIdExistente_RetornaOkConProducto() {
        var producto = new ProductoDto(1, "Laptop", "", 999.99m, 10, "", new CategoriaResumenDto(1, "Electrónica"),
            null, DateTime.UtcNow, DateTime.UtcNow);

        _mockService.Setup(s => s.FindByIdAsync(1))
            .ReturnsAsync(Result.Success<ProductoDto, DomainError>(producto));

        ActionResult<ProductoDto> result = await _controller.GetById(1);

        ProductoDto? okResult = result.Value;
        ProductoDto returnedProducto = okResult.Should().BeAssignableTo<ProductoDto>().Subject;
        returnedProducto.Id.Should().Be(1);
        returnedProducto.Nombre.Should().Be("Laptop");
    }

    /// <summary>
    ///     Dado que no existe un producto, cuando se obtiene por ID, entonces retorna 404 Not Found.
    ///     Returns: 404 Not Found
    /// </summary>
    [Test]
    public async Task GetById_ConIdNoExistente_RetornaNotFound() {
        var error = new NotFoundError("Producto no encontrado");

        _mockService.Setup(s => s.FindByIdAsync(999))
            .ReturnsAsync(Result.Failure<ProductoDto, DomainError>(error));

        ActionResult<ProductoDto> result = await _controller.GetById(999);

        NotFoundObjectResult notFoundResult = result.Result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFoundResult.Value.Should().NotBeNull();
    }

    #endregion

    #region GetByCategoria Tests

    /// <summary>
    ///     Dado que existen productos de una categoría, cuando se obtienen, entonces retorna 200 OK con la lista.
    ///     Returns: 200 OK con lista de productos
    /// </summary>
    [Test]
    public async Task GetByCategoria_ConCategoriaExistente_RetornaOkConLista() {
        var productos = new List<ProductoDto> {
            new(1, "Laptop", "", 999.99m, 10, "", new CategoriaResumenDto(1, "Electrónica"), null, DateTime.UtcNow,
                DateTime.UtcNow)
        };

        _mockService.Setup(s => s.FindByCategoriaIdAsync(1))
            .ReturnsAsync(Result.Success<IEnumerable<ProductoDto>, DomainError>(productos));

        ActionResult<IEnumerable<ProductoDto>> result = await _controller.GetByCategoria(1);

        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        IEnumerable<ProductoDto> returnedProductos =
            okResult!.Value.Should().BeAssignableTo<IEnumerable<ProductoDto>>().Subject;
        returnedProductos.Should().HaveCount(1);
    }

    /// <summary>
    ///     Dado que no existe la categoría, cuando se obtienen productos, entonces retorna 404 Not Found.
    ///     Returns: 404 Not Found
    /// </summary>
    [Test]
    public async Task GetByCategoria_ConCategoriaNoExistente_RetornaNotFound() {
        var error = new NotFoundError("Categoría no encontrada");

        _mockService.Setup(s => s.FindByCategoriaIdAsync(999))
            .ReturnsAsync(Result.Failure<IEnumerable<ProductoDto>, DomainError>(error));

        ActionResult<IEnumerable<ProductoDto>> result = await _controller.GetByCategoria(999);

        NotFoundObjectResult notFoundResult = result.Result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFoundResult.Value.Should().NotBeNull();
    }

    #endregion

    #region Create Tests

    /// <summary>
    ///     Dado un DTO válido, cuando se crea un producto, entonces retorna 201 Created con el producto.
    ///     Returns: 201 Created con producto creado
    /// </summary>
    [Test]
    public async Task Create_ConDtoValido_RetornaCreatedConProducto() {
        var requestDto = new ProductoRequestDto {
            Nombre = "Nuevo Producto",
            Descripcion = "Descripción",
            Precio = 99.99m,
            Stock = 10,
            CategoriaId = 1
        };
        var productoDto = new ProductoDto(1, "Nuevo Producto", "Descripción", 99.99m, 10, "",
            new CategoriaResumenDto(1, ""), null, DateTime.UtcNow, DateTime.UtcNow);

        _mockService.Setup(s => s.CreateAsync(requestDto))
            .ReturnsAsync(Result.Success<ProductoDto, DomainError>(productoDto));

        ActionResult<ProductoDto> result = await _controller.Create(requestDto);

        CreatedAtActionResult createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.ActionName.Should().Be(nameof(ProductosController.GetById));
        createdResult.RouteValues.Should().ContainKey("id");
        ProductoDto returnedProducto = createdResult.Value.Should().BeAssignableTo<ProductoDto>().Subject;
        returnedProducto.Nombre.Should().Be("Nuevo Producto");
    }

    /// <summary>
    ///     Dado un DTO con precio negativo, cuando se crea un producto, entonces retorna 400 Bad Request.
    ///     Returns: 400 Bad Request
    /// </summary>
    [Test]
    public async Task Create_ConPrecioNegativo_RetornaBadRequest() {
        var requestDto = new ProductoRequestDto {
            Nombre = "Producto",
            Precio = -10m,
            Stock = 10
        };
        var error = ValidationError.Create("El precio debe ser mayor a 0");

        _mockService.Setup(s => s.CreateAsync(requestDto))
            .ReturnsAsync(Result.Failure<ProductoDto, DomainError>(error));

        ActionResult<ProductoDto> result = await _controller.Create(requestDto);

        BadRequestObjectResult badRequestResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.Value.Should().NotBeNull();
    }

    /// <summary>
    ///     Dado un DTO con categoría inexistente, cuando se crea un producto, entonces retorna 404 Not Found.
    ///     Returns: 404 Not Found
    /// </summary>
    [Test]
    public async Task Create_ConCategoriaNoExistente_RetornaNotFound() {
        var requestDto = new ProductoRequestDto {
            Nombre = "Producto",
            Precio = 99.99m,
            Stock = 10,
            CategoriaId = 999
        };
        var error = new NotFoundError("La categoría especificada no existe");

        _mockService.Setup(s => s.CreateAsync(requestDto))
            .ReturnsAsync(Result.Failure<ProductoDto, DomainError>(error));

        ActionResult<ProductoDto> result = await _controller.Create(requestDto);

        NotFoundObjectResult notFoundResult = result.Result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFoundResult.Value.Should().NotBeNull();
    }

    #endregion

    #region Update Tests

    /// <summary>
    ///     Dado un ID válido y DTO válido, cuando se actualiza, entonces retorna 200 OK con el producto actualizado.
    ///     Returns: 200 OK con producto actualizado
    /// </summary>
    [Test]
    public async Task Update_ConIdValido_RetornaOkConProductoActualizado() {
        long id = 1L;
        var requestDto = new ProductoRequestDto {
            Nombre = "Producto Actualizado",
            Precio = 149.99m,
            Stock = 20
        };
        var productoDto = new ProductoDto(1, "Producto Actualizado", "", 149.99m, 20, "",
            new CategoriaResumenDto(1, ""), null, DateTime.UtcNow, DateTime.UtcNow);

        _mockService.Setup(s => s.UpdateAsync(id, requestDto))
            .ReturnsAsync(Result.Success<ProductoDto, DomainError>(productoDto));

        ActionResult<ProductoDto> result = await _controller.Update(id, requestDto);

        ProductoDto? okResult = result.Value;
        ProductoDto returnedProducto = okResult.Should().BeAssignableTo<ProductoDto>().Subject;
        returnedProducto.Nombre.Should().Be("Producto Actualizado");
    }

    /// <summary>
    ///     Dado un ID no existente, cuando se actualiza, entonces retorna 404 Not Found.
    ///     Returns: 404 Not Found
    /// </summary>
    [Test]
    public async Task Update_ConIdNoExistente_RetornaNotFound() {
        long id = 999L;
        var requestDto = new ProductoRequestDto { Nombre = "Actualizado", Precio = 99.99m, Stock = 10 };
        var error = new NotFoundError("Producto no encontrado");

        _mockService.Setup(s => s.UpdateAsync(id, requestDto))
            .ReturnsAsync(Result.Failure<ProductoDto, DomainError>(error));

        ActionResult<ProductoDto> result = await _controller.Update(id, requestDto);

        NotFoundObjectResult notFoundResult = result.Result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFoundResult.Value.Should().NotBeNull();
    }

    /// <summary>
    ///     Dado un DTO con stock negativo, cuando se actualiza, entonces retorna 400 Bad Request.
    ///     Returns: 400 Bad Request
    /// </summary>
    [Test]
    public async Task Update_ConStockNegativo_RetornaBadRequest() {
        long id = 1L;
        var requestDto = new ProductoRequestDto { Nombre = "Producto", Precio = 99.99m, Stock = -5 };
        var error = ValidationError.Create("El stock no puede ser negativo");

        _mockService.Setup(s => s.UpdateAsync(id, requestDto))
            .ReturnsAsync(Result.Failure<ProductoDto, DomainError>(error));

        ActionResult<ProductoDto> result = await _controller.Update(id, requestDto);

        BadRequestObjectResult badRequestResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.Value.Should().NotBeNull();
    }

    #endregion

    #region Delete Tests

    /// <summary>
    ///     Dado un ID existente, cuando se elimina, entonces retorna 204 No Content.
    ///     Returns: 204 No Content
    /// </summary>
    [Test]
    public async Task Delete_ConIdExistente_RetornaNoContent() {
        _mockService.Setup(s => s.DeleteAsync(1))
            .ReturnsAsync(UnitResult.Success<DomainError>());

        ActionResult result = await _controller.Delete(1);

        result.Should().BeOfType<NoContentResult>();
    }

    /// <summary>
    ///     Dado un ID no existente, cuando se elimina, entonces retorna 404 Not Found.
    ///     Returns: 404 Not Found
    /// </summary>
    [Test]
    public async Task Delete_ConIdNoExistente_RetornaNotFound() {
        var error = new NotFoundError("Producto no encontrado");

        _mockService.Setup(s => s.DeleteAsync(999))
            .ReturnsAsync(UnitResult.Failure<DomainError>(error));

        ActionResult result = await _controller.Delete(999);

        NotFoundObjectResult notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFoundResult.Value.Should().NotBeNull();
    }

    #endregion

    #region Tests Adicionales - Casos Borde

    [Test]
    public async Task GetById_ConIdCero_RetornaNotFound() {
        var error = new NotFoundError("Producto no encontrado");

        _mockService.Setup(s => s.FindByIdAsync(0))
            .ReturnsAsync(Result.Failure<ProductoDto, DomainError>(error));

        ActionResult<ProductoDto> result = await _controller.GetById(0);

        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Test]
    public async Task GetById_ConIdNegativo_RetornaNotFound() {
        var error = new NotFoundError("Producto no encontrado");

        _mockService.Setup(s => s.FindByIdAsync(-1))
            .ReturnsAsync(Result.Failure<ProductoDto, DomainError>(error));

        ActionResult<ProductoDto> result = await _controller.GetById(-1);

        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Test]
    public async Task Create_ConNombreVacio_RetornaBadRequest() {
        var requestDto = new ProductoRequestDto {
            Nombre = "",
            Precio = 99.99m,
            Stock = 10
        };
        var error = ValidationError.Create("El nombre es obligatorio");

        _mockService.Setup(s => s.CreateAsync(requestDto))
            .ReturnsAsync(Result.Failure<ProductoDto, DomainError>(error));

        ActionResult<ProductoDto> result = await _controller.Create(requestDto);

        BadRequestObjectResult badRequestResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.Value.Should().NotBeNull();
    }

    [Test]
    public async Task Create_ConCategoriaIdCero_RetornaBadRequest() {
        var requestDto = new ProductoRequestDto {
            Nombre = "Producto",
            Precio = 99.99m,
            Stock = 10,
            CategoriaId = 0
        };
        var error = ValidationError.Create("Debe seleccionar una categoría válida");

        _mockService.Setup(s => s.CreateAsync(requestDto))
            .ReturnsAsync(Result.Failure<ProductoDto, DomainError>(error));

        ActionResult<ProductoDto> result = await _controller.Create(requestDto);

        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Test]
    public async Task Update_ConflictoDeStock_RetornaUnprocessableEntity() {
        long id = 1L;
        var requestDto = new ProductoRequestDto {
            Nombre = "Producto",
            Precio = 99.99m,
            Stock = 1000
        };
        var error = new BusinessRuleError("No hay suficiente stock");

        _mockService.Setup(s => s.UpdateAsync(id, requestDto))
            .ReturnsAsync(Result.Failure<ProductoDto, DomainError>(error));

        ActionResult<ProductoDto> result = await _controller.Update(id, requestDto);

        // BusinessRuleError → 422 Unprocessable Entity (regla de negocio, no malformación)
        result.Result.Should().BeOfType<ObjectResult>();
        ((ObjectResult)result.Result!).StatusCode.Should().Be(422);
    }

    [Test]
    public async Task Create_ErrorInterno_Retorna500() {
        var requestDto = new ProductoRequestDto {
            Nombre = "Producto",
            Precio = 99.99m,
            Stock = 10
        };
        var error = new InternalError("Error en base de datos");

        _mockService.Setup(s => s.CreateAsync(requestDto))
            .ReturnsAsync(Result.Failure<ProductoDto, DomainError>(error));

        ActionResult<ProductoDto> result = await _controller.Create(requestDto);

        ObjectResult objectResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(500);
    }

    [Test]
    public async Task GetByCategoria_ConCategoriaValida_RetornaOk() {
        var productos = new List<ProductoDto> {
            new(1, "Laptop", "", 999.99m, 10, "", new CategoriaResumenDto(1, ""), null, DateTime.UtcNow,
                DateTime.UtcNow),
            new(2, "Mouse", "", 29.99m, 50, "", new CategoriaResumenDto(1, ""), null, DateTime.UtcNow, DateTime.UtcNow)
        };

        _mockService.Setup(s => s.FindByCategoriaIdAsync(1))
            .ReturnsAsync(Result.Success<IEnumerable<ProductoDto>, DomainError>(productos));

        ActionResult<IEnumerable<ProductoDto>> result = await _controller.GetByCategoria(1);

        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        IEnumerable<ProductoDto> returnedProductos =
            okResult!.Value.Should().BeAssignableTo<IEnumerable<ProductoDto>>().Subject;
        returnedProductos.Should().HaveCount(2);
    }

    [Test]
    public async Task GetByCategoria_CategoriaVacia_RetornaOkConListaVacia() {
        _mockService.Setup(s => s.FindByCategoriaIdAsync(999))
            .ReturnsAsync(Result.Success<IEnumerable<ProductoDto>, DomainError>(new List<ProductoDto>()));

        ActionResult<IEnumerable<ProductoDto>> result = await _controller.GetByCategoria(999);

        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        IEnumerable<ProductoDto> returnedProductos =
            okResult!.Value.Should().BeAssignableTo<IEnumerable<ProductoDto>>().Subject;
        returnedProductos.Should().BeEmpty();
    }

    [Test]
    public async Task Create_ConflictoPorDuplicado_Retorna409() {
        var requestDto = new ProductoRequestDto {
            Nombre = "Producto Existente",
            Precio = 99.99m,
            Stock = 10
        };
        var error = new ConflictError("El producto ya existe");

        _mockService.Setup(s => s.CreateAsync(requestDto))
            .ReturnsAsync(Result.Failure<ProductoDto, DomainError>(error));

        ActionResult<ProductoDto> result = await _controller.Create(requestDto);

        ObjectResult objectResult = result.Result.Should().BeAssignableTo<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(409);
    }

    #endregion

    #region UpdateImage Tests

    [Test]
    public async Task UpdateImage_ConImagenValida_RetornaOkConProducto() {
        long id = 1L;
        IFormFile mockFile = CreateMockFormFile("test.jpg", "image/jpeg", 1024);
        var productoDto = new ProductoDto(1, "Producto", "uploads/test.jpg", 0m, 0, "uploads/test.jpg",
            new CategoriaResumenDto(1, ""), null, DateTime.UtcNow, DateTime.UtcNow);

        _mockService.Setup(s => s.UpdateImageAsync(id, It.IsAny<IFormFile>()))
            .ReturnsAsync(Result.Success<ProductoDto, DomainError>(productoDto));

        ActionResult<ProductoDto> result = await _controller.UpdateImage(id, mockFile);

        ProductoDto? okResult = result.Value;
        ProductoDto returnedProducto = okResult.Should().BeAssignableTo<ProductoDto>().Subject;
        returnedProducto.Imagen.Should().Contain("test.jpg");
    }

    [Test]
    public async Task UpdateImage_SinArchivo_RetornaBadRequest() {
        ActionResult<ProductoDto> result = await _controller.UpdateImage(1, null!);

        BadRequestObjectResult badRequestResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.Value.Should().NotBeNull();
    }

    [Test]
    public async Task UpdateImage_ConArchivoVacio_RetornaBadRequest() {
        IFormFile mockFile = CreateMockFormFile("test.jpg", "image/jpeg", 0);

        ActionResult<ProductoDto> result = await _controller.UpdateImage(1, mockFile);

        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Test]
    public async Task UpdateImage_ConTipoNoPermitido_RetornaBadRequest() {
        IFormFile mockFile = CreateMockFormFile("test.pdf", "application/pdf", 1024);

        ActionResult<ProductoDto> result = await _controller.UpdateImage(1, mockFile);

        BadRequestObjectResult badRequestResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.Value.Should().NotBeNull();
    }

    [Test]
    public async Task UpdateImage_ConProductoNoExistente_RetornaNotFound() {
        long id = 999L;
        IFormFile mockFile = CreateMockFormFile("test.jpg", "image/jpeg", 1024);
        var error = new NotFoundError("Producto no encontrado");

        _mockService.Setup(s => s.UpdateImageAsync(id, It.IsAny<IFormFile>()))
            .ReturnsAsync(Result.Failure<ProductoDto, DomainError>(error));

        ActionResult<ProductoDto> result = await _controller.UpdateImage(id, mockFile);

        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Test]
    public async Task UpdateImage_ConErrorInterno_Retorna500() {
        long id = 1L;
        IFormFile mockFile = CreateMockFormFile("test.jpg", "image/jpeg", 1024);
        var error = new InternalError("Error al guardar imagen");

        _mockService.Setup(s => s.UpdateImageAsync(id, It.IsAny<IFormFile>()))
            .ReturnsAsync(Result.Failure<ProductoDto, DomainError>(error));

        ActionResult<ProductoDto> result = await _controller.UpdateImage(id, mockFile);

        ObjectResult objectResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(500);
    }

    [Test]
    public async Task UpdateImage_ConImagenPng_RetornaOk() {
        long id = 1L;
        IFormFile mockFile = CreateMockFormFile("test.png", "image/png", 2048);
        var productoDto = new ProductoDto(1, "", "uploads/test.png", 0m, 0, "uploads/test.png",
            new CategoriaResumenDto(1, ""), null, DateTime.UtcNow, DateTime.UtcNow);

        _mockService.Setup(s => s.UpdateImageAsync(id, It.IsAny<IFormFile>()))
            .ReturnsAsync(Result.Success<ProductoDto, DomainError>(productoDto));

        ActionResult<ProductoDto> result = await _controller.UpdateImage(id, mockFile);

        result.Value.Should().NotBeNull();
    }

    [Test]
    public async Task UpdateImage_ConImagenGif_RetornaOk() {
        long id = 1L;
        IFormFile mockFile = CreateMockFormFile("test.gif", "image/gif", 512);
        var productoDto = new ProductoDto(1, "", "uploads/test.gif", 0m, 0, "uploads/test.gif",
            new CategoriaResumenDto(1, ""), null, DateTime.UtcNow, DateTime.UtcNow);

        _mockService.Setup(s => s.UpdateImageAsync(id, It.IsAny<IFormFile>()))
            .ReturnsAsync(Result.Success<ProductoDto, DomainError>(productoDto));

        ActionResult<ProductoDto> result = await _controller.UpdateImage(id, mockFile);

        result.Value.Should().NotBeNull();
    }

    [Test]
    public async Task UpdateImage_ConImagenWebp_RetornaOk() {
        long id = 1L;
        IFormFile mockFile = CreateMockFormFile("test.webp", "image/webp", 1024);
        var productoDto = new ProductoDto(1, "", "uploads/test.webp", 0m, 0, "uploads/test.webp",
            new CategoriaResumenDto(1, ""), null, DateTime.UtcNow, DateTime.UtcNow);

        _mockService.Setup(s => s.UpdateImageAsync(id, It.IsAny<IFormFile>()))
            .ReturnsAsync(Result.Success<ProductoDto, DomainError>(productoDto));

        ActionResult<ProductoDto> result = await _controller.UpdateImage(id, mockFile);

        result.Value.Should().NotBeNull();
    }

    private static IFormFile CreateMockFormFile(string fileName, string contentType, long length) {
        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.FileName).Returns(fileName);
        mockFile.Setup(f => f.ContentType).Returns(contentType);
        mockFile.Setup(f => f.Length).Returns(length);
        mockFile.Setup(f => f.OpenReadStream()).Returns(new MemoryStream(new byte[length]));
        return mockFile.Object;
    }

    #endregion

    #region UpdatePartial Tests

    [Test]
    public async Task UpdatePartial_ConCamposValidos_RetornaOkConProducto() {
        long id = 1L;
        var patchDto = new ProductoPatchDto { Nombre = "Nombre Actualizado" };
        var productoDto = new ProductoDto(1, "Nombre Actualizado", "", 99.99m, 0, "", new CategoriaResumenDto(1, ""),
            null, DateTime.UtcNow, DateTime.UtcNow);

        _mockService.Setup(s => s.UpdatePartialAsync(id, patchDto))
            .ReturnsAsync(Result.Success<ProductoDto, DomainError>(productoDto));

        ActionResult<ProductoDto> result = await _controller.UpdatePartial(id, patchDto);

        ProductoDto? okResult = result.Value;
        ProductoDto returnedProducto = okResult.Should().BeAssignableTo<ProductoDto>().Subject;
        returnedProducto.Nombre.Should().Be("Nombre Actualizado");
    }

    [Test]
    public async Task UpdatePartial_SoloPrecio_RetornaOk() {
        long id = 1L;
        var patchDto = new ProductoPatchDto { Precio = 199.99m };
        var productoDto = new ProductoDto(1, "Producto", "", 199.99m, 0, "", new CategoriaResumenDto(1, ""), null,
            DateTime.UtcNow, DateTime.UtcNow);

        _mockService.Setup(s => s.UpdatePartialAsync(id, patchDto))
            .ReturnsAsync(Result.Success<ProductoDto, DomainError>(productoDto));

        ActionResult<ProductoDto> result = await _controller.UpdatePartial(id, patchDto);

        ProductoDto? okResult = result.Value;
        ProductoDto returnedProducto = okResult.Should().BeAssignableTo<ProductoDto>().Subject;
        returnedProducto.Precio.Should().Be(199.99m);
    }

    [Test]
    public async Task UpdatePartial_SoloStock_RetornaOk() {
        long id = 1L;
        var patchDto = new ProductoPatchDto { Stock = 50 };
        var productoDto = new ProductoDto(1, "Producto", "", 0m, 50, "", new CategoriaResumenDto(1, ""), null,
            DateTime.UtcNow, DateTime.UtcNow);

        _mockService.Setup(s => s.UpdatePartialAsync(id, patchDto))
            .ReturnsAsync(Result.Success<ProductoDto, DomainError>(productoDto));

        ActionResult<ProductoDto> result = await _controller.UpdatePartial(id, patchDto);

        ProductoDto? okResult = result.Value;
        ProductoDto returnedProducto = okResult.Should().BeAssignableTo<ProductoDto>().Subject;
        returnedProducto.Stock.Should().Be(50);
    }

    [Test]
    public async Task UpdatePartial_ConProductoNoExistente_RetornaNotFound() {
        long id = 999L;
        var patchDto = new ProductoPatchDto { Nombre = "Actualizado" };
        var error = new NotFoundError("Producto no encontrado");

        _mockService.Setup(s => s.UpdatePartialAsync(id, patchDto))
            .ReturnsAsync(Result.Failure<ProductoDto, DomainError>(error));

        ActionResult<ProductoDto> result = await _controller.UpdatePartial(id, patchDto);

        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Test]
    public async Task UpdatePartial_ConPrecioNegativo_RetornaBadRequest() {
        long id = 1L;
        var patchDto = new ProductoPatchDto { Precio = -10m };
        var error = ValidationError.Create("El precio debe ser mayor a 0");

        _mockService.Setup(s => s.UpdatePartialAsync(id, patchDto))
            .ReturnsAsync(Result.Failure<ProductoDto, DomainError>(error));

        ActionResult<ProductoDto> result = await _controller.UpdatePartial(id, patchDto);

        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Test]
    public async Task UpdatePartial_ConStockNegativo_RetornaBadRequest() {
        long id = 1L;
        var patchDto = new ProductoPatchDto { Stock = -5 };
        var error = ValidationError.Create("El stock no puede ser negativo");

        _mockService.Setup(s => s.UpdatePartialAsync(id, patchDto))
            .ReturnsAsync(Result.Failure<ProductoDto, DomainError>(error));

        ActionResult<ProductoDto> result = await _controller.UpdatePartial(id, patchDto);

        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Test]
    public async Task UpdatePartial_ConErrorInterno_Retorna500() {
        long id = 1L;
        var patchDto = new ProductoPatchDto { Nombre = "Actualizado" };
        var error = new InternalError("Error en base de datos");

        _mockService.Setup(s => s.UpdatePartialAsync(id, patchDto))
            .ReturnsAsync(Result.Failure<ProductoDto, DomainError>(error));

        ActionResult<ProductoDto> result = await _controller.UpdatePartial(id, patchDto);

        ObjectResult objectResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(500);
    }

    [Test]
    public async Task UpdatePartial_ConMultiplesCampos_RetornaOk() {
        long id = 1L;
        var patchDto = new ProductoPatchDto {
            Nombre = "Nuevo Nombre",
            Descripcion = "Nueva Descripción",
            Precio = 299.99m,
            Stock = 100
        };
        var productoDto = new ProductoDto(1, "Nuevo Nombre", "Nueva Descripción", 299.99m, 100, "",
            new CategoriaResumenDto(1, ""), null, DateTime.UtcNow, DateTime.UtcNow);

        _mockService.Setup(s => s.UpdatePartialAsync(id, patchDto))
            .ReturnsAsync(Result.Success<ProductoDto, DomainError>(productoDto));

        ActionResult<ProductoDto> result = await _controller.UpdatePartial(id, patchDto);

        ProductoDto? okResult = result.Value;
        ProductoDto returnedProducto = okResult.Should().BeAssignableTo<ProductoDto>().Subject;
        returnedProducto.Nombre.Should().Be("Nuevo Nombre");
        returnedProducto.Precio.Should().Be(299.99m);
    }

    #endregion

    #region Authorization Attribute Tests

    [Test]
    public void GetAll_TieneAtributoAllowAnonymous() {
        MethodInfo? methodInfo = typeof(ProductosController).GetMethod(nameof(ProductosController.GetAll));
        object[] attribute = methodInfo!.GetCustomAttributes(typeof(AllowAnonymousAttribute), true);
        attribute.Should().NotBeEmpty();
    }

    [Test]
    public void GetById_TieneAtributoAllowAnonymous() {
        MethodInfo? methodInfo = typeof(ProductosController).GetMethod(nameof(ProductosController.GetById));
        object[] attribute = methodInfo!.GetCustomAttributes(typeof(AllowAnonymousAttribute), true);
        attribute.Should().NotBeEmpty();
    }

    [Test]
    public void GetByCategoria_TieneAtributoAllowAnonymous() {
        MethodInfo? methodInfo = typeof(ProductosController).GetMethod(nameof(ProductosController.GetByCategoria));
        object[] attribute = methodInfo!.GetCustomAttributes(typeof(AllowAnonymousAttribute), true);
        attribute.Should().NotBeEmpty();
    }

    [Test]
    public void Create_TieneAtributoAuthorizeAdmin() {
        MethodInfo? methodInfo = typeof(ProductosController).GetMethod(nameof(ProductosController.Create));
        AuthorizeAttribute? attribute = methodInfo!.GetCustomAttributes(typeof(AuthorizeAttribute), true)
            .Cast<AuthorizeAttribute>().FirstOrDefault();
        attribute.Should().NotBeNull();
        attribute!.Policy.Should().Be("RequireAdminRole");
    }

    [Test]
    public void Update_TieneAtributoAuthorizeAdmin() {
        MethodInfo? methodInfo = typeof(ProductosController).GetMethod(nameof(ProductosController.Update));
        AuthorizeAttribute? attribute = methodInfo!.GetCustomAttributes(typeof(AuthorizeAttribute), true)
            .Cast<AuthorizeAttribute>().FirstOrDefault();
        attribute.Should().NotBeNull();
        attribute!.Policy.Should().Be("RequireAdminRole");
    }

    [Test]
    public void Delete_TieneAtributoAuthorizeAdmin() {
        MethodInfo? methodInfo = typeof(ProductosController).GetMethod(nameof(ProductosController.Delete));
        AuthorizeAttribute? attribute = methodInfo!.GetCustomAttributes(typeof(AuthorizeAttribute), true)
            .Cast<AuthorizeAttribute>().FirstOrDefault();
        attribute.Should().NotBeNull();
        attribute!.Policy.Should().Be("RequireAdminRole");
    }

    [Test]
    public void UpdateImage_TieneAtributoAuthorizeAdmin() {
        MethodInfo? methodInfo = typeof(ProductosController).GetMethod(nameof(ProductosController.UpdateImage));
        AuthorizeAttribute? attribute = methodInfo!.GetCustomAttributes(typeof(AuthorizeAttribute), true)
            .Cast<AuthorizeAttribute>().FirstOrDefault();
        attribute.Should().NotBeNull();
        attribute!.Policy.Should().Be("RequireAdminRole");
    }

    [Test]
    public void UpdatePartial_TieneAtributoAuthorizeAdmin() {
        MethodInfo? methodInfo = typeof(ProductosController).GetMethod(nameof(ProductosController.UpdatePartial));
        AuthorizeAttribute? attribute = methodInfo!.GetCustomAttributes(typeof(AuthorizeAttribute), true)
            .Cast<AuthorizeAttribute>().FirstOrDefault();
        attribute.Should().NotBeNull();
        attribute!.Policy.Should().Be("RequireAdminRole");
    }

    #endregion

    #region Error Handling Tests

    [Test]
    public async Task GetAll_ConErrorInterno_Retorna500() {
        var error = new InternalError("Error de conexión a base de datos");

        _mockService.Setup(s => s.FindAllPagedAsync(It.IsAny<ProductoFilterDto>()))
            .ReturnsAsync(Result.Failure<PagedResult<ProductoDto>, DomainError>(error));

        ActionResult<PagedResult<ProductoDto>> result = await _controller.GetAll();

        ObjectResult objectResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(500);
    }

    [Test]
    public async Task GetById_ConErrorInterno_Retorna500() {
        var error = new InternalError("Error inesperado");

        _mockService.Setup(s => s.FindByIdAsync(1))
            .ReturnsAsync(Result.Failure<ProductoDto, DomainError>(error));

        ActionResult<ProductoDto> result = await _controller.GetById(1);

        ObjectResult objectResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(500);
    }

    [Test]
    public async Task GetByCategoria_ConErrorInterno_Retorna500() {
        var error = new InternalError("Error de base de datos");

        _mockService.Setup(s => s.FindByCategoriaIdAsync(1))
            .ReturnsAsync(Result.Failure<IEnumerable<ProductoDto>, DomainError>(error));

        ActionResult<IEnumerable<ProductoDto>> result = await _controller.GetByCategoria(1);

        ObjectResult objectResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(500);
    }

    [Test]
    public async Task Delete_ConErrorInterno_Retorna500() {
        var error = new InternalError("Error al eliminar");

        _mockService.Setup(s => s.DeleteAsync(1))
            .ReturnsAsync(UnitResult.Failure<DomainError>(error));

        ActionResult result = await _controller.Delete(1);

        ObjectResult objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(500);
    }

    #endregion
}

using CSharpFunctionalExtensions;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using TiendaApi.Api.Controllers;
using TiendaApi.Api.Dtos.Categorias;
using TiendaApi.Api.Dtos.Common;
using TiendaApi.Api.Errors;
using TiendaApi.Api.Services.Categorias;

namespace TiendaApi.Tests.Unit.Controllers;

public class CategoriasControllerTests
{
    private readonly Mock<ICategoriaService> _mockService;
    private readonly CategoriasController _controller;

    public CategoriasControllerTests()
    {
        _mockService = new Mock<ICategoriaService>();
        var mockLogger = new Mock<ILogger<CategoriasController>>();
        _controller = new CategoriasController(_mockService.Object, mockLogger.Object);
    }

    #region GetAll Tests

    /// <summary>
    /// Dado que existen categorías, cuando se obtienen todas paginadas, entonces retorna 200 OK con la lista paginada.
    /// Returns: 200 OK con lista de categorías paginada
    /// </summary>
    [Test]
    public async Task GetAll_ConCategoriasExistentes_RetornaOkConLista()
    {
        var categorias = new List<CategoriaDto>
        {
            new CategoriaDto(1, "Electrónica", null, DateTime.UtcNow, DateTime.UtcNow),
            new CategoriaDto(2, "Ropa", null, DateTime.UtcNow, DateTime.UtcNow)
        };
        var pagedResult = new PagedResult<CategoriaDto>
        {
            Items = categorias,
            TotalCount = 2,
            Page = 1,
            PageSize = 10
        };

        _mockService.Setup(s => s.FindAllPagedAsync(It.IsAny<CategoriaFilterDto>()))
            .ReturnsAsync(Result.Success<PagedResult<CategoriaDto>, DomainError>(pagedResult));

        var result = await _controller.GetAll();

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedCategorias = okResult.Value.Should().BeAssignableTo<PagedResult<CategoriaDto>>().Subject;
        returnedCategorias.Items.Should().HaveCount(2);
    }

    /// <summary>
    /// Dado que no existen categorías, cuando se obtienen todas paginadas, entonces retorna 200 OK con lista vacía.
    /// Returns: 200 OK con lista vacía
    /// </summary>
    [Test]
    public async Task GetAll_SinCategorias_RetornaOkConListaVacia()
    {
        var pagedResult = new PagedResult<CategoriaDto>
        {
            Items = new List<CategoriaDto>(),
            TotalCount = 0,
            Page = 1,
            PageSize = 10
        };

        _mockService.Setup(s => s.FindAllPagedAsync(It.IsAny<CategoriaFilterDto>()))
            .ReturnsAsync(Result.Success<PagedResult<CategoriaDto>, DomainError>(pagedResult));

        var result = await _controller.GetAll();

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedCategorias = okResult.Value.Should().BeAssignableTo<PagedResult<CategoriaDto>>().Subject;
        returnedCategorias.Items.Should().BeEmpty();
    }

    /// <summary>
    /// Dado un filtro por nombre, cuando se obtienen categorías, entonces retorna solo las que coinciden.
    /// Returns: 200 OK con lista filtrada
    /// </summary>
    [Test]
    public async Task GetAll_ConFiltroNombre_RetornaListaFiltrada()
    {
        var categorias = new List<CategoriaDto>
        {
            new CategoriaDto(1, "Electrónica", null, DateTime.UtcNow, DateTime.UtcNow)
        };
        var pagedResult = new PagedResult<CategoriaDto>
        {
            Items = categorias,
            TotalCount = 1,
            Page = 1,
            PageSize = 10
        };

        _mockService.Setup(s => s.FindAllPagedAsync(It.IsAny<CategoriaFilterDto>()))
            .ReturnsAsync(Result.Success<PagedResult<CategoriaDto>, DomainError>(pagedResult));

        var result = await _controller.GetAll(nombre: "Electrónica");

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedCategorias = okResult.Value.Should().BeAssignableTo<PagedResult<CategoriaDto>>().Subject;
        returnedCategorias.Items.Should().HaveCount(1);
    }

    #endregion

    #region GetById Tests

    /// <summary>
    /// Dado que existe una categoría, cuando se obtiene por ID, entonces retorna 200 OK con la categoría.
    /// Returns: 200 OK con categoría encontrada
    /// </summary>
    [Test]
    public async Task GetById_ConIdExistente_RetornaOkConCategoria()
    {
        var categoria = new CategoriaDto(1, "Electrónica", null, DateTime.UtcNow, DateTime.UtcNow);

        _mockService.Setup(s => s.FindByIdAsync(1))
            .ReturnsAsync(Result.Success<CategoriaDto, DomainError>(categoria));

        var result = await _controller.GetById(1);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedCategoria = okResult.Value.Should().BeAssignableTo<CategoriaDto>().Subject;
        returnedCategoria.Id.Should().Be(1);
        returnedCategoria.Nombre.Should().Be("Electrónica");
    }

    /// <summary>
    /// Dado que no existe una categoría, cuando se obtiene por ID, entonces retorna 404 Not Found.
    /// Returns: 404 Not Found
    /// </summary>
    [Test]
    public async Task GetById_ConIdNoExistente_RetornaNotFound()
    {
        var error = new NotFoundError("Categoría no encontrada");

        _mockService.Setup(s => s.FindByIdAsync(999))
            .ReturnsAsync(Result.Failure<CategoriaDto, DomainError>(error));

        var result = await _controller.GetById(999);

        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFoundResult.Value.Should().NotBeNull();
    }

    #endregion

    #region Create Tests

    /// <summary>
    /// Dado un DTO válido, cuando se crea una categoría, entonces retorna 201 Created con la categoría.
    /// Returns: 201 Created con categoría creada
    /// </summary>
    [Test]
    public async Task Create_ConDtoValido_RetornaCreatedConCategoria()
    {
        var requestDto = new CategoriaRequestDto { Nombre = "Nueva Categoría" };
        var categoriaDto = new CategoriaDto(1, "Nueva Categoría", null, DateTime.UtcNow, DateTime.UtcNow);

        _mockService.Setup(s => s.CreateAsync(requestDto))
            .ReturnsAsync(Result.Success<CategoriaDto, DomainError>(categoriaDto));

        var result = await _controller.Create(requestDto);

        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.ActionName.Should().Be(nameof(CategoriasController.GetById));
        createdResult.RouteValues.Should().ContainKey("id");
        var returnedCategoria = createdResult.Value.Should().BeAssignableTo<CategoriaDto>().Subject;
        returnedCategoria.Nombre.Should().Be("Nueva Categoría");
    }

    /// <summary>
    /// Dado un DTO con nombre vacío, cuando se crea una categoría, entonces retorna 400 Bad Request.
    /// Returns: 400 Bad Request
    /// </summary>
    [Test]
    public async Task Create_ConNombreVacio_RetornaBadRequest()
    {
        var requestDto = new CategoriaRequestDto { Nombre = "" };
        var error = ValidationError.Create("Nombre no puede estar vacío");

        _mockService.Setup(s => s.CreateAsync(requestDto))
            .ReturnsAsync(Result.Failure<CategoriaDto, DomainError>(error));

        var result = await _controller.Create(requestDto);

        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.Value.Should().NotBeNull();
    }

    /// <summary>
    /// Dado un DTO con nombre duplicado, cuando se crea una categoría, entonces retorna 409 Conflict.
    /// Returns: 409 Conflict
    /// </summary>
    [Test]
    public async Task Create_ConNombreDuplicado_RetornaConflict()
    {
        var requestDto = new CategoriaRequestDto { Nombre = "Existente" };
        var error = new ConflictError("Ya existe una categoría con ese nombre");

        _mockService.Setup(s => s.CreateAsync(requestDto))
            .ReturnsAsync(Result.Failure<CategoriaDto, DomainError>(error));

        var result = await _controller.Create(requestDto);

        var conflictResult = result.Should().BeOfType<ConflictObjectResult>().Subject;
        conflictResult.Value.Should().NotBeNull();
    }

    #endregion

    #region Update Tests

    /// <summary>
    /// Dado un ID válido y DTO válido, cuando se actualiza, entonces retorna 200 OK con la categoría actualizada.
    /// Returns: 200 OK con categoría actualizada
    /// </summary>
    [Test]
    public async Task Update_ConIdValido_RetornaOkConCategoriaActualizada()
    {
        var id = 1L;
        var requestDto = new CategoriaRequestDto { Nombre = "Actualizada" };
        var categoriaDto = new CategoriaDto(1, "Actualizada", null, DateTime.UtcNow, DateTime.UtcNow);

        _mockService.Setup(s => s.UpdateAsync(id, requestDto))
            .ReturnsAsync(Result.Success<CategoriaDto, DomainError>(categoriaDto));

        var result = await _controller.Update(id, requestDto);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedCategoria = okResult.Value.Should().BeAssignableTo<CategoriaDto>().Subject;
        returnedCategoria.Nombre.Should().Be("Actualizada");
    }

    /// <summary>
    /// Dado un ID no existente, cuando se actualiza, entonces retorna 404 Not Found.
    /// Returns: 404 Not Found
    /// </summary>
    [Test]
    public async Task Update_ConIdNoExistente_RetornaNotFound()
    {
        var id = 999L;
        var requestDto = new CategoriaRequestDto { Nombre = "Actualizada" };
        var error = new NotFoundError("Categoría no encontrada");

        _mockService.Setup(s => s.UpdateAsync(id, requestDto))
            .ReturnsAsync(Result.Failure<CategoriaDto, DomainError>(error));

        var result = await _controller.Update(id, requestDto);

        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFoundResult.Value.Should().NotBeNull();
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
        var error = new NotFoundError("Categoría no encontrada");

        _mockService.Setup(s => s.DeleteAsync(999))
            .ReturnsAsync(UnitResult.Failure<DomainError>(error));

        var result = await _controller.Delete(999);

        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFoundResult.Value.Should().NotBeNull();
    }

    #endregion

    #region Tests Adicionales - Casos Borde

    [Test]
    public async Task GetById_ConIdCero_RetornaNotFound()
    {
        var error = new NotFoundError("Categoría no encontrada");

        _mockService.Setup(s => s.FindByIdAsync(0))
            .ReturnsAsync(Result.Failure<CategoriaDto, DomainError>(error));

        var result = await _controller.GetById(0);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Test]
    public async Task GetById_ConIdNegativo_RetornaNotFound()
    {
        var error = new NotFoundError("Categoría no encontrada");

        _mockService.Setup(s => s.FindByIdAsync(-1))
            .ReturnsAsync(Result.Failure<CategoriaDto, DomainError>(error));

        var result = await _controller.GetById(-1);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Test]
    public async Task Create_ConNombreLargo_RetornaCreated()
    {
        var nombreLargo = new string('A', 100);
        var requestDto = new CategoriaRequestDto { Nombre = nombreLargo };
        var categoriaDto = new CategoriaDto(1, nombreLargo, null, DateTime.UtcNow, DateTime.UtcNow);

        _mockService.Setup(s => s.CreateAsync(requestDto))
            .ReturnsAsync(Result.Success<CategoriaDto, DomainError>(categoriaDto));

        var result = await _controller.Create(requestDto);

        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.Value.Should().NotBeNull();
    }

    [Test]
    public async Task Create_ConNombreConCaracteresEspeciales_RetornaCreated()
    {
        var requestDto = new CategoriaRequestDto { Nombre = "Categoría #1 - Test®" };
        var categoriaDto = new CategoriaDto(1, requestDto.Nombre, null, DateTime.UtcNow, DateTime.UtcNow);

        _mockService.Setup(s => s.CreateAsync(requestDto))
            .ReturnsAsync(Result.Success<CategoriaDto, DomainError>(categoriaDto));

        var result = await _controller.Create(requestDto);

        result.Should().BeOfType<CreatedAtActionResult>();
    }

    [Test]
    public async Task Update_ConflictoPorNombreDuplicado_RetornaConflict()
    {
        var id = 1L;
        var requestDto = new CategoriaRequestDto { Nombre = "Existente" };
        var error = new ConflictError("Ya existe una categoría con ese nombre");

        _mockService.Setup(s => s.UpdateAsync(id, requestDto))
            .ReturnsAsync(Result.Failure<CategoriaDto, DomainError>(error));

        var result = await _controller.Update(id, requestDto);

        result.Should().BeOfType<ConflictObjectResult>();
    }

    [Test]
    public async Task Update_ErrorInterno_Retorna500()
    {
        var id = 1L;
        var requestDto = new CategoriaRequestDto { Nombre = "Actualizada" };
        var error = new InternalError("Error en base de datos");

        _mockService.Setup(s => s.UpdateAsync(id, requestDto))
            .ReturnsAsync(Result.Failure<CategoriaDto, DomainError>(error));

        var result = await _controller.Update(id, requestDto);

        var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(500);
    }

    [Test]
    public async Task Delete_CategoriaConProductos_Retorna500()
    {
        var error = new BusinessRuleError("No se puede eliminar una categoría con productos asociados");

        _mockService.Setup(s => s.DeleteAsync(1))
            .ReturnsAsync(UnitResult.Failure<DomainError>(error));

        var result = await _controller.Delete(1);

        var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(500);
    }

    #endregion
}

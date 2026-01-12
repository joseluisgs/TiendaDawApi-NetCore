using CSharpFunctionalExtensions;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using TiendaApi.Controllers;
using TiendaApi.Dtos.Categorias;
using TiendaApi.Errors;
using TiendaApi.Services.Categorias;

namespace TiendaApi.Tests.Unit.Controllers;

public class CategoriasControllerTests
{
    private readonly Mock<ICategoriaService> _mockService;
    private readonly Mock<ILogger<CategoriasController>> _mockLogger;
    private readonly CategoriasController _controller;

    public CategoriasControllerTests()
    {
        _mockService = new Mock<ICategoriaService>();
        _mockLogger = new Mock<ILogger<CategoriasController>>();
        _controller = new CategoriasController(_mockService.Object, _mockLogger.Object);
    }

    #region GetAll Tests

    /// <summary>
    /// Dado que existen categorías, cuando se obtienen todas, entonces retorna 200 OK con la lista.
    /// Returns: 200 OK con lista de categorías
    /// </summary>
    [Test]
    public async Task GetAll_ConCategoriasExistentes_RetornaOkConLista()
    {
        var categorias = new List<CategoriaDto>
        {
            new CategoriaDto { Id = 1, Nombre = "Electrónica" },
            new CategoriaDto { Id = 2, Nombre = "Ropa" }
        };

        _mockService.Setup(s => s.FindAllAsync())
            .ReturnsAsync(Result.Success<IEnumerable<CategoriaDto>, DomainError>(categorias));

        var result = await _controller.GetAll();

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedCategorias = okResult.Value.Should().BeAssignableTo<IEnumerable<CategoriaDto>>().Subject;
        returnedCategorias.Should().HaveCount(2);
    }

    /// <summary>
    /// Dado que no existen categorías, cuando se obtienen todas, entonces retorna 200 OK con lista vacía.
    /// Returns: 200 OK con lista vacía
    /// </summary>
    [Test]
    public async Task GetAll_SinCategorias_RetornaOkConListaVacia()
    {
        _mockService.Setup(s => s.FindAllAsync())
            .ReturnsAsync(Result.Success<IEnumerable<CategoriaDto>, DomainError>(new List<CategoriaDto>()));

        var result = await _controller.GetAll();

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedCategorias = okResult.Value.Should().BeAssignableTo<IEnumerable<CategoriaDto>>().Subject;
        returnedCategorias.Should().BeEmpty();
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
        var categoria = new CategoriaDto { Id = 1, Nombre = "Electrónica" };

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
        var error = DomainError.NotFound("Categoría no encontrada");

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
        var categoriaDto = new CategoriaDto { Id = 1, Nombre = "Nueva Categoría" };

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
        var error = DomainError.Validation("Nombre no puede estar vacío");

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
        var error = DomainError.Conflict("Ya existe una categoría con ese nombre");

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
        var categoriaDto = new CategoriaDto { Id = 1, Nombre = "Actualizada" };

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
        var error = DomainError.NotFound("Categoría no encontrada");

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
        var error = DomainError.NotFound("Categoría no encontrada");

        _mockService.Setup(s => s.DeleteAsync(999))
            .ReturnsAsync(UnitResult.Failure<DomainError>(error));

        var result = await _controller.Delete(999);

        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFoundResult.Value.Should().NotBeNull();
    }

    #endregion
}

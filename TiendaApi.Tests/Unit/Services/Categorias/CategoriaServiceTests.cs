using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using TiendaApi.Apis.Dtos.Categorias;
using TiendaApi.Apis.Errors;
using TiendaApi.Apis.Models;
using TiendaApi.Apis.Repositories.Categorias;
using TiendaApi.Apis.Services.Categorias;

namespace TiendaApi.Tests.Unit.Services.Categorias;

/// <summary>
/// Suite de tests para CategoriaService
/// Prueba el enfoque Result Pattern para operaciones de categorías
/// </summary>
public class CategoriaServiceTests
{
    private Mock<ICategoriaRepository> _mockRepository = null!;
    private Mock<ILogger<CategoriaService>> _mockLogger = null!;
    private CategoriaService _service = null!;

    [SetUp]
    public void Setup()
    {
        _mockRepository = new Mock<ICategoriaRepository>();
        _mockLogger = new Mock<ILogger<CategoriaService>>();
        _service = new CategoriaService(_mockRepository.Object, _mockLogger.Object);
    }

    #region FindAllAsync Tests

    [Test]
    public async Task FindAllAsync_ConCategorias_RetornaTodasLasCategorias()
    {
        // Arrange
        var categorias = new List<Categoria>
        {
            new() { Id = 1, Nombre = "Electronics" },
            new() { Id = 2, Nombre = "Books" }
        };

        _mockRepository.Setup(r => r.FindAllAsync())
            .ReturnsAsync(categorias);

        // Act
        var result = await _service.FindAllAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
    }

    [Test]
    public async Task FindAllAsync_SinCategorias_RetornaListaVacia()
    {
        // Arrange
        _mockRepository.Setup(r => r.FindAllAsync())
            .ReturnsAsync(new List<Categoria>());

        // Act
        var result = await _service.FindAllAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    #endregion

    #region FindByIdAsync Tests

    [Test]
    public async Task FindByIdAsync_ConIdExistente_RetornaExito()
    {
        // Arrange
        var categoria = new Categoria { Id = 1, Nombre = "Electronics" };
        _mockRepository.Setup(r => r.FindByIdAsync(1))
            .ReturnsAsync(categoria);

        // Act
        var result = await _service.FindByIdAsync(1);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(1);
        result.Value.Nombre.Should().Be("Electronics");
    }

    [Test]
    public async Task FindByIdAsync_ConIdNoExistente_RetornaNoEncontrado()
    {
        // Arrange
        _mockRepository.Setup(r => r.FindByIdAsync(999))
            .ReturnsAsync((Categoria?)null);

        // Act
        var result = await _service.FindByIdAsync(999);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.NotFound);
    }

    #endregion

    #region CreateAsync Tests

    [Test]
    public async Task CreateAsync_ConDatosValidos_RetornaExito()
    {
        // Arrange
        var dto = new CategoriaRequestDto { Nombre = "New Category" };
        var savedCategoria = new Categoria { Id = 1, Nombre = "New Category" };

        _mockRepository.Setup(r => r.ExistsByNombreAsync("New Category", null))
            .ReturnsAsync(false);
        _mockRepository.Setup(r => r.SaveAsync(It.IsAny<Categoria>()))
            .ReturnsAsync(savedCategoria);

        // Act
        var result = await _service.CreateAsync(dto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Nombre.Should().Be("New Category");
    }

    [Test]
    public async Task CreateAsync_ConNombreVacio_RetornaErrorValidacion()
    {
        // Arrange
        var dto = new CategoriaRequestDto { Nombre = "" };

        // Act
        var result = await _service.CreateAsync(dto);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
    }

    [Test]
    public async Task CreateAsync_ConNombreDuplicado_RetornaErrorConflicto()
    {
        // Arrange
        var dto = new CategoriaRequestDto { Nombre = "Existing" };

        _mockRepository.Setup(r => r.ExistsByNombreAsync("Existing", null))
            .ReturnsAsync(true);

        // Act
        var result = await _service.CreateAsync(dto);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Conflict);
    }

    #endregion

    #region UpdateAsync Tests

    [Test]
    public async Task UpdateAsync_ConDatosValidos_RetornaExito()
    {
        // Arrange
        var dto = new CategoriaRequestDto { Nombre = "Updated Category" };
        var existingCategoria = new Categoria { Id = 1, Nombre = "Old Category" };
        var updatedCategoria = new Categoria { Id = 1, Nombre = "Updated Category" };

        _mockRepository.Setup(r => r.FindByIdAsync(1))
            .ReturnsAsync(existingCategoria);
        _mockRepository.Setup(r => r.ExistsByNombreAsync("Updated Category", 1))
            .ReturnsAsync(false);
        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<Categoria>()))
            .ReturnsAsync(updatedCategoria);

        // Act
        var result = await _service.UpdateAsync(1, dto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Nombre.Should().Be("Updated Category");
    }

    [Test]
    public async Task UpdateAsync_ConIdNoExistente_RetornaNoEncontrado()
    {
        // Arrange
        var dto = new CategoriaRequestDto { Nombre = "Updated" };
        _mockRepository.Setup(r => r.FindByIdAsync(999))
            .ReturnsAsync((Categoria?)null);

        // Act
        var result = await _service.UpdateAsync(999, dto);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.NotFound);
    }

    #endregion

    #region DeleteAsync Tests

    [Test]
    public async Task DeleteAsync_ConIdExistente_RetornaExito()
    {
        // Arrange
        var categoria = new Categoria { Id = 1, Nombre = "To Delete" };
        _mockRepository.Setup(r => r.FindByIdAsync(1))
            .ReturnsAsync(categoria);

        // Act
        var result = await _service.DeleteAsync(1);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Test]
    public async Task DeleteAsync_ConIdNoExistente_RetornaNoEncontrado()
    {
        // Arrange
        _mockRepository.Setup(r => r.FindByIdAsync(999))
            .ReturnsAsync((Categoria?)null);

        // Act
        var result = await _service.DeleteAsync(999);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.NotFound);
    }

    #endregion
}

using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using TiendaApi.Dtos.Categorias;
using TiendaApi.Errors;
using TiendaApi.Models;
using TiendaApi.Repositories.Categorias;
using TiendaApi.Services.Categorias;

namespace TiendaApi.Tests.Unit.Services.Categorias;

/// <summary>
/// Test suite for CategoriaService
/// Tests Result Pattern approach for category operations
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
    public async Task FindAllAsync_WithCategorias_ReturnsAllCategorias()
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
    public async Task FindAllAsync_WithNoCategorias_ReturnsEmptyList()
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
    public async Task FindByIdAsync_WithExistingId_ReturnsSuccess()
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
    public async Task FindByIdAsync_WithNonExistentId_ReturnsNotFound()
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
    public async Task CreateAsync_WithValidData_ReturnsSuccess()
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
    public async Task CreateAsync_WithEmptyNombre_ReturnsValidationError()
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
    public async Task CreateAsync_WithDuplicateNombre_ReturnsConflictError()
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
    public async Task UpdateAsync_WithValidData_ReturnsSuccess()
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
    public async Task UpdateAsync_WithNonExistentId_ReturnsNotFound()
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
    public async Task DeleteAsync_WithExistingId_ReturnsSuccess()
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
    public async Task DeleteAsync_WithNonExistentId_ReturnsNotFound()
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

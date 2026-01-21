using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using TiendaApi.Api.Dtos.Categorias;
using TiendaApi.Api.Errors;
using TiendaApi.Api.Models;
using TiendaApi.Api.Repositories.Categorias;
using TiendaApi.Api.Services.Cache;
using TiendaApi.Api.Services.Categorias;
using TiendaApi.Api.Validators.Categorias;

namespace TiendaApi.Tests.Unit.Services.Categorias;

/// <summary>
/// Suite de tests para CategoriaService
/// Prueba el enfoque Result Pattern para operaciones de categorías
/// </summary>
public class CategoriaServiceTests
{
    private Mock<ICategoriaRepository> _mockRepository = null!;
    private Mock<ILogger<CategoriaService>> _mockLogger = null!;
    private Mock<IValidator<CategoriaRequestDto>> _mockValidator = null!;
    private Mock<ICacheService> _mockCacheService = null!;
    private Mock<IConfiguration> _mockConfiguration = null!;
    private CategoriaService _service = null!;

    private void CreateService()
    {
        _service = new CategoriaService(
            _mockRepository.Object,
            _mockLogger.Object,
            _mockValidator.Object,
            _mockCacheService.Object,
            _mockConfiguration.Object);
    }

    [SetUp]
    public void Setup()
    {
        _mockRepository = new Mock<ICategoriaRepository>();
        _mockLogger = new Mock<ILogger<CategoriaService>>();
        _mockValidator = new Mock<IValidator<CategoriaRequestDto>>();
        _mockCacheService = new Mock<ICacheService>();
        _mockConfiguration = new Mock<IConfiguration>();

        _mockValidator.Setup(v => v.ValidateAsync(It.IsAny<CategoriaRequestDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _mockCacheService.Setup(x => x.GetAsync<IEnumerable<CategoriaDto>>(It.IsAny<string>()))
            .ReturnsAsync((IEnumerable<CategoriaDto>?)null);

        _mockCacheService.Setup(x => x.GetAsync<CategoriaDto>(It.IsAny<string>()))
            .ReturnsAsync((CategoriaDto?)null);

        _mockConfiguration.SetupGet(c => c["Cache:CategoriaCacheTTLMinutes"])
            .Returns("10");

        CreateService();
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
    public async Task FindByIdAsync_ConCategoriaExistente_RetornaCategoria()
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
    public async Task FindByIdAsync_ConCategoriaNoExistente_RetornaNotFound()
    {
        // Arrange
        _mockRepository.Setup(r => r.FindByIdAsync(999))
            .ReturnsAsync((Categoria?)null);

        // Act
        var result = await _service.FindByIdAsync(999);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<NotFoundError>();
    }

    #endregion

    #region CreateAsync Tests

    [Test]
    public async Task CreateAsync_ConDatosValidos_RetornaCategoriaCreada()
    {
        // Arrange
        var dto = new CategoriaRequestDto { Nombre = "Electronics" };
        var categoriaGuardada = new Categoria { Id = 1, Nombre = dto.Nombre };

        _mockRepository.Setup(r => r.ExistsByNombreAsync("Electronics", null))
            .ReturnsAsync(false);
        _mockRepository.Setup(r => r.SaveAsync(It.IsAny<Categoria>()))
            .ReturnsAsync(categoriaGuardada);

        // Act
        var result = await _service.CreateAsync(dto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(1);
        result.Value.Nombre.Should().Be("Electronics");
    }

    [Test]
    public async Task CreateAsync_ConNombreVacio_RetornaErrorValidacion()
    {
        // Arrange
        var dto = new CategoriaRequestDto { Nombre = "" };

        _mockValidator.Setup(v => v.ValidateAsync(dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(new[]
            {
                new ValidationFailure("Nombre", "El nombre es obligatorio")
            }));

        // Re-crear servicio con mock configurado
        _service = new CategoriaService(
            _mockRepository.Object,
            _mockLogger.Object,
            _mockValidator.Object,
            _mockCacheService.Object,
            _mockConfiguration.Object);

        // Act
        var result = await _service.CreateAsync(dto);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<ValidationError>();
        ((ValidationError)result.Error).ValidationErrors.Should().ContainKey("Nombre");
    }

    [Test]
    public async Task CreateAsync_ConNombreDuplicado_RetornaConflicto()
    {
        // Arrange
        var dto = new CategoriaRequestDto { Nombre = "Electronics" };

        _mockRepository.Setup(r => r.ExistsByNombreAsync("Electronics", null))
            .ReturnsAsync(true);

        // Act
        var result = await _service.CreateAsync(dto);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<ConflictError>();
    }

    #endregion

    #region UpdateAsync Tests

    [Test]
    public async Task UpdateAsync_ConDatosValidos_RetornaCategoriaActualizada()
    {
        // Arrange
        var dto = new CategoriaRequestDto { Nombre = "Updated Category" };
        var categoriaExistente = new Categoria { Id = 1, Nombre = "Old Category" };
        var categoriaActualizada = new Categoria { Id = 1, Nombre = dto.Nombre };

        _mockRepository.Setup(r => r.FindByIdAsync(1))
            .ReturnsAsync(categoriaExistente);
        _mockRepository.Setup(r => r.ExistsByNombreAsync("Updated Category", 1))
            .ReturnsAsync(false);
        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<Categoria>()))
            .ReturnsAsync(categoriaActualizada);

        // Act
        var result = await _service.UpdateAsync(1, dto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Nombre.Should().Be("Updated Category");
    }

    [Test]
    public async Task UpdateAsync_ConCategoriaNoExistente_RetornaNotFound()
    {
        // Arrange
        var dto = new CategoriaRequestDto { Nombre = "Updated Category" };

        _mockRepository.Setup(r => r.FindByIdAsync(999))
            .ReturnsAsync((Categoria?)null);

        // Act
        var result = await _service.UpdateAsync(999, dto);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<NotFoundError>();
    }

    #endregion

    #region DeleteAsync Tests

    [Test]
    public async Task DeleteAsync_ConCategoriaExistente_RetornaExito()
    {
        // Arrange
        var categoria = new Categoria { Id = 1, Nombre = "Electronics" };
        _mockRepository.Setup(r => r.FindByIdAsync(1))
            .ReturnsAsync(categoria);
        _mockRepository.Setup(r => r.DeleteAsync(1))
            .ReturnsAsync(true);

        // Act
        var result = await _service.DeleteAsync(1);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Test]
    public async Task DeleteAsync_ConCategoriaNoExistente_RetornaNotFound()
    {
        // Arrange
        _mockRepository.Setup(r => r.FindByIdAsync(999))
            .ReturnsAsync((Categoria?)null);

        // Act
        var result = await _service.DeleteAsync(999);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<NotFoundError>();
    }

    #endregion
}

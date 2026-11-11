using System.Text;
using CSharpFunctionalExtensions;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using TiendaApi.Api.Errors;
using TiendaApi.Api.Services.Storage;
using Path = System.IO.Path;

namespace TiendaApi.Tests.Integration.Services.Storage;

/// <summary>
///     Tests de integración para FileSystemStorageService.
///     Verifica operaciones reales con el sistema de archivos.
/// </summary>
[TestFixture]
[Category("Integration")]
[Category("Storage")]
public class FileSystemStorageServiceIntegrationTests {
    [SetUp]
    public void SetUp() {
        _testDirectory = Path.Combine(Path.GetTempPath(), $"storage_integration_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testDirectory);

        IConfigurationRoot configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> {
                { "Storage:UploadPath", "uploads" },
                { "Storage:MaxFileSize", "5242880" },
                { "Storage:AllowedExtensions", ".jpg,.jpeg,.png,.gif" },
                { "Storage:AllowedContentTypes", "image/jpeg,image/png,image/gif" }
            }!)
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddLogging(builder => builder.AddConsole());

        ServiceProvider serviceProvider = services.BuildServiceProvider();
        ILogger<FileSystemStorageService> logger =
            serviceProvider.GetRequiredService<ILogger<FileSystemStorageService>>();

        var mockEnvironment = new Mock<IWebHostEnvironment>();
        mockEnvironment.Setup(e => e.WebRootPath).Returns(_testDirectory);

        _service = new FileSystemStorageService(configuration, logger, mockEnvironment.Object);
    }

    [TearDown]
    public void TearDown() {
        if (Directory.Exists(_testDirectory)) {
            Directory.Delete(_testDirectory, true);
        }
    }

    private string _testDirectory = null!;
    private FileSystemStorageService _service = null!;

    private static IFormFile CreateMockFile(string filename, string contentType, long size) {
        var mock = new Mock<IFormFile>();
        mock.Setup(f => f.FileName).Returns(filename);
        mock.Setup(f => f.Length).Returns(size);
        mock.Setup(f => f.ContentType).Returns(contentType);
        mock.Setup(f => f.OpenReadStream()).Returns(() => {
            var stream = new MemoryStream(Encoding.UTF8.GetBytes("test file content"));
            stream.Position = 0;
            return stream;
        });
        return mock.Object;
    }

    [Test]
    public async Task SaveFileAsync_ArchivoValido_GuardaEnSistemaArchivos() {
        // Arrange
        IFormFile file = CreateMockFile("integration-test.jpg", "image/jpeg", 1024);
        string folder = "test-folder";

        // Act
        Result<string, DomainError> result = await _service.SaveFileAsync(file, folder);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNullOrEmpty();
        result.Value.Should().Contain(folder);
        result.Value.Should().EndWith(".jpg");
    }

    [Test]
    public async Task SaveFileAsync_MultiplesArchivos_GuardaArchivosSeparados() {
        // Arrange
        IFormFile file1 = CreateMockFile("file1.png", "image/png", 512);
        IFormFile file2 = CreateMockFile("file2.gif", "image/gif", 256);

        // Act
        Result<string, DomainError> result1 = await _service.SaveFileAsync(file1, "multi-test");
        Result<string, DomainError> result2 = await _service.SaveFileAsync(file2, "multi-test");

        // Assert
        result1.IsSuccess.Should().BeTrue();
        result2.IsSuccess.Should().BeTrue();
        result1.Value.Should().NotBe(result2.Value);
    }

    [Test]
    public async Task SaveFileAsync_CreaDirectorioSiNoExiste() {
        // Arrange
        IFormFile file = CreateMockFile("test.jpg", "image/jpeg", 1024);
        string newFolder = $"new-folder-{Guid.NewGuid():N}";

        // Act
        Result<string, DomainError> result = await _service.SaveFileAsync(file, newFolder);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Contain(newFolder);
    }

    [Test]
    public async Task SaveFileAsync_OverrideNombre_GeneraNombreUnico() {
        // Arrange
        IFormFile file1 = CreateMockFile("same-name.jpg", "image/jpeg", 1024);
        IFormFile file2 = CreateMockFile("same-name.jpg", "image/jpeg", 1024);

        // Act
        Result<string, DomainError> result1 = await _service.SaveFileAsync(file1, "override-test");
        Result<string, DomainError> result2 = await _service.SaveFileAsync(file2, "override-test");

        // Assert
        result1.IsSuccess.Should().BeTrue();
        result2.IsSuccess.Should().BeTrue();
        result1.Value.Should().NotBe(result2.Value);
    }

    [Test]
    public async Task DeleteFileAsync_ArchivoExistente_EliminaDelDisco() {
        // Arrange
        IFormFile file = CreateMockFile("to-delete.jpg", "image/jpeg", 1024);
        Result<string, DomainError> saveResult = await _service.SaveFileAsync(file, "delete-test");
        saveResult.IsSuccess.Should().BeTrue();

        // Act
        Result<bool, DomainError> deleteResult = await _service.DeleteFileAsync(saveResult.Value);

        // Assert
        deleteResult.IsSuccess.Should().BeTrue();
    }

    [Test]
    public async Task DeleteFileAsync_ArchivoNoExistente_NoLanzaExcepcion() {
        // Act
        Result<bool, DomainError> result = await _service.DeleteFileAsync("/uploads/nonexistent/file.jpg");

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Test]
    public void FileExists_ConPathValido_RetornaBool() {
        // Arrange - just verify the method works without throwing
        bool result = _service.FileExists("/uploads/test.jpg");

        // Assert - returns false for non-existent file
        result.Should().BeFalse();
    }

    [Test]
    public void FileExists_ConPathVacio_RetornaFalse() {
        // Act
        bool result = _service.FileExists("");

        // Assert
        result.Should().BeFalse();
    }

    [Test]
    public void FileExists_ConPathNull_RetornaFalse() {
        // Act
        bool result = _service.FileExists(null!);

        // Assert
        result.Should().BeFalse();
    }

    [Test]
    public void GetFullPath_DiferentesPrefijos_ResuelveCorrectamente() {
        // Test /storage/ prefix
        string result1 = _service.GetFullPath("/storage/test.jpg");
        result1.Should().Contain("test.jpg");

        // Test /uploads/ prefix
        string result2 = _service.GetFullPath("/uploads/test.jpg");
        result2.Should().Contain("test.jpg");

        // Test filename only
        string result3 = _service.GetFullPath("simple.jpg");
        result3.Should().Contain("simple.jpg");
    }

    [Test]
    public void GetRelativePath_DiferentesFolders_RetornaRutasCorrectas() {
        // Test default folder
        string result1 = _service.GetRelativePath("test.jpg");
        result1.Should().Be("/uploads/productos/test.jpg");

        // Test custom folder
        string result2 = _service.GetRelativePath("test.jpg", "categorias");
        result2.Should().Be("/uploads/categorias/test.jpg");

        // Test another custom folder
        string result3 = _service.GetRelativePath("avatar.png", "avatars");
        result3.Should().Be("/uploads/avatars/avatar.png");
    }

    [Test]
    public async Task SaveFileAsync_ExtensionJPEGMayuscula_Acepta() {
        // Arrange
        IFormFile file = CreateMockFile("test.JPEG", "image/jpeg", 1024);

        // Act
        Result<string, DomainError> result = await _service.SaveFileAsync(file, "validation-test");

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Test]
    public async Task SaveFileAsync_TamanoMaximoPermitido_Acepta() {
        // Arrange - 5MB exactly
        IFormFile file = CreateMockFile("max-size.jpg", "image/jpeg", 5 * 1024 * 1024);

        // Act
        Result<string, DomainError> result = await _service.SaveFileAsync(file, "validation-test");

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Test]
    public async Task SaveFileAsync_TamanoExcedido_Rechaza() {
        // Arrange - 6MB (exceeds 5MB limit)
        IFormFile file = CreateMockFile("too-big.jpg", "image/jpeg", 6 * 1024 * 1024);

        // Act
        Result<string, DomainError> result = await _service.SaveFileAsync(file, "validation-test");

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Test]
    public async Task SaveFileAsync_TipoContenidoWebP_Rechaza() {
        // Arrange
        IFormFile file = CreateMockFile("test.jpg", "image/webp", 1024);

        // Act
        Result<string, DomainError> result = await _service.SaveFileAsync(file, "validation-test");

        // Assert
        result.IsFailure.Should().BeTrue();
    }
}

using CSharpFunctionalExtensions;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using TiendaApi.Api.Errors;
using TiendaApi.Api.Services.Storage;

namespace TiendaApi.Tests.Unit.Services.Storage;

/// <summary>
/// Tests unitarios para FileSystemStorageService.
/// Verifica la gestión de archivos en el sistema de archivos local.
/// </summary>
public class FileSystemStorageServiceTests
{
    private readonly Mock<ILogger<FileSystemStorageService>> _mockLogger;
    private readonly Mock<IWebHostEnvironment> _mockEnv;
    private readonly IConfiguration _configuration;
    private readonly FileSystemStorageService _storageService;

    public FileSystemStorageServiceTests()
    {
        _mockLogger = new Mock<ILogger<FileSystemStorageService>>();
        _mockEnv = new Mock<IWebHostEnvironment>();
        _mockEnv.Setup(e => e.ContentRootPath).Returns(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "tienda-tests"));
        _mockEnv.Setup(e => e.WebRootPath).Returns(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "tienda-tests", "wwwroot"));

        var inMemorySettings = new Dictionary<string, string>
        {
            { "Storage:UploadPath", "uploads" },
            { "Storage:MaxFileSize", "5242880" },
            { "Storage:AllowedExtensions", ".jpg,.jpeg,.png,.gif" },
            { "Storage:AllowedContentTypes", "image/jpeg,image/png,image/gif" }
        };

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings!)
            .Build();

        _storageService = new FileSystemStorageService(_configuration, _mockLogger.Object, _mockEnv.Object);
    }

    #region SaveFileAsync Tests

    /// <summary>
    /// Verifica que guardar un archivo válido retorna la ruta relativa.
    /// </summary>
    [Test]
    public async Task SaveFileAsync_ConArchivoValido_RetornaRutaRelativa()
    {
        var file = CreateMockFormFile("test.jpg", "image/jpeg", 1024);

        var result = await _storageService.SaveFileAsync(file, "productos");

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Contain("/uploads/productos/");
        result.Value.Should().Contain(".jpg");
    }

    /// <summary>
    /// Verifica que guardar un archivo vacío retorna error de validación.
    /// </summary>
    [Test]
    public async Task SaveFileAsync_ConArchivoVacio_RetornaError()
    {
        var file = CreateMockFormFile("test.jpg", "image/jpeg", 0);

        var result = await _storageService.SaveFileAsync(file, "productos");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().NotBeNull();
    }

    /// <summary>
    /// Verifica que guardar un archivo nulo retorna error de validación.
    /// </summary>
    [Test]
    public async Task SaveFileAsync_ConArchivoNulo_RetornaError()
    {
        var result = await _storageService.SaveFileAsync(null!, "productos");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().NotBeNull();
    }

    /// <summary>
    /// Verifica que guardar un archivo con extensión no permitida retorna error.
    /// </summary>
    [Test]
    public async Task SaveFileAsync_ConExtensionNoPermitida_RetornaError()
    {
        var file = CreateMockFormFile("test.pdf", "application/pdf", 1024);

        var result = await _storageService.SaveFileAsync(file, "productos");

        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Contain("Extensión de archivo no permitida");
    }

    /// <summary>
    /// Verifica que guardar un archivo con tipo de contenido no permitido retorna error.
    /// </summary>
    [Test]
    public async Task SaveFileAsync_ConTipoContenidoNoPermitido_RetornaError()
    {
        var file = CreateMockFormFile("test.jpg", "image/webp", 1024);

        var result = await _storageService.SaveFileAsync(file, "productos");

        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Contain("Tipo de contenido no permitido");
    }

    /// <summary>
    /// Verifica que la validación de tamaño máximo funciona correctamente.
    /// </summary>
    [Test]
    public async Task SaveFileAsync_ConArchivoMuyGrande_RetornaError()
    {
        var file = CreateMockFormFile("test.jpg", "image/jpeg", 10 * 1024 * 1024);

        var result = await _storageService.SaveFileAsync(file, "productos");

        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Contain("excede el tamaño máximo");
    }

    /// <summary>
    /// Verifica que la validación de tamaño máximo acepta archivos dentro del límite.
    /// </summary>
    [Test]
    public async Task SaveFileAsync_ConArchivoEnLimite_RetornaExito()
    {
        var file = CreateMockFormFile("test.jpg", "image/jpeg", 4 * 1024 * 1024);

        var result = await _storageService.SaveFileAsync(file, "productos");

        result.IsSuccess.Should().BeTrue();
    }

    /// <summary>
    /// Verifica que la validación de tamaño máximo acepta archivos de 5MB exactos.
    /// </summary>
    [Test]
    public async Task SaveFileAsync_ConArchivoDe5MB_RetornaExito()
    {
        var file = CreateMockFormFile("test.jpg", "image/jpeg", 5 * 1024 * 1024);

        var result = await _storageService.SaveFileAsync(file, "productos");

        result.IsSuccess.Should().BeTrue();
    }

    /// <summary>
    /// Verifica que guardar un archivo PNG retorna la ruta correcta.
    /// </summary>
    [Test]
    public async Task SaveFileAsync_ConArchivoPng_RetornaRutaCorrecta()
    {
        var file = CreateMockFormFile("imagen.png", "image/png", 2048);

        var result = await _storageService.SaveFileAsync(file, "productos");

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().EndWith(".png");
    }

    /// <summary>
    /// Verifica que guardar un archivo GIF retorna la ruta correcta.
    /// </summary>
    [Test]
    public async Task SaveFileAsync_ConArchivoGif_RetornaRutaCorrecta()
    {
        var file = CreateMockFormFile("animacion.gif", "image/gif", 512);

        var result = await _storageService.SaveFileAsync(file, "categorias");

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Contain("/uploads/categorias/");
        result.Value.Should().EndWith(".gif");
    }

    /// <summary>
    /// Verifica que el nombre de archivo generado es único.
    /// </summary>
    [Test]
    public async Task SaveFileAsync_GuardaDosArchivos_GeneraNombresUnicos()
    {
        var file1 = CreateMockFormFile("test.jpg", "image/jpeg", 1024);
        var file2 = CreateMockFormFile("test.jpg", "image/jpeg", 1024);

        var result1 = await _storageService.SaveFileAsync(file1, "productos");
        var result2 = await _storageService.SaveFileAsync(file2, "productos");

        result1.IsSuccess.Should().BeTrue();
        result2.IsSuccess.Should().BeTrue();
        result1.Value.Should().NotBe(result2.Value);
    }

    #endregion

    #region DeleteFileAsync Tests

    /// <summary>
    /// Verifica que eliminar un archivo vacío retorna éxito.
    /// </summary>
    [Test]
    public async Task DeleteFileAsync_ConNombreVacio_RetornaTrue()
    {
        var result = await _storageService.DeleteFileAsync(string.Empty);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
    }

    /// <summary>
    /// Verifica que eliminar un archivo nulo retorna éxito.
    /// </summary>
    [Test]
    public async Task DeleteFileAsync_ConNombreNulo_RetornaTrue()
    {
        var result = await _storageService.DeleteFileAsync(null!);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
    }

    /// <summary>
    /// Verifica que eliminar un archivo que no existe retorna éxito (sin excepción).
    /// </summary>
    [Test]
    public async Task DeleteFileAsync_ArchivoNoExistente_RetornaTrue()
    {
        var result = await _storageService.DeleteFileAsync("/uploads/productos/noexiste.jpg");

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
    }

    /// <summary>
    /// Verifica que la eliminación de un archivo manejable retorna éxito.
    /// </summary>
    [Test]
    public async Task DeleteFileAsync_ConPathRelativo_RetornaTrue()
    {
        var file = CreateMockFormFile("to-delete.jpg", "image/jpeg", 1024);
        var saveResult = await _storageService.SaveFileAsync(file, "productos");
        saveResult.IsSuccess.Should().BeTrue();

        var deleteResult = await _storageService.DeleteFileAsync(saveResult.Value);

        deleteResult.IsSuccess.Should().BeTrue();
        deleteResult.Value.Should().BeTrue();
    }

    #endregion

    #region FileExists Tests

    /// <summary>
    /// Verifica que FileExists retorna false para nombre vacío.
    /// </summary>
    [Test]
    public void FileExists_ConNombreVacio_RetornaFalse()
    {
        var result = _storageService.FileExists(string.Empty);

        result.Should().BeFalse();
    }

    /// <summary>
    /// Verifica que FileExists retorna false para nombre nulo.
    /// </summary>
    [Test]
    public void FileExists_ConNombreNulo_RetornaFalse()
    {
        var result = _storageService.FileExists(null!);

        result.Should().BeFalse();
    }

    /// <summary>
    /// Verifica que FileExists retorna false para archivo que no existe.
    /// </summary>
    [Test]
    public void FileExists_ArchivoNoExistente_RetornaFalse()
    {
        var result = _storageService.FileExists("/uploads/productos/noexiste.jpg");

        result.Should().BeFalse();
    }

    #endregion

    #region GetFullPath Tests

    /// <summary>
    /// Verifica que GetFullPath retorna la ruta completa correcta.
    /// </summary>
    [Test]
    public void GetFullPath_ConPathRelativo_RetornaRutaCompleta()
    {
        var path = _storageService.GetFullPath("/uploads/productos/test.jpg");

        path.Should().Contain("uploads");
        path.Should().Contain("productos");
        path.Should().Contain("test.jpg");
    }

    /// <summary>
    /// Verifica que GetFullPath limpia el prefijo /storage/.
    /// </summary>
    [Test]
    public void GetFullPath_ConPrefijoStorage_RetornaPathLimpio()
    {
        var path = _storageService.GetFullPath("/storage/uploads/productos/test.jpg");

        path.Should().Contain("uploads");
        path.Should().Contain("productos");
    }

    /// <summary>
    /// Verifica que GetFullPath con ruta absoluta la retorna sin modificación.
    /// </summary>
    [Test]
    public void GetFullPath_ConRutaAbsoluta_RetornaMismaRuta()
    {
        var absolutePath = "C:\\temp\\uploads\\productos\\test.jpg";

        var path = _storageService.GetFullPath(absolutePath);

        path.Should().Be(absolutePath);
    }

    #endregion

    #region GetRelativePath Tests

    /// <summary>
    /// Verifica que GetRelativePath retorna la ruta relativa correcta.
    /// </summary>
    [Test]
    public void GetRelativePath_ConFolderProducto_RetornaRutaCorrecta()
    {
        var path = _storageService.GetRelativePath("test.jpg", "productos");

        path.Should().Be("/uploads/productos/test.jpg");
    }

    /// <summary>
    /// Verifica que GetRelativePath usa "productos" como carpeta por defecto.
    /// </summary>
    [Test]
    public void GetRelativePath_SinFolder_UsaProductoComoDefecto()
    {
        var path = _storageService.GetRelativePath("test.jpg");

        path.Should().Be("/uploads/productos/test.jpg");
    }

    /// <summary>
    /// Verifica que GetRelativePath funciona con diferentes carpetas.
    /// </summary>
    [Test]
    public void GetRelativePath_ConFolderCategoria_RetornaRutaCorrecta()
    {
        var path = _storageService.GetRelativePath("categoria.jpg", "categorias");

        path.Should().Be("/uploads/categorias/categoria.jpg");
    }

    #endregion

    #region Validation Tests

    /// <summary>
    /// Verifica que la validación acepta extensiones en mayúsculas.
    /// </summary>
    [Test]
    public async Task SaveFileAsync_ExtensionMayuscula_RetornaExito()
    {
        var file = CreateMockFormFile("test.JPG", "image/jpeg", 1024);

        var result = await _storageService.SaveFileAsync(file, "productos");

        result.IsSuccess.Should().BeTrue();
    }

    /// <summary>
    /// Verifica que la validación acepta tipos de contenido con case insensitive.
    /// </summary>
    [Test]
    public async Task SaveFileAsync_TipoContenidoMayuscula_RetornaExito()
    {
        var file = CreateMockFormFile("test.jpg", "IMAGE/JPEG", 1024);

        var result = await _storageService.SaveFileAsync(file, "productos");

        result.IsSuccess.Should().BeTrue();
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Crea un objeto IFormFile simulado para testing.
    /// </summary>
    private static IFormFile CreateMockFormFile(string fileName, string contentType, long length)
    {
        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.FileName).Returns(fileName);
        mockFile.Setup(f => f.ContentType).Returns(contentType);
        mockFile.Setup(f => f.Length).Returns(length);
        mockFile.Setup(f => f.OpenReadStream()).Returns(() =>
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(new byte[length]);
            writer.Flush();
            stream.Position = 0;
            return stream;
        });
        return mockFile.Object;
    }

    #endregion
}

using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using TiendaApi.Api.Controllers;

namespace TiendaApi.Tests.Unit.Controllers;

public class StorageControllerTests
{
    private readonly Mock<IWebHostEnvironment> _mockEnvironment;
    private readonly Mock<ILogger<StorageController>> _mockLogger;
    private readonly StorageController _controller;
    private readonly string _testDirectory;

    public StorageControllerTests()
    {
        _mockEnvironment = new Mock<IWebHostEnvironment>();
        _mockLogger = new Mock<ILogger<StorageController>>();
        _testDirectory = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"TiendaApi_StorageTests_{Guid.NewGuid():N}");
        System.IO.Directory.CreateDirectory(_testDirectory);
        _mockEnvironment.Setup(e => e.ContentRootPath).Returns(_testDirectory);
        _controller = new StorageController(_mockEnvironment.Object, _mockLogger.Object);
    }

    [TearDown]
    public void TearDown()
    {
        try
        {
            if (System.IO.Directory.Exists(_testDirectory))
            {
                System.IO.Directory.Delete(_testDirectory, true);
            }
        }
        catch
        {
            // Ignore cleanup errors
        }
    }

    [Test]
    public void GetFile_ConRutaVacia_Retorna404()
    {
        // Act
        var result = _controller.GetFile("");

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [Test]
    public void GetFile_ConArchivoNoExistente_Retorna404()
    {
        // Act
        var result = _controller.GetFile("no_existe.jpg");

        // Assert
        result.Should().Match(r => r is NotFoundResult || r is NotFoundObjectResult);
    }

    [Test]
    public void GetFile_ConArchivoExistente_RetornaFileStream()
    {
        // Arrange
        var testFilePath = System.IO.Path.Combine(_testDirectory, "wwwroot", "productos", "test.jpg");
        System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(testFilePath)!);
        System.IO.File.WriteAllBytes(testFilePath, new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 });

        // Act
        var result = _controller.GetFile("productos/test.jpg");

        // Assert
        var fileResult = result.Should().BeOfType<FileStreamResult>().Subject;
        fileResult.ContentType.Should().Be("image/jpeg");
    }

    [Test]
    public void GetFile_ConPNG_RetornaContentTypeImagenPng()
    {
        // Arrange
        var testFilePath = System.IO.Path.Combine(_testDirectory, "wwwroot", "productos", "test.png");
        System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(testFilePath)!);
        System.IO.File.WriteAllBytes(testFilePath, new byte[] { 0x89, 0x50, 0x4E, 0x47 });

        // Act
        var result = _controller.GetFile("productos/test.png");

        // Assert
        var fileResult = result.Should().BeOfType<FileStreamResult>().Subject;
        fileResult.ContentType.Should().Be("image/png");
    }

    [Test]
    public void GetFile_ConGif_RetornaContentTypeImagenGif()
    {
        // Arrange
        var testFilePath = System.IO.Path.Combine(_testDirectory, "wwwroot", "productos", "test.gif");
        System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(testFilePath)!);
        System.IO.File.WriteAllBytes(testFilePath, new byte[] { 0x47, 0x49, 0x46, 0x38 });

        // Act
        var result = _controller.GetFile("productos/test.gif");

        // Assert
        var fileResult = result.Should().BeOfType<FileStreamResult>().Subject;
        fileResult.ContentType.Should().Be("image/gif");
    }

    [Test]
    public void GetFile_ConSubdirectorio_RetornaArchivo()
    {
        // Arrange
        var testFilePath = System.IO.Path.Combine(_testDirectory, "wwwroot", "categorias", "electronics", "test.jpg");
        System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(testFilePath)!);
        System.IO.File.WriteAllBytes(testFilePath, new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 });

        // Act
        var result = _controller.GetFile("categorias/electronics/test.jpg");

        // Assert
        result.Should().BeOfType<FileStreamResult>();
    }
}

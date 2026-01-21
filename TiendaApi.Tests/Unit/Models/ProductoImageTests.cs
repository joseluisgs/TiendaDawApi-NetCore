using FluentAssertions;
using TiendaApi.Api.Models;

namespace TiendaApi.Tests.Unit.Models;

public class ProductoImageTests
{
    [Test]
    public void IsLocalImage_ConImagenLocal_RetornaTrue()
    {
        // Arrange
        var producto = new Producto
        {
            Imagen = "/storage/uploads/productos/test.jpg"
        };

        // Act
        var result = producto.IsLocalImage();

        // Assert
        result.Should().BeTrue();
    }

    [Test]
    public void IsLocalImage_ConImagenLocalMayusculas_RetornaTrue()
    {
        // Arrange
        var producto = new Producto
        {
            Imagen = "/STORAGE/UPLOADS/PRODUCTOS/test.jpg"
        };

        // Act
        var result = producto.IsLocalImage();

        // Assert
        result.Should().BeTrue();
    }

    [Test]
    public void IsLocalImage_ConUrlHttp_RetornaFalse()
    {
        // Arrange
        var producto = new Producto
        {
            Imagen = "http://ejemplo.com/imagen.jpg"
        };

        // Act
        var result = producto.IsLocalImage();

        // Assert
        result.Should().BeFalse();
    }

    [Test]
    public void IsLocalImage_ConUrlHttps_RetornaFalse()
    {
        // Arrange
        var producto = new Producto
        {
            Imagen = "https://ejemplo.com/imagen.jpg"
        };

        // Act
        var result = producto.IsLocalImage();

        // Assert
        result.Should().BeFalse();
    }

    [Test]
    public void IsLocalImage_ConImagenNull_RetornaFalse()
    {
        // Arrange
        var producto = new Producto
        {
            Imagen = null
        };

        // Act
        var result = producto.IsLocalImage();

        // Assert
        result.Should().BeFalse();
    }

    [Test]
    public void IsLocalImage_ConImagenVacia_RetornaFalse()
    {
        // Arrange
        var producto = new Producto
        {
            Imagen = ""
        };

        // Act
        var result = producto.IsLocalImage();

        // Assert
        result.Should().BeFalse();
    }

    [Test]
    public void HasDefaultImage_ConImagenNull_RetornaTrue()
    {
        // Arrange
        var producto = new Producto
        {
            Imagen = null
        };

        // Act
        var result = producto.HasDefaultImage();

        // Assert
        result.Should().BeTrue();
    }

    [Test]
    public void HasDefaultImage_ConImagenVacia_RetornaTrue()
    {
        // Arrange
        var producto = new Producto
        {
            Imagen = ""
        };

        // Act
        var result = producto.HasDefaultImage();

        // Assert
        result.Should().BeTrue();
    }

    [Test]
    public void HasDefaultImage_ConImagenDefault_RetornaTrue()
    {
        // Arrange
        var producto = new Producto
        {
            Imagen = Producto.IMAGE_DEFAULT
        };

        // Act
        var result = producto.HasDefaultImage();

        // Assert
        result.Should().BeTrue();
    }

    [Test]
    public void HasDefaultImage_ConImagenLocal_RetornaFalse()
    {
        // Arrange
        var producto = new Producto
        {
            Imagen = "/storage/uploads/productos/test.jpg"
        };

        // Act
        var result = producto.HasDefaultImage();

        // Assert
        result.Should().BeFalse();
    }

    [Test]
    public void GetImagenUrl_ConNull_RetornaDefault()
    {
        // Arrange
        var producto = new Producto
        {
            Imagen = null
        };

        // Act
        var result = producto.GetImagenUrl();

        // Assert
        result.Should().Be(Producto.IMAGE_DEFAULT);
    }

    [Test]
    public void GetImagenUrl_ConUrlHttp_RetornaUrlOriginal()
    {
        // Arrange
        var producto = new Producto
        {
            Imagen = "http://ejemplo.com/imagen.jpg"
        };

        // Act
        var result = producto.GetImagenUrl();

        // Assert
        result.Should().Be("http://ejemplo.com/imagen.jpg");
    }

    [Test]
    public void GetImagenUrl_ConUrlHttps_RetornaUrlOriginal()
    {
        // Arrange
        var producto = new Producto
        {
            Imagen = "https://ejemplo.com/imagen.jpg"
        };

        // Act
        var result = producto.GetImagenUrl();

        // Assert
        result.Should().Be("https://ejemplo.com/imagen.jpg");
    }

    [Test]
    public void GetImagenUrl_ConRutaLocalSinPrefijo_RetornaConPrefijoStorage()
    {
        // Arrange
        var producto = new Producto
        {
            Imagen = "/uploads/productos/test.jpg"
        };

        // Act
        var result = producto.GetImagenUrl();

        // Assert
        result.Should().Be("/storage/uploads/productos/test.jpg");
    }

    [Test]
    public void GetImagenUrl_ConRutaLocalConPrefijo_RetornaIgual()
    {
        // Arrange
        var producto = new Producto
        {
            Imagen = "/storage/uploads/productos/test.jpg"
        };

        // Act
        var result = producto.GetImagenUrl();

        // Assert
        result.Should().Be("/storage/uploads/productos/test.jpg");
    }

    [Test]
    public void GetImagenUrl_ConSoloNombre_archivo_RetornaConPrefijoCompleto()
    {
        // Arrange
        var producto = new Producto
        {
            Imagen = "test.jpg"
        };

        // Act
        var result = producto.GetImagenUrl();

        // Assert
        result.Should().Be(Producto.IMAGE_LOCAL_PREFIX + "test.jpg");
    }

    [Test]
    public void ImageDefault_ConstanteCorrecta()
    {
        // Assert
        Producto.IMAGE_DEFAULT.Should().Be("https://via.placeholder.com/150");
    }

    [Test]
    public void ImageLocalPrefix_ConstanteCorrecta()
    {
        // Assert
        Producto.IMAGE_LOCAL_PREFIX.Should().Be("/storage/uploads/productos/");
    }
}

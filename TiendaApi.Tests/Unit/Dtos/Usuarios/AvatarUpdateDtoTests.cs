using FluentAssertions;
using TiendaApi.Api.Dtos.Usuarios;

namespace TiendaApi.Tests.Unit.Dtos.Usuarios;

/// <summary>
/// Tests unitarios para AvatarUpdateDto.
/// Verifica el funcionamiento del DTO de actualización de avatar de usuario.
/// </summary>
public class AvatarUpdateDtoTests
{
    #region Tests de Constructor

    /// <summary>
    /// Verifica que el constructor asigna la URL correctamente.
    /// </summary>
    [Test]
    public void Constructor_ConUrlValido_AsignaCorrectamente()
    {
        // Arrange & Act
        var dto = new AvatarUpdateDto { AvatarUrl = "https://example.com/avatar.jpg" };

        // Assert
        dto.AvatarUrl.Should().Be("https://example.com/avatar.jpg");
    }

    /// <summary>
    /// Verifica que por defecto devuelve una cadena vacía.
    /// </summary>
    [Test]
    public void Constructor_PorDefecto_RetornaCadenaVacia()
    {
        // Arrange & Act
        var dto = new AvatarUpdateDto();

        // Assert
        dto.AvatarUrl.Should().BeEmpty();
    }

    /// <summary>
    /// Verifica que el constructor asigna una cadena vacía correctamente.
    /// </summary>
    [Test]
    public void Constructor_ConUrlVacia_AsignaCadenaVacia()
    {
        // Arrange & Act
        var dto = new AvatarUpdateDto { AvatarUrl = "" };

        // Assert
        dto.AvatarUrl.Should().BeEmpty();
    }

    #endregion

    #region Tests de Propiedades

    /// <summary>
    /// Verifica que soporta URLs con protocolo https.
    /// </summary>
    [Test]
    public void AvatarUrl_PuedeContenerUrlsHttps()
    {
        // Arrange & Act
        var dto = new AvatarUpdateDto { AvatarUrl = "https://cdn.example.com/avatars/usuario123.png" };

        // Assert
        dto.AvatarUrl.Should().StartWith("https://");
        dto.AvatarUrl.Should().EndWith(".png");
    }

    /// <summary>
    /// Verifica que soporta URLs con protocolo http.
    /// </summary>
    [Test]
    public void AvatarUrl_PuedeContenerUrlsHttp()
    {
        // Arrange & Act
        var dto = new AvatarUpdateDto { AvatarUrl = "http://example.com/avatar.jpg" };

        // Assert
        dto.AvatarUrl.Should().StartWith("http://");
    }

    /// <summary>
    /// Verifica que rutas locales son aceptadas (sin esquema http/https).
    /// </summary>
    [Test]
    public void AvatarUrl_PuedeContenerRutasLocales()
    {
        // Arrange & Act
        var dto = new AvatarUpdateDto { AvatarUrl = "/storage/usuarios/avatar.png" };

        // Assert - Las rutas locales son aceptadas aunque no sean URLs válidas
        dto.AvatarUrl.Should().Be("/storage/usuarios/avatar.png");
    }

    /// <summary>
    /// Verifica que soporta URLs largas.
    /// </summary>
    [Test]
    public void AvatarUrl_SoportaUrlsLargas()
    {
        // Arrange
        var urlLarga = "https://example.com/ruta/muy/larga/hacia/el/avatar/imagen/nombre/con/muchos/segmentos.png";

        // Act
        var dto = new AvatarUpdateDto { AvatarUrl = urlLarga };

        // Assert
        dto.AvatarUrl.Should().Be(urlLarga);
    }

    #endregion

    #region Tests de Inmutabilidad del Record

    /// <summary>
    /// Verifica que se puede crear una nueva instancia con cambios usando 'with'.
    /// </summary>
    [Test]
    public void With_PuedeCrearNuevaInstanciaConCambios()
    {
        // Arrange
        var original = new AvatarUpdateDto { AvatarUrl = "original.jpg" };

        // Act
        var modificado = original with { AvatarUrl = "nuevo.jpg" };

        // Assert
        modificado.AvatarUrl.Should().Be("nuevo.jpg");
        original.AvatarUrl.Should().Be("original.jpg");
    }

    #endregion

    #region Tests de Equality

    /// <summary>
    /// Verifica que dos DTOs con los mismos valores son iguales.
    /// </summary>
    [Test]
    public void Equals_ConMismosValores_RetornaTrue()
    {
        // Arrange
        var dto1 = new AvatarUpdateDto { AvatarUrl = "https://example.com/avatar.jpg" };
        var dto2 = new AvatarUpdateDto { AvatarUrl = "https://example.com/avatar.jpg" };

        // Assert
        dto1.Should().Be(dto2);
    }

    /// <summary>
    /// Verifica que dos DTOs con valores distintos no son iguales.
    /// </summary>
    [Test]
    public void Equals_ConValoresDistintos_RetornaFalse()
    {
        // Arrange
        var dto1 = new AvatarUpdateDto { AvatarUrl = "https://example.com/avatar1.jpg" };
        var dto2 = new AvatarUpdateDto { AvatarUrl = "https://example.com/avatar2.jpg" };

        // Assert
        dto1.Should().NotBe(dto2);
    }

    /// <summary>
    /// Verifica que dos DTOs vacíos son iguales.
    /// </summary>
    [Test]
    public void Equals_ConValoresVacios_RetornaTrue()
    {
        // Arrange
        var dto1 = new AvatarUpdateDto();
        var dto2 = new AvatarUpdateDto();

        // Assert
        dto1.Should().Be(dto2);
    }

    #endregion
}

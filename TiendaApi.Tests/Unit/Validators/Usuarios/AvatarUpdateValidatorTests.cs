using FluentAssertions;
using TiendaApi.Api.Dtos.Usuarios;
using TiendaApi.Api.Validators.Usuarios;

namespace TiendaApi.Tests.Unit.Validators.Usuarios;

/// <summary>
/// Tests unitarios para AvatarUpdateValidator.
/// Verifica la validación de URLs de avatar.
/// </summary>
public class AvatarUpdateValidatorTests
{
    private readonly AvatarUpdateValidator _validator = new();

    #region URL Válida Tests

    [Test]
    public void Validate_ConUrlHttpsValida_NoDeberiaTenerErrores()
    {
        var dto = new AvatarUpdateDto { AvatarUrl = "https://example.com/avatar.jpg" };

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeTrue();
    }

    [Test]
    public void Validate_ConUrlHttpValida_NoDeberiaTenerErrores()
    {
        var dto = new AvatarUpdateDto { AvatarUrl = "http://example.com/avatar.jpg" };

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeTrue();
    }

    [Test]
    public void Validate_ConUrlConQueryString_NoDeberiaTenerErrores()
    {
        var dto = new AvatarUpdateDto { AvatarUrl = "https://cdn.example.com/avatar.png?size=100&format=webp" };

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeTrue();
    }

    #endregion

    #region URL Inválida Tests

    [Test]
    public void Validate_ConUrlVacia_DeberiaTenerError()
    {
        var dto = new AvatarUpdateDto { AvatarUrl = "" };

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(AvatarUpdateDto.AvatarUrl));
    }

    [Test]
    public void Validate_ConUrlSinProtocolo_DeberiaTenerError()
    {
        var dto = new AvatarUpdateDto { AvatarUrl = "www.example.com/avatar.jpg" };

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeFalse();
    }

    [Test]
    public void Validate_ConUrlFtp_DeberiaTenerError()
    {
        var dto = new AvatarUpdateDto { AvatarUrl = "ftp://ftp.example.com/avatar.jpg" };

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeFalse();
    }

    [Test]
    public void Validate_ConUrlFile_DeberiaTenerError()
    {
        var dto = new AvatarUpdateDto { AvatarUrl = "file:///C:/avatar.jpg" };

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeFalse();
    }

    [Test]
    public void Validate_ConUrlInvalida_DeberiaTenerError()
    {
        var dto = new AvatarUpdateDto { AvatarUrl = "not-a-valid-url" };

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeFalse();
    }

    #endregion

    #region Longitud Tests

    [Test]
    public void Validate_ConUrlExcede500Caracteres_DeberiaTenerError()
    {
        var urlLarga = new string('a', 501);
        var dto = new AvatarUpdateDto { AvatarUrl = $"https://example.com/{urlLarga}.jpg" };

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeFalse();
    }

    [Test]
    public void Validate_ConUrlExactamente500Caracteres_NoDeberiaTenerErrores()
    {
        var urlLarga = new string('a', 470);
        var dto = new AvatarUpdateDto { AvatarUrl = $"https://example.com/{urlLarga}.jpg" };

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeTrue();
    }

    #endregion
}

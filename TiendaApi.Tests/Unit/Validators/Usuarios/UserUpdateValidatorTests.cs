using FluentAssertions;
using FluentValidation;
using TiendaApi.Api.Dtos.Usuarios;
using TiendaApi.Api.Validators.Usuarios;

namespace TiendaApi.Tests.Unit.Validators.Usuarios;

/// <summary>
/// Tests unitarios para UserUpdateValidator.
/// Verifica la validación de los campos de actualización de usuario.
/// </summary>
public class UserUpdateValidatorTests
{
    private readonly UserUpdateValidator _validator;

    public UserUpdateValidatorTests()
    {
        _validator = new UserUpdateValidator();
    }

    #region Validación de Email

    /// <summary>
    /// Verifica que un email válido no produce errores de validación.
    /// </summary>
    [Test]
    public void Validate_ConEmailValido_NoTieneErrores()
    {
        // Arrange
        var dto = new UserUpdateDto { Email = "test@example.com" };

        // Act
        var result = _validator.Validate(dto);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    /// <summary>
    /// Verifica que un email vacío es válido (el campo es opcional).
    /// </summary>
    [Test]
    public void Validate_ConEmailVacio_NoTieneErrores()
    {
        // Arrange
        var dto = new UserUpdateDto { Email = "" };

        // Act
        var result = _validator.Validate(dto);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    /// <summary>
    /// Verifica que un email null es válido (el campo es opcional).
    /// </summary>
    [Test]
    public void Validate_ConEmailNull_NoTieneErrores()
    {
        // Arrange
        var dto = new UserUpdateDto { Email = null };

        // Act
        var result = _validator.Validate(dto);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    /// <summary>
    /// Verifica que un email inválido produce un error de validación.
    /// </summary>
    [Test]
    public void Validate_ConEmailInvalido_TieneError()
    {
        // Arrange
        var dto = new UserUpdateDto { Email = "invalid-email" };

        // Act
        var result = _validator.Validate(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("correo electrónico"));
    }

    /// <summary>
    /// Verifica que un email sin arroba produce un error de validación.
    /// </summary>
    [Test]
    public void Validate_ConEmailSinArroba_TieneError()
    {
        // Arrange
        var dto = new UserUpdateDto { Email = "testexample.com" };

        // Act
        var result = _validator.Validate(dto);

        // Assert
        result.IsValid.Should().BeFalse();
    }

    /// <summary>
    /// Verifica que un email que excede 100 caracteres produce un error de validación.
    /// </summary>
    [Test]
    public void Validate_ConEmailMuyLargo_TieneError()
    {
        // Arrange
        var email = new string('a', 100) + "@example.com";
        var dto = new UserUpdateDto { Email = email };

        // Act
        var result = _validator.Validate(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("100 caracteres"));
    }

    #endregion

    #region Validación de Password

    /// <summary>
    /// Verifica que un password válido no produce errores de validación.
    /// </summary>
    [Test]
    public void Validate_ConPasswordValido_NoTieneErrores()
    {
        // Arrange
        var dto = new UserUpdateDto { Password = "Password123" };

        // Act
        var result = _validator.Validate(dto);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    /// <summary>
    /// Verifica que un password vacío es válido (el campo es opcional).
    /// </summary>
    [Test]
    public void Validate_ConPasswordVacio_NoTieneErrores()
    {
        // Arrange
        var dto = new UserUpdateDto { Password = "" };

        // Act
        var result = _validator.Validate(dto);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    /// <summary>
    /// Verifica que un password null es válido (el campo es opcional).
    /// </summary>
    [Test]
    public void Validate_ConPasswordNull_NoTieneErrores()
    {
        // Arrange
        var dto = new UserUpdateDto { Password = null };

        // Act
        var result = _validator.Validate(dto);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    /// <summary>
    /// Verifica que un password con menos de 6 caracteres produce un error de validación.
    /// </summary>
    [Test]
    public void Validate_ConPasswordMuyCorto_TieneError()
    {
        // Arrange
        var dto = new UserUpdateDto { Password = "12345" };

        // Act
        var result = _validator.Validate(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("6 caracteres"));
    }

    /// <summary>
    /// Verifica que un password de exactamente 6 caracteres es válido.
    /// </summary>
    [Test]
    public void Validate_ConPasswordDe6Caracteres_EsValido()
    {
        // Arrange
        var dto = new UserUpdateDto { Password = "123456" };

        // Act
        var result = _validator.Validate(dto);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    /// <summary>
    /// Verifica que un password que excede 100 caracteres produce un error de validación.
    /// </summary>
    [Test]
    public void Validate_ConPasswordMuyLargo_TieneError()
    {
        // Arrange
        var dto = new UserUpdateDto { Password = new string('a', 101) };

        // Act
        var result = _validator.Validate(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("100 caracteres"));
    }

    #endregion

    #region Validación Combinada

    /// <summary>
    /// Verifica que un email y password válidos no producen errores de validación.
    /// </summary>
    [Test]
    public void Validate_ConEmailYPasswordValidos_NoTieneErrores()
    {
        // Arrange
        var dto = new UserUpdateDto
        {
            Email = "test@example.com",
            Password = "Password123"
        };

        // Act
        var result = _validator.Validate(dto);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    /// <summary>
    /// Verifica que con email inválido y password válido solo se produce error de email.
    /// </summary>
    [Test]
    public void Validate_ConEmailInvalidoYPasswordValido_TieneSoloErrorDeEmail()
    {
        // Arrange
        var dto = new UserUpdateDto
        {
            Email = "invalid",
            Password = "Password123"
        };

        // Act
        var result = _validator.Validate(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("correo electrónico"));
        result.Errors.Should().NotContain(e => e.ErrorMessage.Contains("contraseña"));
    }

    /// <summary>
    /// Verifica que con email válido y password corto solo se produce error de password.
    /// </summary>
    [Test]
    public void Validate_ConEmailValidoYPasswordCorto_TieneSoloErrorDePassword()
    {
        // Arrange
        var dto = new UserUpdateDto
        {
            Email = "test@example.com",
            Password = "123"
        };

        // Act
        var result = _validator.Validate(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("6 caracteres"));
        result.Errors.Should().NotContain(e => e.ErrorMessage.Contains("correo"));
    }

    /// <summary>
    /// Verifica que con ambos campos vacíos no se producen errores de validación.
    /// </summary>
    [Test]
    public void Validate_ConAmbosCamposVacios_NoTieneErrores()
    {
        // Arrange
        var dto = new UserUpdateDto();

        // Act
        var result = _validator.Validate(dto);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    #endregion
}

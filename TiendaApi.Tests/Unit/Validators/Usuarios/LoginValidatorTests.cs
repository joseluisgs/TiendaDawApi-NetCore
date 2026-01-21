using FluentValidation.TestHelper;
using TiendaApi.Api.Dtos.Usuarios;
using TiendaApi.Api.Validators.Usuarios;

namespace TiendaApi.Tests.Unit.Validators.Usuarios;

/// <summary>
/// Tests unitarios para LoginValidator.
/// </summary>
public class LoginValidatorTests
{
    private readonly LoginValidator _validator = new();

    #region Username Tests

    [Test]
    public void SignInAsync_ConUsernameVacio_DeberiaTenerError()
    {
        var dto = new LoginDto { Username = "", Password = "Password123" };

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.Username)
            .WithErrorMessage("El nombre de usuario es obligatorio");
    }

    [Test]
    public void SignInAsync_ConUsernameValido_DeberiaPasar()
    {
        var dto = new LoginDto { Username = "johndoe", Password = "Password123" };

        var result = _validator.TestValidate(dto);

        result.ShouldNotHaveValidationErrorFor(x => x.Username);
    }

    #endregion

    #region Password Tests

    [Test]
    public void SignInAsync_ConPasswordVacio_DeberiaTenerError()
    {
        var dto = new LoginDto { Username = "johndoe", Password = "" };

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.Password)
            .WithErrorMessage("La contraseña es obligatoria");
    }

    [Test]
    public void SignInAsync_ConPasswordValido_DeberiaPasar()
    {
        var dto = new LoginDto { Username = "johndoe", Password = "Password123" };

        var result = _validator.TestValidate(dto);

        result.ShouldNotHaveValidationErrorFor(x => x.Password);
    }

    #endregion

    #region DTO Completo Tests

    [Test]
    public void SignInAsync_ConDtoInvalido_DeberiaTenerErrores()
    {
        var dto = new LoginDto { Username = "", Password = "" };

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.Username);
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Test]
    public void SignInAsync_ConDtoValido_NoDeberiaTenerErrores()
    {
        var dto = new LoginDto { Username = "johndoe", Password = "SecurePass123" };

        var result = _validator.TestValidate(dto);

        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion
}

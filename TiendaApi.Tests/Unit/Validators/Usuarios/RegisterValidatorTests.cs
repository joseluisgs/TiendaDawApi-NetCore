using FluentValidation.TestHelper;
using TiendaApi.Api.Dtos.Usuarios;
using TiendaApi.Api.Validators.Usuarios;

namespace TiendaApi.Tests.Unit.Validators.Usuarios;

/// <summary>
/// Tests unitarios para RegisterValidator.
/// </summary>
public class RegisterValidatorTests
{
    private readonly RegisterValidator _validator = new();

    #region Username Tests

    [Test]
    public void SignUpAsync_ConUsernameVacio_DeberiaTenerError()
    {
        var dto = new RegisterDto { Username = "", Email = "test@test.com", Password = "Password123" };

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.Username)
            .WithErrorMessage("El nombre de usuario es obligatorio");
    }

    [Test]
    public void SignUpAsync_ConUsernameMuyCorto_DeberiaTenerError()
    {
        var dto = new RegisterDto { Username = "AB", Email = "test@test.com", Password = "Password123" };

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.Username)
            .WithErrorMessage("El nombre de usuario debe tener al menos 3 caracteres");
    }

    [Test]
    public void SignUpAsync_ConUsernameMuyLargo_DeberiaTenerError()
    {
        var dto = new RegisterDto
        {
            Username = new string('A', 51),
            Email = "test@test.com",
            Password = "Password123"
        };

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.Username)
            .WithErrorMessage("El nombre de usuario no puede exceder 50 caracteres");
    }

    [Test]
    public void SignUpAsync_ConUsernameCaracteresInvalidos_DeberiaTenerError()
    {
        var dto = new RegisterDto
        {
            Username = "user@name!",
            Email = "test@test.com",
            Password = "Password123"
        };

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.Username)
            .WithErrorMessage("Solo se permiten letras, números y guiones bajos");
    }

    [Test]
    public void SignUpAsync_ConUsernameValido_DeberiaPasar()
    {
        var dto = new RegisterDto { Username = "john_doe", Email = "test@test.com", Password = "Password123" };

        var result = _validator.TestValidate(dto);

        result.ShouldNotHaveValidationErrorFor(x => x.Username);
    }

    [Test]
    public void SignUpAsync_ConUsernameConSoloLetras_DeberiaPasar()
    {
        var dto = new RegisterDto { Username = "johndoe", Email = "test@test.com", Password = "Password123" };

        var result = _validator.TestValidate(dto);

        result.ShouldNotHaveValidationErrorFor(x => x.Username);
    }

    [Test]
    public void SignUpAsync_ConUsernameConNumeros_DeberiaPasar()
    {
        var dto = new RegisterDto { Username = "user123", Email = "test@test.com", Password = "Password123" };

        var result = _validator.TestValidate(dto);

        result.ShouldNotHaveValidationErrorFor(x => x.Username);
    }

    [Test]
    public void SignUpAsync_ConUsernameConUnderscore_DeberiaPasar()
    {
        var dto = new RegisterDto { Username = "john_doe_123", Email = "test@test.com", Password = "Password123" };

        var result = _validator.TestValidate(dto);

        result.ShouldNotHaveValidationErrorFor(x => x.Username);
    }

    #endregion

    #region Email Tests

    [Test]
    public void SignUpAsync_ConEmailVacio_DeberiaTenerError()
    {
        var dto = new RegisterDto { Username = "johndoe", Email = "", Password = "Password123" };

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.Email)
            .WithErrorMessage("El correo electrónico es obligatorio");
    }

    [Test]
    public void SignUpAsync_ConEmailInvalido_DeberiaTenerError()
    {
        var dto = new RegisterDto { Username = "johndoe", Email = "invalid-email", Password = "Password123" };

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.Email)
            .WithErrorMessage("Debe ser un correo electrónico válido");
    }

    [Test]
    public void SignUpAsync_ConEmailSinArroba_DeberiaTenerError()
    {
        var dto = new RegisterDto { Username = "johndoe", Email = "johndoegmail.com", Password = "Password123" };

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Test]
    public void SignUpAsync_ConEmailMuyLargo_DeberiaTenerError()
    {
        // Email válido en formato pero con más de 100 caracteres totales
        var localPart = new string('a', 80);
        var dto = new RegisterDto
        {
            Username = "johndoe",
            Email = $"{localPart}@test.com", // 80 + @test.com = 89 caracteres (bajo el límite)
            Password = "Password123"
        };

        var result = _validator.TestValidate(dto);

        // El EmailAddress validation pasa, verifiquemos que no hay error
        result.ShouldNotHaveValidationErrorFor(x => x.Email);

        // Ahora probemos con un email que exceda el límite
        var dto2 = new RegisterDto
        {
            Username = "johndoe",
            Email = new string('a', 95) + "@test.com", // 95 + @test.com = 104 caracteres
            Password = "Password123"
        };

        var result2 = _validator.TestValidate(dto2);
        result2.ShouldHaveValidationErrorFor(x => x.Email)
            .WithErrorMessage("El correo no puede exceder 100 caracteres");
    }

    [Test]
    public void SignUpAsync_ConEmailValido_DeberiaPasar()
    {
        var dto = new RegisterDto { Username = "johndoe", Email = "john.doe@example.com", Password = "Password123" };

        var result = _validator.TestValidate(dto);

        result.ShouldNotHaveValidationErrorFor(x => x.Email);
    }

    [Test]
    public void SignUpAsync_ConEmailConSubdominio_DeberiaPasar()
    {
        var dto = new RegisterDto { Username = "johndoe", Email = "john@mail.subdomain.com", Password = "Password123" };

        var result = _validator.TestValidate(dto);

        result.ShouldNotHaveValidationErrorFor(x => x.Email);
    }

    #endregion

    #region Password Tests

    [Test]
    public void SignUpAsync_ConPasswordVacio_DeberiaTenerError()
    {
        var dto = new RegisterDto { Username = "johndoe", Email = "john@test.com", Password = "" };

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.Password)
            .WithErrorMessage("La contraseña es obligatoria");
    }

    [Test]
    public void SignUpAsync_ConPasswordMuyCorto_DeberiaTenerError()
    {
        var dto = new RegisterDto { Username = "johndoe", Email = "john@test.com", Password = "12345" };

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.Password)
            .WithErrorMessage("La contraseña debe tener al menos 6 caracteres");
    }

    [Test]
    public void SignUpAsync_ConPasswordMuyLargo_DeberiaTenerError()
    {
        var dto = new RegisterDto
        {
            Username = "johndoe",
            Email = "john@test.com",
            Password = new string('A', 101)
        };

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.Password)
            .WithErrorMessage("La contraseña no puede exceder 100 caracteres");
    }

    [Test]
    public void SignUpAsync_ConPasswordValido_DeberiaPasar()
    {
        var dto = new RegisterDto { Username = "johndoe", Email = "john@test.com", Password = "SecurePass123" };

        var result = _validator.TestValidate(dto);

        result.ShouldNotHaveValidationErrorFor(x => x.Password);
    }

    [Test]
    public void SignUpAsync_ConPasswordMinimo_DeberiaPasar()
    {
        var dto = new RegisterDto { Username = "johndoe", Email = "john@test.com", Password = "123456" };

        var result = _validator.TestValidate(dto);

        result.ShouldNotHaveValidationErrorFor(x => x.Password);
    }

    #endregion

    #region DTO Completo Tests

    [Test]
    public void SignUpAsync_ConDtoInvalido_DeberiaTenerMultiplesErrores()
    {
        var dto = new RegisterDto
        {
            Username = "",
            Email = "invalid",
            Password = ""
        };

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.Username);
        result.ShouldHaveValidationErrorFor(x => x.Email);
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Test]
    public void SignUpAsync_ConDtoValido_NoDeberiaTenerErrores()
    {
        var dto = new RegisterDto
        {
            Username = "john_doe",
            Email = "john.doe@example.com",
            Password = "SecurePass123"
        };

        var result = _validator.TestValidate(dto);

        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion
}

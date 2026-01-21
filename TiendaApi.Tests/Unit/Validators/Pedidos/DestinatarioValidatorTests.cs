using FluentValidation.TestHelper;
using TiendaApi.Api.Dtos.Common;
using TiendaApi.Api.Dtos.Pedidos;
using TiendaApi.Api.Validators.Pedidos;

namespace TiendaApi.Tests.Unit.Validators.Pedidos;

public class DestinatarioValidatorTests
{
    private readonly DestinatarioValidator _validator = new();

    #region NombreCompleto Tests

    [Test]
    public void NombreCompleto_ConLongitudExcedida_DeberiaTenerError()
    {
        var dto = new DestinatarioDto { NombreCompleto = new string('A', 201) };

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.NombreCompleto)
            .WithErrorMessage("El nombre completo no puede superar los 200 caracteres");
    }

    [Test]
    public void NombreCompleto_ConLongitudMaxima_DeberiaPasar()
    {
        var dto = new DestinatarioDto { NombreCompleto = new string('A', 200) };

        var result = _validator.TestValidate(dto);

        result.ShouldNotHaveValidationErrorFor(x => x.NombreCompleto);
    }

    [Test]
    public void NombreCompleto_Vacio_DeberiaTenerError()
    {
        var dto = new DestinatarioDto { NombreCompleto = string.Empty };

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.NombreCompleto);
    }

    [Test]
    public void NombreCompleto_ConValorValido_DeberiaPasar()
    {
        var dto = new DestinatarioDto { NombreCompleto = "Test User" };

        var result = _validator.TestValidate(dto);

        result.ShouldNotHaveValidationErrorFor(x => x.NombreCompleto);
    }

    #endregion

    #region Email Tests

    [Test]
    public void Email_ConLongitudExcedida_DeberiaTenerError()
    {
        var dto = new DestinatarioDto { Email = new string('a', 250) + "@test.com" };

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.Email)
            .WithErrorMessage("El email no puede superar los 254 caracteres");
    }

    [Test]
    public void Email_ConFormatoInvalido_DeberiaTenerError()
    {
        var dto = new DestinatarioDto { Email = "email-invalido" };

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.Email)
            .WithErrorMessage("El email del destinatario no es válido");
    }

    [Test]
    public void Email_SinArroba_DeberiaTenerError()
    {
        var dto = new DestinatarioDto { Email = "emailtest.com" };

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.Email)
            .WithErrorMessage("El email del destinatario no es válido");
    }

    [Test]
    public void Email_ConFormatoValido_DeberiaPasar()
    {
        var dto = new DestinatarioDto { Email = "test@email.com" };

        var result = _validator.TestValidate(dto);

        result.ShouldNotHaveValidationErrorFor(x => x.Email);
    }

    [Test]
    public void Email_Vacio_DeberiaTenerError()
    {
        var dto = new DestinatarioDto { Email = string.Empty };

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    #endregion

    #region Telefono Tests

    [Test]
    public void Telefono_ConLongitudExcedida_DeberiaTenerError()
    {
        var dto = new DestinatarioDto
        {
            NombreCompleto = "Test User",
            Email = "test@email.com",
            Telefono = "+12345678901234567890"  // 21 characters
        };

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.Telefono)
            .WithErrorMessage("El teléfono no puede superar los 20 caracteres");
    }

    [Test]
    public void Telefono_ConMuyPocosDigitos_DeberiaTenerError()
    {
        var dto = new DestinatarioDto
        {
            NombreCompleto = "Test User",
            Email = "test@email.com",
            Telefono = "+34612"  // Solo 5 dígitos
        };

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.Telefono)
            .WithErrorMessage("El teléfono debe tener entre 9 y 15 dígitos");
    }

    [Test]
    public void Telefono_ConMuchosDigitos_DeberiaTenerError()
    {
        var dto = new DestinatarioDto
        {
            NombreCompleto = "Test User",
            Email = "test@email.com",
            Telefono = "+3461234567890123456"
        };

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.Telefono)
            .WithErrorMessage("El teléfono debe tener entre 9 y 15 dígitos");
    }

    [Test]
    public void Telefono_ConCaracteresInvalidos_DeberiaTenerError()
    {
        var dto = new DestinatarioDto
        {
            NombreCompleto = "Test User",
            Email = "test@email.com",
            Telefono = "+34abcde678"
        };

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.Telefono)
            .WithErrorMessage("El teléfono debe tener entre 9 y 15 dígitos");
    }

    [Test]
    public void Telefono_ConPrefijoInternacionalValido_DeberiaPasar()
    {
        var dto = new DestinatarioDto { Telefono = "+34612345678" };

        var result = _validator.TestValidate(dto);

        result.ShouldNotHaveValidationErrorFor(x => x.Telefono);
    }

    [Test]
    public void Telefono_SinPrefijoValido_DeberiaPasar()
    {
        var dto = new DestinatarioDto { Telefono = "612345678" };

        var result = _validator.TestValidate(dto);

        result.ShouldNotHaveValidationErrorFor(x => x.Telefono);
    }

    [Test]
    public void Telefono_Null_DeberiaPasar()
    {
        var dto = new DestinatarioDto { Telefono = null };

        var result = _validator.TestValidate(dto);

        result.ShouldNotHaveValidationErrorFor(x => x.Telefono);
    }

    [Test]
    public void Telefono_Vacio_DeberiaPasar()
    {
        var dto = new DestinatarioDto { Telefono = string.Empty };

        var result = _validator.TestValidate(dto);

        result.ShouldNotHaveValidationErrorFor(x => x.Telefono);
    }

    #endregion

    #region Direccion Tests

    [Test]
    public void Direccion_SinCamposObligatorios_DeberiaTenerError()
    {
        var dto = new DestinatarioDto
        {
            Direccion = new DireccionDto()
        };

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.Direccion);
    }

    [Test]
    public void Direccion_ConCodigoPostalInvalido_DeberiaTenerError()
    {
        var dto = new DestinatarioDto
        {
            Direccion = new DireccionDto { CodigoPostal = "1234" }
        };

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.Direccion)
            .WithErrorMessage("La dirección del destinatario no es válida");
    }

    [Test]
    public void Direccion_ConDatosValidos_DeberiaPasar()
    {
        var dto = new DestinatarioDto
        {
            NombreCompleto = "Test User",
            Email = "test@email.com",
            Direccion = new DireccionDto
            {
                Calle = "Gran Vía",
                Numero = "42",
                Ciudad = "Madrid",
                Provincia = "Madrid",
                Pais = "España",
                CodigoPostal = "28013"
            }
        };

        var result = _validator.TestValidate(dto);

        result.ShouldNotHaveValidationErrorFor(x => x.Direccion);
    }

    #endregion

    #region Destinatario Completo Tests

    [Test]
    public void DestinatarioCompleto_ConDatosValidos_NoDeberiaTenerErrores()
    {
        var dto = new DestinatarioDto
        {
            NombreCompleto = "María García López",
            Email = "maria.garcia@email.com",
            Telefono = "+34612345678",
            Direccion = new DireccionDto
            {
                Calle = "Gran Vía",
                Numero = "42",
                Ciudad = "Madrid",
                Provincia = "Madrid",
                Pais = "España",
                CodigoPostal = "28013"
            }
        };

        var result = _validator.TestValidate(dto);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public void DestinatarioCompleto_SoloConDireccionValida_NoDeberiaTenerErrores()
    {
        var dto = new DestinatarioDto
        {
            NombreCompleto = "Test User",
            Email = "test@email.com",
            Direccion = new DireccionDto
            {
                Calle = "Calle Test",
                Ciudad = "Barcelona",
                Pais = "España"
            }
        };

        var result = _validator.TestValidate(dto);

        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion
}

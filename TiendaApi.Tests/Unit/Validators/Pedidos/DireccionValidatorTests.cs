using FluentValidation.TestHelper;
using TiendaApi.Api.Dtos.Pedidos;
using TiendaApi.Api.Validators.Pedidos;

namespace TiendaApi.Tests.Unit.Validators.Pedidos;

public class DireccionValidatorTests
{
    private readonly DireccionValidator _validator = new();

    #region Calle Tests

    [Test]
    public void Calle_ConLongitudExcedida_DeberiaTenerError()
    {
        var dto = new DireccionDto { Calle = new string('A', 201) };

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.Calle)
            .WithErrorMessage("La calle no puede superar los 200 caracteres");
    }

    [Test]
    public void Calle_ConLongitudMaxima_DeberiaPasar()
    {
        var dto = new DireccionDto { Calle = new string('A', 200) };

        var result = _validator.TestValidate(dto);

        result.ShouldNotHaveValidationErrorFor(x => x.Calle);
    }

    [Test]
    public void Calle_Vacia_DeberiaTenerError()
    {
        var dto = new DireccionDto { Calle = string.Empty };

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.Calle);
    }

    #endregion

    #region Numero Tests

    [Test]
    public void Numero_ConLongitudExcedida_DeberiaTenerError()
    {
        var dto = new DireccionDto { Numero = new string('1', 21) };

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.Numero)
            .WithErrorMessage("El número no puede superar los 20 caracteres");
    }

    [Test]
    public void Numero_ConLongitudMaxima_DeberiaPasar()
    {
        var dto = new DireccionDto { Numero = new string('1', 20) };

        var result = _validator.TestValidate(dto);

        result.ShouldNotHaveValidationErrorFor(x => x.Numero);
    }

    [Test]
    public void Numero_Null_DeberiaPasar()
    {
        var dto = new DireccionDto { Numero = null };

        var result = _validator.TestValidate(dto);

        result.ShouldNotHaveValidationErrorFor(x => x.Numero);
    }

    #endregion

    #region Ciudad Tests

    [Test]
    public void Ciudad_ConLongitudExcedida_DeberiaTenerError()
    {
        var dto = new DireccionDto { Ciudad = new string('C', 101) };

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.Ciudad)
            .WithErrorMessage("La ciudad no puede superar los 100 caracteres");
    }

    [Test]
    public void Ciudad_ConLongitudMaxima_DeberiaPasar()
    {
        var dto = new DireccionDto { Ciudad = new string('C', 100) };

        var result = _validator.TestValidate(dto);

        result.ShouldNotHaveValidationErrorFor(x => x.Ciudad);
    }

    [Test]
    public void Ciudad_Vacia_DeberiaTenerError()
    {
        var dto = new DireccionDto { Ciudad = string.Empty };

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.Ciudad);
    }

    #endregion

    #region Provincia Tests

    [Test]
    public void Provincia_ConLongitudExcedida_DeberiaTenerError()
    {
        var dto = new DireccionDto { Provincia = new string('P', 101) };

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.Provincia)
            .WithErrorMessage("La provincia no puede superar los 100 caracteres");
    }

    [Test]
    public void Provincia_ConLongitudMaxima_DeberiaPasar()
    {
        var dto = new DireccionDto { Provincia = new string('P', 100) };

        var result = _validator.TestValidate(dto);

        result.ShouldNotHaveValidationErrorFor(x => x.Provincia);
    }

    [Test]
    public void Provincia_Null_DeberiaPasar()
    {
        var dto = new DireccionDto { Provincia = null };

        var result = _validator.TestValidate(dto);

        result.ShouldNotHaveValidationErrorFor(x => x.Provincia);
    }

    #endregion

    #region Pais Tests

    [Test]
    public void Pais_ConLongitudExcedida_DeberiaTenerError()
    {
        var dto = new DireccionDto { Pais = new string('P', 101) };

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.Pais)
            .WithErrorMessage("El país no puede superar los 100 caracteres");
    }

    [Test]
    public void Pais_ConLongitudMaxima_DeberiaPasar()
    {
        var dto = new DireccionDto { Pais = new string('P', 100) };

        var result = _validator.TestValidate(dto);

        result.ShouldNotHaveValidationErrorFor(x => x.Pais);
    }

    [Test]
    public void Pais_Vacio_DeberiaTenerError()
    {
        var dto = new DireccionDto { Pais = string.Empty };

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.Pais);
    }

    #endregion

    #region CodigoPostal Tests

    [Test]
    public void CodigoPostal_ConLongitudExcedida_DeberiaTenerError()
    {
        var dto = new DireccionDto { CodigoPostal = new string('1', 21) };

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.CodigoPostal)
            .WithErrorMessage("El código postal no puede superar los 20 caracteres");
    }

    [Test]
    public void CodigoPostal_ConFormatoInvalido_DeberiaTenerError()
    {
        var dto = new DireccionDto { CodigoPostal = "1234" };

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.CodigoPostal)
            .WithErrorMessage("El código postal debe tener exactamente 5 dígitos");
    }

    [Test]
    public void CodigoPostal_ConLetras_DeberiaTenerError()
    {
        var dto = new DireccionDto { CodigoPostal = "12AB3" };

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.CodigoPostal)
            .WithErrorMessage("El código postal debe tener exactamente 5 dígitos");
    }

    [Test]
    public void CodigoPostal_ConFormatoValido_DeberiaPasar()
    {
        var dto = new DireccionDto { CodigoPostal = "28013" };

        var result = _validator.TestValidate(dto);

        result.ShouldNotHaveValidationErrorFor(x => x.CodigoPostal);
    }

    [Test]
    public void CodigoPostal_Null_DeberiaPasar()
    {
        var dto = new DireccionDto { CodigoPostal = null };

        var result = _validator.TestValidate(dto);

        result.ShouldNotHaveValidationErrorFor(x => x.CodigoPostal);
    }

    [Test]
    public void CodigoPostal_Vacio_DeberiaPasar()
    {
        var dto = new DireccionDto { CodigoPostal = string.Empty };

        var result = _validator.TestValidate(dto);

        result.ShouldNotHaveValidationErrorFor(x => x.CodigoPostal);
    }

    #endregion

    #region Direccion Completa Tests

    [Test]
    public void DireccionCompleta_ConDatosValidos_NoDeberiaTenerErrores()
    {
        var dto = new DireccionDto
        {
            Calle = "Gran Vía",
            Numero = "42",
            Ciudad = "Madrid",
            Provincia = "Madrid",
            Pais = "España",
            CodigoPostal = "28013"
        };

        var result = _validator.TestValidate(dto);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public void DireccionCompleta_Vacia_NoDeberiaTenerErrores()
    {
        var dto = new DireccionDto();

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.Calle);
        result.ShouldHaveValidationErrorFor(x => x.Ciudad);
        result.ShouldHaveValidationErrorFor(x => x.Pais);
    }

    [Test]
    public void DireccionCompleta_ConDireccionParcial_NoDeberiaTenerErrores()
    {
        var dto = new DireccionDto
        {
            Calle = "Gran Vía",
            Ciudad = "Madrid",
            Pais = "España"
        };

        var result = _validator.TestValidate(dto);

        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion
}

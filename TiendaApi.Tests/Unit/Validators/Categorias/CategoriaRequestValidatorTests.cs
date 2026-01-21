using FluentValidation.TestHelper;
using TiendaApi.Api.Dtos.Categorias;
using TiendaApi.Api.Validators.Categorias;

namespace TiendaApi.Tests.Unit.Validators.Categorias;

/// <summary>
/// Tests unitarios para CategoriaRequestValidator.
/// </summary>
public class CategoriaRequestValidatorTests
{
    private readonly CategoriaRequestValidator _validator = new();

    #region Nombre Tests

    [Test]
    public void CreateAsync_ConNombreVacio_DeberiaTenerError()
    {
        var dto = new CategoriaRequestDto { Nombre = "" };

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.Nombre)
            .WithErrorMessage("El nombre es obligatorio");
    }

    [Test]
    public void CreateAsync_ConNombreSoloEspacios_DeberiaTenerError()
    {
        var dto = new CategoriaRequestDto { Nombre = "   " };

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.Nombre)
            .WithErrorMessage("El nombre es obligatorio");
    }

    [Test]
    public void CreateAsync_ConNombreMuyCorto_DeberiaTenerError()
    {
        var dto = new CategoriaRequestDto { Nombre = "AB" };

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.Nombre)
            .WithErrorMessage("El nombre debe tener al menos 3 caracteres");
    }

    [Test]
    public void CreateAsync_ConNombreDeTresCaracteres_DeberiaPasar()
    {
        var dto = new CategoriaRequestDto { Nombre = "ABC" };

        var result = _validator.TestValidate(dto);

        result.ShouldNotHaveValidationErrorFor(x => x.Nombre);
    }

    [Test]
    public void CreateAsync_ConNombreMuyLargo_DeberiaTenerError()
    {
        var dto = new CategoriaRequestDto { Nombre = new string('A', 101) };

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.Nombre)
            .WithErrorMessage("El nombre no puede exceder 100 caracteres");
    }

    [Test]
    public void CreateAsync_ConNombreValido_DeberiaPasar()
    {
        var dto = new CategoriaRequestDto { Nombre = "Electrónica" };

        var result = _validator.TestValidate(dto);

        result.ShouldNotHaveValidationErrorFor(x => x.Nombre);
    }

    [Test]
    public void CreateAsync_ConNombreConCaracteresEspeciales_DeberiaPasar()
    {
        var dto = new CategoriaRequestDto { Nombre = "Electrónica & Hogar" };

        var result = _validator.TestValidate(dto);

        result.ShouldNotHaveValidationErrorFor(x => x.Nombre);
    }

    [Test]
    public void CreateAsync_ConNombreConNumeros_DeberiaPasar()
    {
        var dto = new CategoriaRequestDto { Nombre = "Category 123" };

        var result = _validator.TestValidate(dto);

        result.ShouldNotHaveValidationErrorFor(x => x.Nombre);
    }

    #endregion

    #region DTO Completo Tests

    [Test]
    public void CreateAsync_ConDtoInvalido_DeberiaTenerError()
    {
        var dto = new CategoriaRequestDto { Nombre = "" };

        var result = _validator.TestValidate(dto);

        result.ShouldHaveAnyValidationError();
    }

    [Test]
    public void CreateAsync_ConDtoValido_NoDeberiaTenerErrores()
    {
        var dto = new CategoriaRequestDto { Nombre = "Electrónica" };

        var result = _validator.TestValidate(dto);

        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion
}

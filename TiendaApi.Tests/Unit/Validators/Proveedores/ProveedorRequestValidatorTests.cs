using FluentValidation.TestHelper;
using TiendaApi.Api.Dtos.Proveedores;
using TiendaApi.Api.Validators.Proveedores;

namespace TiendaApi.Tests.Unit.Validators.Proveedores;

/// <summary>
///     Tests unitarios para ProveedorRequestValidator.
///     El validador separa dos compuertas distintas: el FORMATO del código (aquí, 400)
///     y la EXISTENCIA del código en base de datos (la comprueba el servicio, 409).
/// </summary>
public class ProveedorRequestValidatorTests {
    private readonly ProveedorRequestValidator _validator = new();

    #region Código: formato

    [Test]
    public void Codigo_Omitido_NoDebeTenerError() {
        var dto = new ProveedorRequestDto { Nombre = "Distribuciones DAW" };

        TestValidationResult<ProveedorRequestDto> result = _validator.TestValidate(dto);

        result.ShouldNotHaveValidationErrorFor(x => x.Codigo);
    }

    [Test]
    public void Codigo_Vacio_DebeTenerError() {
        // Un vacío no es "omitir el campo": el validador lo rechaza.
        var dto = new ProveedorRequestDto { Codigo = "", Nombre = "Distribuciones DAW" };

        TestValidationResult<ProveedorRequestDto> result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.Codigo)
            .WithErrorMessage("El código no puede estar vacío: omítalo y el servidor generará uno");
    }

    [Test]
    public void Codigo_SoloEspacios_DebeTenerError() {
        var dto = new ProveedorRequestDto { Codigo = "           ", Nombre = "Distribuciones DAW" };

        TestValidationResult<ProveedorRequestDto> result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.Codigo);
    }

    [Test]
    public void Codigo_DiezCaracteres_DebeTenerErrorDeFormato() {
        var dto = new ProveedorRequestDto { Codigo = "abcdefghij", Nombre = "Distribuciones DAW" };

        TestValidationResult<ProveedorRequestDto> result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.Codigo);
    }

    [Test]
    public void Codigo_DoceCaracteres_DebeTenerErrorDeFormato() {
        var dto = new ProveedorRequestDto { Codigo = "abcdefghijkl", Nombre = "Distribuciones DAW" };

        TestValidationResult<ProveedorRequestDto> result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.Codigo);
    }

    [Test]
    public void Codigo_ConCaracteresFueraDelAlfabeto_DebeTenerError() {
        var dto = new ProveedorRequestDto { Codigo = "abc/def?ghi", Nombre = "Distribuciones DAW" };

        TestValidationResult<ProveedorRequestDto> result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.Codigo);
    }

    [Test]
    public void Codigo_DeOnceCaracteresValidos_NoDebeTenerError() {
        var dto = new ProveedorRequestDto { Codigo = "XqZsoesa55w", Nombre = "Distribuciones DAW" };

        TestValidationResult<ProveedorRequestDto> result = _validator.TestValidate(dto);

        result.ShouldNotHaveValidationErrorFor(x => x.Codigo);
    }

    [Test]
    public void Codigo_DeOnceCaracteresConGuionesYSubrayado_NoDebeTenerError() {
        var dto = new ProveedorRequestDto { Codigo = "aB3-_zY9_Qw", Nombre = "Distribuciones DAW" };

        TestValidationResult<ProveedorRequestDto> result = _validator.TestValidate(dto);

        result.ShouldNotHaveValidationErrorFor(x => x.Codigo);
    }

    [Test]
    public void Codigo_ConTextoDeMas_NoDebeValidarPorPrefijo() {
        // El patrón debe estar anclado: si no lo estuviera, "abcdefghijk" (11) más
        // un sufijo basura pasaría por válido.
        var dto = new ProveedorRequestDto { Codigo = "abcdefghijkSOBRA", Nombre = "Distribuciones DAW" };

        TestValidationResult<ProveedorRequestDto> result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.Codigo);
    }

    #endregion

    #region Nombre

    [Test]
    public void Nombre_Vacio_DebeTenerError() {
        var dto = new ProveedorRequestDto { Nombre = "" };

        TestValidationResult<ProveedorRequestDto> result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.Nombre)
            .WithErrorMessage("El nombre es obligatorio");
    }

    [Test]
    public void Nombre_DeDosCaracteres_DebeTenerError() {
        var dto = new ProveedorRequestDto { Nombre = "DA" };

        TestValidationResult<ProveedorRequestDto> result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.Nombre);
    }

    [Test]
    public void Nombre_DeCienCaracteres_NoDebeTenerError() {
        var dto = new ProveedorRequestDto { Nombre = new string('N', 100) };

        TestValidationResult<ProveedorRequestDto> result = _validator.TestValidate(dto);

        result.ShouldNotHaveValidationErrorFor(x => x.Nombre);
    }

    [Test]
    public void Nombre_DeCientoUnCaracteres_DebeTenerError() {
        var dto = new ProveedorRequestDto { Nombre = new string('N', 101) };

        TestValidationResult<ProveedorRequestDto> result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.Nombre);
    }

    #endregion

    #region Descripción y contacto

    [Test]
    public void Descripcion_DeQuinientosCaracteres_NoDebeTenerError() {
        var dto = new ProveedorRequestDto { Nombre = "Distribuciones DAW", Descripcion = new string('D', 500) };

        TestValidationResult<ProveedorRequestDto> result = _validator.TestValidate(dto);

        result.ShouldNotHaveValidationErrorFor(x => x.Descripcion);
    }

    [Test]
    public void Descripcion_DeQuinientosUnoCaracteres_DebeTenerError() {
        var dto = new ProveedorRequestDto { Nombre = "Distribuciones DAW", Descripcion = new string('D', 501) };

        TestValidationResult<ProveedorRequestDto> result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.Descripcion);
    }

    [Test]
    public void Contacto_DeDoscientosUnoCaracteres_DebeTenerError() {
        var dto = new ProveedorRequestDto { Nombre = "Distribuciones DAW", Contacto = new string('C', 201) };

        TestValidationResult<ProveedorRequestDto> result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.Contacto);
    }

    #endregion

    #region Petición válida completa

    [Test]
    public void PeticiónValida_NoDebeTenerNingunError() {
        var dto = new ProveedorRequestDto {
            Codigo = "Distribuc01",
            Nombre = "Distribuciones DAW",
            Descripcion = "Distribuidor general",
            Contacto = "pedidos@distribucionesdaw.example"
        };

        TestValidationResult<ProveedorRequestDto> result = _validator.TestValidate(dto);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public void PeticiónSinCodigo_NoDebeTenerNingunError() {
        var dto = new ProveedorRequestDto { Nombre = "Distribuciones DAW" };

        TestValidationResult<ProveedorRequestDto> result = _validator.TestValidate(dto);

        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion
}

using FluentValidation;
using TiendaApi.Apis.Dtos.Pedidos;

namespace TiendaApi.Apis.Validators.Pedidos;

/// <summary>
/// Validador FluentValidation para DireccionDto.
/// Valida los campos de una dirección postal estructurada.
/// </summary>
/// <remarks>
/// <para>
/// Este validador verifica que los campos de dirección cumplan con las restricciones
/// de longitud y formato especificadas en el modelo.
/// </para>
/// <para>
/// <b>Campos obligatorios:</b> Calle, Ciudad, País
/// </para>
/// </remarks>
public class DireccionValidator : AbstractValidator<DireccionDto>
{
    /// <summary>
    /// Constructor que define las reglas de validación para DireccionDto.
    /// </summary>
    /// <remarks>
    /// <para><b>Reglas de validación:</b></para>
    /// <list type="bullet">
    ///   <item><description>Calle: obligatoria, máximo 200 caracteres</description></item>
    ///   <item><description>Número: opcional, máximo 20 caracteres</description></item>
    ///   <item><description>Ciudad: obligatoria, máximo 100 caracteres</description></item>
    ///   <item><description>Provincia: opcional, máximo 100 caracteres</description></item>
    ///   <item><description>País: obligatorio, máximo 100 caracteres</description></item>
    ///   <item><description>Código Postal: opcional, máximo 20 caracteres, 5 dígitos</description></item>
    /// </list>
    /// </remarks>
    public DireccionValidator()
    {
        RuleFor(d => d.Calle)
            .NotEmpty().WithMessage("La calle es obligatoria.")
            .MaximumLength(200).WithMessage("La calle no puede superar los 200 caracteres.");

        RuleFor(d => d.Numero)
            .MaximumLength(20).WithMessage("El número no puede superar los 20 caracteres.");

        RuleFor(d => d.Ciudad)
            .NotEmpty().WithMessage("La ciudad es obligatoria.")
            .MaximumLength(100).WithMessage("La ciudad no puede superar los 100 caracteres.");

        RuleFor(d => d.Provincia)
            .MaximumLength(100).WithMessage("La provincia no puede superar los 100 caracteres.");

        RuleFor(d => d.Pais)
            .NotEmpty().WithMessage("El país es obligatorio.")
            .MaximumLength(100).WithMessage("El país no puede superar los 100 caracteres.");

        RuleFor(d => d.CodigoPostal)
            .MaximumLength(20).WithMessage("El código postal no puede superar los 20 caracteres.")
            .Matches(@"^[0-9]{5}$").WithMessage("El código postal debe tener exactamente 5 dígitos.")
            .When(d => !string.IsNullOrEmpty(d.CodigoPostal));
    }
}

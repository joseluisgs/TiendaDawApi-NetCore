using FluentValidation;
using TiendaApi.Api.Dtos.Pedidos;

namespace TiendaApi.Api.Validators.Pedidos;

/// <summary>
/// Validador FluentValidation para DireccionDto.
/// Reglas: Calle(oblig, max200), Ciudad(oblig, max100), Pais(oblig, max100), CP(opc, 5 dig).
/// </summary>
public class DireccionValidator : AbstractValidator<DireccionDto>
{
    /// <summary>
    /// Define reglas de validación para DireccionDto.
    /// </summary>
    public DireccionValidator()
    {
        RuleFor(d => d.Calle)
            .NotEmpty().WithMessage("La calle es obligatoria")
            .MaximumLength(200).WithMessage("La calle no puede superar los 200 caracteres");

        RuleFor(d => d.Numero)
            .MaximumLength(20).WithMessage("El número no puede superar los 20 caracteres");

        RuleFor(d => d.Ciudad)
            .NotEmpty().WithMessage("La ciudad es obligatoria")
            .MaximumLength(100).WithMessage("La ciudad no puede superar los 100 caracteres");

        RuleFor(d => d.Provincia)
            .MaximumLength(100).WithMessage("La provincia no puede superar los 100 caracteres");

        RuleFor(d => d.Pais)
            .NotEmpty().WithMessage("El país es obligatorio")
            .MaximumLength(100).WithMessage("El país no puede superar los 100 caracteres");

        RuleFor(d => d.CodigoPostal)
            .MaximumLength(20).WithMessage("El código postal no puede superar los 20 caracteres")
            .Matches(@"^[0-9]{5}$").WithMessage("El código postal debe tener exactamente 5 dígitos")
            .When(d => !string.IsNullOrEmpty(d.CodigoPostal));
    }
}

using FluentValidation;
using TiendaApi.Api.Dtos.Pedidos;

namespace TiendaApi.Api.Validators.Pedidos;

/// <summary>
/// Validador FluentValidation para DestinatarioDto.
/// Reglas: NombreCompleto(oblig, max200), Email(valido, max254), Telefono(opc, 9-15 dig), Direccion(oblig).
/// </summary>
public class DestinatarioValidator : AbstractValidator<DestinatarioDto>
{
    private static readonly DireccionValidator DireccionValidator = new();

    /// <summary>
    /// Define reglas de validación para DestinatarioDto.
    /// </summary>
    public DestinatarioValidator()
    {
        RuleFor(d => d.NombreCompleto)
            .NotEmpty().WithMessage("El nombre completo es obligatorio")
            .MaximumLength(200).WithMessage("El nombre completo no puede superar los 200 caracteres");

        RuleFor(d => d.Email)
            .NotEmpty().WithMessage("El email es obligatorio")
            .MaximumLength(254).WithMessage("El email no puede superar los 254 caracteres")
            .EmailAddress().WithMessage("El email del destinatario no es válido");

        RuleFor(d => d.Telefono)
            .MaximumLength(20).WithMessage("El teléfono no puede superar los 20 caracteres")
            .Matches(@"^\+?[0-9]{9,15}$").WithMessage("El teléfono debe tener entre 9 y 15 dígitos")
            .When(d => !string.IsNullOrEmpty(d.Telefono));

        RuleFor(d => d.Direccion)
            .NotNull().WithMessage("La dirección es obligatoria")
            .Must(d => d == null || ValidateDireccion(d))
            .WithMessage("La dirección del destinatario no es válida");
    }

    private static bool ValidateDireccion(DireccionDto direccion)
    {
        var validator = new DireccionValidator();
        var result = validator.Validate(direccion);
        return result.IsValid;
    }
}

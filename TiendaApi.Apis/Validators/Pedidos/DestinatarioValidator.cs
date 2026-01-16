using FluentValidation;
using TiendaApi.Apis.Dtos.Pedidos;
using TiendaApi.Apis.Validators.Pedidos;

namespace TiendaApi.Apis.Validators.Pedidos;

/// <summary>
/// Validador FluentValidation para DestinatarioDto.
/// Valida los campos de información del destinatario de un pedido.
/// </summary>
/// <remarks>
/// <para>
/// Este validador verifica que los datos del destinatario sean válidos.
/// </para>
/// <para>
/// <b>Reglas de validación:</b>
/// <list type="bullet">
///   <item><description>Nombre completo: obligatorio, máximo 200 caracteres</description></item>
///   <item><description>Email: obligatorio, formato válido, máximo 254 caracteres</description></item>
///   <item><description>Teléfono: opcional, formato internacional, 9-15 dígitos</description></item>
///   <item><description>Dirección: obligatoria, validación anidada con DireccionValidator</description></item>
/// </list>
/// </para>
/// </remarks>
public class DestinatarioValidator : AbstractValidator<DestinatarioDto>
{
    private static readonly DireccionValidator DireccionValidator = new();

    /// <summary>
    /// Constructor que define las reglas de validación para DestinatarioDto.
    /// </summary>
    public DestinatarioValidator()
    {
        RuleFor(d => d.NombreCompleto)
            .NotEmpty().WithMessage("El nombre completo es obligatorio.")
            .MaximumLength(200).WithMessage("El nombre completo no puede superar los 200 caracteres.");

        RuleFor(d => d.Email)
            .NotEmpty().WithMessage("El email es obligatorio.")
            .MaximumLength(254).WithMessage("El email no puede superar los 254 caracteres.")
            .EmailAddress().WithMessage("El email del destinatario no es válido.");

        RuleFor(d => d.Telefono)
            .MaximumLength(20).WithMessage("El teléfono no puede superar los 20 caracteres.")
            .Matches(@"^\+?[0-9]{9,15}$").WithMessage("El teléfono debe tener entre 9 y 15 dígitos.")
            .When(d => !string.IsNullOrEmpty(d.Telefono));

        RuleFor(d => d.Direccion)
            .NotNull().WithMessage("La dirección es obligatoria.")
            .Must(direccion => direccion == null || ValidateDireccion(direccion))
            .WithMessage("La dirección del destinatario no es válida.");
    }

    private static bool ValidateDireccion(DireccionDto direccion)
    {
        var validator = new DireccionValidator();
        var result = validator.Validate(direccion);
        return result.IsValid;
    }
}

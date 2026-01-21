using FluentValidation;
using TiendaApi.Apis.Dtos.Usuarios;

namespace TiendaApi.Apis.Validators.Usuarios;

/// <summary>
/// Validador FluentValidation para RegisterDto.
/// Reglas: Username(3-50), Email válido, Password(6-100).
/// </summary>
public class RegisterValidator : AbstractValidator<RegisterDto>
{
    /// <summary>
    /// Define reglas de validación para RegisterDto.
    /// </summary>
    public RegisterValidator()
    {
        RuleFor(r => r.Username)
            .NotEmpty().WithMessage("El nombre de usuario es obligatorio")
            .Length(3, 50).WithMessage("El nombre de usuario debe tener entre 3 y 50 caracteres")
            .Matches(@"^[a-zA-Z0-9_]+$").WithMessage("Solo se permiten letras, números y guiones bajos");

        RuleFor(r => r.Email)
            .NotEmpty().WithMessage("El email es obligatorio")
            .EmailAddress().WithMessage("Debe ser un correo electrónico válido")
            .MaximumLength(100).WithMessage("El email no puede exceder 100 caracteres");

        RuleFor(r => r.Password)
            .NotEmpty().WithMessage("La contraseña es obligatoria")
            .MinimumLength(6).WithMessage("La contraseña debe tener al menos 6 caracteres")
            .MaximumLength(100).WithMessage("La contraseña no puede exceder 100 caracteres");
    }
}

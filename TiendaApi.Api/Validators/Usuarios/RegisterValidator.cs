using FluentValidation;
using TiendaApi.Api.Dtos.Usuarios;

namespace TiendaApi.Api.Validators.Usuarios;

/// <summary>
/// Validador FluentValidation para RegisterDto.
/// Reglas: Username(3-50), Email válido, Password(8-100 con mayúscula, minúscula y dígito).
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
            .MinimumLength(3).WithMessage("El nombre de usuario debe tener al menos 3 caracteres")
            .MaximumLength(50).WithMessage("El nombre de usuario no puede exceder 50 caracteres")
            .Matches(@"^[a-zA-Z0-9_]+$").WithMessage("Solo se permiten letras, números y guiones bajos");

        RuleFor(r => r.Email)
            .NotEmpty().WithMessage("El correo electrónico es obligatorio")
            .EmailAddress().WithMessage("Debe ser un correo electrónico válido")
            .MaximumLength(100).WithMessage("El correo no puede exceder 100 caracteres");

        RuleFor(r => r.Password)
            .NotEmpty().WithMessage("La contraseña es obligatoria")
            // 🎓 Misma política que Identity (AuthenticationConfig): ≥8, mayúscula,
            // minúscula y dígito. Si el validador permitiera menos, Custom aceptaría
            // contraseñas que Identity rechaza → el modo cambiaría el comportamiento.
            .MinimumLength(8).WithMessage("La contraseña debe tener al menos 8 caracteres")
            .MaximumLength(100).WithMessage("La contraseña no puede exceder 100 caracteres")
            .Matches("[A-Z]").WithMessage("La contraseña debe contener al menos una mayúscula")
            .Matches("[a-z]").WithMessage("La contraseña debe contener al menos una minúscula")
            .Matches("[0-9]").WithMessage("La contraseña debe contener al menos un dígito");
    }
}

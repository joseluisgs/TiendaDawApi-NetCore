using FluentValidation;
using TiendaApi.Api.Dtos.Usuarios;

namespace TiendaApi.Api.Validators.Usuarios;

/// <summary>
/// Validador FluentValidation para UserUpdateDto.
/// Reglas opcionales: Email válido, Password(8-100 con mayúscula, minúscula y dígito).
/// </summary>
public class UserUpdateValidator : AbstractValidator<UserUpdateDto>
{
    /// <summary>
    /// Define reglas de validación para UserUpdateDto.
    /// </summary>
    public UserUpdateValidator()
    {
        RuleFor(u => u.Email)
            .EmailAddress().WithMessage("Debe ser un correo electrónico válido")
            .MaximumLength(100).WithMessage("El correo no puede exceder 100 caracteres")
            .When(u => !string.IsNullOrEmpty(u.Email));

        RuleFor(u => u.Password)
            // 🎓 Misma política que Identity (AuthenticationConfig): ≥8, mayúscula,
            // minúscula y dígito — un cambio de contraseña no puede ser más laxo
            // que el registro, o el modo determinaría qué contraseñas viven.
            .MinimumLength(8).WithMessage("La contraseña debe tener al menos 8 caracteres")
            .MaximumLength(100).WithMessage("La contraseña no puede exceder 100 caracteres")
            .Matches("[A-Z]").WithMessage("La contraseña debe contener al menos una mayúscula")
            .Matches("[a-z]").WithMessage("La contraseña debe contener al menos una minúscula")
            .Matches("[0-9]").WithMessage("La contraseña debe contener al menos un dígito")
            .When(u => !string.IsNullOrEmpty(u.Password));
    }
}

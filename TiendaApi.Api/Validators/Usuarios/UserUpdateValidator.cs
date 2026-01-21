using FluentValidation;
using TiendaApi.Api.Dtos.Usuarios;

namespace TiendaApi.Api.Validators.Usuarios;

/// <summary>
/// Validador FluentValidation para UserUpdateDto.
/// Reglas opcionales: Email válido, Password(6-100).
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
            .MinimumLength(6).WithMessage("La contraseña debe tener al menos 6 caracteres")
            .MaximumLength(100).WithMessage("La contraseña no puede exceder 100 caracteres")
            .When(u => !string.IsNullOrEmpty(u.Password));
    }
}

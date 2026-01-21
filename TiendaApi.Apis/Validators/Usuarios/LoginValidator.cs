using FluentValidation;
using TiendaApi.Apis.Dtos.Usuarios;

namespace TiendaApi.Apis.Validators.Usuarios;

/// <summary>
/// Validador FluentValidation para LoginDto.
/// Reglas: Username y Password obligatorios.
/// </summary>
public class LoginValidator : AbstractValidator<LoginDto>
{
    /// <summary>
    /// Define reglas de validación para LoginDto.
    /// </summary>
    public LoginValidator()
    {
        RuleFor(l => l.Username)
            .NotEmpty().WithMessage("El nombre de usuario es obligatorio");

        RuleFor(l => l.Password)
            .NotEmpty().WithMessage("La contraseña es obligatoria");
    }
}

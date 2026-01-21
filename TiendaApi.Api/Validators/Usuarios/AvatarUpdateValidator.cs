using FluentValidation;
using TiendaApi.Api.Dtos.Usuarios;

namespace TiendaApi.Api.Validators.Usuarios;

/// <summary>
/// Validador FluentValidation para AvatarUpdateDto.
/// Reglas: URL válida con máximo 500 caracteres.
/// </summary>
public class AvatarUpdateValidator : AbstractValidator<AvatarUpdateDto>
{
    /// <summary>
    /// Define reglas de validación para AvatarUpdateDto.
    /// </summary>
    public AvatarUpdateValidator()
    {
        RuleFor(a => a.AvatarUrl)
            .NotEmpty().WithMessage("La URL del avatar es obligatoria")
            .MaximumLength(500).WithMessage("La URL del avatar no puede exceder 500 caracteres")
            .Must(url => string.IsNullOrEmpty(url) ||
                (Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
                 (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)))
            .WithMessage("Debe ser una URL válida (http:// o https://)");
    }
}

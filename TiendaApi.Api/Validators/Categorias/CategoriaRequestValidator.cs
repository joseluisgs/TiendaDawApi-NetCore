using FluentValidation;
using TiendaApi.Api.Dtos.Categorias;

namespace TiendaApi.Api.Validators.Categorias;

/// <summary>
/// Validador FluentValidation para CategoriaRequestDto.
/// Reglas: Nombre(3-100 caracteres, obligatorio).
/// </summary>
public class CategoriaRequestValidator : AbstractValidator<CategoriaRequestDto>
{
    /// <summary>
    /// Define reglas de validación para CategoriaRequestDto.
    /// </summary>
    public CategoriaRequestValidator()
    {
        RuleFor(c => c.Nombre)
            .NotEmpty().WithMessage("El nombre es obligatorio")
            .MinimumLength(3).WithMessage("El nombre debe tener al menos 3 caracteres")
            .MaximumLength(100).WithMessage("El nombre no puede exceder 100 caracteres");
    }
}

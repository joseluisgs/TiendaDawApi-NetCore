using FluentValidation;
using TiendaApi.Apis.Dtos.Categorias;

namespace TiendaApi.Apis.Validators.Categorias;

/// <summary>
/// Validador FluentValidation para CategoriaRequestDto.
/// </summary>
public class CategoriaRequestValidator : AbstractValidator<CategoriaRequestDto>
{
    public CategoriaRequestValidator()
    {
        RuleFor(c => c.Nombre)
            .NotEmpty().WithMessage("El nombre es obligatorio")
            .MinimumLength(3).WithMessage("El nombre debe tener al menos 3 caracteres")
            .MaximumLength(100).WithMessage("El nombre no puede exceder 100 caracteres");
    }
}

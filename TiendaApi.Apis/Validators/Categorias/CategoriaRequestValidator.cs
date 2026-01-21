using FluentValidation;
using TiendaApi.Apis.Dtos.Categorias;

namespace TiendaApi.Apis.Validators.Categorias;

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
            .Length(3, 100).WithMessage("El nombre debe tener entre 3 y 100 caracteres");
    }
}

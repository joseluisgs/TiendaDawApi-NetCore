using FluentValidation;
using TiendaApi.Api.Dtos.Productos;

namespace TiendaApi.Api.Validators.Productos;

/// <summary>
/// Validador FluentValidation para ProductoRequestDto.
/// Reglas: Nombre(3-200), Descripcion(max 1000), Precio(>0), Stock(>=0), CategoriaId(>0).
/// </summary>
public class ProductoRequestValidator : AbstractValidator<ProductoRequestDto>
{
    /// <summary>
    /// Define reglas de validación para ProductoRequestDto.
    /// </summary>
    public ProductoRequestValidator()
    {
        RuleFor(p => p.Nombre)
            .NotEmpty().WithMessage("El nombre es obligatorio")
            .MinimumLength(3).WithMessage("El nombre debe tener al menos 3 caracteres")
            .MaximumLength(200).WithMessage("El nombre no puede exceder 200 caracteres");

        RuleFor(p => p.Descripcion)
            .MaximumLength(1000).WithMessage("La descripción no puede exceder 1000 caracteres");

        RuleFor(p => p.Precio)
            .GreaterThan(0).WithMessage("El precio debe ser mayor a 0");

        RuleFor(p => p.Stock)
            .GreaterThanOrEqualTo(0).WithMessage("El stock no puede ser negativo");

        RuleFor(p => p.Imagen)
            .MaximumLength(500).WithMessage("La URL de la imagen no puede exceder 500 caracteres")
            .Must(url => string.IsNullOrEmpty(url) ||
                (Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
                 (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)))
            .WithMessage("Debe ser una URL válida (http:// o https://)")
            .When(p => !string.IsNullOrEmpty(p.Imagen));

        RuleFor(p => p.CategoriaId)
            .GreaterThan(0).WithMessage("Debe seleccionar una categoría válida");
    }
}

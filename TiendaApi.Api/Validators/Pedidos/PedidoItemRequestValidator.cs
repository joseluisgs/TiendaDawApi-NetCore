using FluentValidation;
using TiendaApi.Api.Dtos.Pedidos;

namespace TiendaApi.Api.Validators.Pedidos;

/// <summary>
/// Validador FluentValidation para PedidoItemRequestDto.
/// Reglas: ProductoId(>0), Cantidad(>0).
/// </summary>
public class PedidoItemRequestValidator : AbstractValidator<PedidoItemRequestDto>
{
    /// <summary>
    /// Define reglas de validación para PedidoItemRequestDto.
    /// </summary>
    public PedidoItemRequestValidator()
    {
        RuleFor(i => i.ProductoId)
            .GreaterThan(0).WithMessage("Debe seleccionar un producto válido");

        RuleFor(i => i.Cantidad)
            .GreaterThan(0).WithMessage("La cantidad debe ser mayor a 0");
    }
}

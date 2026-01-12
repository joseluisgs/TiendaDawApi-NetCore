using FluentValidation;
using TiendaApi.Apis.Dtos.Pedidos;

namespace TiendaApi.Apis.Validators.Pedidos;

/// <summary>
/// Validador FluentValidation para PedidoItemRequestDto.
/// </summary>
public class PedidoItemRequestValidator : AbstractValidator<PedidoItemRequestDto>
{
    public PedidoItemRequestValidator()
    {
        RuleFor(i => i.ProductoId)
            .GreaterThan(0).WithMessage("Debe seleccionar un producto válido");

        RuleFor(i => i.Cantidad)
            .GreaterThan(0).WithMessage("La cantidad debe ser mayor a 0");
    }
}

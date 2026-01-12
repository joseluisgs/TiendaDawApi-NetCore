using FluentValidation;
using TiendaApi.Apis.Dtos.Pedidos;

namespace TiendaApi.Apis.Validators.Pedidos;

/// <summary>
/// Validador FluentValidation para PedidoRequestDto.
/// </summary>
public class PedidoRequestValidator : AbstractValidator<PedidoRequestDto>
{
    public PedidoRequestValidator()
    {
        RuleFor(p => p.Items)
            .NotNull().WithMessage("El pedido debe contener artículos")
            .NotEmpty().WithMessage("El pedido debe contener al menos un artículo")
            .Must(items => items == null || items.Count >= 1).WithMessage("El pedido debe contener al menos un artículo");
    }
}

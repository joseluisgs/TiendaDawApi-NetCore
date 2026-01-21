using FluentValidation;
using TiendaApi.Api.Dtos.Pedidos;

namespace TiendaApi.Api.Validators.Pedidos;

/// <summary>
/// Validador FluentValidation para PedidoRequestDto.
/// Reglas: Items no vacíos, destinatario válido.
/// </summary>
public class PedidoRequestValidator : AbstractValidator<PedidoRequestDto>
{
    private static readonly DestinatarioValidator DestinatarioValidator = new();

    /// <summary>
    /// Define reglas de validación para PedidoRequestDto.
    /// </summary>
    public PedidoRequestValidator()
    {
        RuleFor(p => p.Items)
            .NotEmpty().WithMessage("El pedido debe contener al menos un artículo");

        RuleFor(p => p.Destinatario)
            .NotNull().WithMessage("El destinatario es obligatorio")
            .SetValidator(DestinatarioValidator);
    }
}

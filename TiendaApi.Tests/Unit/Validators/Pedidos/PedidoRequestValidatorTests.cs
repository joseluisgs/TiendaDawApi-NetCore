using FluentValidation.TestHelper;
using TiendaApi.Api.Dtos.Common;
using TiendaApi.Api.Dtos.Pedidos;
using TiendaApi.Api.Validators.Pedidos;

namespace TiendaApi.Tests.Unit.Validators.Pedidos;

/// <summary>
/// Tests unitarios para PedidoRequestValidator.
/// </summary>
public class PedidoRequestValidatorTests
{
    private readonly PedidoRequestValidator _validator = new();

    #region Items Tests

    [Test]
    public void CreateAsync_ConItemsNulo_DeberiaTenerError()
    {
        var dto = new PedidoRequestDto { Items = null! };

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.Items)
            .WithErrorMessage("El pedido debe contener al menos un artículo");
    }

    [Test]
    public void CreateAsync_ConItemsVacios_DeberiaTenerError()
    {
        var dto = new PedidoRequestDto { Items = new List<PedidoItemRequestDto>() };

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.Items)
            .WithErrorMessage("El pedido debe contener al menos un artículo");
    }

    [Test]
    public void CreateAsync_ConItemsValidos_NoDeberiaTenerError()
    {
        var dto = new PedidoRequestDto
        {
            Items = new List<PedidoItemRequestDto>
            {
                new() { ProductoId = 1, Cantidad = 2 }
            }
        };

        var result = _validator.TestValidate(dto);

        result.ShouldNotHaveValidationErrorFor(x => x.Items);
    }

    [Test]
    public void CreateAsync_ConMultipleItemsValidos_NoDeberiaTenerError()
    {
        var dto = new PedidoRequestDto
        {
            Items = new List<PedidoItemRequestDto>
            {
                new() { ProductoId = 1, Cantidad = 2 },
                new() { ProductoId = 2, Cantidad = 3 },
                new() { ProductoId = 3, Cantidad = 1 }
            }
        };

        var result = _validator.TestValidate(dto);

        result.ShouldNotHaveValidationErrorFor(x => x.Items);
    }

    #endregion

    #region DTO Completo Tests

    [Test]
    public void CreateAsync_ConDtoInvalido_DeberiaTenerError()
    {
        var dto = new PedidoRequestDto { Items = new List<PedidoItemRequestDto>() };

        var result = _validator.TestValidate(dto);

        result.ShouldHaveAnyValidationError();
    }

    [Test]
    public void CreateAsync_ConDtoValido_NoDeberiaTenerErrores()
    {
        var dto = new PedidoRequestDto
        {
            Destinatario = new DestinatarioDto
            {
                NombreCompleto = "Test User",
                Email = "test@email.com",
                Direccion = new DireccionDto
                {
                    Calle = "Calle Test",
                    Ciudad = "Madrid",
                    Pais = "España"
                }
            },
            Items = new List<PedidoItemRequestDto>
            {
                new() { ProductoId = 1, Cantidad = 2 }
            }
        };

        var result = _validator.TestValidate(dto);

        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion
}

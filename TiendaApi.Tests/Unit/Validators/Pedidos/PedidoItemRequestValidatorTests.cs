using FluentValidation.TestHelper;
using TiendaApi.Api.Dtos.Pedidos;
using TiendaApi.Api.Validators.Pedidos;

namespace TiendaApi.Tests.Unit.Validators.Pedidos;

/// <summary>
/// Tests unitarios para PedidoItemRequestValidator.
/// </summary>
public class PedidoItemRequestValidatorTests
{
    private readonly PedidoItemRequestValidator _validator = new();

    #region ProductoId Tests

    [Test]
    public void CreateAsync_ConProductoIdCero_DeberiaTenerError()
    {
        var dto = new PedidoItemRequestDto { ProductoId = 0, Cantidad = 2 };

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.ProductoId)
            .WithErrorMessage("Debe seleccionar un producto válido");
    }

    [Test]
    public void CreateAsync_ConProductoIdNegativo_DeberiaTenerError()
    {
        var dto = new PedidoItemRequestDto { ProductoId = -1, Cantidad = 2 };

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.ProductoId)
            .WithErrorMessage("Debe seleccionar un producto válido");
    }

    [Test]
    public void CreateAsync_ConProductoIdValido_DeberiaPasar()
    {
        var dto = new PedidoItemRequestDto { ProductoId = 1, Cantidad = 2 };

        var result = _validator.TestValidate(dto);

        result.ShouldNotHaveValidationErrorFor(x => x.ProductoId);
    }

    [Test]
    public void CreateAsync_ConProductoIdGrande_DeberiaPasar()
    {
        var dto = new PedidoItemRequestDto { ProductoId = 999999, Cantidad = 2 };

        var result = _validator.TestValidate(dto);

        result.ShouldNotHaveValidationErrorFor(x => x.ProductoId);
    }

    #endregion

    #region Cantidad Tests

    [Test]
    public void CreateAsync_ConCantidadCero_DeberiaTenerError()
    {
        var dto = new PedidoItemRequestDto { ProductoId = 1, Cantidad = 0 };

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.Cantidad)
            .WithErrorMessage("La cantidad debe ser mayor a 0");
    }

    [Test]
    public void CreateAsync_ConCantidadNegativa_DeberiaTenerError()
    {
        var dto = new PedidoItemRequestDto { ProductoId = 1, Cantidad = -1 };

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.Cantidad)
            .WithErrorMessage("La cantidad debe ser mayor a 0");
    }

    [Test]
    public void CreateAsync_ConCantidadUno_DeberiaPasar()
    {
        var dto = new PedidoItemRequestDto { ProductoId = 1, Cantidad = 1 };

        var result = _validator.TestValidate(dto);

        result.ShouldNotHaveValidationErrorFor(x => x.Cantidad);
    }

    [Test]
    public void CreateAsync_ConCantidadGrande_DeberiaPasar()
    {
        var dto = new PedidoItemRequestDto { ProductoId = 1, Cantidad = 1000 };

        var result = _validator.TestValidate(dto);

        result.ShouldNotHaveValidationErrorFor(x => x.Cantidad);
    }

    #endregion

    #region DTO Completo Tests

    [Test]
    public void CreateAsync_ConDtoInvalido_DeberiaTenerErrores()
    {
        var dto = new PedidoItemRequestDto { ProductoId = 0, Cantidad = 0 };

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.ProductoId);
        result.ShouldHaveValidationErrorFor(x => x.Cantidad);
    }

    [Test]
    public void CreateAsync_ConDtoValido_NoDeberiaTenerErrores()
    {
        var dto = new PedidoItemRequestDto { ProductoId = 1, Cantidad = 5 };

        var result = _validator.TestValidate(dto);

        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion
}

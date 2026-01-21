using FluentValidation;
using FluentValidation.TestHelper;
using TiendaApi.Api.Dtos.Productos;
using TiendaApi.Api.Validators.Productos;

namespace TiendaApi.Tests.Unit.Validators.Productos;

/// <summary>
/// Tests unitarios para ProductoRequestValidator.
/// </summary>
public class ProductoRequestValidatorTests
{
    private readonly ProductoRequestValidator _validator = new();

    #region Nombre Tests

    [Test]
    public void CreateAsync_ConNombreVacio_DeberiaTenerError()
    {
        var dto = new ProductoRequestDto
        {
            Nombre = "",
            Precio = 10,
            Stock = 5,
            CategoriaId = 1
        };

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.Nombre)
            .WithErrorMessage("El nombre es obligatorio");
    }

    [Test]
    public void CreateAsync_ConNombreMuyCorto_DeberiaTenerError()
    {
        var dto = new ProductoRequestDto
        {
            Nombre = "AB",
            Precio = 10,
            Stock = 5,
            CategoriaId = 1
        };

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.Nombre)
            .WithErrorMessage("El nombre debe tener al menos 3 caracteres");
    }

    [Test]
    public void CreateAsync_ConNombreMuyLargo_DeberiaTenerError()
    {
        var dto = new ProductoRequestDto
        {
            Nombre = new string('A', 201),
            Precio = 10,
            Stock = 5,
            CategoriaId = 1
        };

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.Nombre)
            .WithErrorMessage("El nombre no puede exceder 200 caracteres");
    }

    [Test]
    public void CreateAsync_ConNombreValido_NoDeberiaTenerError()
    {
        var dto = new ProductoRequestDto
        {
            Nombre = "iPhone 15 Pro Max",
            Precio = 999.99m,
            Stock = 10,
            CategoriaId = 1
        };

        var result = _validator.TestValidate(dto);

        result.ShouldNotHaveValidationErrorFor(x => x.Nombre);
    }

    #endregion

    #region Precio Tests

    [Test]
    public void CreateAsync_ConPrecioCero_DeberiaTenerError()
    {
        var dto = new ProductoRequestDto
        {
            Nombre = "Test Product",
            Precio = 0,
            Stock = 5,
            CategoriaId = 1
        };

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.Precio)
            .WithErrorMessage("El precio debe ser mayor a 0");
    }

    [Test]
    public void CreateAsync_ConPrecioNegativo_DeberiaTenerError()
    {
        var dto = new ProductoRequestDto
        {
            Nombre = "Test Product",
            Precio = -10,
            Stock = 5,
            CategoriaId = 1
        };

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.Precio)
            .WithErrorMessage("El precio debe ser mayor a 0");
    }

    [Test]
    public void CreateAsync_ConPrecioValido_NoDeberiaTenerError()
    {
        var dto = new ProductoRequestDto
        {
            Nombre = "Test Product",
            Precio = 99.99m,
            Stock = 5,
            CategoriaId = 1
        };

        var result = _validator.TestValidate(dto);

        result.ShouldNotHaveValidationErrorFor(x => x.Precio);
    }

    #endregion

    #region Stock Tests

    [Test]
    public void CreateAsync_ConStockNegativo_DeberiaTenerError()
    {
        var dto = new ProductoRequestDto
        {
            Nombre = "Test Product",
            Precio = 10,
            Stock = -5,
            CategoriaId = 1
        };

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.Stock)
            .WithErrorMessage("El stock no puede ser negativo");
    }

    [Test]
    public void CreateAsync_ConStockCero_DeberiaPasar()
    {
        var dto = new ProductoRequestDto
        {
            Nombre = "Test Product",
            Precio = 10,
            Stock = 0,
            CategoriaId = 1
        };

        var result = _validator.TestValidate(dto);

        result.ShouldNotHaveValidationErrorFor(x => x.Stock);
    }

    [Test]
    public void CreateAsync_ConStockPositivo_DeberiaPasar()
    {
        var dto = new ProductoRequestDto
        {
            Nombre = "Test Product",
            Precio = 10,
            Stock = 100,
            CategoriaId = 1
        };

        var result = _validator.TestValidate(dto);

        result.ShouldNotHaveValidationErrorFor(x => x.Stock);
    }

    #endregion

    #region CategoriaId Tests

    [Test]
    public void CreateAsync_ConCategoriaIdCero_DeberiaTenerError()
    {
        var dto = new ProductoRequestDto
        {
            Nombre = "Test Product",
            Precio = 10,
            Stock = 5,
            CategoriaId = 0
        };

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.CategoriaId)
            .WithErrorMessage("Debe seleccionar una categoría válida");
    }

    [Test]
    public void CreateAsync_ConCategoriaIdNegativo_DeberiaTenerError()
    {
        var dto = new ProductoRequestDto
        {
            Nombre = "Test Product",
            Precio = 10,
            Stock = 5,
            CategoriaId = -1
        };

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.CategoriaId)
            .WithErrorMessage("Debe seleccionar una categoría válida");
    }

    [Test]
    public void CreateAsync_ConCategoriaIdValido_DeberiaPasar()
    {
        var dto = new ProductoRequestDto
        {
            Nombre = "Test Product",
            Precio = 10,
            Stock = 5,
            CategoriaId = 1
        };

        var result = _validator.TestValidate(dto);

        result.ShouldNotHaveValidationErrorFor(x => x.CategoriaId);
    }

    #endregion

    #region Descripcion Tests

    [Test]
    public void CreateAsync_ConDescripcionMuyLarga_DeberiaTenerError()
    {
        var dto = new ProductoRequestDto
        {
            Nombre = "Test Product",
            Descripcion = new string('A', 1001),
            Precio = 10,
            Stock = 5,
            CategoriaId = 1
        };

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.Descripcion)
            .WithErrorMessage("La descripción no puede exceder 1000 caracteres");
    }

    [Test]
    public void CreateAsync_ConDescripcionVacia_DeberiaPasar()
    {
        var dto = new ProductoRequestDto
        {
            Nombre = "Test Product",
            Descripcion = "",
            Precio = 10,
            Stock = 5,
            CategoriaId = 1
        };

        var result = _validator.TestValidate(dto);

        result.ShouldNotHaveValidationErrorFor(x => x.Descripcion);
    }

    #endregion

    #region Imagen Tests

    [Test]
    public void CreateAsync_ConUrlInvalida_DeberiaTenerError()
    {
        var dto = new ProductoRequestDto
        {
            Nombre = "Test Product",
            Precio = 10,
            Stock = 5,
            CategoriaId = 1,
            Imagen = "not-a-valid-url"
        };

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.Imagen);
    }

    [Test]
    public void CreateAsync_ConUrlHttpValida_DeberiaPasar()
    {
        var dto = new ProductoRequestDto
        {
            Nombre = "Test Product",
            Precio = 10,
            Stock = 5,
            CategoriaId = 1,
            Imagen = "http://ejemplo.com/imagen.jpg"
        };

        var result = _validator.TestValidate(dto);

        result.ShouldNotHaveValidationErrorFor(x => x.Imagen);
    }

    [Test]
    public void CreateAsync_ConUrlHttpsValida_DeberiaPasar()
    {
        var dto = new ProductoRequestDto
        {
            Nombre = "Test Product",
            Precio = 10,
            Stock = 5,
            CategoriaId = 1,
            Imagen = "https://ejemplo.com/imagen.jpg"
        };

        var result = _validator.TestValidate(dto);

        result.ShouldNotHaveValidationErrorFor(x => x.Imagen);
    }

    [Test]
    public void CreateAsync_ConImagenVacia_DeberiaPasar()
    {
        var dto = new ProductoRequestDto
        {
            Nombre = "Test Product",
            Precio = 10,
            Stock = 5,
            CategoriaId = 1,
            Imagen = null
        };

        var result = _validator.TestValidate(dto);

        result.ShouldNotHaveValidationErrorFor(x => x.Imagen);
    }

    #endregion

    #region DTO Completo Tests

    [Test]
    public void CreateAsync_ConDtoCompletoInvalido_DeberiaTenerMultiplesErrores()
    {
        var dto = new ProductoRequestDto
        {
            Nombre = "",
            Descripcion = new string('A', 1001),
            Precio = -10,
            Stock = -5,
            CategoriaId = 0,
            Imagen = "invalid"
        };

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.Nombre);
        result.ShouldHaveValidationErrorFor(x => x.Descripcion);
        result.ShouldHaveValidationErrorFor(x => x.Precio);
        result.ShouldHaveValidationErrorFor(x => x.Stock);
        result.ShouldHaveValidationErrorFor(x => x.CategoriaId);
        result.ShouldHaveValidationErrorFor(x => x.Imagen);
    }

    [Test]
    public void CreateAsync_ConDtoCompletoValido_NoDeberiaTenerErrores()
    {
        var dto = new ProductoRequestDto
        {
            Nombre = "iPhone 15 Pro",
            Descripcion = "El último iPhone de Apple",
            Precio = 999.99m,
            Stock = 50,
            CategoriaId = 1,
            Imagen = "https://ejemplo.com/iphone15.jpg"
        };

        var result = _validator.TestValidate(dto);

        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion

    #region Casos Borde Adicionales

    [Test]
    public void CreateAsync_ConNombreConCaracteresEspeciales_DeberiaPasar()
    {
        var dto = new ProductoRequestDto
        {
            Nombre = "Producto #1 - Deluxe Edition®",
            Precio = 10,
            Stock = 5,
            CategoriaId = 1
        };

        var result = _validator.TestValidate(dto);

        result.ShouldNotHaveValidationErrorFor(x => x.Nombre);
    }

    [Test]
    public void CreateAsync_ConNombreConEspacios_DeberiaPasar()
    {
        var dto = new ProductoRequestDto
        {
            Nombre = " producto con espacios ",
            Precio = 10,
            Stock = 5,
            CategoriaId = 1
        };

        var result = _validator.TestValidate(dto);

        result.ShouldNotHaveValidationErrorFor(x => x.Nombre);
    }

    [Test]
    public void CreateAsync_ConPrecioDecimal_DeberiaPasar()
    {
        var dto = new ProductoRequestDto
        {
            Nombre = "Test Product",
            Precio = 0.01m,
            Stock = 5,
            CategoriaId = 1
        };

        var result = _validator.TestValidate(dto);

        result.ShouldNotHaveValidationErrorFor(x => x.Precio);
    }

    [Test]
    public void CreateAsync_ConPrecioMuyAlto_DeberiaPasar()
    {
        var dto = new ProductoRequestDto
        {
            Nombre = "Test Product",
            Precio = 9999999.99m,
            Stock = 5,
            CategoriaId = 1
        };

        var result = _validator.TestValidate(dto);

        result.ShouldNotHaveValidationErrorFor(x => x.Precio);
    }

    [Test]
    public void CreateAsync_ConStockMuyAlto_DeberiaPasar()
    {
        var dto = new ProductoRequestDto
        {
            Nombre = "Test Product",
            Precio = 10,
            Stock = int.MaxValue,
            CategoriaId = 1
        };

        var result = _validator.TestValidate(dto);

        result.ShouldNotHaveValidationErrorFor(x => x.Stock);
    }

    [Test]
    public void CreateAsync_ConCategoriaIdMuyAlto_DeberiaPasar()
    {
        var dto = new ProductoRequestDto
        {
            Nombre = "Test Product",
            Precio = 10,
            Stock = 5,
            CategoriaId = 999999999
        };

        var result = _validator.TestValidate(dto);

        result.ShouldNotHaveValidationErrorFor(x => x.CategoriaId);
    }

    [Test]
    public void CreateAsync_ConUrlSinProtocolo_DeberiaTenerError()
    {
        var dto = new ProductoRequestDto
        {
            Nombre = "Test Product",
            Precio = 10,
            Stock = 5,
            CategoriaId = 1,
            Imagen = "www.ejemplo.com/imagen.jpg"
        };

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.Imagen);
    }

    [Test]
    public void CreateAsync_ConUrlConQueryString_DeberiaPasar()
    {
        var dto = new ProductoRequestDto
        {
            Nombre = "Test Product",
            Precio = 10,
            Stock = 5,
            CategoriaId = 1,
            Imagen = "https://ejemplo.com/imagen.jpg?width=100&height=200"
        };

        var result = _validator.TestValidate(dto);

        result.ShouldNotHaveValidationErrorFor(x => x.Imagen);
    }

    [Test]
    public void CreateAsync_ConDescripcionConSaltoDeLinea_DeberiaPasar()
    {
        var dto = new ProductoRequestDto
        {
            Nombre = "Test Product",
            Descripcion = "Primera línea\nSegunda línea\r\nTercera línea",
            Precio = 10,
            Stock = 5,
            CategoriaId = 1
        };

        var result = _validator.TestValidate(dto);

        result.ShouldNotHaveValidationErrorFor(x => x.Descripcion);
    }

    [Test]
    public void CreateAsync_ConDescripcionConCaracteresEspeciales_DeberiaPasar()
    {
        var dto = new ProductoRequestDto
        {
            Nombre = "Test Product",
            Descripcion = "Producto con acentos: áéíóúñÑ y símbolos: @#$%",
            Precio = 10,
            Stock = 5,
            CategoriaId = 1
        };

        var result = _validator.TestValidate(dto);

        result.ShouldNotHaveValidationErrorFor(x => x.Descripcion);
    }

    [Test]
    public void CreateAsync_ConDescripcionEnIngles_DeberiaPasar()
    {
        var dto = new ProductoRequestDto
        {
            Nombre = "Test Product",
            Descripcion = "This is a product description in English with special chars: ñ, ü, ö",
            Precio = 10,
            Stock = 5,
            CategoriaId = 1
        };

        var result = _validator.TestValidate(dto);

        result.ShouldNotHaveValidationErrorFor(x => x.Descripcion);
    }

    [Test]
    public void CreateAsync_ConSoloErroresDeStock_DeberiaTenerErrorSoloEnStock()
    {
        var dto = new ProductoRequestDto
        {
            Nombre = "Test Product",
            Descripcion = "A valid description",
            Precio = 10,
            Stock = -1,
            CategoriaId = 1,
            Imagen = "https://ejemplo.com/imagen.jpg"
        };

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.Stock);
        result.ShouldNotHaveValidationErrorFor(x => x.Nombre);
        result.ShouldNotHaveValidationErrorFor(x => x.Precio);
        result.ShouldNotHaveValidationErrorFor(x => x.CategoriaId);
        result.ShouldNotHaveValidationErrorFor(x => x.Descripcion);
        result.ShouldNotHaveValidationErrorFor(x => x.Imagen);
    }

    [Test]
    public void CreateAsync_ConSoloErroresDeCategoria_DeberiaTenerErrorSoloEnCategoria()
    {
        var dto = new ProductoRequestDto
        {
            Nombre = "Test Product",
            Descripcion = "A valid description",
            Precio = 10,
            Stock = 5,
            CategoriaId = 0,
            Imagen = "https://ejemplo.com/imagen.jpg"
        };

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.CategoriaId);
        result.ShouldNotHaveValidationErrorFor(x => x.Nombre);
        result.ShouldNotHaveValidationErrorFor(x => x.Precio);
        result.ShouldNotHaveValidationErrorFor(x => x.Stock);
        result.ShouldNotHaveValidationErrorFor(x => x.Descripcion);
        result.ShouldNotHaveValidationErrorFor(x => x.Imagen);
    }

    #endregion
}

using FluentValidation.TestHelper;
using TiendaApi.Api.Dtos.Productos;
using TiendaApi.Api.Validators.Productos;

namespace TiendaApi.Tests.Unit.Validators.Productos;

public class ProductoRequestValidatorEdgeTests
{
    private readonly ProductoRequestValidator _validator = new();

    #region Nombre Edge Cases

    [Test]
    public void CreateAsync_ConNombreExactamente3Caracteres_DeberiaPasar()
    {
        var dto = new ProductoRequestDto
        {
            Nombre = "ABC",
            Precio = 10,
            Stock = 5,
            CategoriaId = 1
        };

        var result = _validator.TestValidate(dto);

        result.ShouldNotHaveValidationErrorFor(x => x.Nombre);
    }

    [Test]
    public void CreateAsync_ConNombreExactamente200Caracteres_DeberiaPasar()
    {
        var dto = new ProductoRequestDto
        {
            Nombre = new string('A', 200),
            Precio = 10,
            Stock = 5,
            CategoriaId = 1
        };

        var result = _validator.TestValidate(dto);

        result.ShouldNotHaveValidationErrorFor(x => x.Nombre);
    }

    [Test]
    public void CreateAsync_ConNombre201Caracteres_DeberiaTenerError()
    {
        var dto = new ProductoRequestDto
        {
            Nombre = new string('A', 201),
            Precio = 10,
            Stock = 5,
            CategoriaId = 1
        };

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.Nombre);
    }

    [Test]
    public void CreateAsync_ConNombreConEmojis_DeberiaPasar()
    {
        var dto = new ProductoRequestDto
        {
            Nombre = "Producto 🎁 #1",
            Precio = 10,
            Stock = 5,
            CategoriaId = 1
        };

        var result = _validator.TestValidate(dto);

        result.ShouldNotHaveValidationErrorFor(x => x.Nombre);
    }

    [Test]
    public void CreateAsync_ConNombreConTildes_DeberiaPasar()
    {
        var dto = new ProductoRequestDto
        {
            Nombre = "José María García Pérez",
            Precio = 10,
            Stock = 5,
            CategoriaId = 1
        };

        var result = _validator.TestValidate(dto);

        result.ShouldNotHaveValidationErrorFor(x => x.Nombre);
    }

    #endregion

    #region Precio Edge Cases

    [Test]
    public void CreateAsync_ConPrecio0_01_DeberiaPasar()
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
    public void CreateAsync_ConPrecio99999999999999_DeberiaPasar()
    {
        var dto = new ProductoRequestDto
        {
            Nombre = "Test Product",
            Precio = 99999999999999m,
            Stock = 5,
            CategoriaId = 1
        };

        var result = _validator.TestValidate(dto);

        result.ShouldNotHaveValidationErrorFor(x => x.Precio);
    }

    [Test]
    public void CreateAsync_ConPrecioNegativoPequeño_DeberiaTenerError()
    {
        var dto = new ProductoRequestDto
        {
            Nombre = "Test Product",
            Precio = -0.01m,
            Stock = 5,
            CategoriaId = 1
        };

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.Precio);
    }

    #endregion

    #region Stock Edge Cases

    [Test]
    public void CreateAsync_ConStockIntMaxValue_DeberiaPasar()
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
    public void CreateAsync_ConStockNegativo1_DeberiaTenerError()
    {
        var dto = new ProductoRequestDto
        {
            Nombre = "Test Product",
            Precio = 10,
            Stock = -1,
            CategoriaId = 1
        };

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.Stock);
    }

    #endregion

    #region CategoriaId Edge Cases

    [Test]
    public void CreateAsync_ConCategoriaId1_DeberiaPasar()
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

    [Test]
    public void CreateAsync_ConCategoriaId999999999_DeberiaPasar()
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
    public void CreateAsync_ConCategoriaIdNegativo1_DeberiaTenerError()
    {
        var dto = new ProductoRequestDto
        {
            Nombre = "Test Product",
            Precio = 10,
            Stock = 5,
            CategoriaId = -1
        };

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.CategoriaId);
    }

    #endregion

    #region Descripcion Edge Cases

    [Test]
    public void CreateAsync_ConDescripcionExactamente1000Caracteres_DeberiaPasar()
    {
        var dto = new ProductoRequestDto
        {
            Nombre = "Test Product",
            Descripcion = new string('A', 1000),
            Precio = 10,
            Stock = 5,
            CategoriaId = 1
        };

        var result = _validator.TestValidate(dto);

        result.ShouldNotHaveValidationErrorFor(x => x.Descripcion);
    }

    [Test]
    public void CreateAsync_ConDescripcion1001Caracteres_DeberiaTenerError()
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

        result.ShouldHaveValidationErrorFor(x => x.Descripcion);
    }

    [Test]
    public void CreateAsync_ConDescripcionNull_DeberiaPasar()
    {
        var dto = new ProductoRequestDto
        {
            Nombre = "Test Product",
            Descripcion = null!,
            Precio = 10,
            Stock = 5,
            CategoriaId = 1
        };

        var result = _validator.TestValidate(dto);

        result.ShouldNotHaveValidationErrorFor(x => x.Descripcion);
    }

    [Test]
    public void CreateAsync_ConDescripcionSoloEspacios_DeberiaPasar()
    {
        var dto = new ProductoRequestDto
        {
            Nombre = "Test Product",
            Descripcion = "   ",
            Precio = 10,
            Stock = 5,
            CategoriaId = 1
        };

        var result = _validator.TestValidate(dto);

        result.ShouldNotHaveValidationErrorFor(x => x.Descripcion);
    }

    #endregion

    #region Imagen URL Edge Cases

    [Test]
    public void CreateAsync_ConUrlVacia_DeberiaPasar()
    {
        var dto = new ProductoRequestDto
        {
            Nombre = "Test Product",
            Precio = 10,
            Stock = 5,
            CategoriaId = 1,
            Imagen = string.Empty
        };

        var result = _validator.TestValidate(dto);

        result.ShouldNotHaveValidationErrorFor(x => x.Imagen);
    }

    [Test]
    public void CreateAsync_ConUrlNull_DeberiaPasar()
    {
        var dto = new ProductoRequestDto
        {
            Nombre = "Test Product",
            Precio = 10,
            Stock = 5,
            CategoriaId = 1,
            Imagen = null!
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
            Imagen = "https://subdominio.dominio.com/pagina/ruta?param1=valor1&param2=valor2"
        };

        var result = _validator.TestValidate(dto);

        result.ShouldNotHaveValidationErrorFor(x => x.Imagen);
    }

    [Test]
    public void CreateAsync_ConUrlFtp_DeberiaTenerError()
    {
        var dto = new ProductoRequestDto
        {
            Nombre = "Test Product",
            Precio = 10,
            Stock = 5,
            CategoriaId = 1,
            Imagen = "ftp://ftp.ejemplo.com/imagen.jpg"
        };

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.Imagen);
    }

    [Test]
    public void CreateAsync_ConUrlFile_DeberiaTenerError()
    {
        var dto = new ProductoRequestDto
        {
            Nombre = "Test Product",
            Precio = 10,
            Stock = 5,
            CategoriaId = 1,
            Imagen = "file:///C:/imagen.jpg"
        };

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.Imagen);
    }

    #endregion
}

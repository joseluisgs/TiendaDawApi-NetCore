using FluentAssertions;
using TiendaApi.Api.Dtos.Productos;
using TiendaApi.Api.Mappers;
using TiendaApi.Api.Models;

namespace TiendaApi.Tests.Unit.Mappers;

/// <summary>
/// Tests unitarios para el mapeador de productos.
/// Prueba todas las conversiones entidad-DTO para el dominio de Producto.
/// </summary>
public class ProductoMapperTests
{
    #region ToDto Tests

    [Test]
    public void ToDto_ConTodosLosCampos_MapeaCorrectamente()
    {
        // Arrange
        var categoria = new Categoria { Id = 1, Nombre = "Electronics" };
        var producto = new Producto
        {
            Id = 100,
            Nombre = "Gaming Laptop",
            Descripcion = "High-performance gaming laptop",
            Precio = 1999.99m,
            Stock = 25,
            Imagen = "laptop.jpg",
            CategoriaId = 1,
            Categoria = categoria,
            CreatedAt = new DateTime(2024, 1, 15),
            UpdatedAt = new DateTime(2024, 6, 20)
        };

        // Act
        var dto = producto.ToDto();

        // Assert
        dto.Id.Should().Be(100);
        dto.Nombre.Should().Be("Gaming Laptop");
        dto.Descripcion.Should().Be("High-performance gaming laptop");
        dto.Precio.Should().Be(1999.99m);
        dto.Stock.Should().Be(25);
        dto.Imagen.Should().Be("laptop.jpg");
        dto.CategoriaId.Should().Be(1);
        dto.CategoriaNombre.Should().Be("Electronics");
    }

    [Test]
    public void ToDto_ConCategoriaNula_RetornaCategoriaNombreVacio()
    {
        // Arrange
        var producto = new Producto
        {
            Id = 1,
            Nombre = "Orphan Product",
            Categoria = null!
        };

        // Act
        var dto = producto.ToDto();

        // Assert
        dto.CategoriaNombre.Should().BeEmpty();
    }

    [Test]
    public void ToDto_ConCategoriaNombreNulo_RetornaVacio()
    {
        // Arrange
        var producto = new Producto
        {
            Id = 1,
            Nombre = "Test",
            Categoria = new Categoria { Id = 1, Nombre = null! }
        };

        // Act
        var dto = producto.ToDto();

        // Assert
        dto.CategoriaNombre.Should().BeEmpty();
    }

    [Test]
    public void ToDto_ConPreciosDecimales_MapeaCorrectamente()
    {
        // Arrange
        var producto = new Producto
        {
            Id = 1,
            Nombre = "Precise Product",
            Precio = 123.456789m
        };

        // Act
        var dto = producto.ToDto();

        // Assert
        dto.Precio.Should().Be(123.456789m);
    }

    [Test]
    public void ToDto_ConStockCero_MapeaCorrectamente()
    {
        // Arrange
        var producto = new Producto
        {
            Id = 1,
            Nombre = "Out of Stock",
            Stock = 0
        };

        // Act
        var dto = producto.ToDto();

        // Assert
        dto.Stock.Should().Be(0);
    }

    [Test]
    public void ToDto_ConImagenNula_MapeaNulo()
    {
        // Arrange
        var producto = new Producto
        {
            Id = 1,
            Nombre = "No Image Product",
            Imagen = null
        };

        // Act
        var dto = producto.ToDto();

        // Assert
        dto.Imagen.Should().BeNull();
    }

    [Test]
    public void ToDto_DebePreservarMarcasDeTiempo()
    {
        // Arrange
        var createdAt = new DateTime(2024, 1, 1, 10, 0, 0);
        var updatedAt = new DateTime(2024, 6, 15, 14, 30, 0);
        var producto = new Producto
        {
            Id = 1,
            Nombre = "Test",
            CreatedAt = createdAt,
            UpdatedAt = updatedAt
        };

        // Act
        var dto = producto.ToDto();

        // Assert
        dto.CreatedAt.Should().Be(createdAt);
        dto.UpdatedAt.Should().Be(updatedAt);
    }

    #endregion

    #region ToEntity Tests

    [Test]
    public void ToEntity_ConTodosLosCampos_MapeaCorrectamente()
    {
        // Arrange
        var dto = new ProductoRequestDto
        {
            Nombre = "New Product",
            Descripcion = "Product description",
            Precio = 299.99m,
            Stock = 50,
            Imagen = "product.jpg",
            CategoriaId = 5
        };

        // Act
        var entity = dto.ToEntity();

        // Assert
        entity.Nombre.Should().Be("New Product");
        entity.Descripcion.Should().Be("Product description");
        entity.Precio.Should().Be(299.99m);
        entity.Stock.Should().Be(50);
        entity.Imagen.Should().Be("product.jpg");
        entity.CategoriaId.Should().Be(5);
    }

    [Test]
    public void ToEntity_DebeEstablecerMarcasDeTiempo()
    {
        // Arrange
        var dto = new ProductoRequestDto { Nombre = "Test" };
        var before = DateTime.UtcNow;

        // Act
        var entity = dto.ToEntity();
        var after = DateTime.UtcNow;

        // Assert
        entity.CreatedAt.Should().BeOnOrAfter(before);
        entity.CreatedAt.Should().BeOnOrBefore(after);
        entity.UpdatedAt.Should().BeOnOrAfter(before);
        entity.UpdatedAt.Should().BeOnOrBefore(after);
    }

    [Test]
    public void ToEntity_ConDescripcionVacia_MapeaVacia()
    {
        // Arrange
        var dto = new ProductoRequestDto
        {
            Nombre = "Test",
            Descripcion = string.Empty
        };

        // Act
        var entity = dto.ToEntity();

        // Assert
        entity.Descripcion.Should().BeEmpty();
    }

    [Test]
    public void ToEntity_ConPrecioCero_MapeaCero()
    {
        // Arrange
        var dto = new ProductoRequestDto
        {
            Nombre = "Free Product",
            Precio = 0m
        };

        // Act
        var entity = dto.ToEntity();

        // Assert
        entity.Precio.Should().Be(0m);
    }

    [Test]
    public void ToEntity_ConStockNegativo_MapeaNegativo()
    {
        // Arrange
        var dto = new ProductoRequestDto
        {
            Nombre = "Backorder Product",
            Stock = -10
        };

        // Act
        var entity = dto.ToEntity();

        // Assert
        entity.Stock.Should().Be(-10);
    }

    #endregion

    #region UpdateEntity Tests

    [Test]
    public void UpdateEntity_DebeActualizarTodosLosCampos()
    {
        // Arrange
        var producto = new Producto
        {
            Id = 1,
            Nombre = "Original",
            Descripcion = "Original desc",
            Precio = 100,
            Stock = 10,
            CategoriaId = 1,
            UpdatedAt = DateTime.UtcNow.AddHours(-1)
        };
        var dto = new ProductoRequestDto
        {
            Nombre = "Updated",
            Descripcion = "Updated desc",
            Precio = 200,
            Stock = 20,
            CategoriaId = 2
        };

        // Act
        dto.UpdateEntity(producto);

        // Assert
        producto.Nombre.Should().Be("Updated");
        producto.Descripcion.Should().Be("Updated desc");
        producto.Precio.Should().Be(200);
        producto.Stock.Should().Be(20);
        producto.CategoriaId.Should().Be(2);
    }

    [Test]
    public void UpdateEntity_NoDebeModificarId()
    {
        // Arrange
        var producto = new Producto { Id = 999, Nombre = "Original" };
        var dto = new ProductoRequestDto { Nombre = "Updated" };

        // Act
        dto.UpdateEntity(producto);

        // Assert
        producto.Id.Should().Be(999);
    }

    [Test]
    public void UpdateEntity_NoDebeModificarCreatedAt()
    {
        // Arrange
        var originalCreatedAt = new DateTime(2023, 6, 15);
        var producto = new Producto
        {
            Id = 1,
            Nombre = "Test",
            CreatedAt = originalCreatedAt
        };
        var dto = new ProductoRequestDto { Nombre = "Updated" };

        // Act
        dto.UpdateEntity(producto);

        // Assert
        producto.CreatedAt.Should().Be(originalCreatedAt);
    }

    [Test]
    public void UpdateEntity_ConImagenNula_NoActualizaImagen()
    {
        // Arrange
        var producto = new Producto
        {
            Id = 1,
            Nombre = "Test",
            Imagen = "existing.jpg"
        };
        var dto = new ProductoRequestDto
        {
            Nombre = "Updated",
            Imagen = null
        };

        // Act
        dto.UpdateEntity(producto);

        // Assert
        producto.Imagen.Should().Be("existing.jpg");
    }

    [Test]
    public void UpdateEntity_ConImagenNoVacia_ActualizaImagen()
    {
        // Arrange
        var producto = new Producto
        {
            Id = 1,
            Nombre = "Test",
            Imagen = "old.jpg"
        };
        var dto = new ProductoRequestDto
        {
            Nombre = "Updated",
            Imagen = "new.jpg"
        };

        // Act
        dto.UpdateEntity(producto);

        // Assert
        producto.Imagen.Should().Be("new.jpg");
    }

    #endregion

    #region ToDtoList Tests

    [Test]
    public void ToDtoList_ConMultiplesProductos_MapeaTodos()
    {
        // Arrange
        var productos = new List<Producto>
        {
            new() { Id = 1, Nombre = "P1" },
            new() { Id = 2, Nombre = "P2" },
            new() { Id = 3, Nombre = "P3" }
        };

        // Act
        var dtos = productos.ToDtoList().ToList();

        // Assert
        dtos.Should().HaveCount(3);
        dtos[0].Nombre.Should().Be("P1");
        dtos[1].Nombre.Should().Be("P2");
        dtos[2].Nombre.Should().Be("P3");
    }

    [Test]
    public void ToDtoList_ConListaVacia_RetornaVacia()
    {
        // Arrange
        var productos = new List<Producto>();

        // Act
        var dtos = productos.ToDtoList().ToList();

        // Assert
        dtos.Should().BeEmpty();
    }

    [Test]
    public void ToDtoList_DebePreservarOrden()
    {
        // Arrange
        var productos = new List<Producto>
        {
            new() { Id = 3, Nombre = "Third" },
            new() { Id = 1, Nombre = "First" },
            new() { Id = 2, Nombre = "Second" }
        };

        // Act
        var dtos = productos.ToDtoList().ToList();

        // Assert
        dtos[0].Id.Should().Be(3);
        dtos[1].Id.Should().Be(1);
        dtos[2].Id.Should().Be(2);
    }

    #endregion

    #region Roundtrip Tests

    [Test]
    public void ToEntity_LuegoToDto_DebePreservarDatosBasicos()
    {
        // Arrange
        var dto = new ProductoRequestDto
        {
            Nombre = "Roundtrip Test",
            Descripcion = "Testing roundtrip",
            Precio = 150.00m,
            Stock = 10,
            CategoriaId = 5
        };

        // Act
        var entity = dto.ToEntity();
        var resultDto = entity.ToDto();

        // Assert
        resultDto.Nombre.Should().Be(dto.Nombre);
        resultDto.Precio.Should().Be(dto.Precio);
        resultDto.Stock.Should().Be(dto.Stock);
        resultDto.CategoriaId.Should().Be(dto.CategoriaId);
    }

    #endregion

    #region Edge Cases Tests

    [Test]
    public void ToDto_ConPreciosMaximos_MapeaCorrectamente()
    {
        // Arrange
        var producto = new Producto
        {
            Id = 1,
            Nombre = "Expensive",
            Precio = decimal.MaxValue
        };

        // Act
        var dto = producto.ToDto();

        // Assert
        dto.Precio.Should().Be(decimal.MaxValue);
    }

    [Test]
    public void ToDto_ConStockMaximo_MapeaCorrectamente()
    {
        // Arrange
        var producto = new Producto
        {
            Id = 1,
            Nombre = "Bulk",
            Stock = int.MaxValue
        };

        // Act
        var dto = producto.ToDto();

        // Assert
        dto.Stock.Should().Be(int.MaxValue);
    }

    [Test]
    public void ToDto_ConDescripcionMuyLarga_MapeaCorrectamente()
    {
        // Arrange
        var longDesc = new string('X', 5000);
        var producto = new Producto
        {
            Id = 1,
            Nombre = "Long Description",
            Descripcion = longDesc
        };

        // Act
        var dto = producto.ToDto();

        // Assert
        dto.Descripcion.Should().Be(longDesc);
        dto.Descripcion.Length.Should().Be(5000);
    }

    [Test]
    public void ToDto_ConEmoji_MapeaCorrectamente()
    {
        // Arrange
        var producto = new Producto
        {
            Id = 1,
            Nombre = "🎁 Gift Box 🎁",
            Descripcion = "Contains 🎮 + 📦 + 🎀"
        };

        // Act
        var dto = producto.ToDto();

        // Assert
        dto.Nombre.Should().Be("🎁 Gift Box 🎁");
        dto.Descripcion.Should().Contain("🎮");
    }

    #endregion
}

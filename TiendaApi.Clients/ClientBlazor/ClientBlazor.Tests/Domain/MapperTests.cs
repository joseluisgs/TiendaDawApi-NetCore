using ClientBlazor.Cliente.Domain.Mappers;
using ClientBlazor.Cliente.Domain.Models;
using ClientBlazor.Cliente.DTOs.Productos;
using FluentAssertions;
using NUnit.Framework;

namespace ClientBlazor.Tests.Domain;

/// <summary>
/// Pruebas unitarias para la capa de mapeo y transformación de datos.
/// Objetivo: Asegurar que la conversión entre DTOs de API y Modelos de UI sea íntegra y funcional.
/// </summary>
[TestFixture]
public class MapperTests
{
    /// <summary>
    /// Verifica que todas las propiedades de un ProductoDto se transfieran correctamente al ProductoModel.
    /// </summary>
    [Test]
    public void ProductoDto_ToModel_Should_Map_All_Fields()
    {
        // Arrange
        var dto = new ProductoDto
        {
            Id = 1,
            Nombre = "Test",
            Descripcion = "Desc",
            Precio = 100.50m,
            Stock = 10,
            Imagen = "test.jpg",
            CategoriaId = 2,
            CategoriaNombre = "Cat",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Act
        var model = dto.ToModel();

        // Assert
        model.Id.Should().Be(dto.Id);
        model.Nombre.Should().Be(dto.Nombre);
        model.Precio.Should().Be(dto.Precio);
    }

    /// <summary>
    /// Valida que el modelo de UI devuelva un marcador de posición (placeholder) 
    /// cuando el producto no tiene una imagen asignada.
    /// </summary>
    [Test]
    public void ProductoModel_ImagenUrl_Should_Return_Placeholder_When_Empty()
    {
        // Arrange
        var model = new ProductoModel { Id = 1, Nombre = "T", Descripcion = "D", Precio = 10, Stock = 0, Imagen = null };

        // Assert
        model.ImagenUrl.Should().Be("/images/placeholder.png");
    }

    /// <summary>
    /// Comprueba que el modelo de UI genere una URL absoluta válida 
    /// concatenando la base del servidor cuando se proporciona un nombre de archivo.
    /// </summary>
    [Test]
    public void ProductoModel_ImagenUrl_Should_Return_FullUrl_When_FileName_Provided()
    {
        // Arrange
        var model = new ProductoModel { Id = 1, Nombre = "T", Descripcion = "D", Precio = 10, Stock = 0, Imagen = "foto.png" };

        // Assert
        model.ImagenUrl.Should().Be("http://localhost:5031/storage/foto.png");
    }

    /// <summary>
    /// Valida las propiedades lógicas de stock para asegurar que la UI pueda 
    /// mostrar alertas de stock bajo o agotado correctamente.
    /// </summary>
    [Test]
    public void ProductoModel_Stock_Properties_Should_Reflect_Correct_State()
    {
        var sinStock = new ProductoModel { Stock = 0 };
        var stockBajo = new ProductoModel { Stock = 3 };
        var stockOk = new ProductoModel { Stock = 20 };

        sinStock.SinStock.Should().BeTrue();
        stockBajo.StockBajo.Should().BeTrue();
        stockOk.SinStock.Should().BeFalse();
        stockOk.StockBajo.Should().BeFalse();
    }
}
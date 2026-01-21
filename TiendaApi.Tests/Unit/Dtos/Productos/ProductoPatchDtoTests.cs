using FluentAssertions;
using TiendaApi.Api.Dtos.Productos;

namespace TiendaApi.Tests.Unit.Dtos.Productos;

/// <summary>
/// Tests unitarios para ProductoPatchDto.
/// Verifica el funcionamiento de la actualización parcial de productos.
/// </summary>
public class ProductoPatchDtoTests
{
    #region Tests de Constructor

    /// <summary>
    /// Verifica que el constructor asigna todos los campos correctamente.
    /// </summary>
    [Test]
    public void Constructor_ConValoresAsignados_AsignaCorrectamente()
    {
        // Arrange & Act
        var dto = new ProductoPatchDto
        {
            Nombre = "Producto Actualizado",
            Descripcion = "Nueva descripción",
            Precio = 99.99m,
            Stock = 50,
            Imagen = "https://example.com/imagen.jpg"
        };

        // Assert
        dto.Nombre.Should().Be("Producto Actualizado");
        dto.Descripcion.Should().Be("Nueva descripción");
        dto.Precio.Should().Be(99.99m);
        dto.Stock.Should().Be(50);
        dto.Imagen.Should().Be("https://example.com/imagen.jpg");
    }

    /// <summary>
    /// Verifica que por defecto los campos tienen valores nulos o predeterminados.
    /// </summary>
    [Test]
    public void Constructor_PorDefecto_TieneValoresNulosOPredeterminados()
    {
        // Arrange & Act
        var dto = new ProductoPatchDto();

        // Assert
        dto.Nombre.Should().BeNull();
        dto.Descripcion.Should().BeNull();
        dto.Precio.Should().BeNull();
        dto.Stock.Should().BeNull();
        dto.Imagen.Should().BeNull();
    }

    #endregion

    #region Tests de Propiedades Individuales

    /// <summary>
    /// Verifica que Nombre puede ser asignado y puesto a null.
    /// </summary>
    [Test]
    public void Nombre_PuedeSerAsignadoANulo()
    {
        // Arrange & Act
        var dto = new ProductoPatchDto { Nombre = "Test" };

        // Assert
        dto.Nombre.Should().Be("Test");

        // Act - Crear nueva instancia con null
        var dto2 = new ProductoPatchDto { Nombre = null };

        // Assert
        dto2.Nombre.Should().BeNull();
    }

    /// <summary>
    /// Verifica que Descripcion puede ser asignada y puesta a null.
    /// </summary>
    [Test]
    public void Descripcion_PuedeSerAsignadaANula()
    {
        // Arrange & Act
        var dto = new ProductoPatchDto { Descripcion = "Descripción de prueba" };

        // Assert
        dto.Descripcion.Should().Be("Descripción de prueba");

        // Act - Crear nueva instancia con null
        var dto2 = new ProductoPatchDto { Descripcion = null };

        // Assert
        dto2.Descripcion.Should().BeNull();
    }

    /// <summary>
    /// Verifica que Precio puede ser asignado y puesto a null.
    /// </summary>
    [Test]
    public void Precio_PuedeSerAsignadoANulo()
    {
        // Arrange & Act
        var dto = new ProductoPatchDto { Precio = 19.99m };

        // Assert
        dto.Precio.Should().Be(19.99m);

        // Act - Crear nueva instancia con null
        var dto2 = new ProductoPatchDto { Precio = null };

        // Assert
        dto2.Precio.Should().BeNull();
    }

    /// <summary>
    /// Verifica que Stock puede ser asignado y puesto a null.
    /// </summary>
    [Test]
    public void Stock_PuedeSerAsignadoANulo()
    {
        // Arrange & Act
        var dto = new ProductoPatchDto { Stock = 100 };

        // Assert
        dto.Stock.Should().Be(100);

        // Act - Crear nueva instancia con null
        var dto2 = new ProductoPatchDto { Stock = null };

        // Assert
        dto2.Stock.Should().BeNull();
    }

    /// <summary>
    /// Verifica que Imagen puede ser asignada y puesta a null.
    /// </summary>
    [Test]
    public void Imagen_PuedeSerAsignadaANula()
    {
        // Arrange & Act
        var dto = new ProductoPatchDto { Imagen = "https://example.com/imagen.png" };

        // Assert
        dto.Imagen.Should().Be("https://example.com/imagen.png");

        // Act - Crear nueva instancia con null
        var dto2 = new ProductoPatchDto { Imagen = null };

        // Assert
        dto2.Imagen.Should().BeNull();
    }

    #endregion

    #region Tests de Inmutabilidad del Record

    /// <summary>
    /// Verifica que se puede crear una nueva instancia con cambios usando 'with'.
    /// </summary>
    [Test]
    public void With_PuedeCrearNuevaInstanciaConCambios()
    {
        // Arrange
        var original = new ProductoPatchDto
        {
            Nombre = "Original",
            Descripcion = "Descripción original"
        };

        // Act
        var modificado = original with { Nombre = "Modificado" };

        // Assert
        modificado.Nombre.Should().Be("Modificado");
        modificado.Descripcion.Should().Be("Descripción original");
        original.Nombre.Should().Be("Original");
    }

    #endregion

    #region Tests de Escenarios de Actualización Parcial

    /// <summary>
    /// Verifica que actualizar solo el nombre no modifica otros campos.
    /// </summary>
    [Test]
    public void Patch_ActualizaSoloNombre_NoModificaOtrosCampos()
    {
        // Arrange
        var dto = new ProductoPatchDto
        {
            Nombre = "Nombre actualizado",
            Precio = 10.00m,
            Stock = 5
        };

        // Simular que solo queremos actualizar el nombre
        var patch = new ProductoPatchDto { Nombre = "Nuevo nombre" };

        // Act & Assert
        patch.Nombre.Should().Be("Nuevo nombre");
        patch.Precio.Should().BeNull();
        patch.Stock.Should().BeNull();
    }

    /// <summary>
    /// Verifica que actualizar solo el stock no modifica el precio.
    /// </summary>
    [Test]
    public void Patch_ActualizaSoloStock_NoModificaPrecio()
    {
        // Arrange
        var patch = new ProductoPatchDto { Stock = 25 };

        // Assert
        patch.Stock.Should().Be(25);
        patch.Precio.Should().BeNull();
        patch.Nombre.Should().BeNull();
    }

    /// <summary>
    /// Verifica que actualizar solo el precio no modifica el stock.
    /// </summary>
    [Test]
    public void Patch_ActualizaSoloPrecio_NoModificaStock()
    {
        // Arrange
        var patch = new ProductoPatchDto { Precio = 49.99m };

        // Assert
        patch.Precio.Should().Be(49.99m);
        patch.Stock.Should().BeNull();
    }

    #endregion

    #region Tests de Casos Límite

    /// <summary>
    /// Verifica que un DTO vacío es válido para patch.
    /// </summary>
    [Test]
    public void DTO_Vacio_EsValidoParaPatch()
    {
        // Arrange
        var dto = new ProductoPatchDto();

        // Assert
        dto.Nombre.Should().BeNull();
        dto.Descripcion.Should().BeNull();
        dto.Precio.Should().BeNull();
        dto.Stock.Should().BeNull();
        dto.Imagen.Should().BeNull();
    }

    /// <summary>
    /// Verifica que un DTO con todos los campos null es válido.
    /// </summary>
    [Test]
    public void DTO_ConTodosLosCamposNull_EsValido()
    {
        // Arrange
        var dto = new ProductoPatchDto
        {
            Nombre = null,
            Descripcion = null,
            Precio = null,
            Stock = null,
            Imagen = null
        };

        // Assert
        dto.Nombre.Should().BeNull();
        dto.Precio.Should().BeNull();
    }

    #endregion
}

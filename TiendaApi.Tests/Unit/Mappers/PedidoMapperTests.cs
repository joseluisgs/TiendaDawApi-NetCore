using FluentAssertions;
using TiendaApi.Dtos.Pedidos;
using TiendaApi.Mappers;
using TiendaApi.Models;

namespace TiendaApi.Tests.Unit.Mappers;

/// <summary>
/// Tests unitarios para el mapeador de pedidos.
/// Prueba todas las conversiones entidad-DTO para el dominio de Pedido.
/// </summary>
public class PedidoMapperTests
{
    #region Pedido ToDto Tests

    [Test]
    public void ToDto_ConTodosLosCampos_MapeaCorrectamente()
    {
        // Arrange
        var pedido = new Pedido
        {
            Id = "order123",
            UserId = 100,
            Total = 299.99m,
            Estado = PedidoEstado.PENDIENTE,
            Items = new List<PedidoItem>
            {
                new() { ProductoId = 1, NombreProducto = "Product 1", Cantidad = 2, Precio = 50, Subtotal = 100 },
                new() { ProductoId = 2, NombreProducto = "Product 2", Cantidad = 1, Precio = 199.99m, Subtotal = 199.99m }
            },
            CreatedAt = new DateTime(2024, 1, 15, 10, 30, 0)
        };

        // Act
        var dto = pedido.ToDto();

        // Assert
        dto.Id.Should().Be("order123");
        dto.UserId.Should().Be(100);
        dto.Total.Should().Be(299.99m);
        dto.Estado.Should().Be(PedidoEstado.PENDIENTE);
        dto.Items.Should().HaveCount(2);
        dto.CreatedAt.Should().Be(pedido.CreatedAt);
    }

    [Test]
    public void ToDto_ConIdNulo_RetornaStringVacio()
    {
        // Arrange
        var pedido = new Pedido
        {
            Id = null,
            UserId = 1,
            Total = 100
        };

        // Act
        var dto = pedido.ToDto();

        // Assert
        dto.Id.Should().BeEmpty();
    }

    [Test]
    public void ToDto_ConEstadoPredeterminado_DebeSerPendiente()
    {
        // Arrange
        var pedido = new Pedido { Id = "123" };

        // Act
        var dto = pedido.ToDto();

        // Assert
        dto.Estado.Should().Be(PedidoEstado.PENDIENTE);
    }

    [Test]
    public void ToDto_ConItemsVacios_RetornaListaVacia()
    {
        // Arrange
        var pedido = new Pedido
        {
            Id = "123",
            Items = new List<PedidoItem>()
        };

        // Act
        var dto = pedido.ToDto();

        // Assert
        dto.Items.Should().NotBeNull();
        dto.Items.Should().BeEmpty();
    }

    [Test]
    public void ToDto_ConItemsNulos_RetornaListaVacia()
    {
        // Arrange
        var pedido = new Pedido
        {
            Id = "123",
            Items = null!
        };

        // Act
        var dto = pedido.ToDto();

        // Assert
        dto.Items.Should().NotBeNull();
        dto.Items.Should().BeEmpty();
    }

    [Test]
    public void ToDto_DebeCalcularSubtotalesDeItems()
    {
        // Arrange
        var pedido = new Pedido
        {
            Id = "123",
            Items = new List<PedidoItem>
            {
                new() { ProductoId = 1, Cantidad = 3, Precio = 25, Subtotal = 75 }
            }
        };

        // Act
        var dto = pedido.ToDto();

        // Assert
        dto.Items[0].Subtotal.Should().Be(75);
    }

    [Test]
    public void ToDto_ConTodosLosEstados_MapeaCorrectamente()
    {
        // Arrange
        var estados = new[] { PedidoEstado.PENDIENTE, PedidoEstado.PROCESANDO, PedidoEstado.ENVIADO, PedidoEstado.ENTREGADO, PedidoEstado.CANCELADO };

        foreach (var estado in estados)
        {
            var pedido = new Pedido { Id = "123", Estado = estado };

            // Act
            var dto = pedido.ToDto();

            // Assert
            dto.Estado.Should().Be(estado, $"El estado {estado} debe mapear correctamente");
        }
    }

    #endregion

    #region PedidoItem ToDto Tests

    [Test]
    public void PedidoItem_ToDto_DebeMapearTodosLosCampos()
    {
        // Arrange
        var item = new PedidoItem
        {
            ProductoId = 50,
            NombreProducto = "Special Product",
            Cantidad = 5,
            Precio = 29.99m,
            Subtotal = 149.95m
        };

        // Act
        var dto = item.ToDto();

        // Assert
        dto.ProductoId.Should().Be(50);
        dto.NombreProducto.Should().Be("Special Product");
        dto.Cantidad.Should().Be(5);
        dto.Precio.Should().Be(29.99m);
        dto.Subtotal.Should().Be(149.95m);
    }

    [Test]
    public void PedidoItem_ToDto_ConNombreVacio_RetornaVacio()
    {
        // Arrange
        var item = new PedidoItem
        {
            ProductoId = 1,
            NombreProducto = string.Empty,
            Cantidad = 1,
            Precio = 10
        };

        // Act
        var dto = item.ToDto();

        // Assert
        dto.NombreProducto.Should().BeEmpty();
    }

    #endregion

    #region ToEntity (PedidoRequestDto) Tests

    [Test]
    public void ToEntity_ConItems_MapeaCorrectamente()
    {
        // Arrange
        var dto = new PedidoRequestDto
        {
            Items = new List<PedidoItemRequestDto>
            {
                new() { ProductoId = 1, Cantidad = 2 },
                new() { ProductoId = 2, Cantidad = 3 }
            }
        };
        var userId = 100L;

        // Act
        var entity = dto.ToEntity(userId);

        // Assert
        entity.UserId.Should().Be(100);
        entity.Items.Should().HaveCount(2);
        entity.Estado.Should().Be("PENDIENTE");
    }

    [Test]
    public void ToEntity_DebeEstablecerEstadoPredeterminado()
    {
        // Arrange
        var dto = new PedidoRequestDto
        {
            Items = new List<PedidoItemRequestDto> { new() { ProductoId = 1, Cantidad = 1 } }
        };

        // Act
        var entity = dto.ToEntity(1);

        // Assert
        entity.Estado.Should().Be("PENDIENTE");
    }

    [Test]
    public void ToEntity_DebeEstablecerMarcasDeTiempo()
    {
        // Arrange
        var dto = new PedidoRequestDto
        {
            Items = new List<PedidoItemRequestDto> { new() { ProductoId = 1, Cantidad = 1 } }
        };
        var before = DateTime.UtcNow;

        // Act
        var entity = dto.ToEntity(1);
        var after = DateTime.UtcNow;

        // Assert
        entity.CreatedAt.Should().BeOnOrAfter(before);
        entity.CreatedAt.Should().BeOnOrBefore(after);
        entity.UpdatedAt.Should().BeOnOrAfter(before);
        entity.UpdatedAt.Should().BeOnOrBefore(after);
    }

    [Test]
    public void ToEntity_ConItemsVacios_CreaListaVacia()
    {
        // Arrange
        var dto = new PedidoRequestDto { Items = new List<PedidoItemRequestDto>() };

        // Act
        var entity = dto.ToEntity(1);

        // Assert
        entity.Items.Should().NotBeNull();
        entity.Items.Should().BeEmpty();
    }

    #endregion

    #region PedidoItemRequestDto ToEntity Tests

    [Test]
    public void PedidoItemRequestDto_ToEntity_ConPredeterminados_Mapea()
    {
        // Arrange
        var dto = new PedidoItemRequestDto
        {
            ProductoId = 10,
            Cantidad = 5
        };

        // Act
        var entity = dto.ToEntity("Default Product", 25.00m);

        // Assert
        entity.ProductoId.Should().Be(10);
        entity.Cantidad.Should().Be(5);
        entity.NombreProducto.Should().Be("Default Product");
        entity.Precio.Should().Be(25.00m);
        entity.Subtotal.Should().Be(125.00m); // 5 * 25
    }

    [Test]
    public void PedidoItemRequestDto_ToEntity_DebeCalcularSubtotal()
    {
        // Arrange
        var dto = new PedidoItemRequestDto
        {
            ProductoId = 1,
            Cantidad = 4
        };

        // Act
        var entity = dto.ToEntity("Product", 15.50m);

        // Assert
        entity.Subtotal.Should().Be(62.00m); // 4 * 15.50
    }

    [Test]
    public void PedidoItemRequestDto_ToEntity_ConNombreNulo_UsaVacio()
    {
        // Arrange
        var dto = new PedidoItemRequestDto { ProductoId = 1, Cantidad = 1 };

        // Act
        var entity = dto.ToEntity(null!, 10);

        // Assert
        entity.NombreProducto.Should().BeEmpty();
    }

    [Test]
    public void PedidoItemRequestDto_ToEntity_ConPrecioNulo_UsaCero()
    {
        // Arrange
        var dto = new PedidoItemRequestDto { ProductoId = 1, Cantidad = 1 };

        // Act
        var entity = dto.ToEntity("Product", null);

        // Assert
        entity.Precio.Should().Be(0);
        entity.Subtotal.Should().Be(0);
    }

    #endregion

    #region ToDtoList Tests

    [Test]
    public void ToDtoList_ConMultiplesPedidos_MapeaTodos()
    {
        // Arrange
        var pedidos = new List<Pedido>
        {
            new() { Id = "1", Total = 100 },
            new() { Id = "2", Total = 200 },
            new() { Id = "3", Total = 300 }
        };

        // Act
        var dtos = pedidos.ToDtoList().ToList();

        // Assert
        dtos.Should().HaveCount(3);
        dtos[0].Id.Should().Be("1");
        dtos[1].Id.Should().Be("2");
        dtos[2].Id.Should().Be("3");
    }

    [Test]
    public void ToDtoList_ConListaVacia_RetornaVacia()
    {
        // Arrange
        var pedidos = new List<Pedido>();

        // Act
        var dtos = pedidos.ToDtoList().ToList();

        // Assert
        dtos.Should().BeEmpty();
    }

    [Test]
    public void ToDtoList_DebePreservarOrden()
    {
        // Arrange
        var pedidos = new List<Pedido>
        {
            new() { Id = "third", Total = 300 },
            new() { Id = "first", Total = 100 },
            new() { Id = "second", Total = 200 }
        };

        // Act
        var dtos = pedidos.ToDtoList().ToList();

        // Assert
        dtos[0].Id.Should().Be("third");
        dtos[1].Id.Should().Be("first");
        dtos[2].Id.Should().Be("second");
    }

    #endregion

    #region Roundtrip Tests

    [Test]
    public void ToEntity_LuegoToDto_DebePreservarUserId()
    {
        // Arrange
        var dto = new PedidoRequestDto
        {
            Items = new List<PedidoItemRequestDto>
            {
                new() { ProductoId = 1, Cantidad = 2 }
            }
        };
        var userId = 42L;

        // Act
        var entity = dto.ToEntity(userId);
        var resultDto = entity.ToDto();

        // Assert
        resultDto.UserId.Should().Be(userId);
    }

    [Test]
    public void ToEntity_LuegoToDto_DebePreservarItems()
    {
        // Arrange
        var dto = new PedidoRequestDto
        {
            Items = new List<PedidoItemRequestDto>
            {
                new() { ProductoId = 1, Cantidad = 2 },
                new() { ProductoId = 2, Cantidad = 3 }
            }
        };

        // Act
        var entity = dto.ToEntity(1);
        var resultDto = entity.ToDto();

        // Assert
        resultDto.Items.Should().HaveCount(2);
    }

    #endregion

    #region Edge Cases Tests

    [Test]
    public void ToDto_ConTotalCero_MapeaCorrectamente()
    {
        // Arrange
        var pedido = new Pedido
        {
            Id = "123",
            Total = 0m
        };

        // Act
        var dto = pedido.ToDto();

        // Assert
        dto.Total.Should().Be(0m);
    }

    [Test]
    public void ToDto_ConTotalMuyGrande_MapeaCorrectamente()
    {
        // Arrange
        var pedido = new Pedido
        {
            Id = "123",
            Total = 9999999.99m
        };

        // Act
        var dto = pedido.ToDto();

        // Assert
        dto.Total.Should().Be(9999999.99m);
    }

    [Test]
    public void ToDto_ConMuchosItems_MapeaTodos()
    {
        // Arrange
        var items = Enumerable.Range(1, 100)
            .Select(i => new PedidoItem
            {
                ProductoId = i,
                Cantidad = i,
                Precio = i,
                Subtotal = i * i
            })
            .ToList();
        var pedido = new Pedido { Id = "123", Items = items };

        // Act
        var dto = pedido.ToDto();

        // Assert
        dto.Items.Should().HaveCount(100);
        dto.Items[99].ProductoId.Should().Be(100);
    }

    [Test]
    public void ToEntity_DebeManejarCantidadesGrandes()
    {
        // Arrange
        var dto = new PedidoRequestDto
        {
            Items = new List<PedidoItemRequestDto>
            {
                new() { ProductoId = 1, Cantidad = int.MaxValue }
            }
        };

        // Act
        var entity = dto.ToEntity(1);

        // Assert
        entity.Items.Should().HaveCount(1);
        entity.Items[0].Cantidad.Should().Be(int.MaxValue);
    }

    #endregion

    #region Null Safety Tests

    [Test]
    public void ToDto_ConPedidoNulos_LanzaExcepcion()
    {
        // Arrange
        Pedido? pedido = null;

        // Act & Assert
        Assert.Throws<NullReferenceException>(() => pedido!.ToDto());
    }

    [Test]
    public void ToDtoList_ConPedidosNulos_LanzaExcepcion()
    {
        // Arrange
        IEnumerable<Pedido>? pedidos = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => pedidos!.ToDtoList().ToList());
    }

    #endregion
}

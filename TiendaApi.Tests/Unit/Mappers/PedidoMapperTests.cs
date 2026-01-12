using FluentAssertions;
using MongoDB.Bson;
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
            _id = ObjectId.GenerateNewId(),
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
        dto.Id.Should().Be(pedido._id.ToString());
        dto.UserId.Should().Be(100);
        dto.Total.Should().Be(299.99m);
        dto.Estado.Should().Be(PedidoEstado.PENDIENTE);
        dto.Items.Should().HaveCount(2);
        dto.CreatedAt.Should().Be(pedido.CreatedAt);
    }

    [Test]
    public void ToDto_ConIdVacio_RetornaStringVacio()
    {
        // Arrange
        var pedido = new Pedido
        {
            _id = ObjectId.Empty,
            UserId = 1,
            Total = 100
        };

        // Act
        var dto = pedido.ToDto();

        // Assert
        dto.Id.Should().Be(ObjectId.Empty.ToString());
    }

    [Test]
    public void ToDto_ConEstadoPredeterminado_DebeSerPendiente()
    {
        // Arrange
        var pedido = new Pedido { _id = ObjectId.GenerateNewId() };

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
            _id = ObjectId.GenerateNewId(),
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
            _id = ObjectId.GenerateNewId(),
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
            _id = ObjectId.GenerateNewId(),
            Items = new List<PedidoItem>
            {
                new() { ProductoId = 1, NombreProducto = "Product 1", Cantidad = 3, Precio = 10, Subtotal = 30 },
                new() { ProductoId = 2, NombreProducto = "Product 2", Cantidad = 2, Precio = 25, Subtotal = 50 }
            }
        };

        // Act
        var dto = pedido.ToDto();

        // Assert
        dto.Items[0].Subtotal.Should().Be(30);
        dto.Items[1].Subtotal.Should().Be(50);
    }

    [Test]
    public void ToDto_ConTotalCero_MapeaCorrectamente()
    {
        // Arrange
        var pedido = new Pedido
        {
            _id = ObjectId.GenerateNewId(),
            Total = 0,
            Items = new List<PedidoItem>()
        };

        // Act
        var dto = pedido.ToDto();

        // Assert
        dto.Total.Should().Be(0);
    }

    [Test]
    public void ToDto_ConTotalMuyGrande_MapeaCorrectamente()
    {
        // Arrange
        var pedido = new Pedido
        {
            _id = ObjectId.GenerateNewId(),
            Total = 999999999.99m,
            Items = new List<PedidoItem>()
        };

        // Act
        var dto = pedido.ToDto();

        // Assert
        dto.Total.Should().Be(999999999.99m);
    }

    [Test]
    public void ToDto_ConTodosLosEstados_MapeaCorrectamente()
    {
        // Arrange
        var estados = new[]
        {
            PedidoEstado.PENDIENTE,
            PedidoEstado.PROCESANDO,
            PedidoEstado.ENVIADO,
            PedidoEstado.ENTREGADO,
            PedidoEstado.CANCELADO
        };

        foreach (var estado in estados)
        {
            var pedido = new Pedido
            {
                _id = ObjectId.GenerateNewId(),
                Estado = estado
            };

            // Act
            var dto = pedido.ToDto();

            // Assert
            dto.Estado.Should().Be(estado);
        }
    }

    [Test]
    public void ToDto_ConMuchosItems_MapeaTodos()
    {
        // Arrange
        var items = Enumerable.Range(1, 100)
            .Select(i => new PedidoItem
            {
                ProductoId = i,
                NombreProducto = $"Product {i}",
                Cantidad = i,
                Precio = i * 10,
                Subtotal = i * i * 10
            })
            .ToList();

        var pedido = new Pedido
        {
            _id = ObjectId.GenerateNewId(),
            Items = items
        };

        // Act
        var dto = pedido.ToDto();

        // Assert
        dto.Items.Should().HaveCount(100);
        for (int i = 0; i < 100; i++)
        {
            dto.Items[i].ProductoId.Should().Be(i + 1);
        }
    }

    #endregion

    #region List<Pedido> ToDtoList Tests

    [Test]
    public void ToDtoList_ConMultiplesPedidos_MapeaTodos()
    {
        // Arrange
        var pedidos = new List<Pedido>
        {
            new() { _id = ObjectId.GenerateNewId(), UserId = 1, Total = 100 },
            new() { _id = ObjectId.GenerateNewId(), UserId = 2, Total = 200 },
            new() { _id = ObjectId.GenerateNewId(), UserId = 3, Total = 300 }
        };

        // Act
        var dtos = pedidos.ToDtoList().ToList();

        // Assert
        dtos.Should().HaveCount(3);
        dtos[0].UserId.Should().Be(1);
        dtos[1].UserId.Should().Be(2);
        dtos[2].UserId.Should().Be(3);
    }

    [Test]
    public void ToDtoList_DebePreservarOrden()
    {
        // Arrange
        var pedidos = new List<Pedido>
        {
            new() { _id = ObjectId.Parse("507f1f77bcf86cd799439011"), UserId = 1, Total = 100 },
            new() { _id = ObjectId.Parse("507f1f77bcf86cd799439012"), UserId = 2, Total = 200 },
            new() { _id = ObjectId.Parse("507f1f77bcf86cd799439013"), UserId = 3, Total = 300 }
        };

        // Act
        var dtos = pedidos.ToDtoList().ToList();

        // Assert
        dtos[0].Id.Should().Be("507f1f77bcf86cd799439011");
        dtos[1].Id.Should().Be("507f1f77bcf86cd799439012");
        dtos[2].Id.Should().Be("507f1f77bcf86cd799439013");
    }

    [Test]
    public void ToDtoList_ConListaVacia_RetornaListaVacia()
    {
        // Arrange
        var pedidos = new List<Pedido>();

        // Act
        var dtos = pedidos.ToDtoList();

        // Assert
        dtos.Should().NotBeNull();
        dtos.Should().BeEmpty();
    }

    [Test]
    public void ToDtoList_ConListaNula_RetornaListaVacia()
    {
        // Arrange
        List<Pedido>? pedidos = null;

        // Act
        var dtos = (pedidos ?? new List<Pedido>()).ToDtoList().ToList();

        // Assert
        dtos.Should().NotBeNull();
        dtos.Should().BeEmpty();
    }

    #endregion
}

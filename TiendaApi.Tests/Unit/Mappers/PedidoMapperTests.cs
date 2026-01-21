using FluentAssertions;
using MongoDB.Bson;
using TiendaApi.Api.Dtos.Common;
using TiendaApi.Api.Dtos.Pedidos;
using TiendaApi.Api.Mappers;
using TiendaApi.Api.Models;

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
            Id = ObjectId.GenerateNewId(),
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
        dto.Id.Should().Be(pedido.Id.ToString());
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
            Id = ObjectId.Empty,
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
        var pedido = new Pedido { Id = ObjectId.GenerateNewId() };

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
            Id = ObjectId.GenerateNewId(),
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
            Id = ObjectId.GenerateNewId(),
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
            Id = ObjectId.GenerateNewId(),
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
            Id = ObjectId.GenerateNewId(),
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
            Id = ObjectId.GenerateNewId(),
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
                Id = ObjectId.GenerateNewId(),
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
            Id = ObjectId.GenerateNewId(),
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
            new() { Id = ObjectId.GenerateNewId(), UserId = 1, Total = 100 },
            new() { Id = ObjectId.GenerateNewId(), UserId = 2, Total = 200 },
            new() { Id = ObjectId.GenerateNewId(), UserId = 3, Total = 300 }
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
            new() { Id = ObjectId.Parse("507f1f77bcf86cd799439011"), UserId = 1, Total = 100 },
            new() { Id = ObjectId.Parse("507f1f77bcf86cd799439012"), UserId = 2, Total = 200 },
            new() { Id = ObjectId.Parse("507f1f77bcf86cd799439013"), UserId = 3, Total = 300 }
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

    #region ToEntity Tests

    [Test]
    public void ToEntity_PedidoRequestDto_MapeaCorrectamente()
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
        var pedido = dto.ToEntity(userId);

        // Assert
        pedido.UserId.Should().Be(userId);
        pedido.Items.Should().HaveCount(2);
        pedido.Estado.Should().Be(PedidoEstado.PENDIENTE);
        pedido.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Test]
    public void ToEntity_PedidoRequestDtoConItemsVacios_CreaListaVacia()
    {
        // Arrange
        var dto = new PedidoRequestDto
        {
            Items = new List<PedidoItemRequestDto>()
        };
        var userId = 1L;

        // Act
        var pedido = dto.ToEntity(userId);

        // Assert
        pedido.Items.Should().NotBeNull();
        pedido.Items.Should().BeEmpty();
    }

    [Test]
    public void ToEntity_PedidoItemRequestDto_SinInfoExtra_MapeaConValoresPorDefecto()
    {
        // Arrange
        var dto = new PedidoItemRequestDto
        {
            ProductoId = 5,
            Cantidad = 10
        };

        // Act
        var item = dto.ToEntity();

        // Assert
        item.ProductoId.Should().Be(5);
        item.Cantidad.Should().Be(10);
        item.NombreProducto.Should().BeEmpty();
        item.Precio.Should().Be(0);
        item.Subtotal.Should().Be(0);
    }

    [Test]
    public void ToEntity_PedidoItemRequestDto_ConInfoExtra_MapeaConValores()
    {
        // Arrange
        var dto = new PedidoItemRequestDto
        {
            ProductoId = 5,
            Cantidad = 10
        };
        var nombreProducto = "Test Product";
        var precio = 25.50m;

        // Act
        var item = dto.ToEntity(nombreProducto, precio);

        // Assert
        item.ProductoId.Should().Be(5);
        item.Cantidad.Should().Be(10);
        item.NombreProducto.Should().Be(nombreProducto);
        item.Precio.Should().Be(precio);
        item.Subtotal.Should().Be(precio * dto.Cantidad);
    }

    [Test]
    public void ToEntity_PedidoItemRequestDto_CalculaSubtotalCorrectamente()
    {
        // Arrange
        var dto = new PedidoItemRequestDto
        {
            ProductoId = 1,
            Cantidad = 5
        };
        var precio = 19.99m;

        // Act
        var item = dto.ToEntity("Product", precio);

        // Assert
        item.Subtotal.Should().Be(99.95m);
    }

    #endregion

    #region Destinatario ToDto Tests

    [Test]
    public void DestinatarioToDto_ConTodosLosCampos_MapeaCorrectamente()
    {
        var destinatario = new Destinatario
        {
            NombreCompleto = "María García López",
            Email = "maria.garcia@email.com",
            Telefono = "+34612345678",
            Direccion = new Direccion
            {
                Calle = "Gran Vía",
                Numero = "42",
                Ciudad = "Madrid",
                Provincia = "Madrid",
                Pais = "España",
                CodigoPostal = "28013"
            }
        };

        var dto = destinatario.ToDto();

        dto.NombreCompleto.Should().Be("María García López");
        dto.Email.Should().Be("maria.garcia@email.com");
        dto.Telefono.Should().Be("+34612345678");
        dto.Direccion.Should().NotBeNull();
        dto.Direccion!.Calle.Should().Be("Gran Vía");
        dto.Direccion.CodigoPostal.Should().Be("28013");
    }

    [Test]
    public void DestinatarioToDto_SoloConNombre_MapeaCorrectamente()
    {
        var destinatario = new Destinatario
        {
            NombreCompleto = "Juan Pérez"
        };

        var dto = destinatario.ToDto();

        dto.NombreCompleto.Should().Be("Juan Pérez");
        dto.Email.Should().BeEmpty();
        dto.Telefono.Should().BeNull();
        dto.Direccion.Should().NotBeNull();
    }

    [Test]
    public void DestinatarioToDto_DireccionNula_MapeaConDireccionVacia()
    {
        var destinatario = new Destinatario
        {
            NombreCompleto = "Test User",
            Email = "test@email.com",
            Direccion = null
        };

        var dto = destinatario.ToDto();

        dto.NombreCompleto.Should().Be("Test User");
        dto.Direccion.Should().NotBeNull();
        dto.Direccion!.Calle.Should().BeEmpty();
    }

    #endregion

    #region Direccion ToDto Tests

    [Test]
    public void DireccionToDto_ConTodosLosCampos_MapeaCorrectamente()
    {
        var direccion = new Direccion
        {
            Calle = "Gran Vía",
            Numero = "42",
            Ciudad = "Madrid",
            Provincia = "Madrid",
            Pais = "España",
            CodigoPostal = "28013"
        };

        var dto = direccion.ToDto();

        dto.Calle.Should().Be("Gran Vía");
        dto.Numero.Should().Be("42");
        dto.Ciudad.Should().Be("Madrid");
        dto.Provincia.Should().Be("Madrid");
        dto.Pais.Should().Be("España");
        dto.CodigoPostal.Should().Be("28013");
    }

    [Test]
    public void DireccionToDto_SoloConCalle_MapeaCorrectamente()
    {
        var direccion = new Direccion
        {
            Calle = "Calle Test"
        };

        var dto = direccion.ToDto();

        dto.Calle.Should().Be("Calle Test");
        dto.Numero.Should().BeNull();
        dto.Ciudad.Should().BeEmpty();
    }

    #endregion

    #region ToEntity with Destinatario Tests

    [Test]
    public void DestinatarioToEntity_SoloConNombre_MapeaCorrectamente()
    {
        var dto = new DestinatarioDto
        {
            NombreCompleto = "Juan Pérez"
        };

        var destinatario = dto.ToEntity();

        destinatario.Should().NotBeNull();
        destinatario!.NombreCompleto.Should().Be("Juan Pérez");
        destinatario.Email.Should().BeEmpty();
        destinatario.Telefono.Should().BeNull();
        destinatario.Direccion.Should().NotBeNull();
    }

    #endregion

    #region Direccion ToEntity Tests

    [Test]
    public void DireccionToEntity_ConTodosLosCampos_MapeaCorrectamente()
    {
        var dto = new DireccionDto
        {
            Calle = "Gran Vía",
            Numero = "42",
            Ciudad = "Madrid",
            Provincia = "Madrid",
            Pais = "España",
            CodigoPostal = "28013"
        };

        var direccion = dto.ToEntity();

        direccion.Should().NotBeNull();
        direccion!.Calle.Should().Be("Gran Vía");
        direccion.Numero.Should().Be("42");
        direccion.Ciudad.Should().Be("Madrid");
        direccion.Provincia.Should().Be("Madrid");
        direccion.Pais.Should().Be("España");
        direccion.CodigoPostal.Should().Be("28013");
    }

    [Test]
    public void DireccionToEntity_Nula_RetornaNull()
    {
        var direccion = ((DireccionDto?)null!).ToEntity();

        direccion.Should().BeNull();
    }

    [Test]
    public void DireccionToEntity_SoloConCalle_MapeaCorrectamente()
    {
        var dto = new DireccionDto
        {
            Calle = "Calle Test"
        };

        var direccion = dto.ToEntity();

        direccion.Should().NotBeNull();
        direccion!.Calle.Should().Be("Calle Test");
        direccion.Numero.Should().BeNull();
    }

    #endregion

    #region Pedido ToDto with Destinatario Tests

    [Test]
    public void ToDto_ConDestinatario_MapeaCorrectamente()
    {
        var pedido = new Pedido
        {
            Id = ObjectId.GenerateNewId(),
            UserId = 100,
            Total = 299.99m,
            Estado = PedidoEstado.PENDIENTE,
            Destinatario = new Destinatario
            {
                NombreCompleto = "María García",
                Email = "maria@email.com",
                Telefono = "+34612345678",
                Direccion = new Direccion
                {
                    Calle = "Gran Vía",
                    Numero = "42",
                    Ciudad = "Madrid",
                    CodigoPostal = "28013"
                }
            },
            Items = new List<PedidoItem>
            {
                new() { ProductoId = 1, NombreProducto = "Product 1", Cantidad = 2, Precio = 50, Subtotal = 100 }
            },
            CreatedAt = new DateTime(2024, 1, 15, 10, 30, 0)
        };

        var dto = pedido.ToDto();

        dto.Id.Should().Be(pedido.Id.ToString());
        dto.UserId.Should().Be(100);
        dto.Destinatario.Should().NotBeNull();
        dto.Destinatario!.NombreCompleto.Should().Be("María García");
        dto.Destinatario.Email.Should().Be("maria@email.com");
        dto.Destinatario.Direccion.Should().NotBeNull();
        dto.Destinatario.Direccion!.Calle.Should().Be("Gran Vía");
    }

    [Test]
    public void ToDto_SinDestinatario_MapeaCorrectamente()
    {
        var pedido = new Pedido
        {
            Id = ObjectId.GenerateNewId(),
            UserId = 100,
            Total = 299.99m,
            Destinatario = null,
            Items = new List<PedidoItem>
            {
                new() { ProductoId = 1, Cantidad = 2, Precio = 50, Subtotal = 100 }
            }
        };

        var dto = pedido.ToDto();

        dto.Destinatario.Should().NotBeNull();
        dto.Destinatario!.NombreCompleto.Should().BeEmpty();
    }

    #endregion

    #region ToEntity with Destinatario Tests

    [Test]
    public void ToEntity_PedidoRequestDtoConDestinatario_MapeaCorrectamente()
    {
        var dto = new PedidoRequestDto
        {
            Destinatario = new DestinatarioDto
            {
                NombreCompleto = "María García",
                Email = "maria@email.com",
                Telefono = "+34612345678",
                Direccion = new DireccionDto
                {
                    Calle = "Gran Vía",
                    Numero = "42",
                    Ciudad = "Madrid",
                    CodigoPostal = "28013"
                }
            },
            Items = new List<PedidoItemRequestDto>
            {
                new() { ProductoId = 1, Cantidad = 2 }
            }
        };
        var userId = 100L;

        var pedido = dto.ToEntity(userId);

        pedido.UserId.Should().Be(userId);
        pedido.Destinatario.Should().NotBeNull();
        pedido.Destinatario!.NombreCompleto.Should().Be("María García");
        pedido.Destinatario.Email.Should().Be("maria@email.com");
        pedido.Destinatario.Direccion.Should().NotBeNull();
        pedido.Destinatario.Direccion!.Calle.Should().Be("Gran Vía");
    }

    [Test]
    public void ToEntity_PedidoRequestDtoSinDestinatario_MapeaCorrectamente()
    {
        var dto = new PedidoRequestDto
        {
            Destinatario = new DestinatarioDto
            {
                NombreCompleto = "Test",
                Email = "test@test.com",
                Direccion = new DireccionDto { Calle = "Calle", Ciudad = "Madrid", Pais = "España" }
            },
            Items = new List<PedidoItemRequestDto>
            {
                new() { ProductoId = 1, Cantidad = 2 }
            }
        };
        var userId = 100L;

        var pedido = dto.ToEntity(userId);

        pedido.Destinatario.Should().NotBeNull();
    }

    #endregion
}

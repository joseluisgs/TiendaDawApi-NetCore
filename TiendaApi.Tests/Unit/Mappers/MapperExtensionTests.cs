using FluentAssertions;
using MongoDB.Bson;
using TiendaApi.Api.Dtos.Categorias;
using TiendaApi.Api.Dtos.Pedidos;
using TiendaApi.Api.Dtos.Productos;
using TiendaApi.Api.Dtos.Usuarios;
using TiendaApi.Api.Mappers;
using TiendaApi.Api.Models;

namespace TiendaApi.Tests.Unit.Mappers;

/// <summary>
/// Tests para métodos de extensión de mapeo
/// Asegura que los métodos de extensión funcionen correctamente para conversiones entidad-DTO
/// </summary>
public class MapperExtensionTests
{
    #region CategoriaMapper Tests

    [Test]
    public void CategoriaMapper_ToDto_DebeMapearTodosLosCampos()
    {
        // Arrange
        var categoria = new Categoria
        {
            Id = 1,
            Nombre = "Electronics",
            CreatedAt = DateTime.UtcNow
        };

        // Act
        var dto = categoria.ToDto();

        // Assert
        dto.Id.Should().Be(1);
        dto.Nombre.Should().Be("Electronics");
        dto.CreatedAt.Should().Be(categoria.CreatedAt);
    }

    [Test]
    public void CategoriaMapper_ToEntity_DebeMapearTodosLosCampos()
    {
        // Arrange
        var dto = new CategoriaRequestDto
        {
            Nombre = "Books"
        };

        // Act
        var entity = dto.ToEntity();

        // Assert
        entity.Nombre.Should().Be("Books");
        entity.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Test]
    public void CategoriaMapper_ToDtoList_DebeMapearMultiplesCategorias()
    {
        // Arrange
        var categorias = new List<Categoria>
        {
            new() { Id = 1, Nombre = "Cat1" },
            new() { Id = 2, Nombre = "Cat2" }
        };

        // Act
        var dtos = categorias.ToDtoList().ToList();

        // Assert
        dtos.Should().HaveCount(2);
        dtos[0].Id.Should().Be(1);
        dtos[0].Nombre.Should().Be("Cat1");
        dtos[1].Id.Should().Be(2);
        dtos[1].Nombre.Should().Be("Cat2");
    }

    [Test]
    public void CategoriaMapper_UpdateEntity_DebeActualizarNombre()
    {
        // Arrange
        var categoria = new Categoria { Id = 1, Nombre = "OldName" };
        var dto = new CategoriaRequestDto { Nombre = "NewName" };

        // Act
        dto.UpdateEntity(categoria);

        // Assert
        categoria.Nombre.Should().Be("NewName");
        categoria.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    #endregion

    #region ProductoMapper Tests

    [Test]
    public void ProductoMapper_ToDto_DebeMapearTodosLosCampos()
    {
        // Arrange
        var producto = new Producto
        {
            Id = 1,
            Nombre = "Laptop",
            Descripcion = "Gaming laptop",
            Precio = 1500.00m,
            Stock = 10,
            Imagen = "laptop.jpg",
            CategoriaId = 1,
            Categoria = new Categoria { Id = 1, Nombre = "Electronics" },
            CreatedAt = DateTime.UtcNow
        };

        // Act
        var dto = producto.ToDto();

        // Assert
        dto.Id.Should().Be(1);
        dto.Nombre.Should().Be("Laptop");
        dto.Descripcion.Should().Be("Gaming laptop");
        dto.Precio.Should().Be(1500.00m);
        dto.Stock.Should().Be(10);
        dto.Imagen.Should().Be("laptop.jpg");
        dto.CategoriaId.Should().Be(1);
        dto.CategoriaNombre.Should().Be("Electronics");
    }

    [Test]
    public void ProductoMapper_ToDto_DebeManejarCategoriaNula()
    {
        // Arrange
        var producto = new Producto
        {
            Id = 1,
            Nombre = "Product",
            Categoria = null!
        };

        // Act
        var dto = producto.ToDto();

        // Assert
        dto.CategoriaNombre.Should().BeEmpty();
    }

    [Test]
    public void ProductoMapper_ToEntity_DebeMapearTodosLosCampos()
    {
        // Arrange
        var dto = new ProductoRequestDto
        {
            Nombre = "Phone",
            Descripcion = "Smartphone",
            Precio = 999.99m,
            Stock = 50,
            Imagen = "phone.jpg",
            CategoriaId = 2
        };

        // Act
        var entity = dto.ToEntity();

        // Assert
        entity.Nombre.Should().Be("Phone");
        entity.Descripcion.Should().Be("Smartphone");
        entity.Precio.Should().Be(999.99m);
        entity.Stock.Should().Be(50);
        entity.Imagen.Should().Be("phone.jpg");
        entity.CategoriaId.Should().Be(2);
    }

    [Test]
    public void ProductoMapper_ToDtoList_DebeMapearMultiplesProductos()
    {
        // Arrange
        var productos = new List<Producto>
        {
            new() { Id = 1, Nombre = "P1" },
            new() { Id = 2, Nombre = "P2" }
        };

        // Act
        var dtos = productos.ToDtoList().ToList();

        // Assert
        dtos.Should().HaveCount(2);
        dtos[0].Nombre.Should().Be("P1");
        dtos[1].Nombre.Should().Be("P2");
    }

    #endregion

    #region UserMapper Tests

    [Test]
    public void UserMapper_ToDto_DebeMapearTodosLosCampos()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Username = "johndoe",
            Email = "john@example.com",
            Role = UserRoles.USER,
            CreatedAt = DateTime.UtcNow
        };

        // Act
        var dto = user.ToDto();

        // Assert
        dto.Id.Should().Be(1);
        dto.Username.Should().Be("johndoe");
        dto.Email.Should().Be("john@example.com");
        dto.Role.Should().Be(UserRoles.USER);
    }

    [Test]
    public void UserMapper_ToEntity_DebeMapearTodosLosCampos()
    {
        // Arrange
        var dto = new RegisterDto
        {
            Username = "janedoe",
            Email = "jane@example.com",
            Password = "password123"
        };
        var passwordHash = "hashedPassword";

        // Act
        var entity = dto.ToEntity(passwordHash);

        // Assert
        entity.Username.Should().Be("janedoe");
        entity.Email.Should().Be("jane@example.com");
        entity.PasswordHash.Should().Be("hashedPassword");
        entity.Role.Should().Be(UserRoles.USER);
        entity.IsDeleted.Should().BeFalse();
    }

    [Test]
    public void UserMapper_ToDtoList_DebeMapearMultiplesUsuarios()
    {
        // Arrange
        var users = new List<User>
        {
            new() { Id = 1, Username = "user1" },
            new() { Id = 2, Username = "user2" }
        };

        // Act
        var dtos = users.ToDtoList().ToList();

        // Assert
        dtos.Should().HaveCount(2);
        dtos[0].Username.Should().Be("user1");
        dtos[1].Username.Should().Be("user2");
    }

    #endregion

    #region PedidoMapper Tests

    [Test]
    public void PedidoMapper_ToDto_DebeMapearTodosLosCampos()
    {
        // Arrange
        var pedidoId = ObjectId.GenerateNewId();
        var pedido = new Pedido
        {
            Id = pedidoId,
            UserId = 1,
            Total = 150.00m,
            Estado = PedidoEstado.PENDIENTE,
            Items = new List<PedidoItem>
            {
                new() { ProductoId = 1, NombreProducto = "Product1", Cantidad = 2, Precio = 50, Subtotal = 100 },
                new() { ProductoId = 2, NombreProducto = "Product2", Cantidad = 1, Precio = 50, Subtotal = 50 }
            },
            CreatedAt = DateTime.UtcNow
        };

        // Act
        var dto = pedido.ToDto();

        // Assert
        dto.Id.Should().Be(pedidoId.ToString());
        dto.UserId.Should().Be(1);
        dto.Total.Should().Be(150.00m);
        dto.Estado.Should().Be(PedidoEstado.PENDIENTE);
        dto.Items.Should().HaveCount(2);
        dto.Items[0].NombreProducto.Should().Be("Product1");
        dto.Items[0].Subtotal.Should().Be(100);
    }

    [Test]
    public void PedidoMapper_ToDto_DebeManejarItemsVacios()
    {
        // Arrange
        var pedidoId = ObjectId.GenerateNewId();
        var pedido = new Pedido
        {
            Id = pedidoId,
            Items = new List<PedidoItem>()
        };

        // Act
        var dto = pedido.ToDto();

        // Assert
        dto.Items.Should().NotBeNull();
        dto.Items.Should().BeEmpty();
        dto.Id.Should().Be(pedidoId.ToString());
    }

    [Test]
    public void PedidoMapper_ToEntity_DebeMapearItems()
    {
        // Arrange
        var dto = new PedidoRequestDto
        {
            Items = new List<PedidoItemRequestDto>
            {
                new() { ProductoId = 1, Cantidad = 2 },
                new() { ProductoId = 2, Cantidad = 1 }
            }
        };
        var userId = 1L;

        // Act
        var entity = dto.ToEntity(userId);

        // Assert
        entity.UserId.Should().Be(1);
        entity.Items.Should().HaveCount(2);
        entity.Estado.Should().Be("PENDIENTE");
    }

    [Test]
    public void PedidoMapper_ToEntity_DebeManejarItemsVacios()
    {
        // Arrange
        var dto = new PedidoRequestDto
        {
            Items = new List<PedidoItemRequestDto>()
        };
        var userId = 1L;

        // Act
        var entity = dto.ToEntity(userId);

        // Assert
        entity.UserId.Should().Be(1);
        entity.Items.Should().BeEmpty();
        entity.Estado.Should().Be("PENDIENTE");
    }

    [Test]
    public void PedidoMapper_ToDtoList_DebeMapearMultiplesPedidos()
    {
        // Arrange
        var pedidoId1 = ObjectId.GenerateNewId();
        var pedidoId2 = ObjectId.GenerateNewId();
        var pedidos = new List<Pedido>
        {
            new() { Id = pedidoId1, Total = 100 },
            new() { Id = pedidoId2, Total = 200 }
        };

        // Act
        var dtos = pedidos.ToDtoList().ToList();

        // Assert
        dtos.Should().HaveCount(2);
        dtos[0].Id.Should().Be(pedidoId1.ToString());
        dtos[1].Id.Should().Be(pedidoId2.ToString());
    }

    #endregion

    #region PedidoItemMapper Tests

    [Test]
    public void PedidoItemMapper_ToDto_DebeCalcularSubtotal()
    {
        // Arrange
        var item = new PedidoItem
        {
            ProductoId = 1,
            NombreProducto = "Product",
            Cantidad = 3,
            Precio = 25.00m,
            Subtotal = 75.00m
        };

        // Act
        var dto = item.ToDto();

        // Assert
        dto.ProductoId.Should().Be(1);
        dto.NombreProducto.Should().Be("Product");
        dto.Cantidad.Should().Be(3);
        dto.Precio.Should().Be(25.00m);
        dto.Subtotal.Should().Be(75.00m);
    }

    [Test]
    public void PedidoItemMapper_ToEntity_DebeCalcularSubtotal()
    {
        // Arrange
        var dto = new PedidoItemRequestDto
        {
            ProductoId = 1,
            Cantidad = 4
        };

        // Act
        var entity = dto.ToEntity("Product", 15.00m);

        // Assert
        entity.ProductoId.Should().Be(1);
        entity.NombreProducto.Should().Be("Product");
        entity.Cantidad.Should().Be(4);
        entity.Precio.Should().Be(15.00m);
        entity.Subtotal.Should().Be(60.00m);
    }

    #endregion
}

using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using TiendaApi.Api.Models;

namespace TiendaApi.Api.Data.Seed.Mongo;

/// <summary>
/// Seeder de pedidos de ejemplo contra MongoDB (driver nativo).
/// Inserta pedidos de muestra solo si la colección está vacía.
/// </summary>
public class MongoDbSeeder
{
    private readonly IMongoCollection<Pedido> _pedidosCollection;
    private readonly ILogger<MongoDbSeeder> _logger;

    /// <summary>
    /// Crea una instancia del seeder de MongoDB.
    /// </summary>
    /// <param name="pedidosCollection">Colección de pedidos de MongoDB.</param>
    /// <param name="logger">Logger de la instancia.</param>
    public MongoDbSeeder(IMongoCollection<Pedido> pedidosCollection, ILogger<MongoDbSeeder> logger)
    {
        _pedidosCollection = pedidosCollection;
        _logger = logger;
    }

    /// <summary>
    /// Siembra los pedidos de ejemplo si la colección está vacía.
    /// </summary>
    public async Task SeedAsync()
    {
        try
        {
            var count = await _pedidosCollection.CountDocumentsAsync(_ => true);

            if (count == 0)
            {
                _logger.LogInformation("Sembrando datos iniciales de pedidos en MongoDB...");

                await SeedPedidosAsync();

                _logger.LogInformation("Datos de pedidos sembrados correctamente");
            }
            else
            {
                _logger.LogInformation("MongoDB ya contiene pedidos, omitiendo sembrado");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error al sembrar datos en MongoDB (puede que el servicio no esté disponible)");
        }
    }

    private async Task SeedPedidosAsync()
    {
        var pedidos = new List<Pedido>
        {
            new()
            {
                Id = ObjectId.GenerateNewId(),
                UserId = 2,
                Items = new List<PedidoItem>
                {
                    new()
                    {
                        ProductoId = 1,
                        NombreProducto = "Laptop Dell XPS 15",
                        Cantidad = 1,
                        Precio = 1299.99m,
                        Subtotal = 1299.99m
                    },
                    new()
                    {
                        ProductoId = 3,
                        NombreProducto = "Clean Code",
                        Cantidad = 2,
                        Precio = 42.99m,
                        Subtotal = 85.98m
                    }
                },
                Total = 1385.97m,
                Estado = PedidoEstado.ENTREGADO,
                DireccionEnvio = "Calle Falsa 123, Madrid",
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow.AddDays(-5),
                UpdatedAt = DateTime.UtcNow.AddDays(-2)
            },
            new()
            {
                Id = ObjectId.GenerateNewId(),
                UserId = 2,
                Items = new List<PedidoItem>
                {
                    new()
                    {
                        ProductoId = 2,
                        NombreProducto = "Camiseta Nike",
                        Cantidad = 3,
                        Precio = 29.99m,
                        Subtotal = 89.97m
                    }
                },
                Total = 89.97m,
                Estado = PedidoEstado.PROCESANDO,
                DireccionEnvio = "Calle Falsa 123, Madrid",
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                UpdatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = ObjectId.GenerateNewId(),
                UserId = 2,
                Items = new List<PedidoItem>
                {
                    new()
                    {
                        ProductoId = 1,
                        NombreProducto = "Laptop Dell XPS 15",
                        Cantidad = 1,
                        Precio = 1299.99m,
                        Subtotal = 1299.99m
                    }
                },
                Total = 1299.99m,
                Estado = PedidoEstado.PENDIENTE,
                DireccionEnvio = "Avenida Principal 456, Barcelona",
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        await _pedidosCollection.InsertManyAsync(pedidos);
        _logger.LogInformation("Insertados {Count} pedidos de ejemplo", pedidos.Count);
    }
}

/// <summary>
/// Seeder de pedidos de ejemplo usando EF Core sobre MongoDB.
/// Inserta los mismos pedidos de muestra que <see cref="MongoDbSeeder"/> pero vía contexto EF.
/// </summary>
public class MongoDbEfCoreSeeder
{
    private readonly TiendaMongoContext _context;
    private readonly ILogger<MongoDbEfCoreSeeder> _logger;

    /// <summary>
    /// Crea una instancia del seeder de MongoDB con EF Core.
    /// </summary>
    /// <param name="context">Contexto de EF Core para MongoDB.</param>
    /// <param name="logger">Logger de la instancia.</param>
    public MongoDbEfCoreSeeder(TiendaMongoContext context, ILogger<MongoDbEfCoreSeeder> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Siembra los pedidos de ejemplo si la colección está vacía.
    /// </summary>
    public async Task SeedAsync()
    {
        try
        {
            var count = _context.Pedidos.Count();

            if (count == 0)
            {
                _logger.LogInformation("Sembrando datos iniciales de pedidos en MongoDB (EfCore)...");

                var pedidos = new List<Pedido>
                {
                    new()
                    {
                        Id = ObjectId.GenerateNewId(),
                        UserId = 2,
                        Items = new List<PedidoItem>
                        {
                            new() { ProductoId = 1, NombreProducto = "Laptop Dell XPS 15", Cantidad = 1, Precio = 1299.99m, Subtotal = 1299.99m },
                            new() { ProductoId = 3, NombreProducto = "Clean Code", Cantidad = 2, Precio = 42.99m, Subtotal = 85.98m }
                        },
                        Total = 1385.97m,
                        Estado = PedidoEstado.ENTREGADO,
                        DireccionEnvio = "Calle Falsa 123, Madrid",
                        IsDeleted = false,
                        CreatedAt = DateTime.UtcNow.AddDays(-5),
                        UpdatedAt = DateTime.UtcNow.AddDays(-2)
                    },
                    new()
                    {
                        Id = ObjectId.GenerateNewId(),
                        UserId = 2,
                        Items = new List<PedidoItem>
                        {
                            new() { ProductoId = 2, NombreProducto = "Camiseta Nike", Cantidad = 3, Precio = 29.99m, Subtotal = 89.97m }
                        },
                        Total = 89.97m,
                        Estado = PedidoEstado.PROCESANDO,
                        DireccionEnvio = "Calle Falsa 123, Madrid",
                        IsDeleted = false,
                        CreatedAt = DateTime.UtcNow.AddDays(-1),
                        UpdatedAt = DateTime.UtcNow
                    },
                    new()
                    {
                        Id = ObjectId.GenerateNewId(),
                        UserId = 2,
                        Items = new List<PedidoItem>
                        {
                            new() { ProductoId = 1, NombreProducto = "Laptop Dell XPS 15", Cantidad = 1, Precio = 1299.99m, Subtotal = 1299.99m }
                        },
                        Total = 1299.99m,
                        Estado = PedidoEstado.PENDIENTE,
                        DireccionEnvio = "Avenida Principal 456, Barcelona",
                        IsDeleted = false,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    }
                };

                _context.Pedidos.AddRange(pedidos);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Insertados {Count} pedidos de ejemplo", pedidos.Count);
            }
            else
            {
                _logger.LogInformation("MongoDB ya contiene pedidos, omitiendo sembrado");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error al sembrar datos en MongoDB EfCore");
        }
    }
}

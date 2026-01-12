# 04. MongoDB EF Core Provider

MongoDB es una base de datos NoSQL orientada a documentos. El nuevo MongoDB Entity Framework Core Provider permite usar EF Core con MongoDB.

---

## 1. Por Que MongoDB para Pedidos

Los pedidos son documentos ideales para MongoDB:
- Estructura variable con items embebidos
- Consultas rapidas por usuario
- Documents sin joins costosos
- Escalabilidad horizontal

```mermaid
flowchart LR
    subgraph "PostgreSQL - Datos Relacionales"
        PG1[Categorias]
        PG2[Productos]
        PG3[Users]
    end
    
    subgraph "MongoDB - Documentos"
        MONGO1[Pedido 1 Items embebidos]
        MONGO2[Pedido 2 Items embebidos]
    end
    
    PG2 --> MONGO1
    PG3 --> MONGO1
 ```

### Documentos Embebidos en MongoDB

```mermaid
classDiagram
    class Pedido {
        +ObjectId _id
        +string Id
        +long UserId
        +List~PedidoItem~ Items
        +decimal Total
        +string Estado
        +DateTime CreatedAt
        +DateTime UpdatedAt
    }
    
    class PedidoItem {
        +long ProductoId
        +string NombreProducto
        +int Cantidad
        +decimal Precio
        +decimal Subtotal
    }
    
    Pedido "1" --> "*" PedidoItem : items embebidos
```

---

## 2. Instalacion

```bash
dotnet add package MongoDB.EntityFrameworkCore
dotnet add package MongoDB.Bson
```

---

## 3. Modelo Pedido

```csharp
using System.ComponentModel.DataAnnotations;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.EntityFrameworkCore.Extensions;

namespace TiendaApi.Apis.Models;

public class Pedido
{
    [BsonId]
    public ObjectId _id { get; set; }

    [BsonIgnore]
    public string Id => _id.ToString();

    public long UserId { get; set; }
    public List<PedidoItem> Items { get; set; } = new();
    public decimal Total { get; set; }
    
    [MaxLength(50)]
    public string Estado { get; set; } = "PENDIENTE";
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class PedidoItem
{
    public long ProductoId { get; set; }
    public string NombreProducto { get; set; } = "";
    public int Cantidad { get; set; }
    public decimal Precio { get; set; }
    public decimal Subtotal { get; set; }
}
```

---

## 4. DbContext Unificado

```csharp
using Microsoft.EntityFrameworkCore;
using MongoDB.EntityFrameworkCore.Extensions;
using TiendaApi.Apis.Models;

namespace TiendaApi.Apis.Data;

public class TiendaDbContext : DbContext
{
    public DbSet<Categoria> Categorias { get; set; } = null!;
    public DbSet<Producto> Productos { get; set; } = null!;
    public DbSet<User> Users { get; set; } = null!;
    public DbSet<Pedido> Pedidos { get; set; } = null!;

    public TiendaDbContext(DbContextOptions<TiendaDbContext> options) 
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // PostgreSQL
        modelBuilder.Entity<Categoria>(e => e.ToTable("categorias"));
        modelBuilder.Entity<Producto>(e => e.ToTable("productos"));

        // MongoDB
        modelBuilder.Entity<Pedido>(e =>
        {
            e.ToCollection("pedidos");
            e.HasKey(p => p.Id);
        });
    }
}
```

---

## 5. Configuracion

```csharp
using MongoDB.Driver;

var builder = WebApplication.CreateBuilder(args);

var mongoConnectionString = builder.Configuration.GetConnectionString("MongoDB")
    ?? "mongodb://admin:admin123@localhost:27017/tienda?authSource=admin";

var mongoClient = new MongoClient(mongoConnectionString);

builder.Services.AddDbContext<TiendaDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default"));
    options.UseMongoDB(mongoClient, "tienda");
});
```

---

## 6. Repository

```csharp
public class PedidosRepository(
    TiendaDbContext context,
    ILogger<PedidosRepository> logger
) : IPedidosRepository {

    public async Task<IEnumerable<Pedido>> FindAllAsync()
    {
        return await context.Pedidos
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Pedido>> FindByUserIdAsync(long userId)
    {
        return await context.Pedidos
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    public async Task<Pedido?> FindByIdAsync(string id)
    {
        return await context.Pedidos
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<Pedido> SaveAsync(Pedido pedido)
    {
        pedido.CreatedAt = DateTime.UtcNow;
        pedido.UpdatedAt = DateTime.UtcNow;
        
        context.Pedidos.Add(pedido);
        await context.SaveChangesAsync();
        
        return pedido;
    }

    public async Task<Pedido> UpdateAsync(Pedido pedido)
    {
        pedido.UpdatedAt = DateTime.UtcNow;
        context.Pedidos.Update(pedido);
        await context.SaveChangesAsync();
        return pedido;
    }
}
```

---

## 7. Beneficios del Provider

| Caracteristica | MongoDB Driver | MongoDB EF Core |
|----------------|----------------|-----------------|
| Change Tracking | Manual | Automatico |
| LINQ Queries | Aggregation Pipeline | Soportado |
| Modelo tipado | BsonDocument | Clases C# |

---

## 8. Consultas LINQ

```csharp
var pedidos = await context.Pedidos
    .Where(p => p.UserId == userId)
    .OrderByDescending(p => p.CreatedAt)
    .ToListAsync();

var pendientes = await context.Pedidos
    .Where(p => p.Estado == "PENDIENTE")
    .ToListAsync();
```

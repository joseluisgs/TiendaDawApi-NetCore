# 03. Entity Framework Core 10 con PostgreSQL

Entity Framework Core 10 es el ORM de Microsoft para .NET. En TiendaDawApi-NetCore gestiona las entidades relacionales: Categorias, Productos y Usuarios.

---

## 1. El DbContext: El Centro del Universo EF

El DbContext representa tu sesion con la base de datos. Es responsable de mapear clases C# a tablas SQL, rastrear cambios y persistirlos.

```mermaid
flowchart TB
    subgraph "Tu Codigo C#"
        ENT["Entidades Producto, Categoria, User"]
    end
    
    subgraph "DbContext"
        CHANGE["Change Tracker"]
        MAPPER["Entity Type Builder"]
    end
    
    subgraph "PostgreSQL"
        SQL["Tablas SQL"]
    end
    
    ENT -->|SaveChanges| CHANGE
    CHANGE -->|INSERT/UPDATE/DELETE| SQL
    SQL -->|SELECT| CHANGE
    CHANGE -->|ToList/First| ENT
 ```

### Diagrama de Entidades Relacionales

```mermaid
classDiagram
    class User {
        +long Id
        +string Username
        +string Email
        +string PasswordHash
        +string Role
        +DateTime CreatedAt
        +DateTime UpdatedAt
        +bool IsDeleted
    }
    
    class Categoria {
        +long Id
        +string Nombre
        +DateTime CreatedAt
        +DateTime UpdatedAt
        +bool IsDeleted
        +List~Producto~ Productos
    }
    
    class Producto {
        +long Id
        +string Nombre
        +decimal Precio
        +int Stock
        +string? Imagen
        +long CategoriaId
        +DateTime CreatedAt
        +DateTime UpdatedAt
        +bool IsDeleted
        +Categoria Categoria
    }
    
    User "1" --> "*" Categoria : relacion
    Categoria "1" --> "*" Producto : tiene
    Producto --> Categoria : pertenece
```

### El DbContext de TiendaApi

```csharp
using Microsoft.EntityFrameworkCore;
using TiendaApi.Apis.Models;

namespace TiendaApi.Apis.Data;

public class TiendaDbContext : DbContext
{
    public TiendaDbContext(DbContextOptions<TiendaDbContext> options) 
        : base(options)
    {
    }

    public DbSet<Categoria> Categorias { get; set; } = null!;
    public DbSet<Producto> Productos { get; set; } = null!;
    public DbSet<User> Users { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Categoria>(entity =>
        {
            entity.ToTable("categorias");
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Nombre).IsRequired().HasMaxLength(100);
            entity.HasIndex(c => c.Nombre).IsUnique();
        });

        modelBuilder.Entity<Producto>(entity =>
        {
            entity.ToTable("productos");
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Nombre).IsRequired().HasMaxLength(200);
            entity.Property(p => p.Precio).HasPrecision(10, 2);
            
            entity.HasOne(p => p.Categoria)
                .WithMany(c => c.Productos)
                .HasForeignKey(p => p.CategoriaId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(u => u.Id);
            entity.Property(u => u.Username).IsRequired().HasMaxLength(50);
            entity.Property(u => u.Email).IsRequired().HasMaxLength(100);
            entity.HasIndex(u => u.Username).IsUnique();
            entity.HasIndex(u => u.Email).IsUnique();
        });
    }
}
```

---

## 2. Configuracion en Program.cs

```csharp
var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string not found");

builder.Services.AddDbContext<TiendaDbContext>(options =>
    options.UseNpgsql(connectionString));

var app = builder.Build();
app.Run();
```

---

## 3. El Patrón Repository

```csharp
public interface ICategoriaRepository
{
    Task<IEnumerable<Categoria>> FindAllAsync();
    Task<Categoria?> FindByIdAsync(long id);
    Task<Categoria> SaveAsync(Categoria categoria);
    Task<Categoria> UpdateAsync(Categoria categoria);
    Task DeleteAsync(long id);
}

public class CategoriaRepository(
    TiendaDbContext context,
    ILogger<CategoriaRepository> logger
) : ICategoriaRepository {

    public async Task<IEnumerable<Categoria>> FindAllAsync()
    {
        return await context.Categorias
            .OrderBy(c => c.Nombre)
            .ToListAsync();
    }

    public async Task<Categoria?> FindByIdAsync(long id)
    {
        return await context.Categorias.FindAsync(id);
    }

    public async Task<Categoria> SaveAsync(Categoria categoria)
    {
        categoria.CreatedAt = DateTime.UtcNow;
        categoria.UpdatedAt = DateTime.UtcNow;
        
        context.Categorias.Add(categoria);
        await context.SaveChangesAsync();
        
        logger.LogInformation("Categoria guardada ID: {Id}", categoria.Id);
        return categoria;
    }

    public async Task<Categoria> UpdateAsync(Categoria categoria)
    {
        categoria.UpdatedAt = DateTime.UtcNow;
        context.Categorias.Update(categoria);
        await context.SaveChangesAsync();
        return categoria;
    }

    public async Task DeleteAsync(long id)
    {
        var categoria = await FindByIdAsync(id);
        if (categoria != null)
        {
            categoria.IsDeleted = true;
            await context.SaveChangesAsync();
        }
    }
}
```

---

## 4. Seed Data

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);

    modelBuilder.Entity<User>().HasData(
        new User {
            Id = 1,
            Username = "admin",
            Email = "admin@tienda.com",
            PasswordHash = "$2a$11$...",
            Role = "ADMIN",
            CreatedAt = DateTime.UtcNow
        },
        new User {
            Id = 2,
            Username = "userdaw",
            Email = "userdaw@tienda.com",
            PasswordHash = "$2a$11$...",
            Role = "USER",
            CreatedAt = DateTime.UtcNow
        }
    );

    modelBuilder.Entity<Categoria>().HasData(
        new Categoria { Id = 1, Nombre = "Electronica" },
        new Categoria { Id = 2, Nombre = "Ropa" },
        new Categoria { Id = 3, Nombre = "Libros" }
    );
}
```

---

## 5. Consultas LINQ

```csharp
// Productos con su categoria
var productos = await context.Productos
    .Include(p => p.Categoria)
    .Where(p => p.Stock > 0)
    .OrderBy(p => p.Nombre)
    .ToListAsync();

// Proyeccion
var summary = await context.Productos
    .Where(p => p.CategoriaId == 1)
    .Select(p => new {
        p.Id,
        p.Nombre,
        p.Precio,
        Categoria = p.Categoria.Nombre
    })
    .ToListAsync();

// Conteo eficiente
var count = await context.Productos.CountAsync(p => p.Stock > 0);
```

---

## 6. Soft Delete con Query Filters

```csharp
modelBuilder.Entity<Categoria>(entity =>
{
    entity.HasQueryFilter(c => !c.IsDeleted);
});
```

Ahora FindAllAsync automaticamente filtra categorias no eliminadas.

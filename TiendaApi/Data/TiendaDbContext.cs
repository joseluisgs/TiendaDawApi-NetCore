using Microsoft.EntityFrameworkCore;
using TiendaApi.Models.Entities;

namespace TiendaApi.Data;

/// <summary>
/// Entity Framework Core DbContext for PostgreSQL
/// 
/// Java equivalent: extends JpaRepository or uses EntityManager
/// Spring Boot: Similar to @Configuration with JPA setup
/// 
/// Manages Categorías, Productos, and Users
/// Pedidos are stored in MongoDB (separate configuration)
/// </summary>
public class TiendaDbContext : DbContext
{
    public TiendaDbContext(DbContextOptions<TiendaDbContext> options) : base(options)
    {
    }

    public DbSet<Categoria> Categorias { get; set; } = null!;
    public DbSet<Producto> Productos { get; set; } = null!;
    public DbSet<User> Users { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Categoria entity
        modelBuilder.Entity<Categoria>(entity =>
        {
            entity.ToTable("categorias");
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Nombre).IsRequired().HasMaxLength(100);
            entity.HasIndex(c => c.Nombre).IsUnique();
            entity.Property(c => c.IsDeleted).HasDefaultValue(false);
            
            // Soft delete filter - similar to Hibernate @Where
            entity.HasQueryFilter(c => !c.IsDeleted);
        });

        // Configure Producto entity
        modelBuilder.Entity<Producto>(entity =>
        {
            entity.ToTable("productos");
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Nombre).IsRequired().HasMaxLength(200);
            entity.Property(p => p.Descripcion).HasMaxLength(1000);
            entity.Property(p => p.Precio).HasPrecision(10, 2);
            entity.Property(p => p.Stock).IsRequired();
            entity.Property(p => p.IsDeleted).HasDefaultValue(false);
            
            // Relationship with Categoria - similar to JPA @ManyToOne
            entity.HasOne(p => p.Categoria)
                .WithMany(c => c.Productos)
                .HasForeignKey(p => p.CategoriaId)
                .OnDelete(DeleteBehavior.Restrict);
            
            entity.HasQueryFilter(p => !p.IsDeleted);
        });

        // Configure User entity
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(u => u.Id);
            entity.Property(u => u.Username).IsRequired().HasMaxLength(50);
            entity.Property(u => u.Email).IsRequired().HasMaxLength(100);
            entity.Property(u => u.PasswordHash).IsRequired();
            entity.Property(u => u.Role).IsRequired().HasMaxLength(20);
            entity.Property(u => u.IsDeleted).HasDefaultValue(false);
            
            entity.HasIndex(u => u.Username).IsUnique();
            entity.HasIndex(u => u.Email).IsUnique();
            
            entity.HasQueryFilter(u => !u.IsDeleted);
        });

        // Seed initial data
        SeedData(modelBuilder);
    }

    private static void SeedData(ModelBuilder modelBuilder)
    {
        // Seed admin user (password: Admin123!)
        modelBuilder.Entity<User>().HasData(
            new User
            {
                Id = 1,
                Username = "admin",
                Email = "admin@tienda.com",
                // BCrypt hash of "Admin123!" - in production use proper hashing
                PasswordHash = "$2a$11$vHqmFyFyRqKtaVJEz0XqFeI/xlPNGOKJbBYGzN0PqnQZQqZm3LzYy",
                Role = UserRoles.ADMIN,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        );

        // Seed categories
        modelBuilder.Entity<Categoria>().HasData(
            new Categoria
            {
                Id = 1,
                Nombre = "Electrónica",
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Categoria
            {
                Id = 2,
                Nombre = "Ropa",
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Categoria
            {
                Id = 3,
                Nombre = "Libros",
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        );

        // Seed products
        modelBuilder.Entity<Producto>().HasData(
            new Producto
            {
                Id = 1,
                Nombre = "Laptop Dell XPS 15",
                Descripcion = "Laptop de alto rendimiento",
                Precio = 1299.99m,
                Stock = 10,
                CategoriaId = 1,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Producto
            {
                Id = 2,
                Nombre = "Camiseta Nike",
                Descripcion = "Camiseta deportiva",
                Precio = 29.99m,
                Stock = 50,
                CategoriaId = 2,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Producto
            {
                Id = 3,
                Nombre = "Clean Code",
                Descripcion = "Libro de Robert C. Martin",
                Precio = 42.99m,
                Stock = 25,
                CategoriaId = 3,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        );
    }
}

using Microsoft.EntityFrameworkCore;
using TiendaApi.Api.Data.Interceptors;
using TiendaApi.Api.Models;

namespace TiendaApi.Api.Data;

/// <summary>
/// DbContext de Entity Framework Core para PostgreSQL.
/// Gestiona Categorías, Productos y Usuarios.
/// </summary>
public class TiendaDbContext : DbContext
{
    private static readonly TimestampInterceptor _timestampInterceptor = new();

    /// <summary>
    /// Constructor con opciones de configuración.
    /// </summary>
    public TiendaDbContext(DbContextOptions<TiendaDbContext> options) : base(options)
    {
    }

    /// <summary>DbSet de Categorías.</summary>
    public DbSet<Categoria> Categorias { get; set; } = null!;

    /// <summary>DbSet de Productos.</summary>
    public DbSet<Producto> Productos { get; set; } = null!;

    /// <summary>DbSet de Usuarios.</summary>
    public DbSet<User> Users { get; set; } = null!;

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
        optionsBuilder.AddInterceptors(_timestampInterceptor);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Categoria>(entity =>
        {
            entity.ToTable("categorias");
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Id).UseIdentityAlwaysColumn();
            entity.Property(c => c.Nombre).IsRequired().HasMaxLength(100);
            entity.Property(c => c.Descripcion).HasMaxLength(500);
            entity.HasIndex(c => c.Nombre).IsUnique();
            entity.Property(c => c.IsDeleted).HasDefaultValue(false);
            entity.ConfigureTimestamps();
            entity.HasQueryFilter(c => !c.IsDeleted);
        });

        modelBuilder.Entity<Producto>(entity =>
        {
            entity.ToTable("productos");
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Id).UseIdentityAlwaysColumn();
            entity.Property(p => p.Nombre).IsRequired().HasMaxLength(200);
            entity.Property(p => p.Descripcion).HasMaxLength(1000);
            entity.Property(p => p.Precio).HasPrecision(10, 2);
            entity.Property(p => p.Stock).IsRequired();
            entity.Property(p => p.IsDeleted).HasDefaultValue(false);
            entity.Property(p => p.RowVersion).IsRequired();
            entity.ConfigureTimestamps();
            entity.HasOne(p => p.Categoria)
                .WithMany(c => c.Productos)
                .HasForeignKey(p => p.CategoriaId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasQueryFilter(p => !p.IsDeleted);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(u => u.Id);
            entity.Property(u => u.Id).UseIdentityAlwaysColumn();
            entity.Property(u => u.Username).IsRequired().HasMaxLength(50);
            entity.Property(u => u.Email).IsRequired().HasMaxLength(100);
            entity.Property(u => u.PasswordHash).IsRequired();
            entity.Property(u => u.Role).IsRequired().HasMaxLength(20);
            entity.Property(u => u.IsDeleted).HasDefaultValue(false);
            entity.Property(u => u.Avatar).HasMaxLength(500);
            entity.ConfigureTimestamps();
            entity.HasIndex(u => u.Username).IsUnique();
            entity.HasIndex(u => u.Email).IsUnique();
            entity.HasQueryFilter(u => !u.IsDeleted);
        });
    }
}

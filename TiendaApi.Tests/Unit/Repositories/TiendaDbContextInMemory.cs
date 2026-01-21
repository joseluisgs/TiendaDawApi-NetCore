using Microsoft.EntityFrameworkCore;
using TiendaApi.Api.Data;
using TiendaApi.Api.Models;

namespace TiendaApi.Tests.Unit.Repositories;

/// <summary>
/// DbContext para tests con InMemory.
/// Solo incluye entidades de PostgreSQL (sin MongoDB).
/// </summary>
public class TiendaDbContextInMemory : DbContext
{
    public TiendaDbContextInMemory(DbContextOptions<TiendaDbContextInMemory> options)
        : base(options)
    {
    }

    public DbSet<Categoria> Categorias { get; set; } = null!;
    public DbSet<Producto> Productos { get; set; } = null!;
    public DbSet<User> Users { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Categoria>(entity =>
        {
            entity.ToTable("categorias");
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Nombre).IsRequired().HasMaxLength(100);
            entity.HasIndex(c => c.Nombre).IsUnique();
            entity.Property(c => c.IsDeleted).HasDefaultValue(false);
            entity.HasQueryFilter(c => !c.IsDeleted);
        });

        modelBuilder.Entity<Producto>(entity =>
        {
            entity.ToTable("productos");
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Nombre).IsRequired().HasMaxLength(200);
            entity.Property(p => p.Descripcion).HasMaxLength(1000);
            entity.Property(p => p.Precio).HasPrecision(10, 2);
            entity.Property(p => p.Stock).IsRequired();
            entity.Property(p => p.IsDeleted).HasDefaultValue(false);

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
            entity.Property(u => u.Username).IsRequired().HasMaxLength(50);
            entity.Property(u => u.Email).IsRequired().HasMaxLength(100);
            entity.Property(u => u.PasswordHash).IsRequired();
            entity.Property(u => u.Role).IsRequired().HasMaxLength(20);
            entity.Property(u => u.IsDeleted).HasDefaultValue(false);

            entity.HasIndex(u => u.Username).IsUnique();
            entity.HasIndex(u => u.Email).IsUnique();

            entity.HasQueryFilter(u => !u.IsDeleted);
        });
    }
}

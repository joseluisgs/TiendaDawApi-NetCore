using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TiendaApi.Api.Data.Interceptors;
using TiendaApi.Api.Models;

namespace TiendaApi.Api.Data;

/// <summary>
/// DbContext de Entity Framework Core para PostgreSQL.
/// Gestiona Categorías, Productos y Usuarios.
/// Hereda de IdentityDbContext para soportar ASP.NET Core Identity.
/// </summary>
public class TiendaDbContext : IdentityDbContext<User, IdentityRole<long>, long>
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
    public new DbSet<User> Users { get; set; } = null!;

    /// <summary>DbSet de vinculaciones OAuth (logins externos). No confundir con
    /// UserLogins de IdentityDbContext (AspNetUserLogins).</summary>
    public DbSet<UserLogin> ExternalLogins { get; set; } = null!;

    /// <summary>
    /// Configura el contexto (interceptores de marcas de tiempo).
    /// </summary>
    /// <param name="optionsBuilder">Constructor de opciones del contexto.</param>
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
        optionsBuilder.AddInterceptors(_timestampInterceptor);
    }

    /// <summary>
    /// Define el modelo del esquema: entidades, relaciones, índices y filtros globales.
    /// </summary>
    /// <param name="modelBuilder">Constructor del modelo.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);  // ← Identity necesita esto para crear sus tablas AspNet*

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
            entity.HasIndex(p => p.CategoriaId);
            entity.HasIndex(p => new { p.CategoriaId, p.Precio });
            entity.HasIndex(p => p.CreatedAt);
            entity.HasIndex(p => p.IsDeleted);
            entity.HasQueryFilter(p => !p.IsDeleted);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(u => u.Id);
            entity.Property(u => u.Id).UseIdentityAlwaysColumn();
            // UserName viene de IdentityUser (antes se llamaba Username)
            entity.Property(u => u.UserName).IsRequired().HasMaxLength(50);
            entity.Property(u => u.Email).IsRequired().HasMaxLength(100);
            // PasswordHash nullable: los usuarios OAuth pueden no tener contraseña.
            // Si lo marcamos IsRequired, Identity (UserStore) guarda el hash real en la
            // propiedad heredada pero la columna requerida nos obliga a rellenarla a mano.
            entity.Property(u => u.PasswordHash);
            entity.Property(u => u.Role).IsRequired().HasMaxLength(20);
            entity.Property(u => u.IsDeleted).HasDefaultValue(false);
            entity.Property(u => u.Avatar).HasMaxLength(500);
            entity.ConfigureTimestamps();
            entity.HasIndex(u => u.UserName).IsUnique();
            entity.HasIndex(u => u.Email).IsUnique();
            entity.HasIndex(u => u.Role);
            entity.HasQueryFilter(u => !u.IsDeleted);
        });

        modelBuilder.Entity<UserLogin>(entity =>
        {
            entity.ToTable("user_logins");
            entity.HasKey(ul => ul.Id);
            entity.Property(ul => ul.Id).UseIdentityAlwaysColumn();
            entity.Property(ul => ul.LoginProvider).IsRequired().HasMaxLength(100);
            entity.Property(ul => ul.ProviderKey).IsRequired().HasMaxLength(450);
            entity.Property(ul => ul.ProviderDisplayName).HasMaxLength(200);
            entity.ConfigureTimestamps();

            // Un proveedor externo solo puede estar vinculado a un usuario local
            entity.HasIndex(ul => new { ul.LoginProvider, ul.ProviderKey }).IsUnique();

            entity.HasOne(ul => ul.User)
                .WithMany()
                .HasForeignKey(ul => ul.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}

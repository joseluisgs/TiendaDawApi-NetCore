using Microsoft.EntityFrameworkCore;
using MongoDB.EntityFrameworkCore.Extensions;
using TiendaApi.Api.Data.Interceptors;
using TiendaApi.Api.Models;

namespace TiendaApi.Api.Data;

/// <summary>
/// DbContext específico para MongoDB.
/// Gestiona únicamente la colección de Pedidos.
/// </summary>
public class TiendaMongoContext : DbContext
{
    /// <summary>
    /// Constructor con opciones de configuración.
    /// </summary>
    public TiendaMongoContext(DbContextOptions<TiendaMongoContext> options) : base(options)
    {
    }

    /// <summary>DbSet de Pedidos.</summary>
    public DbSet<Pedido> Pedidos { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Pedido>(entity =>
        {
            entity.ToCollection("pedidos");
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Estado).IsRequired().HasMaxLength(50);
            entity.Property(p => p.Total).HasPrecision(10, 2);
            entity.ConfigureTimestamps();
        });
    }
}

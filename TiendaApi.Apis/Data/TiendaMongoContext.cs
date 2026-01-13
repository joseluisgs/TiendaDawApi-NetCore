using Microsoft.EntityFrameworkCore;
using MongoDB.EntityFrameworkCore.Extensions;
using TiendaApi.Apis.Models;

namespace TiendaApi.Apis.Data;

/// <summary>
/// DbContext específico para MongoDB.
/// Gestiona únicamente la colección de Pedidos.
/// </summary>
public class TiendaMongoContext : DbContext
{
    public TiendaMongoContext(DbContextOptions<TiendaMongoContext> options) : base(options)
    {
    }

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
        });
    }
}

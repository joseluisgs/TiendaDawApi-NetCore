using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TiendaApi.Api.Data.Abstractions;

namespace TiendaApi.Api.Data.Interceptors;

/// <summary>
/// Interceptor de EF Core que asigna CreatedAt y UpdatedAt automáticamente.
/// </summary>
public class TimestampInterceptor : SaveChangesInterceptor
{
    /// <summary>
    /// Asigna timestamps antes de guardar cambios (sync).
    /// </summary>
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        if (eventData.Context == null)
            return base.SavingChanges(eventData, result);

        UpdateTimestamps(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    /// <summary>
    /// Asigna timestamps antes de guardar cambios (async).
    /// </summary>
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context == null)
            return base.SavingChangesAsync(eventData, result, cancellationToken);

        UpdateTimestamps(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private static void UpdateTimestamps(DbContext context)
    {
        var now = DateTime.UtcNow;

        foreach (var entry in context.ChangeTracker.Entries<ITimestamped>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Property(e => e.CreatedAt).CurrentValue = now;
                    entry.Property(e => e.UpdatedAt).CurrentValue = now;
                    break;
                case EntityState.Modified:
                    entry.Property(e => e.UpdatedAt).CurrentValue = now;
                    break;
            }
        }
    }
}

/// <summary>
/// Extensiones para configurar timestamps en entidades.
/// </summary>
public static class TimestampExtensions
{
    /// <summary>
    /// Configura CreatedAt y UpdatedAt como GeneratedOnAdd/Update.
    /// </summary>
    public static void ConfigureTimestamps(this EntityTypeBuilder entity)
    {
        entity.Property("CreatedAt")
            .IsRequired()
            .ValueGeneratedOnAdd()
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        entity.Property("UpdatedAt")
            .IsRequired();
    }
}

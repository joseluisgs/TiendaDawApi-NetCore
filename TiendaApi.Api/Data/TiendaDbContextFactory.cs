using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace TiendaApi.Api.Data;

/// <summary>
/// Factory design-time para las herramientas de EF Core (<c>dotnet ef</c>).
/// Sin ella, los tools ejecutarían <c>Program.cs</c> y en desarrollo se dispararía
/// <c>EnsureDeleted + EnsureCreated</c> al generar cada migración.
/// Lee la connection string de appsettings.json (copiado al bin de salida).
/// </summary>
public class TiendaDbContextFactory : IDesignTimeDbContextFactory<TiendaDbContext>
{
    /// <summary>
    /// Crea el DbContext para tiempo de diseño.
    /// </summary>
    /// <param name="args">Argumentos de la herramienta (no usados; usar <c>--environment</c> si se necesita otro entorno).</param>
    /// <returns>DbContext configurado con la conexión por defecto.</returns>
    public TiendaDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true)
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Host=localhost;Database=tienda;Username=admin;Password=admin123";

        var options = new DbContextOptionsBuilder<TiendaDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new TiendaDbContext(options);
    }
}

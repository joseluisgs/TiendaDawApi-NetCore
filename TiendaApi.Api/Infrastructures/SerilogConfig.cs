using Serilog;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;

namespace TiendaApi.Api.Infrastructures;

/// <summary>
/// Extensiones de configuración para Serilog.
/// Configura el logger con salida a consola y filtros de nivel.
/// </summary>
public static class SerilogConfig
{
    /// <summary>
    /// Configura Serilog con salida a consola y filtros de nivel.
    /// Filtra los logs verbose de base de datos para mayor claridad.
    /// </summary>
    /// <returns>Configuración de logger lista para usar.</returns>
    public static LoggerConfiguration Configure()
    {
        return new LoggerConfiguration()
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Error)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database", LogEventLevel.Error)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", LogEventLevel.Error)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore.ChangeTracking", LogEventLevel.Error)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Infrastructure", LogEventLevel.Error)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Model", LogEventLevel.Error)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Query", LogEventLevel.Error)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Validation", LogEventLevel.Error)
            .MinimumLevel.Override("Npgsql", LogEventLevel.Error)
            .MinimumLevel.Override("MongoDB", LogEventLevel.Error)
            .WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}",
                theme: AnsiConsoleTheme.Code);
    }
}

using Serilog;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;

namespace TiendaApi.Api.Infrastructures;

/// <summary>
/// Extensiones de configuración para Serilog.
/// Configura el logger con salida a consola y niveles personalizados.
/// </summary>
public static class SerilogConfig
{
    /// <summary>
    /// Configura Serilog con salida a consola y filtros de nivel.
    /// </summary>
    /// <returns>Configuración de logger lista para usar.</returns>
    public static LoggerConfiguration Configure()
    {
        return new LoggerConfiguration()
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", LogEventLevel.Warning)
            .WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}",
                theme: AnsiConsoleTheme.Code);
    }
}

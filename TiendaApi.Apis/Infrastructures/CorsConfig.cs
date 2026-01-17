using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace TiendaApi.Apis.Infrastructures;

/// <summary>
/// Extensiones de configuración de CORS.
/// </summary>
public static class CorsConfig
{
    /// <summary>
    /// Configura la política CORS permitiendo todos los orígenes.
    /// Útil para desarrollo. En producción restringir.
    /// </summary>
    public static IServiceCollection AddCorsPolicy(this IServiceCollection services)
    {
        Log.Information("🌐 Configurando CORS...");
        return services.AddCors(options =>
        {
            options.AddPolicy("AllowAll", policy =>
            {
                policy.AllowAnyOrigin()
                      .AllowAnyMethod()
                      .AllowAnyHeader();
            });
        });
    }
}

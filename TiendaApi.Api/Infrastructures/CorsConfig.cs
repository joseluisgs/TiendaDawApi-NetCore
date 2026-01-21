using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace TiendaApi.Api.Infrastructures;

/// <summary>
/// Extensiones de configuración de CORS.
/// </summary>
public static class CorsConfig
{
    /// <summary>
    /// Configura la política CORS según el entorno.
    /// Desarrollo: AllowAll (permite todo)
    /// Producción: Solo orígenes configurados en Cors:AllowedOrigins
    /// </summary>
    public static IServiceCollection AddCorsPolicy(this IServiceCollection services, IConfiguration configuration, bool isDevelopment)
    {
        Log.Information("🌐 Configurando CORS para {Environment}...", isDevelopment ? "DESARROLLO" : "PRODUCCIÓN");

        return services.AddCors(options =>
        {
            if (isDevelopment)
            {
                options.AddPolicy("AllowAll", policy =>
                {
                    policy.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader();
                });
                Log.Information("🌐 CORS: AllowAll (desarrollo)");
            }
            else
            {
                var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                    ?? throw new InvalidOperationException("Cors:AllowedOrigins no configurado");

                options.AddPolicy("ProductionPolicy", policy =>
                {
                    policy.WithOrigins(allowedOrigins)
                          .AllowAnyMethod()
                          .AllowAnyHeader()
                          .AllowCredentials();
                });
                Log.Information("🌐 CORS: ProductionPolicy con {Count} orígenes", allowedOrigins.Length);
            }
        });
    }
}

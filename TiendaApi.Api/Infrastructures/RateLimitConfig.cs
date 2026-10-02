using TiendaApi.Api.Middleware;

namespace TiendaApi.Api.Infrastructures;

/// <summary>
/// Extension methods para configurar Rate Limiting con la API nativa de .NET
/// (<c>System.Threading.RateLimiting</c>), sin dependencias de terceros.
/// Protege la API contra DDoS, fuerza bruta y abuso.
/// </summary>
/// <remarks>
/// Las reglas aplican por cliente (IP + verbo + ruta), igual que una ventana
/// de conteo independiente por endpoint:
/// <list type="bullet">
/// <item>Todas las peticiones: 100 por 15 segundos.</item>
/// <item>Autenticación (<c>/api/v1/auth/*</c>): 10 por minuto.</item>
/// <item>Escritura (POST): 20 por minuto.</item>
/// </list>
/// Si dos reglas comparten periodo se aplica la más restrictiva
/// (p.ej. un POST a autenticación queda en 10/min, no en 20/min).
/// </remarks>
public static class RateLimitConfig
{
    /// <summary>Límite de peticiones para la ventana general.</summary>
    public const int GeneralLimit = 100;

    /// <summary>Periodo de la ventana general.</summary>
    public static readonly TimeSpan GeneralWindow = TimeSpan.FromSeconds(15);

    /// <summary>Límite por minuto para endpoints de autenticación.</summary>
    public const int AuthLimit = 10;

    /// <summary>Límite por minuto para peticiones de escritura (POST).</summary>
    public const int WriteLimit = 20;

    /// <summary>Periodo de las ventanas de un minuto.</summary>
    public static readonly TimeSpan MinuteWindow = TimeSpan.FromMinutes(1);

    /// <summary>Prefijo de ruta que identifica los endpoints de autenticación.</summary>
    public const string AuthPathPrefix = "/api/v1/auth/";

    /// <summary>
    /// Registra el estado del rate limiting (particiones y limitadores nativos).
    /// </summary>
    /// <param name="services">Colección de servicios de la aplicación.</param>
    /// <returns>La misma colección de servicios.</returns>
    public static IServiceCollection AddRateLimitingPolicy(this IServiceCollection services)
    {
        services.AddSingleton<RateLimitingState>();
        return services;
    }

    /// <summary>
    /// Aplica el middleware de Rate Limiting.
    /// </summary>
    /// <param name="app">Constructor de la aplicación.</param>
    /// <returns>El mismo constructor de aplicación.</returns>
    public static IApplicationBuilder UseRateLimiting(this IApplicationBuilder app)
    {
        app.UseMiddleware<RateLimitMiddleware>();
        return app;
    }
}

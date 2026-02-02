using AspNetCoreRateLimit;

namespace TiendaApi.Api.Infrastructures;

/// <summary>
/// Extension methods para configurar Rate Limiting.
/// Protege la API contra DDoS, fuerza bruta y abuso.
/// </summary>
public static class RateLimitConfig
{
    /// <summary>
    /// Configura Rate Limiting con reglas por defecto.
    /// </summary>
    public static IServiceCollection AddRateLimitingPolicy(this IServiceCollection services)
    {
        services.AddMemoryCache();
        services.Configure<RateLimitOptions>(options =>
        {
            options.EnableEndpointRateLimiting = true;
            options.HttpStatusCode = 429;
            options.QuotaExceededMessage = "Demasiadas solicitudes. Por favor, intente más tarde.";
            
            options.GeneralRules = new List<RateLimitRule>
            {
                // API General: 100 requests por 15 segundos
                new RateLimitRule
                {
                    Endpoint = "*",
                    Limit = 100,
                    Period = "15s"
                },
                // Endpoints de autenticación: más estrictos (fuerza bruta)
                new RateLimitRule
                {
                    Endpoint = "*/api/v1/auth/*",
                    Limit = 10,
                    Period = "1m"
                },
                // Endpoints de escritura: más estrictos
                new RateLimitRule
                {
                    Endpoint = "POST:*",
                    Limit = 20,
                    Period = "1m"
                },
                // GraphQL: más permisivo para queries
                new RateLimitRule
                {
                    Endpoint = "POST:/graphql",
                    Limit = 200,
                    Period = "1m"
                }
            };
        });

        services.AddSingleton<IIpPolicyStore, MemoryCacheIpPolicyStore>();
        services.AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore>();
        services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
        services.AddSingleton<IProcessingStrategy, AsyncKeyLockProcessingStrategy>();
        
        return services;
    }

    /// <summary>
    /// Aplica el middleware de Rate Limiting.
    /// </summary>
    public static IApplicationBuilder UseRateLimiting(this IApplicationBuilder app)
    {
        app.UseIpRateLimiting();
        return app;
    }
}

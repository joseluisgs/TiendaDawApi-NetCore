using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Serilog;
using StackExchange.Redis;
using TiendaApi.Api.Services.Cache;

namespace TiendaApi.Api.Infrastructures;

/// <summary>
/// Extensiones de configuración de caché.
/// </summary>
public static class CacheConfig
{
    /// <summary>
    /// Configura el servicio de caché.
    /// Desarrollo: MemoryCache.
    /// Producción: Redis (configuración obligatoria).
    /// </summary>
    public static IServiceCollection AddCache(this IServiceCollection services, IWebHostEnvironment environment, IConfiguration configuration)
    {
        if (environment.IsDevelopment())
        {
            Log.Information("💾 Configurando caché en memoria (desarrollo local)...");
            services.AddMemoryCache();
            services.TryAddSingleton<ICacheService, MemoryCacheService>();
        }
        else
        {
            // 🎓 Fail-fast: en producción, Redis debe configurarse explícitamente.
            var redisConfig = configuration["Cache:RedisConfiguration"];
            if (string.IsNullOrEmpty(redisConfig))
            {
                throw new InvalidOperationException(
                    "Cache:RedisConfiguration no está definida. " +
                    "En producción es obligatorio configurarla (appsettings.json o variables de entorno).");
            }

            Log.Information("💾 Configurando caché Redis (producción)...");
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisConfig;
                options.InstanceName = configuration["Cache:RedisInstanceName"] ?? "TiendaApi:";
            });
            services.TryAddSingleton<ICacheService, RedisCacheService>();
        }

        return services;
    }
}

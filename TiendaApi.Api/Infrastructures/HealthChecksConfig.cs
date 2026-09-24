using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using MongoDB.Bson;
using MongoDB.Driver;
using Serilog;
using TiendaApi.Api.Data;

namespace TiendaApi.Api.Infrastructures;

/// <summary>
/// Configuración de Health Checks (sondeo de dependencias en /health).
/// Sin paquetes NuGet adicionales: implementaciones propias sobre PostgreSQL, MongoDB y Redis.
/// </summary>
public static class HealthChecksConfig
{
    /// <summary>
    /// Registra los health checks de la API.
    /// PostgreSQL y MongoDB siempre; Redis solo fuera de desarrollo (donde no está configurado).
    /// </summary>
    /// <param name="services">Colección de servicios.</param>
    /// <param name="environment">Entorno de la app (define si se añade el check de Redis).</param>
    /// <returns>IServiceCollection para encadenar.</returns>
    public static IServiceCollection AddHealthChecks(this IServiceCollection services, IWebHostEnvironment environment)
    {
        Log.Information("Configurando Health Checks (/health)...");

        var builder = services.AddHealthChecks();

        builder.AddCheck<PostgresHealthCheck>("postgresql");
        builder.AddCheck<MongoHealthCheck>("mongodb");

        // Redis solo existe como caché distribuida fuera de desarrollo (ver CacheConfig.AddCache)
        if (!environment.IsDevelopment())
        {
            builder.AddCheck<RedisHealthCheck>("redis");
        }

        return services;
    }

    /// <summary>
    /// Expone GET /health con respuesta JSON: 200 si todo está OK, 503 si alguna dependencia cae.
    /// </summary>
    /// <param name="endpoints">Constructor de endpoints.</param>
    /// <returns>IEndpointRouteBuilder para encadenar.</returns>
    public static IEndpointRouteBuilder MapHealthEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapHealthChecks("/health", new HealthCheckOptions
        {
            ResponseWriter = WriteHealthReportAsync
        });

        return endpoints;
    }

    /// <summary>
    /// Escribe el informe de salud como JSON: status, totalDuration y checks[] (name, status, duration, description, error).
    /// </summary>
    private static Task WriteHealthReportAsync(HttpContext context, HealthReport report)
    {
        var payload = new
        {
            status = MapStatus(report.Status),
            totalDuration = report.TotalDuration,
            checks = report.Entries.Select(entry => new
            {
                name = entry.Key,
                status = MapStatus(entry.Value.Status),
                duration = entry.Value.Duration,
                description = entry.Value.Description,
                error = entry.Value.Exception?.Message
            })
        };

        return context.Response.WriteAsJsonAsync(payload);
    }

    /// <summary>
    /// Traduce el estado interno al semáforo de la API: OK / DEGRADED / ERROR.
    /// </summary>
    private static string MapStatus(HealthStatus status) => status switch
    {
        HealthStatus.Healthy => "OK",
        HealthStatus.Degraded => "DEGRADED",
        _ => "ERROR"
    };

    /// <summary>
    /// Comprobación de PostgreSQL mediante EF Core (CanConnectAsync).
    /// </summary>
    public class PostgresHealthCheck(TiendaDbContext dbContext) : IHealthCheck
    {
        /// <inheritdoc />
        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(5));

            try
            {
                var canConnect = await dbContext.Database.CanConnectAsync(cts.Token);
                return canConnect
                    ? HealthCheckResult.Healthy("PostgreSQL accesible")
                    : HealthCheckResult.Unhealthy("PostgreSQL no responde");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy("PostgreSQL no accesible", ex);
            }
        }
    }

    /// <summary>
    /// Comprobación de MongoDB mediante un ping a la base de datos configurada.
    /// </summary>
    public class MongoHealthCheck(IMongoClient client, IConfiguration configuration) : IHealthCheck
    {
        /// <inheritdoc />
        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(5));

            try
            {
                var databaseName = configuration["MongoDbSettings:DatabaseName"] ?? "tienda";
                await client.GetDatabase(databaseName)
                    .RunCommandAsync<BsonDocument>(new BsonDocument("ping", 1), cancellationToken: cts.Token);
                return HealthCheckResult.Healthy("MongoDB accesible");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy("MongoDB no accesible", ex);
            }
        }
    }

    /// <summary>
    /// Comprobación de Redis a través de la caché distribuida configurada (misma conexión que usa la API).
    /// </summary>
    public class RedisHealthCheck(IDistributedCache cache) : IHealthCheck
    {
        /// <inheritdoc />
        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                await cache.SetStringAsync(
                    "health:probe",
                    "ping",
                    new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1) },
                    cancellationToken);
                return HealthCheckResult.Healthy("Redis accesible");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy("Redis no accesible", ex);
            }
        }
    }
}

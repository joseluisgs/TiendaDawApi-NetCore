using Microsoft.AspNetCore.Builder;
using Serilog;

namespace TiendaApi.Api.Infrastructures;

/// <summary>
/// Extension methods para CORS.
/// </summary>
public static class CorsExtensions
{
    /// <summary>
    /// Aplica la política CORS configurada según el entorno.
    /// </summary>
    public static IApplicationBuilder UseCorsPolicy(this IApplicationBuilder app)
    {
        var env = ((WebApplication)app).Environment;

        var policyName = env.IsDevelopment() ? "AllowAll" : "ProductionPolicy";

        Log.Information("🌐 Aplicando política CORS: {PolicyName}", policyName);
        return app.UseCors(policyName);
    }
}

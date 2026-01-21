namespace TiendaApi.Api.Middleware;

/// <summary>
/// Middleware de headers de seguridad HTTP.
/// Añade headers de protección contra ataques comunes web.
/// </summary>
public class SecurityHeadersMiddleware(RequestDelegate next)
{
    private readonly RequestDelegate _next = next;

    private static readonly Dictionary<string, string> SecurityHeaders = new(StringComparer.OrdinalIgnoreCase)
    {
        ["X-Content-Type-Options"] = "nosniff",
        ["X-Frame-Options"] = "DENY",
        ["X-XSS-Protection"] = "1; mode=block",
        ["Referrer-Policy"] = "strict-origin-when-cross-origin",
        ["Permissions-Policy"] = "accelerometer=(), camera=(), geolocation=(), gyroscope=(), magnetometer=(), microphone=(), payment=(), usb=()"
    };

    public async Task InvokeAsync(HttpContext context)
    {
        foreach (var header in SecurityHeaders)
        {
            context.Response.Headers.TryAdd(header.Key, header.Value);
        }

        await _next(context);
    }
}

/// <summary>
/// Extensiones para registro del middleware.
/// </summary>
public static class SecurityHeadersMiddlewareExtensions
{
    /// <summary>Registra el middleware de headers de seguridad.</summary>
    /// <param name="app">Constructor de la aplicación.</param>
    /// <returns>IApplicationBuilder.</returns>
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app)
    {
        return app.UseMiddleware<SecurityHeadersMiddleware>();
    }
}

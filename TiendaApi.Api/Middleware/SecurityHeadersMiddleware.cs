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
        ["Content-Security-Policy"] = "default-src 'none'; frame-ancestors 'none'; base-uri 'none'; form-action 'none'",
        ["Referrer-Policy"] = "strict-origin-when-cross-origin",
        ["Permissions-Policy"] = "accelerometer=(), camera=(), geolocation=(), gyroscope=(), magnetometer=(), microphone=(), payment=(), usb=()"
    };

    /// <summary>
    /// Política para Swagger UI: es la única página HTML que sirve esta API y
    /// su script interno no admite el default-src 'none' de la API, así que
    /// recibe la mínima que le deja funcionar (solo en /swagger).
    /// </summary>
    private const string ContentSecurityPolicySwagger =
        "default-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline'; img-src 'self' data:; base-uri 'self'; frame-ancestors 'none'";

    /// <summary>
    /// Añade los headers de seguridad a la respuesta y continúa con la petición.
    /// X-XSS-Protection no se envía: es una cabecera obsoleta (los navegadores
    /// modernos la ignoran y OWASP recomienda no enviarla); la protección
    /// real contra XSS es Content-Security-Policy.
    /// </summary>
    /// <param name="context">Contexto HTTP de la petición en curso.</param>
    public async Task InvokeAsync(HttpContext context)
    {
        var esSwagger = context.Request.Path.StartsWithSegments("/swagger");

        foreach (var header in SecurityHeaders)
        {
            if (esSwagger && header.Key == "Content-Security-Policy")
                context.Response.Headers[header.Key] = ContentSecurityPolicySwagger;
            else
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

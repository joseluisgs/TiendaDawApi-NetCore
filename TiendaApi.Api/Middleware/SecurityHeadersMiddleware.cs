namespace TiendaApi.Api.Middleware;

/// <summary>
///     Middleware de headers de seguridad HTTP.
///     Añade headers de protección contra ataques comunes web.
/// </summary>
public class SecurityHeadersMiddleware(RequestDelegate next) {
    /// <summary>
    ///     Política para las páginas HTML que sirve esta API: Swagger UI y la UI
    ///     de GraphQL (Banana Cake Pop en /graphql). Son SPAs que cargan sus
    ///     propios scripts, estilos, workers y assets, así que reciben la mínima
    ///     que les deja funcionar; el resto de respuestas (la API JSON) mantiene
    ///     el default-src 'none' de arriba.
    /// </summary>
    private const string ContentSecurityPolicyUi =
        "default-src 'self' 'unsafe-inline'; script-src 'self' 'unsafe-inline' 'unsafe-eval'; style-src 'self' 'unsafe-inline'; " +
        "img-src 'self' data: blob:; font-src 'self' data:; connect-src 'self' ws: wss:; worker-src 'self' blob:; " +
        "manifest-src 'self'; base-uri 'self'; frame-ancestors 'none'";

    private static readonly Dictionary<string, string> SecurityHeaders = new(StringComparer.OrdinalIgnoreCase) {
        ["X-Content-Type-Options"] = "nosniff",
        ["X-Frame-Options"] = "DENY",
        ["Content-Security-Policy"] = "default-src 'none'; frame-ancestors 'none'; base-uri 'none'; form-action 'none'",
        ["Referrer-Policy"] = "strict-origin-when-cross-origin",
        ["Permissions-Policy"] =
            "accelerometer=(), camera=(), geolocation=(), gyroscope=(), magnetometer=(), microphone=(), payment=(), usb=()"
    };

    private readonly RequestDelegate _next = next;

    /// <summary>
    ///     Añade los headers de seguridad a la respuesta y continúa con la petición.
    ///     X-XSS-Protection no se envía: es una cabecera obsoleta (los navegadores
    ///     modernos la ignoran y OWASP recomienda no enviarla); la protección
    ///     real contra XSS es Content-Security-Policy.
    /// </summary>
    /// <param name="context">Contexto HTTP de la petición en curso.</param>
    public async Task InvokeAsync(HttpContext context) {
        bool esPaginaUi = context.Request.Path.StartsWithSegments("/swagger")
                          || context.Request.Path.StartsWithSegments("/graphql");

        foreach (KeyValuePair<string, string> header in SecurityHeaders) {
            if (esPaginaUi && header.Key == "Content-Security-Policy") {
                context.Response.Headers[header.Key] = ContentSecurityPolicyUi;
            } else {
                context.Response.Headers.TryAdd(header.Key, header.Value);
            }
        }

        await _next(context);
    }
}

/// <summary>
///     Extensiones para registro del middleware.
/// </summary>
public static class SecurityHeadersMiddlewareExtensions {
    /// <summary>Registra el middleware de headers de seguridad.</summary>
    /// <param name="app">Constructor de la aplicación.</param>
    /// <returns>IApplicationBuilder.</returns>
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app) {
        return app.UseMiddleware<SecurityHeadersMiddleware>();
    }
}

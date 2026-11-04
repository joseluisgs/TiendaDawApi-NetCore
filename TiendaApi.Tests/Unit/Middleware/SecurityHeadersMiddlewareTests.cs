using FluentAssertions;
using Microsoft.AspNetCore.Http;
using TiendaApi.Api.Middleware;

namespace TiendaApi.Tests.Unit.Middleware;

/// <summary>
/// Tests unitarios del middleware de cabeceras de seguridad.
///
/// 🎓 El contrato: cada respuesta lleva Content-Security-Policy (la protección
/// real contra XSS) y NUNCA X-XSS-Protection, que es una cabecera obsoleta que
/// los navegadores modernos ignoran. Swagger UI, la única página HTML que sirve
/// la API, recibe una política propia para no romper su script interno.
/// </summary>
[TestFixture]
public class SecurityHeadersMiddlewareTests
{
    private static async Task<DefaultHttpContext> InvocarAsync(string path)
    {
        var context = new DefaultHttpContext { Request = { Path = path, Method = HttpMethods.Get } };
        context.Response.Body = new MemoryStream();

        var middleware = new SecurityHeadersMiddleware(_ => Task.CompletedTask);
        await middleware.InvokeAsync(context);

        return context;
    }

    [Test]
    public async Task Invoke_RespuestaApi_LlevaContentSecurityPolicy()
    {
        var context = await InvocarAsync("/api/productos");

        context.Response.Headers["Content-Security-Policy"].ToString()
            .Should().Contain("default-src 'none'", "una API JSON no debe cargar ningún recurso");
        context.Response.Headers["Content-Security-Policy"].ToString()
            .Should().Contain("frame-ancestors 'none'", "nadie debe embeber la API en un iframe");
    }

    [Test]
    public async Task Invoke_RespuestaApi_NoEnviaXssProtection()
    {
        var context = await InvocarAsync("/api/productos");

        context.Response.Headers.ContainsKey("X-XSS-Protection").Should().BeFalse(
            "X-XSS-Protection es obsoleta: los navegadores modernos la ignoran y OWASP recomienda no enviarla");
    }

    [Test]
    public async Task Invoke_SwaggerUsaPoliticaPropiaParaNoRomperselo()
    {
        var context = await InvocarAsync("/swagger/index.html");

        var csp = context.Response.Headers["Content-Security-Policy"].ToString();
        csp.Should().Contain("default-src 'self'", "Swagger UI necesita sus propios scripts");
        csp.Should().NotContain("default-src 'none'",
            "la política estricta de la API dejaría Swagger UI en blanco");
    }

    [Test]
    public async Task Invoke_GraphQLUi_RecibePoliticaQueLeDejaCargarSusAssets()
    {
        // 🎓 Regresión: la UI de GraphQL (Banana Cake Pop en /graphql) se
        // servía con el default-src 'none' de la API y el navegador bloqueaba
        // sus scripts, estilos, manifest y conexiones → página en blanco.
        var context = await InvocarAsync("/graphql/");

        var csp = context.Response.Headers["Content-Security-Policy"].ToString();
        csp.Should().Contain("default-src 'self'", "la UI de GraphQL necesita cargar sus propios recursos");
        csp.Should().NotContain("default-src 'none'",
            "la política estricta de la API dejaría la UI de GraphQL en blanco");
        csp.Should().Contain("connect-src 'self'", "la UI consulta el endpoint /graphql en el mismo origen");
        csp.Should().Contain("worker-src", "el editor Monaco de la UI carga web workers");
        csp.Should().Contain("manifest-src 'self'", "la UI carga su manifest.webmanifest");
        csp.Should().Contain("'unsafe-eval'",
            "HotChocolate compila sus validadores de esquema con new Function() y sin unsafe-eval la UI muere en runtime");
    }

    [Test]
    public async Task Invoke_OtrasCabecerasDeSeguridad_SiguenPresentes()
    {
        var context = await InvocarAsync("/api/productos");

        context.Response.Headers["X-Content-Type-Options"].ToString().Should().Be("nosniff");
        context.Response.Headers["X-Frame-Options"].ToString().Should().Be("DENY");
        context.Response.Headers["Referrer-Policy"].ToString().Should().Be("strict-origin-when-cross-origin");
        context.Response.Headers.ContainsKey("Permissions-Policy").Should().BeTrue();
    }
}

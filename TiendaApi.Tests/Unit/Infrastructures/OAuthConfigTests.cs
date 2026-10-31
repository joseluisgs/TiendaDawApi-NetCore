using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using TiendaApi.Api.Infrastructures;

namespace TiendaApi.Tests.Unit.Infrastructures;

/// <summary>
/// 🎓 Tests de AddOAuthDoors: las seis ramas de decisión de las tres puertas
/// externas (Google, GitHub, Microsoft): puerta cerrada, puerta abierta sin
/// credenciales y puerta abierta con credenciales. El efecto observable es si
/// el esquema del proveedor aparece en IAuthenticationSchemeProvider: con la
/// puerta cerrada la app ni siquiera conoce al proveedor.
/// </summary>
public class OAuthConfigTests
{
    private static IConfiguration Config(Dictionary<string, string?> valores)
        => new ConfigurationBuilder().AddInMemoryCollection(valores).Build();

    /// <summary>
    /// Registra el OAuth de la configuración dada y pregunta si el esquema del
    /// proveedor quedó montado (mismo camino que usará el Challenge en runtime).
    /// </summary>
    private static async Task<bool> TieneEsquema(string proveedor, Dictionary<string, string?> valores)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddOAuthDoors(Config(valores));

        using var sp = services.BuildServiceProvider();
        var schemeProvider = sp.GetRequiredService<IAuthenticationSchemeProvider>();
        var esquemas = await schemeProvider.GetAllSchemesAsync();

        return esquemas.Any(s => s.Name == proveedor);
    }

    [TestCase("Google")]
    [TestCase("GitHub")]
    [TestCase("Microsoft")]
    public async Task PuertaCerrada_NoRegistraElEsquema(string proveedor)
    {
        // Sin Auth:{Proveedor}:Enabled el proveedor no se registra: no hay
        // endpoint de login externo que exponer
        var resultado = await TieneEsquema(proveedor, new());

        resultado.Should().BeFalse();
    }

    [TestCase("Google")]
    [TestCase("GitHub")]
    [TestCase("Microsoft")]
    public async Task PuertaAbiertaSinCredenciales_NoRegistraElEsquema(string proveedor)
    {
        // Enabled=true pero sin ClientId/ClientSecret: falta el material del
        // OAuth y no se puede montar una puerta a medias
        var valores = new Dictionary<string, string?> { [$"Auth:{proveedor}:Enabled"] = "true" };

        var resultado = await TieneEsquema(proveedor, valores);

        resultado.Should().BeFalse();
    }

    [TestCase("Google")]
    [TestCase("GitHub")]
    [TestCase("Microsoft")]
    public async Task PuertaAbiertaConCredenciales_RegistraElEsquema(string proveedor)
    {
        // Con Enabled y credenciales completas, el esquema queda disponible
        // para el Challenge del endpoint /auth/external/{provider}
        var valores = new Dictionary<string, string?>
        {
            [$"Auth:{proveedor}:Enabled"] = "true",
            [$"{proveedor}:ClientId"] = "id-de-prueba",
            [$"{proveedor}:ClientSecret"] = "secreto-de-prueba"
        };

        var resultado = await TieneEsquema(proveedor, valores);

        resultado.Should().BeTrue();
    }
}

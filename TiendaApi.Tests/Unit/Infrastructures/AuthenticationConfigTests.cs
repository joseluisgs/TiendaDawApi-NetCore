using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TiendaApi.Api.Infrastructures;
using TiendaApi.Api.Models;
using TiendaApi.Api.Services.Auth;

namespace TiendaApi.Tests.Unit.Infrastructures;

/// <summary>
/// Tests del switch Auth:Mode en AuthenticationConfig.
///
/// 🎓 Patrón Strategy: la configuración "Auth:Mode" decide qué implementación de
/// IAuthService se registra en el contenedor. Estos tests verifican que el switch
/// selecciona el servicio correcto y que el default es "Custom".
/// </summary>
[TestFixture]
[Category("Unit")]
[Category("Infrastructure")]
[Category("Auth")]
public class AuthenticationConfigTests
{
    private const string ValidJwtKey = "test-jwt-key-with-at-least-32-characters!!";

    private static IConfiguration BuildConfiguration(string? authMode)
    {
        var data = new Dictionary<string, string?>
        {
            ["Jwt:Key"] = ValidJwtKey,
            ["Jwt:Issuer"] = "TiendaApiTest",
            ["Jwt:Audience"] = "TiendaApiTest"
        };

        if (authMode is not null)
            data["Auth:Mode"] = authMode;

        return new ConfigurationBuilder()
            .AddInMemoryCollection(data)
            .Build();
    }

    private static Type? GetRegisteredAuthServiceImplementation(IServiceCollection services)
    {
        var descriptor = services.LastOrDefault(d => d.ServiceType == typeof(IAuthService));
        return descriptor?.ImplementationType;
    }

    [Test]
    public void AddAuthentication_ModoCustom_DebeRegistrarAuthService()
    {
        var services = new ServiceCollection();

        services.AddAuthentication(BuildConfiguration("Custom"));

        GetRegisteredAuthServiceImplementation(services)
            .Should().Be(typeof(AuthService), "Auth:Mode=Custom debe registrar el servicio BCrypt");
    }

    [Test]
    public void AddAuthentication_ModoIdentity_DebeRegistrarIdentityAuthService()
    {
        var services = new ServiceCollection();

        services.AddAuthentication(BuildConfiguration("Identity"));

        GetRegisteredAuthServiceImplementation(services)
            .Should().Be(typeof(IdentityAuthService), "Auth:Mode=Identity debe registrar el servicio Identity");
    }

    [Test]
    public void AddAuthentication_ModoIdentity_DebeRegistrarUserManagerDeIdentity()
    {
        var services = new ServiceCollection();

        services.AddAuthentication(BuildConfiguration("Identity"));

        services.Any(d => d.ServiceType == typeof(UserManager<User>))
            .Should().BeTrue("el modo Identity necesita UserManager<User> en el contenedor");

        services.Any(d => d.ServiceType == typeof(SignInManager<User>))
            .Should().BeTrue("el modo Identity necesita SignInManager<User> en el contenedor");
    }

    [Test]
    public void AddAuthentication_SinAuthMode_DebeUsarCustomPorDefecto()
    {
        var services = new ServiceCollection();

        // Sin "Auth:Mode" en configuración → default "Custom"
        services.AddAuthentication(BuildConfiguration(authMode: null));

        GetRegisteredAuthServiceImplementation(services)
            .Should().Be(typeof(AuthService), "el modo por defecto debe ser Custom (BCrypt)");
    }

    [Test]
    public void AddAuthentication_AmbosModos_DebeRegistrarElMismoMiddlewareJwt()
    {
        // El middleware JWT es idéntico en ambos modos: la diferencia es SOLO
        // cómo se valida la contraseña, no cómo se valida el token.
        foreach (var mode in new[] { "Custom", "Identity" })
        {
            var services = new ServiceCollection();

            services.AddAuthentication(BuildConfiguration(mode));

            services.Any(d => d.ServiceType.GenericTypeArguments.Contains(typeof(JwtBearerOptions)))
                .Should().BeTrue($"modo {mode} debe configurar el middleware JWT Bearer");
        }
    }

    [Test]
    public void AddAuthentication_SinJwtKey_DebeLanzarInvalidOperationException()
    {
        var services = new ServiceCollection();
        var configWithoutKey = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Auth:Mode"] = "Custom" })
            .Build();

        var act = () => services.AddAuthentication(configWithoutKey);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*JWT Key*");
    }
}

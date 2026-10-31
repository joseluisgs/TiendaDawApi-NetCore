using Serilog;

namespace TiendaApi.Api.Infrastructures;

/// <summary>
/// Puertas de login externo (OAuth2/OIDC), activables por configuración.
///
/// Cada puerta solo se registra si Auth:{Proveedor}:Enabled es true: con la
/// puerta cerrada la app ni siquiera conoce al proveedor. Los ClientId y
/// ClientSecret van en user-secrets o variables de entorno, nunca en appsettings.json:
///   dotnet user-secrets set "Google:ClientId" "xxxx.apps.googleusercontent.com"
///   dotnet user-secrets set "Google:ClientSecret" "GOCSPX-xxxx"
///
/// La puerta principal (JWT Bearer) ya está registrada en AuthenticationConfig.
/// Aquí solo se AÑADEN esquemas de proveedores encima: el default sigue siendo JWT.
/// </summary>
public static class OAuthConfig
{
    /// <summary>
    /// Lee las credenciales OAuth de un proveedor (Google, GitHub, Microsoft) y
    /// comprueba que estén completas. Extraído de AddOAuthDoors para que ese
    /// método se quede con el menú de puertas (activar/probar) y no con la
    /// lectura de config; null = faltan credenciales.
    /// </summary>
    private static (string ClientId, string ClientSecret)? ResolverCredenciales(IConfiguration configuration, string proveedor)
    {
        var clientId = configuration[$"{proveedor}:ClientId"];
        var clientSecret = configuration[$"{proveedor}:ClientSecret"];
        if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
            return null;

        return (clientId, clientSecret);
    }

    /// <summary>
    /// Registra las puertas externas activadas en la configuración.
    /// </summary>
    /// <param name="services">Colección de servicios.</param>
    /// <param name="configuration">Configuración de la app (sección Auth).</param>
    /// <returns>IServiceCollection para encadenar.</returns>
    public static IServiceCollection AddOAuthDoors(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Un único AddAuthentication() como menú de puertas: los esquemas
        // defaults (JWT Bearer) ya los puso AuthenticationConfig, aquí solo se
        // añaden esquemas de proveedores encima.
        var auth = services.AddAuthentication();

        if (configuration.GetValue<bool>("Auth:Google:Enabled"))
        {
            if (ResolverCredenciales(configuration, "Google") is { } google)
            {
                auth.AddGoogle(options =>
                {
                    options.ClientId = google.ClientId;
                    options.ClientSecret = google.ClientSecret;
                    options.UsePkce = true;
                });
                Log.Information("🚪 Puerta de login externo activa: Google");
            }
            else
            {
                Log.Warning("🚪 Auth:Google:Enabled=true pero falta ClientId/ClientSecret — puerta Google NO registrada");
            }
        }
        else
        {
            Log.Information("🚪 Auth:Google:Enabled=false — puerta desactivada");
        }

        if (configuration.GetValue<bool>("Auth:GitHub:Enabled"))
        {
            if (ResolverCredenciales(configuration, "GitHub") is { } github)
            {
                auth.AddGitHub(options =>
                {
                    options.ClientId = github.ClientId;
                    options.ClientSecret = github.ClientSecret;
                });
                Log.Information("🚪 Puerta de login externo activa: GitHub");
            }
            else
            {
                Log.Warning("🚪 Auth:GitHub:Enabled=true pero falta ClientId/ClientSecret — puerta GitHub NO registrada");
            }
        }
        else
        {
            Log.Information("🚪 Auth:GitHub:Enabled=false — puerta desactivada");
        }

        if (configuration.GetValue<bool>("Auth:Microsoft:Enabled"))
        {
            if (ResolverCredenciales(configuration, "Microsoft") is { } microsoft)
            {
                auth.AddMicrosoftAccount(options =>
                {
                    options.ClientId = microsoft.ClientId;
                    options.ClientSecret = microsoft.ClientSecret;
                });
                Log.Information("🚪 Puerta de login externo activa: Microsoft");
            }
            else
            {
                Log.Warning("🚪 Auth:Microsoft:Enabled=true pero falta ClientId/ClientSecret — puerta Microsoft NO registrada");
            }
        }
        else
        {
            Log.Information("🚪 Auth:Microsoft:Enabled=false — puerta desactivada");
        }

        return services;
    }
}

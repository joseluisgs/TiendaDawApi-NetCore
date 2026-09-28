using System.Reflection;
using Serilog;

namespace TiendaApi.Api.Infrastructures;

/// <summary>
/// Endpoint GET /version que devuelve información de la versión de la API.
/// </summary>
public static class VersionConfig
{
    /// <summary>
    /// Mapea el endpoint /version.
    /// </summary>
    public static IEndpointRouteBuilder MapVersionEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/version", () =>
        {
            var assembly = Assembly.GetExecutingAssembly();
            var version = assembly.GetName().Version?.ToString() ?? "0.0.0";
            var fileVersion = assembly.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version ?? version;
            var informationVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? version;

            Log.Information("Consultada versión de la API: {Version}", informationVersion);

            return Results.Ok(new
            {
                version = informationVersion,
                fileVersion,
                runtime = Environment.Version.ToString(),
                framework = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription
            });
        })
        .WithName("GetVersion")
        .WithDescription("Devuelve la versión de la API y el runtime")
        .AllowAnonymous();

        return endpoints;
    }
}

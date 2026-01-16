using Microsoft.AspNetCore.Mvc;

namespace TiendaApi.Apis.Controllers;

/// <summary>
/// Controlador temporal de prueba para verificar configuración.
/// BORRAR DESPUÉS DE PROBAR.
/// </summary>
[ApiController]
[Route("api/v1/test")]
public class ConfigTestController : ControllerBase
{
    private readonly IConfiguration _config;

    public ConfigTestController(IConfiguration config)
    {
        _config = config;
    }

    /// <summary>
    /// GET /api/v1/test/config
    /// Muestra los valores de configuración actuales.
    /// </summary>
    [HttpGet("config")]
    public IActionResult GetConfig()
    {
        var jwtKey = _config["Jwt:Key"];
        var redisConn = _config["ConnectionStrings:Redis"];
        var smtpUser = _config["Smtp:Username"];
        var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

        return Ok(new
        {
            entorno = env ?? "Development",
            mensaje = "Valores actuales de configuración",
            jwt = new
            {
                key = jwtKey,
                longitud = jwtKey?.Length ?? 0,
                issource = "appsettings.json o variable de entorno Jwt__Key"
            },
            redis = new
            {
                connection = redisConn,
                issource = "appsettings.json o variable de entorno ConnectionStrings__Redis"
            },
            smtp = new
            {
                username = smtpUser,
                issource = "appsettings.json o variable de entorno Smtp__Username"
            },
            instruccion = "Para cambiar valores, establece las variables de entorno antes de ejecutar"
        });
    }
}

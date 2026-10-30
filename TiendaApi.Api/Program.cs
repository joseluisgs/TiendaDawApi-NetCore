using System.Text;
using System.Text.Json.Serialization.Metadata;
using Serilog;
using Serilog.Extensions.Logging;
using TiendaApi.Api;
using TiendaApi.Api.Data;
using TiendaApi.Api.Data.Seed.Cassandra;
using TiendaApi.Api.Data.Seed.Mongo;
using TiendaApi.Api.Infrastructures;
using TiendaApi.Api.Middleware;

Console.OutputEncoding = Encoding.UTF8;

// Configuración de Serilog (antes del builder)
Log.Logger = SerilogConfig.Configure().CreateLogger();

var builder = WebApplication.CreateBuilder(args);

// Serilog como provider principal de logging (unifica ASP.NET Core + Serilog)
builder.Logging.AddSerilog(Log.Logger);
builder.Host.UseSerilog(Log.Logger);

Log.Information("🚀 Inicializando TiendaApi...");

// ============================================================================
// 🔧 CONFIGURACIÓN DE SERVICIOS (Extension Methods en Infrastructure)
// ============================================================================

var services = builder.Services;
var configuration = builder.Configuration;
var environment = builder.Environment;

// Core - Controllers
services.AddMvcControllers();

// Serialización JSON con source-gen (AppJsonContext) + fallback a reflexión
// para los tipos no declarados (anónimos, ProblemDetails, etc.)
services.ConfigureHttpJsonOptions(options => options.SerializerOptions.TypeInfoResolver =
    JsonTypeInfoResolver.Combine(AppJsonContext.Default, new DefaultJsonTypeInfoResolver()));

services.AddFluentValidationServices();

// Compresión HTTP (Brotli + Gzip)
services.AddResponseCompressionConfig();

// TimeProvider: no se registra en DI porque JwtService lo acepta como
// parámetro opcional con default TimeProvider.System. FakeTimeProvider
// se inyecta directamente en los tests que lo necesitan.

// API
services.AddApiVersioningPolicy();
services.AddSwagger();
services.AddCorsPolicy(configuration, environment.IsDevelopment());
services.AddRateLimitingPolicy();

// Data
services.AddDatabases(configuration, environment);

// Auth
services.AddAuthentication(configuration);
// Puertas OAuth externas (Google/GitHub/Microsoft), activas según Auth:{Proveedor}:Enabled
services.AddOAuthDoors(configuration);

// Business
services.AddRepositories(configuration);
// Cassandra (series: histórico de precios y feed) - ISession singleton
services.AddCassandra(configuration);
services.AddServices();

// Servicios Adicionales (desarrollo vs producción)
services.AddCache(environment, configuration);
services.AddEmail(environment);
services.AddStorage();
services.AddWebSockets();
services.AddBackgroundJobs();

// Health Checks (sondeo de dependencias en /health)
services.AddHealthChecks(environment);

// Caché HTTP (OutputCache + ETag + 304) — solo endpoints con [OutputCache]
services.AddOutputCacheConfig();

// SignalR (Realtime)
services.AddRealtimeSignalR();

// GraphQL
services.AddGraphQL(environment);

// AutoMapper
services.AddAutoMapper();

// ============================================================================
// 🚀 CONSTRUCCIÓN DE LA APLICACIÓN
// ============================================================================

var app = builder.Build();
var isDevelopment = app.Environment.IsDevelopment();

Log.Information("✅ Aplicación construida");

// ============================================================================
// 📍 PIPELINE DE MIDDLEWARES (Extension Methods)
// ============================================================================

app.UseSwaggerUI(isDevelopment);
app.UseResponseCompression();
app.UseGlobalExceptionHandler();

// Security Headers - Siempre activo (no afecta funcionalidad)
app.UseSecurityHeaders();

// Rate Limiting - Protege contra DDoS y fuerza bruta
app.UseRateLimiting();

// HTTPS + HSTS - Solo en producción (para desarrollo/testing local, permitir HTTP)
if (!isDevelopment)
{
    app.UseHsts();
    app.UseHttpsRedirection();
}
else
{
    Log.Information("🔓 Modo desarrollo: HTTP permitido (sin redirección HTTPS)");
}
app.UseCorsPolicy();
app.UseAuthentication();
app.UseAuthorization();
app.UseWebSockets();
app.MapWebSocketEndpoints();
app.MapSignalRHubs();
app.UseStaticFiles();

// Caché HTTP: antes de MapControllers; solo afecta a los endpoints con [OutputCache]
app.UseOutputCacheConfig();
app.MapControllers();
app.MapGraphQLEndpoints();

// Health Check (GET /health → JSON: 200 OK, 503 si alguna dependencia cae)
app.MapHealthEndpoint();

// Versión de la API (GET /version)
app.MapVersionEndpoint();

// ============================================================================
// 🗄️ INICIALIZACIÓN DE DATOS
// ============================================================================

await app.InitializeDatabaseAsync(isDevelopment);
app.InitializeStorage(isDevelopment);

// Cassandra: CREATE IF NOT EXISTS + espera al nodo (~60 s), el tiempo que
// tarda la imagen oficial (JVM) en estar lista.
await app.InitializeCassandraAsync();

// Seed de Cassandra (solo desarrollo): serie histórica de precios y feed de
// ejemplo. Va DESPUÉS del esquema y con scope propio: son datos de ejemplo,
// no un prerequisito del arranque (mismo criterio que el resto de seeders).
if (isDevelopment)
{
    using var cassandraScope = app.Services.CreateScope();
    var cassandraSeeder = cassandraScope.ServiceProvider.GetService<CassandraSeeder>();
    if (cassandraSeeder != null)
        await cassandraSeeder.SeedAsync();
}

PrintStartupInfo(isDevelopment, configuration);

// ============================================================================
// ▶️ ARRANQUE DE LA APLICACIÓN
// ============================================================================

try
{
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "💥 La aplicación falló al iniciar");
    throw;
}
finally
{
    Log.CloseAndFlush();
}


// <summary>
// Imprime en los logs la información de inicio de la aplicación.
// </summary>
// <param name="isDevelopment">Indica si el entorno es de desarrollo.</param>
// <param name="configuration">La configuración de la aplicación.</param>
static void PrintStartupInfo(bool isDevelopment, IConfiguration configuration)
{
    var urls = configuration["ASPNETCORE_URLS"]?.Split(';') ?? new[] { "http://localhost:5000" };
    var firstUrl = urls.FirstOrDefault() ?? "http://localhost:5000";
    var protocol = firstUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ? "https" : "http";
    var host = firstUrl.Contains("://") ? firstUrl.Split("://")[1].Split(':')[0] : "localhost";
    var port = firstUrl.Contains(':') ? firstUrl.Split(':').Last() : "5000";

    var mode = isDevelopment ? "DESARROLLO" : "PRODUCCION";
    var baseUrl = $"{protocol}://{host}:{port}";

    Log.Information("=================================================================");
    Log.Information("TiendaApi - API REST Educativa");
    Log.Information("=================================================================");
    Log.Information("DOCUMENTACIÓN:");
    Log.Information("Documentacion Swagger:  {BaseUrl}/", baseUrl);
    Log.Information("=================================================================");
    Log.Information("GRAPHQL:");
    Log.Information("GraphQL UI:            {BaseUrl}/graphql", baseUrl);
    Log.Information("=================================================================");
    Log.Information("WEBSOCKETS:");
    Log.Information("  Productos (publico):  ws://{Host}:{Port}/ws/productos", host, port);
    Log.Information("  Pedidos (auth JWT):   ws://{Host}:{Port}/ws/pedidos?token=JWT", host, port);
    Log.Information("=================================================================");
    Log.Information("SIGNALR (Realtime):");
    Log.Information("  Productos (publico):  ws://{Host}:{Port}/hubs/productos", host, port);
    Log.Information("  Pedidos (auth JWT):   ws://{Host}:{Port}/hubs/pedidos", host, port);
    Log.Information("=================================================================");
    Log.Information("ENDPOINTS REST:");
    Log.Information("  Auth:       POST {BaseUrl}/api/v1/auth/signup, /api/v1/auth/signin", baseUrl);
    Log.Information("  Auth Ext:   GET  {BaseUrl}/api/v1/auth/external/{provider} + /external-callback", baseUrl);
    Log.Information("  Categorias: GET/POST/PUT/DELETE {BaseUrl}/api/categorias", baseUrl);
    Log.Information("  Productos:  GET/POST/PUT/DELETE {BaseUrl}/api/productos", baseUrl);
    Log.Information("  Pedidos:    GET/POST {BaseUrl}/api/pedidos", baseUrl);
    Log.Information("  Usuarios:   GET/POST/PUT/DELETE {BaseUrl}/api/users", baseUrl);

    // 🔐 Los 4 modos de autenticación del proyecto: 2 de contraseña (Auth:Mode)
    // y 2 de login externo (el callback usa el servicio del modo activo)
    var authMode = configuration["Auth:Mode"] ?? "Custom";
    Log.Information("=================================================================");
    Log.Information("AUTENTICACIÓN:");
    if (authMode == "Identity")
    {
        Log.Information("  Contraseña:  JWT con Identity (UserManager / PBKDF2) [Auth:Mode=Identity]");
        Log.Information("  Callback OAuth: IdentityAuthService (vincula en AspNetUserLogins)");
    }
    else
    {
        Log.Information("  Contraseña:  JWT Manual (BCrypt) [Auth:Mode=Custom]");
        Log.Information("  Callback OAuth: AuthService (vincula en user_logins, dummy hash OAUTH-*)");
    }
    foreach (var provider in new[] { "Google", "GitHub", "Microsoft" })
    {
        var enabled = string.Equals(configuration[$"Auth:{provider}:Enabled"], "true", StringComparison.OrdinalIgnoreCase);
        var hasCreds = !string.IsNullOrEmpty(configuration[$"{provider}:ClientId"])
                       && !string.IsNullOrEmpty(configuration[$"{provider}:ClientSecret"]);
        var estado = !enabled ? "desactivada"
            : hasCreds ? "ACTIVA"
            : "habilitada sin credenciales (puerta cerrada)";
        Log.Information("  OAuth {Provider,-9}: {Estado}", provider, estado);
    }
    Log.Information("=================================================================");
    Log.Information("DATOS SEMBRADOS (Seed):");
    if (authMode == "Identity")
    {
        Log.Information("  PostgreSQL: usuarios seed con hash PBKDF2 (ver CREDENCIALES)");
    }
    else
    {
        Log.Information("  PostgreSQL: usuarios seed con hash BCrypt (ver CREDENCIALES)");
    }
    Log.Information("              Categorias: Electronica, Ropa, Libros");
    Log.Information("              Productos: Laptop Dell XPS 15, Camiseta Nike, Clean Code");
    Log.Information("  MongoDB:    3 pedidos de ejemplo");
    Log.Information("=================================================================");
    Log.Information("CREDENCIALES DE PRUEBA:");
    if (authMode == "Identity")
    {
        // La política de Identity (>=6 chars, dígito y mayúscula) rechaza "admin"/"userdaw"
        Log.Information("  Admin:   admin / Admin123! (ROLE_ADMIN)");
        Log.Information("  Usuario: userdaw / Admin123! (ROLE_USER)");
    }
    else
    {
        Log.Information("  Admin:   admin / admin (ROLE_ADMIN)");
        Log.Information("  Usuario: userdaw / userdaw (ROLE_USER)");
    }
    Log.Information("=================================================================");
    Log.Information("🚀 Aplicacion iniciada correctamente en {BaseUrl} ({Mode})",
        baseUrl, mode);
    Log.Information("=================================================================");
}

/// <summary>
/// Declaración parcial del programa para que los tests de integración
/// puedan crear la aplicación con <c>WebApplicationFactory&lt;Program&gt;</c>.
/// </summary>
public partial class Program;

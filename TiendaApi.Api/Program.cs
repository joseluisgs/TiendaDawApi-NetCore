using System.Text;
using Serilog;
using Serilog.Extensions.Logging;
using TiendaApi.Api;
using TiendaApi.Api.Data;
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
services.AddFluentValidationServices();

// API
services.AddApiVersioningPolicy();
services.AddSwagger();
services.AddCorsPolicy(configuration, environment.IsDevelopment());
services.AddRateLimitingPolicy();

// Data
services.AddDatabases(configuration);

// Auth
services.AddAuthentication(configuration);

// Business
services.AddRepositories(configuration);
services.AddServices();

// Servicios Adicionales (desarrollo vs producción)
services.AddCache(environment);
services.AddEmail(environment);
services.AddStorage();
services.AddWebSockets();
services.AddBackgroundJobs();

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
app.MapControllers();
app.MapGraphQLEndpoints();

// ============================================================================
// 🗄️ INICIALIZACIÓN DE DATOS
// ============================================================================

await app.InitializeDatabaseAsync(isDevelopment);
app.InitializeStorage(isDevelopment);

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


/// <summary>
/// Imprime en los logs la información de inicio de la aplicación.
/// </summary>
/// <param name="isDevelopment">Indica si el entorno es de desarrollo.</param>
/// <param name="configuration">La configuración de la aplicación.</param>
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
    Log.Information("  Categorias: GET/POST/PUT/DELETE {BaseUrl}/api/categorias", baseUrl);
    Log.Information("  Productos:  GET/POST/PUT/DELETE {BaseUrl}/api/productos", baseUrl);
    Log.Information("  Pedidos:    GET/POST {BaseUrl}/api/pedidos", baseUrl);
    Log.Information("  Usuarios:   GET/POST/PUT/DELETE {BaseUrl}/api/users", baseUrl);
    Log.Information("=================================================================");
    Log.Information("DATOS SEMBRADOS (Seed):");
    Log.Information("  PostgreSQL: admin (admin/admin), userdaw (userdaw/userdaw)");
    Log.Information("              Categorias: Electronica, Ropa, Libros");
    Log.Information("              Productos: Laptop Dell XPS 15, Camiseta Nike, Clean Code");
    Log.Information("  MongoDB:    3 pedidos de ejemplo");
    Log.Information("=================================================================");
    Log.Information("CREDENCIALES DE PRUEBA:");
    Log.Information("  Admin:   admin / admin (ROLE_ADMIN)");
    Log.Information("  Usuario: userdaw / userdaw (ROLE_USER)");
    Log.Information("=================================================================");
    Log.Information("🚀 Aplicacion iniciada correctamente en {BaseUrl} ({Mode})",
        baseUrl, mode);
    Log.Information("=================================================================");
}

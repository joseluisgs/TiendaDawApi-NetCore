using Serilog;
using TiendaApi.Apis;
using TiendaApi.Apis.Data;
using TiendaApi.Apis.Data.Seed.Mongo;
using TiendaApi.Apis.Infrastructures;
using TiendaApi.Apis.Middleware;
using TiendaApi.Apis.WebSockets.Pedidos;
using TiendaApi.Apis.WebSockets.Productos;

// Configuración de Serilog
Log.Logger = SerilogConfig.Configure().CreateLogger();

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog();

Log.Information("🚀 Inicializando TiendaApi...");

// ============================================================================
// 🔧 CONFIGURACIÓN DE SERVICIOS (Extension Methods en Infrastructure)
// ============================================================================

var services = builder.Services;
var configuration = builder.Configuration;
var environment = builder.Environment;

// Core - Controllers
services.AddMvcControllers();
services.AddFluentValidation();

// API
services.AddApiVersioningPolicy();
services.AddSwagger();
services.AddCorsPolicy();

// Data
services.AddDatabases(configuration);

// Auth
services.AddAuthentication(configuration);

// Business
services.AddRepositories();
services.AddServices();

// Additional Services (desarrollo vs producción)
services.AddCache(environment);
services.AddEmail(environment);
services.AddStorage();
services.AddWebSockets();

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
app.UseGraphiQL();
app.UseGlobalExceptionHandler();
app.UseHttpsRedirection();
app.UseCorsPolicy();
app.UseAuthentication();
app.UseAuthorization();
app.UseWebSockets();
app.MapWebSocketEndpoints();
app.UseStaticFiles();
app.MapControllers();
app.MapGraphQL();

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
    var port = urls.FirstOrDefault()?.Split(':').LastOrDefault() ?? "5000";

    Log.Information("=================================================================");
    Log.Information("TiendaApi - API REST Educativa");
    Log.Information("=================================================================");
    Log.Information("Documentacion Swagger:  http://localhost:{Port}/", port);
    Log.Information("GraphiQL UI:            http://localhost:{Port}/graphiql", port);
    Log.Information("=================================================================");
    Log.Information("WEBSOCKETS:");
    Log.Information("  Productos (broadcast): ws://localhost:{Port}/ws/v1/productos", port);
    Log.Information("  Pedidos (auth JWT):     ws://localhost:{Port}/ws/v1/pedidos?token=JWT", port);
    Log.Information("=================================================================");
    Log.Information("ENDPOINTS REST:");
    Log.Information("  Auth:       POST /api/v1/auth/signup, /api/v1/auth/signin");
    Log.Information("  Categorias: GET/POST/PUT/DELETE /api/categorias");
    Log.Information("  Productos:  GET/POST/PUT/DELETE /api/productos");
    Log.Information("  Pedidos:    GET/POST /api/pedidos");
    Log.Information("  Usuarios:   GET/POST/PUT/DELETE /api/users");
    Log.Information("=================================================================");
    Log.Information("DATOS SEMBRADOS (Seed):");
    Log.Information("  PostgreSQL: admin (admin@tienda.com/admin), userdaw (userdaw@tienda.com/userdaw)");
    Log.Information("              Categorias: Electronica, Ropa, Libros");
    Log.Information("              Productos: Laptop Dell XPS 15, Camiseta Nike, Clean Code");
    Log.Information("  MongoDB:    3 pedidos de ejemplo");
    Log.Information("=================================================================");
    Log.Information("CREDENCIALES DE PRUEBA:");
    Log.Information("  Admin:   admin@tienda.com / admin (ROLE_ADMIN)");
    Log.Information("  Usuario: userdaw@tienda.com / userdaw (ROLE_USER)");
    Log.Information("=================================================================");
    Log.Information("🚀 Aplicacion iniciada correctamente en http://localhost:{Port} ({Mode})",
        port, isDevelopment ? "DESARROLLO" : "PRODUCCION");
    Log.Information("=================================================================");
}

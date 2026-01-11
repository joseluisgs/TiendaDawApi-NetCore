using System.Text;
using System.Threading.Channels;
using GraphQL;
using GraphQL.Types;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using TiendaApi.Data;
using TiendaApi.GraphQL;
using TiendaApi.GraphQL.Types;
using TiendaApi.Middleware;
using TiendaApi.Mappers;
using TiendaApi.Repositories.Categorias;
using TiendaApi.Repositories.Productos;
using TiendaApi.Repositories.Usuarios;
using TiendaApi.Repositories.Pedidos;
using TiendaApi.Services.Auth;
using TiendaApi.Services.Cache;
using TiendaApi.Services.Categorias;
using TiendaApi.Services.Email;
using TiendaApi.Services.Pedidos;
using TiendaApi.Services.Productos;
using TiendaApi.Services.Users;
using TiendaApi.WebSockets.Pedidos;
using TiendaApi.WebSockets.Productos;

var builder = WebApplication.CreateBuilder(args);

// ============================================================================
// CONFIGURACIÓN DE LA APLICACIÓN
// ============================================================================

// Añadimos soporte para controladores MVC con negociación de contenido.
// Esto permite que la API devuelva diferentes formatos (JSON, XML) según
// lo que solicite el cliente (header Accept).
builder.Services.AddControllers(options =>
{
    options.RespectBrowserAcceptHeader = true;
    options.ReturnHttpNotAcceptable = true;
})
.AddXmlSerializerFormatters()
.AddXmlDataContractSerializerFormatters();

// Configuración de Swagger/OpenAPI para documentación interactiva de la API.
// Swagger proporciona una interfaz web para explorar y probar los endpoints.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    { 
        Title = "TiendaApi - API REST con Doble Enfoque de Manejo de Errores",
        Version = "v1",
        Description = @"API REST educativa que demuestra DOS enfoques diferentes 
de manejo de errores en el desarrollo de software:
        
**Categorías**: Enfoque Tradicional con Excepciones (método clásico)
**Productos**: Patrón Result Moderno (programación funcional)
        
Esta API permite comparar ambos enfoques para entender cuándo usar cada uno.

## 🔐 Autenticación

Esta API utiliza **JWT (JSON Web Tokens)** para la autenticación.

### Pasos para autenticarse:

1. **Registrar un usuario**: `POST /v1/auth/signup`
2. **Iniciar sesión**: `POST /v1/auth/signin` → Recibirás un token JWT
3. **Usar el token**: Haz clic en el botón 🔒 **Authorize** arriba
4. **Introduce el token** en el campo que aparece (sin 'Bearer')
5. Todos los endpoints protegidos ahora funcionarán automáticamente

## 📚 Credenciales de prueba

- **Usuario Admin**: 
  - Email: `admin@tienda.com`
  - Password: `Admin123`

- **Usuario Normal**: 
  - Email: `user@tienda.com`
  - Password: `User123`

## 🎯 Conceptos Clave

### Programación Orientada al Resultado (ROP)
Los endpoints de **Productos** usan el patrón Result<T, E>:
- ✅ Camino feliz: Operación exitosa devuelve Result.Success
- ❌ Camino de error: Fallo devuelve Result.Failure con detalles
- 🔗 Los errores fluyen automáticamente sin necesidad de try/catch

### Comparación de enfoques:
- **Categorías** (tradicional): Lanza excepciones, GlobalExceptionHandler las captura
- **Productos** (moderno): Sin excepciones, pattern matching con Result<T,E>

Explora ambos para entender las ventajas de cada enfoque! 🚀",
        Contact = new OpenApiContact
        {
            Name = "José Luis González Sánchez",
            Email = "joseluis.gonzalez@iesluisvives.org",
            Url = new Uri("https://joseluisgs.dev")
        },
        License = new OpenApiLicense
        {
            Name = "Creative Commons BY-NC-SA 4.0",
            Url = new Uri("https://creativecommons.org/licenses/by-nc-sa/4.0/")
        }
    });

    // Configuración del esquema de seguridad JWT para Swagger.
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = @"Autenticación JWT usando el esquema Bearer.

**Introduce solo el token JWT** (sin la palabra 'Bearer').

Ejemplo: Si tu token es `eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...`
Simplemente pega ese valor aquí.

Pasos:
1. Obtén tu token llamando a POST /v1/auth/signin
2. Haz clic en el botón 🔒 Authorize arriba
3. Pega el token JWT en el campo 'Value'
4. Haz clic en Authorize"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });

    // Incluir comentarios XML en la documentación de Swagger.
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
    }
});

// ============================================================================
// CONFIGURACIÓN DE BASE DE DATOS
// ============================================================================

// Configuración de PostgreSQL con Entity Framework Core.
// La cadena de conexión se lee del archivo de configuración (appsettings.json).
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? "Host=localhost;Database=tienda;Username=admin;Password=admin123";

// Registro del DbContext con soporte para PostgreSQL.
// UseNpgsql es el proveedor específico para PostgreSQL.
builder.Services.AddDbContext<TiendaDbContext>(options =>
    options.UseNpgsql(connectionString));

// ============================================================================
// INYECCIÓN DE DEPENDENCIAS
// ============================================================================

// Registramos los repositorios como serviciosScoped (una instancia por solicitud).
// Los repositorios encapsulan el acceso a datos y proporcionan abstracción.
builder.Services.AddScoped<ICategoriaRepository, CategoriaRepository>();
builder.Services.AddScoped<IProductoRepository, ProductoRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IPedidosRepository, PedidosRepository>();

// Registramos los servicios de dominio.
// Los servicios contienen la lógica de negocio y usan los repositorios.
builder.Services.AddScoped<ICategoriaService, CategoriaService>();
builder.Services.AddScoped<IProductoService, ProductoService>();
builder.Services.AddScoped<IPedidosService, PedidosService>();

// Servicios de autenticación y gestión de usuarios.
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();

// Servicio de caché distribuida con Redis.
// Redis almacena datos en memoria para acceso rápido.
builder.Services.AddStackExchangeRedisCache(options =>
{
    var redisConnection = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";
    options.Configuration = redisConnection;
    options.InstanceName = "TiendaApi:";
});
builder.Services.AddScoped<ICacheService, RedisCacheService>();

// Servicio de correo electrónico con procesamiento en segundo plano.
// Channel<T> permite comunicación asíncrona entre productores y consumidores.
builder.Services.AddSingleton(Channel.CreateUnbounded<EmailMessage>());
builder.Services.AddScoped<IEmailService, MailKitEmailService>();
builder.Services.AddHostedService<EmailBackgroundService>();

// Handlers de WebSocket para notificaciones en tiempo real.
builder.Services.AddSingleton<ProductoWebSocketHandler>();
builder.Services.AddSingleton<PedidoWebSocketHandler>();

// Servicios de GraphQL para consultas dinámicas.
builder.Services.AddScoped<IDocumentExecuter, DocumentExecuter>();
builder.Services.AddScoped<ISchema, TiendaSchema>();
builder.Services.AddScoped<TiendaQuery>();
builder.Services.AddScoped<ProductoType>();
builder.Services.AddScoped<CategoriaType>();

// Configuración de AutoMapper para mapeo automático entre entidades y DTOs.
builder.Services.AddAutoMapper(typeof(MappingProfile), typeof(PedidoProfile));

// ============================================================================
// AUTENTICACIÓN Y AUTORIZACIÓN
// ============================================================================

// Configuración de autenticación JWT (JSON Web Tokens).
// JWT es un estándar para crear tokens de acceso seguros.
var jwtKey = builder.Configuration["Jwt:Key"] 
    ?? throw new InvalidOperationException("JWT Key not configured");
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "TiendaApi";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "TiendaApi";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ValidateIssuer = true,
        ValidIssuer = jwtIssuer,
        ValidateAudience = true,
        ValidAudience = jwtAudience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

// Definición de políticas de autorización basadas en roles.
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("RequireAdminRole", policy => policy.RequireRole("ADMIN"))
    .AddPolicy("RequireUserRole", policy => policy.RequireRole("USER", "ADMIN"));

// ============================================================================
// CONFIGURACIÓN CORS (Cross-Origin Resource Sharing)
// ============================================================================

// CORS permite que aplicaciones frontend desde dominios diferentes
// puedan acceder a esta API.
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// ============================================================================
// CONSTRUCCIÓN DE LA APLICACIÓN
// ============================================================================
var app = builder.Build();

// ============================================================================
// PIPELINE DE MIDDLEWARE
// ============================================================================

// Middleware de Swagger: solo disponible en desarrollo.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "TiendaApi v1");
        options.RoutePrefix = string.Empty; // Swagger en la URL raíz
    });
}

// Interfaz GraphiQL para probar consultas GraphQL.
// Accesible en /graphiql
app.MapGet("/graphiql", async context =>
{
    context.Response.ContentType = "text/html";
    await context.Response.WriteAsync(@"
<!DOCTYPE html>
<html>
<head>
    <title>GraphiQL</title>
    <link href=""https://unpkg.com/graphiql/graphiql.min.css"" rel=""stylesheet"" />
</head>
<body style=""margin: 0;"">
    <div id=""graphiql"" style=""height: 100vh;""></div>
    <script crossorigin src=""https://unpkg.com/react/umd/react.production.min.js""></script>
    <script crossorigin src=""https://unpkg.com/react-dom/umd/react-dom.production.min.js""></script>
    <script crossorigin src=""https://unpkg.com/graphiql/graphiql.min.js""></script>
    <script>
        const fetcher = GraphiQL.createFetcher({ url: '/graphql' });
        ReactDOM.render(
            React.createElement(GraphiQL, { fetcher: fetcher }),
            document.getElementById('graphiql')
        );
    </script>
</body>
</html>
");
});

// Middleware global para manejo de excepciones.
// Captura excepciones no controladas y las convierte en respuestas HTTP apropiadas.
app.UseGlobalExceptionHandler();

app.UseHttpsRedirection();

// Middleware de CORS.
app.UseCors("AllowAll");

// Middleware de autenticación y autorización.
app.UseAuthentication();
app.UseAuthorization();

// Soporte para conexiones WebSocket.
app.UseWebSockets();

// Endpoint WebSocket para notificaciones de productos.
app.Map("/ws/v1/productos", async context =>
{
    if (context.WebSockets.IsWebSocketRequest)
    {
        var webSocket = await context.WebSockets.AcceptWebSocketAsync();
        var handler = context.RequestServices.GetRequiredService<ProductoWebSocketHandler>();
        await handler.HandleConnectionAsync(context, webSocket);
    }
    else
    {
        context.Response.StatusCode = 400;
    }
});

// Endpoint WebSocket para notificaciones de pedidos.
app.Map("/ws/v1/pedidos", async context =>
{
    if (context.WebSockets.IsWebSocketRequest)
    {
        var webSocket = await context.WebSockets.AcceptWebSocketAsync();
        var handler = context.RequestServices.GetRequiredService<PedidoWebSocketHandler>();
        await handler.HandleConnectionAsync(context, webSocket);
    }
    else
    {
        context.Response.StatusCode = 400;
    }
});

// Mapeo automático de controladores.
app.MapControllers();

// ============================================================================
// INICIALIZACIÓN DE BASE DE DATOS
// ============================================================================

// Aplicamos migraciones y sembramos datos iniciales al iniciar.
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<TiendaDbContext>();
        
        // Crear la base de datos si no existe.
        context.Database.EnsureCreated();
        
        // O aplicar migraciones pendientes (usar en producción).
        // context.Database.Migrate();
        
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("Base de datos inicializada");
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Error al inicializar la base de datos");
    }
}

// ============================================================================
// INICIO DE LA APLICACIÓN
// ============================================================================


app.Run();

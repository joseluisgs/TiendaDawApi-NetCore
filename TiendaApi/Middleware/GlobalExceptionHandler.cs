using System.Net;
using System.Text.Json;
using TiendaApi.Exceptions;

namespace TiendaApi.Middleware;

/// <summary>
/// Global exception handler middleware
/// Catches exceptions thrown by Categorías endpoints (traditional approach)
/// 
/// Java Spring Boot equivalent:
/// @ControllerAdvice with @ExceptionHandler methods
/// Centralizes exception-to-HTTP response mapping
/// 
/// Note: This ONLY handles exceptions from Categorías
/// Productos/Pedidos/Users use Result Pattern and don't throw exceptions
/// </summary>
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
            _logger.LogError(ex, "Exception occurred: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, message, errors) = exception switch
        {
            NotFoundException notFound => 
                (HttpStatusCode.NotFound, notFound.Message, (Dictionary<string, string[]>?)null),
            
            ValidationException validation => 
                (HttpStatusCode.BadRequest, validation.Message, validation.Errors),
            
            BusinessException business => 
                (HttpStatusCode.BadRequest, business.Message, (Dictionary<string, string[]>?)null),
            
            _ => 
                (HttpStatusCode.InternalServerError, "Error interno del servidor", (Dictionary<string, string[]>?)null)
        };

        object response = errors != null
            ? new { message, errors }
            : new { message };

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };

        return context.Response.WriteAsync(
            JsonSerializer.Serialize(response, jsonOptions));
    }
}

/// <summary>
/// Extension method to register the middleware
/// </summary>
public static class GlobalExceptionHandlerExtensions
{
    public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder app)
    {
        return app.UseMiddleware<GlobalExceptionHandler>();
    }
}

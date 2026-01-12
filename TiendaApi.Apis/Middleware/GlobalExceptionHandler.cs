using System.Net;
using System.Text.Json;
using TiendaApi.Apis.Exceptions;

namespace TiendaApi.Apis.Middleware;

/// <summary>
/// Manejador global de excepciones.
/// Captura excepciones lanzadas por los endpoints de Categorías.
/// Convierte excepciones en respuestas HTTP consistentes.
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
            _logger.LogError(ex, "Ocurrió una excepción: {Message}", ex.Message);
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
/// Método de extensión para registrar el middleware.
/// </summary>
public static class GlobalExceptionHandlerExtensions
{
    public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder app)
    {
        return app.UseMiddleware<GlobalExceptionHandler>();
    }
}

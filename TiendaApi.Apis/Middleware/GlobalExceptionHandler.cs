using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using TiendaApi.Apis.Errors;
using TiendaApi.Apis.Exceptions;

namespace TiendaApi.Apis.Middleware;

/// <summary>
/// Manejador global de excepciones.
/// Maneja excepciones y errores del dominio (Result Pattern).
/// Genera respuestas HTTP consistentes y trazables.
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
            var errorId = Guid.NewGuid().ToString()[..8];
            _logger.LogError(ex, "Excepción no manejada. ErrorId: {ErrorId}, Message: {Message}", 
                errorId, ex.Message);
            await HandleExceptionAsync(context, ex, errorId);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception, string errorId)
    {
        context.Response.ContentType = "application/json";
        
        var (statusCode, message, errors, errorType) = exception switch
        {
            // Excepciones personalizadas
            NotFoundException notFound => (
                HttpStatusCode.NotFound,
                notFound.Message,
                (Dictionary<string, string[]>?)null,
                ErrorType.NotFound
            ),
            
            ValidationException validation => (
                HttpStatusCode.BadRequest,
                validation.Message,
                validation.Errors,
                ErrorType.Validation
            ),
            
            BusinessException business => (
                HttpStatusCode.BadRequest,
                business.Message,
                (Dictionary<string, string[]>?)null,
                ErrorType.BusinessRule
            ),
            
            UnauthorizedAccessException => (
                HttpStatusCode.Unauthorized,
                "No autorizado",
                (Dictionary<string, string[]>?)null,
                ErrorType.Unauthorized
            ),
            
            ArgumentException argument => (
                HttpStatusCode.BadRequest,
                argument.Message,
                (Dictionary<string, string[]>?)null,
                ErrorType.Validation
            ),
            
            DbUpdateException dbUpdate => (
                HttpStatusCode.Conflict,
                "Error al actualizar la base de datos",
                (Dictionary<string, string[]>?)null,
                ErrorType.Internal
            ),
            
            TimeoutException => (
                HttpStatusCode.RequestTimeout,
                "Tiempo de espera agotado",
                (Dictionary<string, string[]>?)null,
                ErrorType.Internal
            ),
            
            // Default - Error interno no manejado
            _ => (
                HttpStatusCode.InternalServerError,
                "Ha ocurrido un error interno",
                (Dictionary<string, string[]>?)null,
                ErrorType.Internal
            )
        };

        context.Response.StatusCode = (int)statusCode;

        var response = new
        {
            errorId,
            message,
            errorType = errorType.ToString(),
            timestamp = DateTime.UtcNow.ToString("o"),
            path = context.Request.Path,
            method = context.Request.Method,
            errors
        };

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response, jsonOptions));
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

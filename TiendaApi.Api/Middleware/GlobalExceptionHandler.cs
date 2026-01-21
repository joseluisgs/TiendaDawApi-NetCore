using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using TiendaApi.Api.Exceptions;

namespace TiendaApi.Api.Middleware;

/// <summary>
/// Manejador global de excepciones.
/// Maneja excepciones y errores del dominio (Result Pattern).
/// Genera respuestas HTTP consistentes y trazables.
/// </summary>
public class GlobalExceptionHandler(
    RequestDelegate next,
    ILogger<GlobalExceptionHandler> logger
)
{
    private readonly RequestDelegate _next = next;
    private readonly ILogger<GlobalExceptionHandler> _logger = logger;

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

    /// <summary>Maneja las excepciones capturadas.</summary>
    /// <param name="context">Contexto HTTP.</param>
    /// <param name="exception">Excepción capturada.</param>
    /// <param name="errorId">ID de seguimiento del error.</param>
    private async Task HandleExceptionAsync(HttpContext context, Exception exception, string errorId)
    {
        context.Response.ContentType = "application/json";

        var (statusCode, message, errors, errorType) = exception switch
        {
            NotFoundException notFound => (
                404,
                notFound.Message,
                (Dictionary<string, string[]>?)null,
                "NotFoundError"
            ),

            TiendaApi.Api.Exceptions.ValidationException validation => (
                400,
                validation.Message,
                validation.Errors,
                "ValidationError"
            ),

            BusinessException business => (
                400,
                business.Message,
                (Dictionary<string, string[]>?)null,
                "BusinessRuleError"
            ),

            UnauthorizedAccessException => (
                401,
                "No autorizado",
                (Dictionary<string, string[]>?)null,
                "UnauthorizedError"
            ),

            ArgumentException argument => (
                400,
                argument.Message,
                (Dictionary<string, string[]>?)null,
                "ValidationError"
            ),

            DbUpdateException => (
                409,
                "Error al actualizar la base de datos",
                (Dictionary<string, string[]>?)null,
                "ConflictError"
            ),

            TimeoutException => (
                408,
                "Tiempo de espera agotado",
                (Dictionary<string, string[]>?)null,
                "InternalError"
            ),

            _ => (
                500,
                "Ha ocurrido un error interno",
                (Dictionary<string, string[]>?)null,
                "InternalError"
            )
        };

        context.Response.StatusCode = statusCode;

        var response = new
        {
            errorId,
            message,
            errorType,
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
/// Extensiones para registro del middleware.
/// </summary>
public static class GlobalExceptionHandlerExtensions
{
    /// <summary>Registra el middleware de excepciones.</summary>
    /// <param name="app">Constructor de la aplicación.</param>
    /// <returns>IApplicationBuilder.</returns>
    public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder app)
    {
        return app.UseMiddleware<GlobalExceptionHandler>();
    }
}

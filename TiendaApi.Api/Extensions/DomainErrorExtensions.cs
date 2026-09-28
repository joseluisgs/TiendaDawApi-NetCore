using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TiendaApi.Api.Errors;

namespace TiendaApi.Api.Extensions;

/// <summary>
/// Opción C (§7.8.3): centraliza el mapeo <see cref="DomainError"/> → HTTP en una
/// única extensión, en lugar de repetir un <c>error switch</c> en cada controlador.
/// Mantiene los códigos y el shape <c>{message, ...}</c> de las respuestas actuales
/// para no romper los tests E2E.
/// </summary>
public static class DomainErrorExtensions
{
    /// <summary>
    /// Convierte un error de dominio en su respuesta HTTP equivalente.
    /// </summary>
    /// <param name="error">Error de dominio tipado.</param>
    /// <returns>
    /// 404 <c>NotFoundError</c> · 400 <c>ValidationError</c> (+ <c>errors</c> por campo) ·
    /// 409 <c>ConflictError</c> · 400 <c>BusinessRuleError</c> · 401 <c>UnauthorizedError</c> ·
    /// 403 <c>ForbiddenError</c> · 500 <c>InternalError</c>/desconocidos.
    /// </returns>
    public static IActionResult ToHttpResult(this DomainError error) => error switch
    {
        NotFoundError e => new NotFoundObjectResult(new { message = e.Message }),
        ValidationError e => new BadRequestObjectResult(new { message = e.Message, errors = e.ValidationErrors }),
        ConflictError e => new ConflictObjectResult(new { message = e.Message }),
        BusinessRuleError e => new BadRequestObjectResult(new { message = e.Message }),
        UnauthorizedError e => new UnauthorizedObjectResult(new { message = e.Message }),
        ForbiddenError e => new ObjectResult(new { message = e.Message }) { StatusCode = StatusCodes.Status403Forbidden },
        _ => new ObjectResult(new { message = error.Message }) { StatusCode = StatusCodes.Status500InternalServerError }
    };
}

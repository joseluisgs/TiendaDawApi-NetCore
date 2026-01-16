using System.Security.Claims;

namespace TiendaApi.Apis.Services.Auth;

/// <summary>
/// Servicio para extraer información de tokens JWT.
/// Proporciona métodos para extraer claims, userId y rol de tokens JWT.
/// </summary>
/// <remarks>
/// <para><b>Propósito:</b></para>
/// <list type="bullet">
///   <item><description>Separar la lógica de extracción de JWT del servicio de generación.</description></item>
///   <item><description>Permitir reutilización en WebSockets, Controllers, y otros servicios.</description></item>
///   <item><description>Facilitar el testing de componentes que dependen de información del JWT.</description></item>
/// </list>
/// 
/// <para><b>Ejemplo de uso en un WebSocket:</b></para>
/// <code>
/// public class MiWebSocketHandler
/// {
///     private readonly IJwtTokenExtractor _tokenExtractor;
///     
///     public MiWebSocketHandler(IJwtTokenExtractor tokenExtractor)
///     {
///         _tokenExtractor = tokenExtractor;
///     }
///     
///     public async Task HandleConnectionAsync(HttpContext context)
///     {
///         var token = context.Request.Query["token"].FirstOrDefault();
///         var (userId, isAdmin) = _tokenExtractor.ExtractUserInfo(token);
///     }
/// }
/// </code>
/// 
/// <para><b>Ejemplo de uso en un Controller:</b></para>
/// <code>
/// [ApiController]
/// [Route("api/[controller]")]
/// public class PedidosController : ControllerBase
/// {
///     private readonly IJwtTokenExtractor _tokenExtractor;
///     
///     [HttpGet("mis-datos")]
///     public IActionResult GetMisDatos()
///     {
///         var userId = _tokenExtractor.ExtractUserId(Request);
///         // Usar userId para obtener datos del usuario
///     }
/// }
/// </code>
/// 
/// <para><b>Claims extraídos:</b></para>
/// <list type="table">
///   <item>
///     <term>NameIdentifier</term>
///     <description>Identificador único del usuario (normalmente el ID de la BD).</description>
///   </item>
///   <item>
///     <term>Role</term>
///     <description>Rol del usuario (admin, cliente, etc.).</description>
///   </item>
///   <item>
///     <term>Email</term>
///     <description>Email del usuario (claim email).</description>
///   </item>
///   <item>
///     <term>Sub</term>
///     <description>Nombre de usuario o identificador textual.</description>
///   </item>
/// </list>
/// </remarks>
public interface IJwtTokenExtractor
{
    /// <summary>
    /// Extrae el identificador de usuario (claim NameIdentifier) del token JWT.
    /// </summary>
    /// <param name="token">Token JWT a procesar.</param>
    /// <returns>El ID del usuario si el token es válido y contiene el claim; null en caso contrario.</returns>
    /// <remarks>
    /// <para><b>Ejemplo:</b></para>
    /// <code>
    /// var userId = _tokenExtractor.ExtractUserId(token);
    /// if (userId.HasValue)
    /// {
    ///     var usuario = await _repo.FindByIdAsync(userId.Value);
    /// }
    /// </code>
    /// </remarks>
    long? ExtractUserId(string token);

    /// <summary>
    /// Extrae el rol del usuario (claim Role) del token JWT.
    /// </summary>
    /// <param name="token">Token JWT a procesar.</param>
    /// <returns>El rol del usuario si el token es válido y contiene el claim; null si no existe.</returns>
    /// <remarks>
    /// <para><b>Ejemplo:</b></para>
    /// <code>
    /// var role = _tokenExtractor.ExtractRole(token);
    /// if (role == "admin")
    /// {
    ///     // Tiene permisos de administrador
    /// }
    /// </code>
    /// </remarks>
    string? ExtractRole(string token);

    /// <summary>
    /// Determina si el usuario es administrador basándose en el claim Role.
    /// </summary>
    /// <param name="token">Token JWT a procesar.</param>
    /// <returns>True si el rol es "admin" (case insensitive); false en caso contrario.</returns>
    /// <remarks>
    /// <para><b>Ejemplo:</b></para>
    /// <code>
    /// if (_tokenExtractor.IsAdmin(token))
    /// {
    ///     // Puede acceder a endpoints de administración
    /// }
    /// </code>
    /// </remarks>
    bool IsAdmin(string token);

    /// <summary>
    /// Extrae información completa del usuario del token JWT.
    /// </summary>
    /// <param name="token">Token JWT a procesar.</param>
    /// <returns>Tupla con (userId, isAdmin, role). userId es null si el token es inválido.</returns>
    /// <remarks>
    /// <para><b>Ejemplo:</b></para>
    /// <code>
    /// var (userId, isAdmin, role) = _tokenExtractor.ExtractUserInfo(token);
    /// 
    /// if (userId.HasValue)
    /// {
    ///     var usuario = await _repo.FindByIdAsync(userId.Value);
    ///     if (isAdmin)
    ///     {
    ///         // Acceso de administrador
    ///     }
    /// }
    /// </code>
    /// </remarks>
    (long? UserId, bool IsAdmin, string? Role) ExtractUserInfo(string token);

    /// <summary>
    /// Extrae todos los claims del token JWT.
    /// </summary>
    /// <param name="token">Token JWT a procesar.</param>
    /// <returns>ClaimsPrincipal con todos los claims del token; null si el token es inválido.</returns>
    /// <remarks>
    /// <para><b>Ejemplo:</b></para>
    /// <code>
    /// var claims = _tokenExtractor.ExtractClaims(token);
    /// if (claims != null)
    /// {
    ///     var email = claims.FindFirstValue(ClaimTypes.Email);
    ///     var username = claims.FindFirstValue(JwtRegisteredClaimNames.Sub);
    /// }
    /// </code>
    /// </remarks>
    ClaimsPrincipal? ExtractClaims(string token);

    /// <summary>
    /// Extrae el email del usuario del token JWT.
    /// </summary>
    /// <param name="token">Token JWT a procesar.</param>
    /// <returns>El email del usuario si existe en el token; null en caso contrario.</returns>
    string? ExtractEmail(string token);

    /// <summary>
    /// Valida si un token JWT tiene el formato correcto y está firmado.
    /// NO verifica expiración ni issuer/audience.
    /// </summary>
    /// <param name="token">Token JWT a validar.</param>
    /// <returns>True si el token tiene formato JWT válido; false en caso contrario.</returns>
    bool IsValidTokenFormat(string token);
}

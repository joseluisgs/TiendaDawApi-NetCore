using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using TiendaApi.Apis.Models;

namespace TiendaApi.Apis.Services.Auth;

/// <summary>
/// Implementación del servicio de autenticación JWT (JSON Web Token).
/// 
/// <para>Este servicio es responsable de:</para>
/// <list type="number">
///   <item><description>Generar tokens JWT firmados para usuarios autenticados.</description></item>
///   <item><description>Validar tokens JWT recibidos en solicitudes.</description></item>
///   <item><description>Extraer información del usuario (claims) de los tokens.</description></item>
/// </list>
/// 
/// <remarks>
/// <para><b>Flujo de autenticación JWT:</b></para>
/// <code>
/// 1. Usuario envía credenciales (username/password)
/// 2. Servidor valida credenciales
/// 3. Servidor genera JWT con claims del usuario
/// 4. Servidor devuelve JWT al cliente
/// 5. Cliente incluye JWT en header Authorization
/// 6. Servidor valida JWT en cada solicitud subsiguiente
/// </code>
/// 
/// <para><b>Configuración requerida en appsettings.json:</b></para>
/// <code>
/// {
///   "Jwt": {
///     "Key": "clave-secreta-muy-larga-para-hmac-sha256",
///     "Issuer": "TiendaApi",
///     "Audience": "TiendaApi",
///     "ExpireMinutes": "60"
///   }
/// }
/// </code>
/// 
/// <para><b>Algoritmos de firma:</b> Utiliza HMAC-SHA256 para firmar los tokens,
/// lo que garantiza que los tokens no pueden ser falsificados sin conocer la clave secreta.</para>
/// 
/// <para><b>Claims incluidos en el token:</b></para>
/// <list type="bullet">
///   <item><description><c>sub</c>: Username del usuario.</description></item>
///   <item><description><c>email</c>: Email del usuario.</description></item>
///   <item><description><c>role</c>: Rol del usuario (ClaimTypes.Role).</description></item>
///   <item><description><c>nameidentifier</c>: ID del usuario.</description></item>
///   <item><description><c>jti</c>: Identificador único del token (GUID).</description></item>
/// </list>
/// </remarks>
/// 
/// <example>
/// <para>Generación de token después de login:</para>
/// <code>
/// public async Task&lt;LoginResponse&gt; LoginAsync(LoginRequest request)
/// {
///     var usuario = await _repo.ValidarCredencialesAsync(request.Username, request.Password);
///     
///     if (usuario != null)
///     {
///         var token = _jwtService.GenerateToken(usuario);
///         return new LoginResponse { Token = token };
///     }
///     
///     return null;
/// }
/// </code>
/// 
/// <para>Validación de token en middleware:</para>
/// <code>
/// public async Task&lt;bool&gt; ValidarTokenAsync(string token)
/// {
///     var username = _jwtService.ValidateToken(token);
///     return username != null;
/// }
/// </code>
/// </example>
public class JwtService(
    IConfiguration configuration,
    ILogger<JwtService> logger
) : IJwtService
{
    private readonly IConfiguration _configuration = configuration;
    private readonly ILogger<JwtService> _logger = logger;

    /// <summary>
    /// Genera un token JWT (JSON Web Token) firmado con la información del usuario.
    /// 
    /// <para>El token contiene claims estándar JWT y claims personalizados
    /// con la información del usuario para su uso en la aplicación.</para>
    /// </summary>
    /// <param name="user">
    /// Objeto <see cref="User"/> que contiene la información del usuario a incluir en el token.
    /// Debe tener valores válidos en las propiedades Id, Username, Email y Role.
    /// </param>
    /// <returns>
    /// Cadena de texto que representa el token JWT firmado.
    /// Esta cadena debe enviarse al cliente y ser almacenada de forma segura.
    /// </returns>
    /// 
    /// <exception cref="InvalidOperationException">
    /// Se lanza si la clave JWT ("Jwt:Key") no está configurada en appsettings.json.
    /// </exception>
    /// 
    /// <remarks>
    /// <para><b>Proceso de generación:</b></para>
    /// <list type="number">
    ///   <item><description>Leer configuración de JWT (Key, Issuer, Audience, ExpireMinutes).</description></item>
    ///   <item><description>Crear clave de seguridad simétrica a partir de la clave configurada.</description></item>
    ///   <item><description>Construir lista de claims con información del usuario.</description></item>
    ///   <item><description>Crear objeto JwtSecurityToken con parámetros de configuración.</description></item>
    ///   <item><description>Firmar el token con las credenciales.</description></item>
    ///   <item><description>Serializar el token a string.</description></item>
    /// </list>
    /// 
    /// <para><b>Configuración de seguridad:</b></para>
    /// <list type="bullet">
    ///   <item><description>La clave debe tener al menos 256 bits (32 bytes) para HMAC-SHA256.</description></item>
    ///   <item><description>Issuer y Audience deben coincidir en configuración del cliente.</description></item>
    ///   <item><description>El tiempo de expiración predeterminado es 60 minutos.</description></item>
    /// </list>
    /// 
    /// <para><b>Tamaño del token:</b> Un token JWT típico tiene aproximadamente 200-400 bytes
    /// dependiendo de los claims incluidos. Esto es aceptable para enviar en headers HTTP.</para>
    /// 
    /// <para><b>Logging:</b> Se registra información sobre el token generado incluyendo
    /// el nombre de usuario para facilitar debugging.</para>
    /// </remarks>
    /// 
    /// <example>
    /// <code>
    /// var user = new User
    /// {
    ///     Id = 1,
    ///     Username = "juanperez",
    ///     Email = "juan@ejemplo.com",
    ///     Role = "cliente"
    /// };
    /// 
    /// string token = _jwtService.GenerateToken(user);
    /// 
    /// // Resultado típico (simplificado):
    /// // eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.
    /// // eyJzdWIiOiJqdWFucGVyZXoiLCJlbWFpbCI6Imp1YW5A
    /// // ...firma_hmac...
    /// </code>
    /// </example>
    public string GenerateToken(User user)
    {
        var key = _configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("JWT Key no configurada en appsettings.json");
        var issuer = _configuration["Jwt:Issuer"] ?? "TiendaApi";
        var audience = _configuration["Jwt:Audience"] ?? "TiendaApi";
        var expireMinutes = int.Parse(_configuration["Jwt:ExpireMinutes"] ?? "60");

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Username),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expireMinutes),
            signingCredentials: credentials
        );

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
        
        _logger.LogInformation("Token JWT generado para usuario: {Username}", user.Username);
        
        return tokenString;
    }

    /// <summary>
    /// Valida un token JWT y extrae el nombre de usuario (subject) del token.
    /// 
    /// <para>Este método verifica la firma del token, la fecha de expiración,
    /// y el issuer/audience configurados.</para>
    /// </summary>
    /// <param name="token">
    /// Cadena de texto del token JWT a validar.
    /// típicamente recibida en el header Authorization de las solicitudes HTTP.
    /// </param>
    /// <returns>
    /// El nombre de usuario (claim "sub") extraído del token si es válido;
    /// o <c>null</c> si el token no es válido, ha expirado, o la firma es incorrecta.
    /// </returns>
    /// 
    /// <remarks>
    /// <para><b>Validaciones realizadas:</b></para>
    /// <list type="bullet">
    ///   <item><description>Firma del token: Verifica que fue generado con la clave correcta.</description></item>
    ///   <item><description>Issuer: Verifica que el emisor coincide con la configuración.</description></item>
    ///   <item><description>Audience: Verifica que el destinatario coincide con la configuración.</description></item>
    ///   <item><description>Expiration: Verifica que el token no ha expirado.</description></item>
    ///   <item><description>ClockSkew = TimeSpan.Zero: No se permite desviación de tiempo.</description></item>
    /// </list>
    /// 
    /// <para><b>Manejo de errores:</b></para>
    /// <list type="bullet">
    ///   <item><description>Token expirado: Devuelve null.</description></item>
    ///   <item><description>Firma inválida: Devuelve null.</description></item>
    ///   <item><description>Issuer/Audience incorrectos: Devuelve null.</description></item>
    ///   <item><description>Formato inválido: Devuelve null.</description></item>
    /// </list>
    /// 
    /// <para><b>Extracción de claims:</b> Se extrae específicamente el claim "sub"
    /// (JwtRegisteredClaimNames.Sub) que contiene el nombre de usuario.</para>
    /// 
    /// <para><b>Logging:</b> Los fallos de validación se registran como advertencias
    /// para facilitar el debugging sin exponer información sensible.</para>
    /// 
    /// <example>
    /// <code>
    /// // En un controlador o middleware de autenticación
    /// var authHeader = Request.Headers.Authorization.FirstOrDefault();
    /// if (authHeader?.StartsWith("Bearer ") == true)
    /// {
    ///     var token = authHeader.Substring("Bearer ".Length);
    ///     var username = _jwtService.ValidateToken(token);
    ///     
    ///     if (username != null)
    ///     {
    ///         // Token válido, username contiene el nombre de usuario
    ///         Console.WriteLine($"Usuario autenticado: {username}");
    ///     }
    ///     else
    ///     {
    ///         // Token inválido o expirado
    ///         return Unauthorized();
    ///     }
    /// }
    /// </code>
    /// </example>
    public string? ValidateToken(string token)
    {
        try
        {
            var key = _configuration["Jwt:Key"]
                ?? throw new InvalidOperationException("JWT Key no configurada");
            var issuer = _configuration["Jwt:Issuer"] ?? "TiendaApi";
            var audience = _configuration["Jwt:Audience"] ?? "TiendaApi";

            var tokenHandler = new JwtSecurityTokenHandler();
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));

            tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = securityKey,
                ValidateIssuer = true,
                ValidIssuer = issuer,
                ValidateAudience = true,
                ValidAudience = audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            }, out SecurityToken validatedToken);

            var jwtToken = (JwtSecurityToken)validatedToken;
            var username = jwtToken.Claims.First(x => x.Type == JwtRegisteredClaimNames.Sub).Value;

            return username;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Validación de token JWT fallida");
            return null;
        }
    }
}

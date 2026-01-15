using CSharpFunctionalExtensions;
using Microsoft.AspNetCore.Mvc;
using TiendaApi.Apis.Dtos.Usuarios;
using TiendaApi.Apis.Errors;
using TiendaApi.Apis.Services.Auth;

namespace TiendaApi.Apis.Controllers;

/// <summary>
/// Controlador REST para la autenticación de usuarios.
/// Maneja el registro e inicio de sesión mediante JWT tokens.
/// </summary>
/// <remarks>
/// <para><b>API REST:</b> Este controlador expone endpoints que siguen los principios de RESTful para autenticación.</para>
/// <para><b>Métodos HTTP:</b></para>
/// <list type="table">
/// <item>
/// <term>POST</term>
/// <description>Enviar credenciales para crear sesión o registro</description>
/// </item>
/// </list>
/// <para><b>Códigos de estado HTTP:</b></para>
/// <list type="table">
/// <item>
/// <term>200 OK</term>
/// <description>Petición exitosa, retorna token de acceso</description>
/// </item>
/// <item>
/// <term>201 Created</term>
/// <description>Usuario registrado exitosamente</description>
/// </item>
/// <item>
/// <term>400 Bad Request</term>
/// <description>Error en los datos enviados por el cliente</description>
/// </item>
/// <item>
/// <term>401 Unauthorized</term>
/// <description>Credenciales inválidas</description>
/// </item>
/// <item>
/// <term>409 Conflict</term>
/// <description>Conflicto con datos existentes (usuario duplicado)</description>
/// </item>
/// <item>
/// <term>500 Internal Server Error</term>
/// <description>Error interno del servidor</description>
/// </item>
/// </list>
/// <para><b>Flujo de autenticación:</b></para>
/// <list type="number">
/// <item>
/// <description>El usuario se registra mediante POST /api/v1/auth/signup</description>
/// </item>
/// <item>
/// <description>El usuario inicia sesión mediante POST /api/v1/auth/signin</description>
/// </item>
/// <item>
/// <description>El servidor retorna un JWT token que debe incluirse en el header Authorization</description>
/// </item>
/// <item>
/// <description>El token expira después de un período configurado (por defecto 1 hora)</description>
/// </item>
/// </list>
/// <para><b>Formato del token:</b></para>
/// <para>Los tokens JWT deben incluirse en el header de las solicitudes protegidas:</para>
/// <para><c>Authorization: Bearer {token}</c></para>
/// <para><b>Roles disponibles:</b></para>
/// <list type="table">
/// <item>
/// <term>USER</term>
/// <description>Usuario estándar, puede ver productos, crear pedidos</description>
/// </item>
/// <item>
/// <term>ADMIN</term>
/// <description>Administrador, acceso total a la gestión de productos, categorías y usuarios</description>
/// </item>
/// </list>
/// </remarks>
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
[Produces("application/json")]
public class AuthController(
    IAuthService authService,
    ILogger<AuthController> logger
) : ControllerBase
{

    /// <summary>
    /// Registra un nuevo usuario en el sistema.
    /// </summary>
    /// <param name="dto">Datos de registro del nuevo usuario.</param>
    /// <returns>Respuesta de autenticación con el usuario creado.</returns>
    /// <remarks>
    /// <para><b>Endpoint:</b> POST /api/v1/auth/signup</para>
    /// <para><b>Descripción:</b> Crea una nueva cuenta de usuario en el sistema. El usuario se crea con rol por defecto "USER".</para>
    /// <para><b>Autenticación:</b> No requerida (público).</para>
    /// <para><b>Validaciones:</b></para>
    /// <list type="table">
    /// <item><term>Username</term><description>Requerido, mínimo 3 caracteres, máximo 50, único.</description></item>
    /// <item><term>Email</term><description>Requerido, formato válido de email, único.</description></item>
    /// <item><term>Password</term><description>Requerido, mínimo 8 caracteres, debe contener mayúsculas, minúsculas y números.</description></item>
    /// </list>
    /// <para><b>Códigos de respuesta:</b></para>
    /// <list type="table">
    /// <item><term>201 Created</term><description>Usuario registrado exitosamente.</description></item>
    /// <item><term>400 Bad Request</term><description>Datos inválidos o errores de validación.</description></item>
    /// <item><term>409 Conflict</term><description>Ya existe un usuario con el mismo username o email.</description></item>
    /// </list>
    /// <para><b>Ejemplo de cuerpo de solicitud:</b></para>
    /// <example>
    /// ```json
    /// {
    ///   "username": "juanperez",
    ///   "email": "juan@example.com",
    ///   "password": "Juan1234",
    ///   "firstName": "Juan",
    ///   "lastName": "Pérez"
    /// }
    /// ```
    /// </example>
    /// <para><b>Ejemplo de respuesta exitosa:</b></para>
    /// <example>
    /// ```json
    /// {
    ///   "user": {
    ///     "id": 1,
    ///     "username": "juanperez",
    ///     "email": "juan@example.com",
    ///     "role": "USER",
    ///     "createdAt": "2024-01-15T10:30:00Z"
    ///   },
    ///   "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    ///   "expiresIn": 3600
    /// }
    /// ```
    /// </example>
    /// <para><b>Ejemplo de solicitud cURL:</b></para>
    /// <example>
    /// ```bash
    /// curl -X POST "http://localhost:5000/api/v1/auth/signup" \
    ///   -H "Content-Type: application/json" \
    ///   -d '{"username": "juanperez", "email": "juan@example.com", "password": "Juan1234", "firstName": "Juan", "lastName": "Pérez"}'
    /// ```
    /// </example>
    /// <para><b>Ejemplo de solicitud HTTP:</b></para>
    /// <example>
    /// ```http
    /// POST /api/v1/auth/signup HTTP/1.1
    /// Host: localhost:5000
    /// Content-Type: application/json
    ///
    /// {
    ///   "username": "juanperez",
    ///   "email": "juan@example.com",
    ///   "password": "Juan1234",
    ///   "firstName": "Juan",
    ///   "lastName": "Pérez"
    /// }
    /// ```
    /// </example>
    /// </remarks>
    [HttpPost("signup")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> SignUp([FromBody] RegisterDto dto)
    {
        logger.LogInformation("Signup request received for user: {Username}", dto.Username);

        var resultado = await authService.SignUpAsync(dto);

        return resultado.Match(
            response => CreatedAtAction(nameof(SignUp), response),
            error => error switch
            {
                ValidationError validationError => BadRequest(new { message = validationError.Message }),
                ConflictError conflictError => Conflict(new { message = conflictError.Message }),
                _ => StatusCode(500, new { message = error.Message })
            }
        );
    }

    /// <summary>
    /// Inicia sesión y obtiene un token JWT de acceso.
    /// </summary>
    /// <param name="dto">Credenciales de inicio de sesión.</param>
    /// <returns>Respuesta de autenticación con el token JWT.</returns>
    /// <remarks>
    /// <para><b>Endpoint:</b> POST /api/v1/auth/signin</para>
    /// <para><b>Descripción:</b> Autentica al usuario y retorna un JWT token para acceder a endpoints protegidos.</para>
    /// <para><b>Autenticación:</b> No requerida (público).</para>
    /// <para><b>Códigos de respuesta:</b></para>
    /// <list type="table">
    /// <item><term>200 OK</term><description>Inicio de sesión exitoso, retorna token JWT.</description></item>
    /// <item><term>400 Bad Request</term><description>Datos inválidos (formato incorrecto).</description></item>
    /// <item><term>401 Unauthorized</term><description>Credenciales inválidas (username o password incorrectos).</description></item>
    /// </list>
    /// <para><b>Ejemplo de cuerpo de solicitud:</b></para>
    /// <example>
    /// ```json
    /// {
    ///   "username": "juanperez",
    ///   "password": "Juan1234"
    /// }
    /// ```
    /// </example>
    /// <para><b>Ejemplo de respuesta exitosa:</b></para>
    /// <example>
    /// ```json
    /// {
    ///   "user": {
    ///     "id": 1,
    ///     "username": "juanperez",
    ///     "email": "juan@example.com",
    ///     "role": "USER",
    ///     "createdAt": "2024-01-15T10:30:00Z"
    ///   },
    ///   "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6Ikp1YW4gUGVyZXoiLCJyb2xlIjoiVVNFUiIsImlhdCI6MTcwNDUxMDAwMCwiZXhwIjoxNzA0NTEzNjAwfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c",
    ///   "expiresIn": 3600
    /// }
    /// ```
    /// </example>
    /// <para><b>Ejemplo de solicitud cURL:</b></para>
    /// <example>
    /// ```bash
    /// curl -X POST "http://localhost:5000/api/v1/auth/signin" \
    ///   -H "Content-Type: application/json" \
    ///   -d '{"username": "juanperez", "password": "Juan1234"}'
    /// ```
    /// </example>
    /// <para><b>Ejemplo de solicitud HTTP:</b></para>
    /// <example>
    /// ```http
    /// POST /api/v1/auth/signin HTTP/1.1
    /// Host: localhost:5000
    /// Content-Type: application/json
    ///
    /// {
    ///   "username": "juanperez",
    ///   "password": "Juan1234"
    /// }
    /// ```
    /// </example>
    /// <para><b>Uso del token en solicitudes protegidas:</b></para>
    /// <para>Una vez obtenido el token, debe incluirse en el header Authorization de las solicitudes:</para>
    /// <example>
    /// ```bash
    /// curl -X GET "http://localhost:5000/api/productos" \
    ///   -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
    /// ```
    /// </example>
    /// </remarks>
    [HttpPost("signin")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> SignIn([FromBody] LoginDto dto)
    {
        logger.LogInformation("Petición de inicio de sesión recibida para usuario: {Username}", dto.Username);

        var resultado = await authService.SignInAsync(dto);

        return resultado.Match(
            response => Ok(response),
            error => error switch
            {
                UnauthorizedError unauthorizedError => Unauthorized(new { message = unauthorizedError.Message }),
                ValidationError validationError => BadRequest(new { message = validationError.Message }),
                _ => StatusCode(500, new { message = error.Message })
            }
        );
    }
}

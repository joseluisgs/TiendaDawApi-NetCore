using CSharpFunctionalExtensions;
using TiendaApi.Apis.Dtos.Usuarios;
using TiendaApi.Apis.Errors;

namespace TiendaApi.Apis.Services.Auth;

/// <summary>
/// Interfaz del servicio de autenticación que implementa el patrón Service Layer.
/// Centraliza toda la lógica de negocio relacionada con la autenticación y registro de usuarios,
/// incluyendo validación de credenciales, generación de tokens JWT y gestión de sesiones.
///
/// <para><b>Patrón Service Layer:</b></para>
/// <list type="bullet">
///   <item><description>Encapsula la lógica de autenticación separándola de los controladores</description></item>
///   <item><description>Coordina múltiples operaciones: validación, hashing, tokens</description></item>
///   <item><description>Proporciona abstracción sobre detalles de implementación de seguridad</description></item>
/// </list>
///
/// <para><b>Patrón Result:</b></para>
/// <list type="bullet">
///   <item><description>Las operaciones de auth pueden fallar por múltiples razones tipadas</description></item>
///   <item><description>Facilita el manejo de errores de seguridad de forma explícita</description></item>
///   <item><description>El tipo <c>Result&lt;AuthResponseDto, DomainError&gt;</c> encapsula respuesta o error</description></item>
/// </list>
///
/// <para><b>Flujo de Autenticación:</b></para>
/// <list type="bullet">
///   <item><description><b>SignUp:</b> Validar → Verificar duplicados → Hashear password → Guardar → Generar token</description></item>
///   <item><description><b>SignIn:</b> Validar → Buscar usuario → Verificar password → Generar token</description></item>
/// </list>
/// </summary>
/// <remarks>
/// <para><b>Manejo de Errores de Seguridad:</b></para>
/// <list type="bullet">
///   <item><description><c>InvalidCredentials</c>: Email o contraseña incorrectos</description></item>
///   <item><description><c>AccountLocked</c>: Cuenta bloqueada por intentos fallidos</description></item>
///   <item><description><c>AccountInactive</c>: Cuenta desactivada o pendiente de verificación</description></item>
///   <item><description><c>PasswordExpired</c>: Contraseña vencida (requiere cambio)</description></item>
/// </list>
/// <para><b>Seguridad:</b></para>
/// <list type="bullet">
///   <item><description>Las contraseñas se hashean con bcrypt o Argon2</description></item>
///   <item><description>Los tokens JWT incluyenclaims de usuario y roles</description></item>
///   <item><description>Rate limitingpreviene ataques de fuerza bruta</description></item>
///   <item><description>Los errores nunca exponen información sensible</description></item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// // Registro de usuario
/// [HttpPost("register")]
/// public async Task&lt;ActionResult&gt; Register(RegisterDto dto)
/// {
///     var resultado = await _authService.SignUpAsync(dto);
///
///     return resultado.Match(
///         response =&gt; {
///             // Guardar token en cookie o devolver
///             Response.Cookies.Append("token", response.Token, new CookieOptions
///             {
///                 HttpOnly = true,
///                 Secure = true,
///                 SameSite = SameSiteMode.Strict
///             });
///             return Ok(new { user = response.User, token = response.Token });
///         },
///         error =&gt; {
///             return error.Code switch
///             {
///                 ErrorCodes.Conflict =&gt; Conflict("Email ya registrado"),
///                 ErrorCodes.Validation =&gt; BadRequest(error.Message),
///                 _ =&gt; Problem(error.Message)
///             };
///         }
///     );
/// }
///
/// // Inicio de sesión
/// [HttpPost("login")]
/// public async Task&lt;ActionResult&gt; Login(LoginDto dto)
/// {
///     var resultado = await _authService.SignInAsync(dto);
///
///     if (resultado.IsFailure)
///     {
///         // Registrar intento fallido para auditoría
///         _auditLog.LoginFailed(dto.Email, resultado.Error.Code);
///
///         return Unauthorized(new { message = "Credenciales inválidas" });
///     }
///
///     var response = resultado.Value;
///     return Ok(new { user = response.User, token = response.Token });
/// }
///
/// // Uso en controlador protegido
/// [Authorize]
/// [HttpGet("profile")]
/// public async Task&lt;ActionResult&gt; GetProfile()
/// {
///     var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
///     var user = await _userService.FindByIdAsync(userId);
///     return Ok(user.Value);
/// }
/// </code>
public interface IAuthService
{
    /// <summary>
    /// Registra un nuevo usuario en el sistema.
    /// Valida los datos de registro, verifica unicidad de email y username,
    /// hashea la contraseña y genera el token JWT de autenticación.
    /// </summary>
    /// <param name="dto">Datos de registro del nuevo usuario</param>
    /// <returns>
    /// <list type="bullet">
    ///   <item><term><c>Result.Success</c></term><description>Contiene <c>AuthResponseDto</c> con usuario creado y token JWT</description></item>
    ///   <item><term><c>Result.Failure</c></term><description>Validation (datos inválidos), Conflict (email/username existe), OperationError</description></item>
    /// </list>
    /// </returns>
    /// <remarks>
    /// <para><b>Validaciones de registro:</b></para>
    /// <list type="bullet">
    ///   <item><description>Email válido y formato correcto</description></item>
    ///   <item><description>Email único en el sistema</description></item>
    ///   <item><description>Username único (3-50 caracteres, solo letras, números y guiones bajos)</description></item>
    ///   <item><description>Contraseña: mínimo 8 caracteres, 1 mayúscula, 1 minúscula, 1 número, 1 carácter especial</description></item>
    ///   <item><description>Nombre y apellido requeridos</description></item>
    /// </list>
    /// <para><b>Flujo interno:</b></para>
    /// <list type="bullet">
    ///   <item><description>1. Validar DTO</description></item>
    ///   <item><description>2. Verificar email no existe</description></item>
    ///   <item><description>3. Verificar username no existe</description></item>
    ///   <item><description>4. Hashear contraseña con bcrypt</description></item>
    ///   <item><description>5. Crear usuario en base de datos</description></item>
    ///   <item><description>6. Generar JWT token</description></item>
    ///   <item><description>7. Retornar AuthResponseDto</description></item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// var registro = new RegisterDto
    /// {
    ///     Email = "juan@email.com",
    ///     Username = "juan123",
    ///     Password = "Juan1234!",
    ///     Nombre = "Juan",
    ///     Apellido = "Pérez"
    /// };
    ///
    /// var resultado = await _authService.SignUpAsync(registro);
    ///
    /// if (resultado.IsFailure)
    /// {
    ///     var error = resultado.Error;
    ///     return error.Code switch
    ///     {
    ///         ErrorCodes.Conflict =&gt; Conflict("El email ya está registrado"),
    ///         ErrorCodes.Validation =&gt; BadRequest(error.Message),
    ///         _ =&gt; StatusCode(500, "Error al registrar usuario")
    ///     };
    /// }
    ///
    /// var response = resultado.Value;
    /// return CreatedAtAction("Profile", new { }, new
    /// {
    ///     usuario = response.User,
    ///     token = response.Token
    /// });
    /// </code>
    /// </example>
    Task<Result<AuthResponseDto, DomainError>> SignUpAsync(RegisterDto dto);

    /// <summary>
    /// Autentica un usuario existente verificando sus credenciales.
    /// Busca el usuario por email, verifica la contraseña y genera un token JWT.
    /// </summary>
    /// <param name="dto">Credenciales de acceso (email y contraseña)</param>
    /// <returns>
    /// <list type="bullet">
    ///   <item><term><c>Result.Success</c></term><description>Contiene <c>AuthResponseDto</c> con usuario y token JWT</description></item>
    ///   <item><term><c>Result.Failure</c></term><description>InvalidCredentials, AccountLocked, AccountInactive, PasswordExpired</description></item>
    /// </list>
    /// </returns>
    /// <remarks>
    /// <para><b>Seguridad:</b></para>
    /// <list type="bullet">
    ///   <item><description>Tiempo de espera exponencial tras intentos fallidos</description></item>
    ///   <item><description>Bloqueo temporal tras 5 intentos fallidos</description></item>
    ///   <item><description>Los errores genéricos evitan enumeración de usuarios</description></item>
    ///   <item><description>El token JWT tiene expiración configurable (típicamente 24h)</description></item>
    /// </list>
    /// <para><b>Flujo interno:</b></para>
    /// <list type="bullet">
    ///   <item><description>1. Validar DTO</description></item>
    ///   <item><description>2. Buscar usuario por email</description></item>
    ///   <item><description>3. Si no existe, retornar error genérico</description></item>
    ///   <item><description>4. Verificar cuenta no esté bloqueada/inactiva</description></item>
    ///   <item><description>5. Verificar contraseña con bcrypt</description></item>
    ///   <item><description>6. Si password incorrecto, registrar intento fallido</description></item>
    ///   <item><description>7. Generar JWT token</description></item>
    ///   <item><description>8. Resetear contador de intentos fallidos</description></item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// var login = new LoginDto
    /// {
    ///     Email = "juan@email.com",
    ///     Password = "Juan1234!"
    /// };
    ///
    /// var resultado = await _authService.SignInAsync(login);
    ///
    /// if (resultado.IsFailure)
    /// {
    ///     var error = resultado.Error;
    ///     _logger.LogWarning("Intento de login fallido para {Email}: {Code}", login.Email, error.Code);
    ///
    ///     return Unauthorized(new {
    ///         message = "Email o contraseña incorrectos",
    ///         remainingAttempts = GetRemainingAttempts(login.Email)
    ///     });
    /// }
    ///
    /// var response = resultado.Value;
    /// return Ok(new
    /// {
    ///     user = new
    ///     {
    ///         id = response.User.Id,
    ///         email = response.User.Email,
    ///         nombre = response.User.Nombre,
    ///         rol = response.User.Rol
    ///     },
    ///     token = response.Token,
    ///     expiresIn = 86400 // 24 horas en segundos
    /// });
    /// </code>
    /// </example>
    Task<Result<AuthResponseDto, DomainError>> SignInAsync(LoginDto dto);
}

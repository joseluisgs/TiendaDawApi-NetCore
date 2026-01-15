namespace TiendaApi.Apis.Errors.Auth;

/// <summary>
/// Fábrica de errores específicos del dominio de autenticación.
/// 
/// <para>
/// Esta clase contiene métodos estáticos para crear errores relacionados
/// con procesos de autenticación y registro de usuarios en la tienda.
/// </para>
/// 
/// <para>
/// <b>Casos de uso cubiertos:</b>
/// <list type="bullet">
///   <item><description>Credenciales inválidas durante login.</description></item>
///   <item><description>Conflicto por nombre de usuario duplicado.</description></item>
///   <item><description>Conflicto por email duplicado.</description></item>
///   <item><description>Errores de validación en datos de autenticación.</description></item>
/// </list>
/// </para>
/// 
/// <para>
/// <b>Ejemplo de uso en un servicio de autenticación:</b>
/// <code>
/// public async Task&lt;Result&lt;AuthResponseDto&gt;&gt; RegisterAsync(RegisterRequestDto request)
/// {
///     if (await _repo.ExistsByEmailAsync(request.Email))
///         return Result.Fail(AuthError.EmailExistente(request.Email));
///         
///     if (await _repo.ExistsByUsernameAsync(request.Username))
///         return Result.Fail(AuthError.UsernameExistente(request.Username));
///         
///     var usuario = new Usuario(request.Username, request.Email, HashPassword(request.Password));
///     await _repo.AddAsync(usuario);
///     
///     var token = _jwtService.GenerateToken(usuario);
///     return Result.Ok(new AuthResponseDto(usuario, token));
/// }
/// </code>
/// </para>
/// 
/// <para>
/// <b>Nota:</b> Esta clase es específica para errores de autenticación y registro.
/// Para errores de autorización (permisos), ver <see cref="TiendaApi.Apis.Errors.Usuarios.UsuarioError"/>.
/// </para>
/// </summary>
public static class AuthError
{
    /// <summary>
    /// Crea un error de autenticación cuando las credenciales proporcionadas son inválidas.
    /// 
    /// <para>
    /// Se usa durante el proceso de login cuando el email o la contraseña
    /// no coinciden con los registros del sistema.
    /// </para>
    /// 
    /// <para>
    /// <b>Seguridad:</b> Por motivos de seguridad, no se especifica si el error
    /// está en el email o en la contraseña, para evitar enumerable información.
///   </para>
///   </summary>
///   <returns>UnauthorizedError indicando credenciales incorrectas.</returns>
///   <example>
///   return AuthError.CredencialesInvalidas();
///   // Genera: "Credenciales inválidas"
///   </example>
public static UnauthorizedError CredencialesInvalidas() =>
    UnauthorizedError.InvalidCredentials();

/// <summary>
/// Crea un error de conflicto cuando ya existe un usuario con el mismo nombre de usuario.
/// 
/// <para>
/// Se usa durante el proceso de registro para garantizar que los nombres
/// de usuario sean únicos en el sistema.
/// </para>
/// </summary>
/// <param name="username">Nombre de usuario que generó el conflicto.</param>
/// <returns>ConflictError indicando duplicado de nombre de usuario.</returns>
/// <example>
/// return AuthError.UsernameExistente("admin123");
/// // Genera: "Ya existe un nombre de usuario con el valor 'admin123'"
/// </example>
public static ConflictError UsernameExistente(string username) =>
    ConflictError.Duplicate("nombre de usuario", username);

/// <summary>
/// Crea un error de conflicto cuando ya existe un usuario con el mismo email.
/// 
/// <para>
/// Se usa durante el proceso de registro para garantizar que los emails
/// sean únicos en el sistema.
/// </para>
/// </summary>
/// <param name="email">Email que generó el conflicto.</param>
/// <returns>ConflictError indicando duplicado de email.</returns>
/// <example>
/// return AuthError.EmailExistente("correo@ejemplo.com");
/// // Genera: "Ya existe un email con el valor 'correo@ejemplo.com'"
/// </example>
public static ConflictError EmailExistente(string email) =>
    ConflictError.Duplicate("email", email);

/// <summary>
/// Crea un error de validación simple para datos de autenticación.
/// 
/// <para>
/// Útil cuando se necesita reportar un error de validación sin detalles
/// específicos por campo, solo un mensaje general.
/// </para>
/// </summary>
/// <param name="mensaje">Descripción del error de validación.</param>
/// <returns>ValidationError con diccionario vacío de detalles por campo.</returns>
/// <example>
/// return AuthError.Validacion("El email es obligatorio");
/// </example>
public static ValidationError Validacion(string mensaje) =>
    new(mensaje, new Dictionary<string, string[]>());

/// <summary>
/// Crea un error de validación con detalles específicos por campo.
/// 
/// <para>
/// Se usa cuando la validación de datos de registro o login genera múltiples
/// errores en diferentes campos del modelo.
/// </para>
/// </summary>
/// <param name="errores">
/// Diccionario donde la clave es el nombre del campo y el valor es un array
/// de mensajes de error para ese campo.
/// </param>
/// <returns>ValidationError con todos los errores por campo.</returns>
/// <example>
/// var errores = new Dictionary&lt;string, string[]&gt;
/// {
///     { "Username", new[] { "El nombre de usuario es obligatorio", "Solo letras y números" } },
///     { "Email", new[] { "El email es obligatorio", "Debe ser un email válido" } },
///     { "Password", new[] { "Mínimo 8 caracteres", "Debe contener al menos una mayúscula" } },
///     { "ConfirmPassword", new[] { "Las contraseñas no coinciden" } }
/// };
/// return AuthError.ValidacionConCampos(errores);
/// </example>
public static ValidationError ValidacionConCampos(Dictionary<string, string[]> errores) =>
    ValidationError.WithFieldErrors(errores);
}

namespace TiendaApi.Apis.Errors.Usuarios;

/// <summary>
/// Fábrica de errores específicos del dominio de usuarios.
/// 
/// <para>
/// Esta clase contiene métodos estáticos para crear errores relacionados
/// con operaciones sobre usuarios en la tienda.
/// </para>
/// 
/// <para>
/// <b>Casos de uso cubiertos:</b>
/// <list type="bullet">
///   <item><description>Usuario no encontrado por ID o email.</description></item>
///   <item><description>Conflicto por nombre de usuario duplicado.</description></item>
///   <item><description>Conflicto por email duplicado.</description></item>
///   <item><description>Credenciales inválidas en login.</description></item>
///   <item><description>Token expirado o inválido.</description></item>
///   <item><description>Usuario tiene pedidos/productos asociados (no se puede eliminar).</description></item>
///   <item><description>Errores de validación de datos de usuario.</description></item>
/// </list>
/// </para>
/// 
/// <para>
/// <b>Ejemplo de uso en un servicio de autenticación:</b>
/// <code>
/// public async Task&lt;Result&lt;UsuarioDto&gt;&gt; LoginAsync(string email, string password)
/// {
///     var usuario = await _repo.GetByEmailAsync(email);
///     if (usuario == null)
///         return Result.Fail(UsuarioError.NotFoundByEmail(email));
///         
///     if (!VerifyPassword(usuario.PasswordHash, password))
///         return Result.Fail(UsuarioError.CredencialesInvalidas());
///         
///     var token = GenerateJwtToken(usuario);
///     return Result.Ok(MapToDto(usuario, token));
/// }
/// </code>
/// </para>
/// </summary>
public static class UsuarioError
{
    /// <summary>
    /// Crea un error de tipo "no encontrado" para un usuario inexistente.
    /// 
    /// <para>
    /// Se usa cuando se intenta acceder, actualizar o eliminar un usuario
    /// que no existe en la base de datos.
    /// </para>
    /// </summary>
    /// <param name="id">Identificador del usuario que no fue encontrado.</param>
    /// <returns>NotFoundError con mensaje formateado para usuarios.</returns>
    /// <example>
    /// return UsuarioError.NotFound(1);
    /// // Genera: "Recurso con ID 1 no encontrado"
    /// </example>
    public static NotFoundError NotFound(long id) =>
        NotFoundError.FromId(id, "Usuario");

    /// <summary>
    /// Crea un error de tipo "no encontrado" para un email inexistente.
    /// 
    /// <para>
    /// Se usa durante el proceso de login para verificar si el email
    /// proporcionado corresponde a un usuario registrado.
    /// </para>
    /// </summary>
    /// <param name="email">Email que no fue encontrado en el sistema.</param>
    /// <returns>NotFoundError indicando que el email no existe.</returns>
    /// <example>
    /// return UsuarioError.NotFoundByEmail("usuario@ejemplo.com");
    /// // Genera: "Usuario con email 'usuario@ejemplo.com' no encontrado"
    /// </example>
    public static NotFoundError NotFoundByEmail(string email) =>
        new($"Usuario con email '{email}' no encontrado");

    /// <summary>
    /// Crea un error de conflicto cuando ya existe un usuario con el mismo nombre de usuario.
    /// 
    /// <para>
    /// Se usa durante el registro o actualización de usuarios para garantizar
    /// que los nombres de usuario sean únicos en el sistema.
    /// </para>
    /// </summary>
    /// <param name="username">Nombre de usuario que generó el conflicto.</param>
    /// <returns>ConflictError indicando duplicado de nombre de usuario.</returns>
    /// <example>
    /// return UsuarioError.UsernameExistente("admin123");
    /// // Genera: "Ya existe un nombre de usuario con el valor 'admin123'"
    /// </example>
    public static ConflictError UsernameExistente(string username) =>
        ConflictError.Duplicate("nombre de usuario", username);

    /// <summary>
    /// Crea un error de conflicto cuando ya existe un usuario con el mismo email.
    /// 
    /// <para>
    /// Se usa durante el registro o actualización de usuarios para garantizar
    /// que los emails sean únicos en el sistema.
    /// </para>
    /// </summary>
    /// <param name="email">Email que generó el conflicto.</param>
    /// <returns>ConflictError indicando duplicado de email.</returns>
    /// <example>
    /// return UsuarioError.EmailExistente("correo@ejemplo.com");
    /// // Genera: "Ya existe un email con el valor 'correo@ejemplo.com'"
    /// </example>
    public static ConflictError EmailExistente(string email) =>
        ConflictError.Duplicate("email", email);

    /// <summary>
    /// Crea un error de autenticación cuando las credenciales proporcionadas son inválidas.
    /// 
    /// <para>
    /// Se usa durante el proceso de login cuando el email no existe
    /// o la contraseña no coincide con la almacenada.
    /// </para>
    /// </summary>
    /// <returns>UnauthorizedError indicando credenciales incorrectas.</returns>
    /// <example>
    /// return UsuarioError.CredencialesInvalidas();
    /// // Genera: "Credenciales inválidas"
    /// </example>
    public static UnauthorizedError CredencialesInvalidas() =>
        UnauthorizedError.InvalidCredentials();

    /// <summary>
    /// Crea un error de autenticación cuando el token JWT ha expirado o es inválido.
    /// 
    /// <para>
    /// Se usa en los middleware de autenticación cuando el token
    /// no puede ser validado (expirado, firma incorrecta, malformado).
    /// </para>
    /// </summary>
    /// <returns>UnauthorizedError indicando token expirado o inválido.</returns>
    /// <example>
    /// return UsuarioError.TokenExpirado();
    /// // Genera: "Token expirado o inválido"
    /// </example>
    public static UnauthorizedError TokenExpirado() =>
        UnauthorizedError.TokenExpired();

    /// <summary>
    /// Crea un error de regla de negocio al intentar eliminar un usuario con pedidos asociados.
    /// 
    /// <para>
    /// Los usuarios no se pueden eliminar si tienen pedidos en el sistema
    /// para mantener el historial y trazabilidad de transacciones.
    /// </para>
    /// </summary>
    /// <param name="id">ID del usuario que no se puede eliminar.</param>
    /// <returns>BusinessRuleError indicando que el usuario tiene pedidos.</returns>
    /// <example>
    /// return UsuarioError.NoSePuedeEliminarConPedidos(42);
    /// // Genera: "No se puede eliminar el usuario con ID 42 porque tiene pedidos asociados"
    /// </example>
    public static BusinessRuleError NoSePuedeEliminarConPedidos(long id) =>
        new($"No se puede eliminar el usuario con ID {id} porque tiene pedidos asociados");

    /// <summary>
    /// Crea un error de regla de negocio al intentar eliminar un usuario con productos a la venta.
    /// 
    /// <para>
    /// Los usuarios (vendedores) no se pueden eliminar si tienen productos
    /// activos en la tienda para evitar productos huérfanos.
    /// </para>
    /// </summary>
    /// <param name="id">ID del usuario que no se puede eliminar.</param>
    /// <returns>BusinessRuleError indicando que el usuario tiene productos.</returns>
    /// <example>
    /// return UsuarioError.NoSePuedeEliminarConProductos(42);
    /// // Genera: "No se puede eliminar el usuario con ID 42 porque tiene productos a la venta"
    /// </example>
    public static BusinessRuleError NoSePuedeEliminarConProductos(long id) =>
        new($"No se puede eliminar el usuario con ID {id} porque tiene productos a la venta");

    /// <summary>
    /// Crea un error de validación simple para operaciones sobre usuarios.
    /// 
    /// <para>
    /// Útil cuando se necesita reportar un error de validación sin detalles
    /// específicos por campo, solo un mensaje general.
    /// </para>
    /// </summary>
    /// <param name="mensaje">Descripción del error de validación.</param>
    /// <returns>ValidationError con diccionario vacío de detalles por campo.</returns>
    /// <example>
    /// return UsuarioError.Validacion("El email debe tener un formato válido");
    /// </example>
    public static ValidationError Validacion(string mensaje) =>
        new(mensaje, new Dictionary<string, string[]>());

    /// <summary>
    /// Crea un error de validación con detalles específicos por campo.
    /// 
    /// <para>
    /// Se usa cuando la validación de datos de usuario genera múltiples
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
    ///     { "Username", new[] { "El nombre de usuario es obligatorio", "Mínimo 4 caracteres" } },
    ///     { "Email", new[] { "El email es obligatorio", "Formato de email inválido" } },
    ///     { "Password", new[] { "La contraseña debe tener al menos 8 caracteres" } }
    /// };
    /// return UsuarioError.ValidacionConCampos(errores);
    /// </example>
    public static ValidationError ValidacionConCampos(Dictionary<string, string[]> errores) =>
        ValidationError.WithFieldErrors(errores);
}

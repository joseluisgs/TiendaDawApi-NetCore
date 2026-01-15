namespace TiendaApi.Apis.Models;

using TiendaApi.Apis.Data;

/// <summary>
/// Entidad de dominio que representa un usuario en el sistema de la tienda.
/// 
/// <para>
/// Los usuarios son la base del sistema de autenticación y autorización.
/// Cada usuario tiene credenciales de acceso (email/contraseña) y un rol
/// que determina sus permisos dentro del sistema.
/// </para>
/// 
/// <para>
/// <b>Características principales:</b>
/// <list type="bullet">
///   <item><description>Autenticación mediante email y contraseña hasheada con BCrypt.</description></item>
///   <item><description>Sistema de roles (USER, ADMIN) para control de acceso.</description></item>
///   <item><description>Gestión de avatar (local o externo).</description></item>
///   <item><description>Implementa soft-delete para mantener historial de usuarios.</description></item>
///   <item><description>Soporte para integración con JWT en autenticación.</description></item>
/// </list>
/// </para>
/// 
/// <para>
/// <b>Patrón de seguridad:</b> Las contraseñas nunca se almacenan en texto plano.
/// Se utiliza BCrypt para generar un hash seguro que incluye un salt aleatorio,
/// protegiendo contra ataques de tabla rainbow y fuerza bruta.
/// </para>
/// 
/// <para>
/// <b>Mapeo a claims JWT:</b> Al generar tokens JWT, los siguientes claims
/// se extraen de esta entidad:
/// <list type="bullet">
///   <item><description>sub (subject): Id del usuario.</description></item>
///   <item><description>email: Correo electrónico del usuario.</description></item>
///   <item><description>role: Rol del usuario (USER o ADMIN).</description></item>
///   <item><description>jti: Identificador único del token.</description></item>
/// </list>
/// </para>
/// </summary>
/// <example>
/// Crear un nuevo usuario:
/// <code>
/// var usuario = new User
/// {
///     Username = "juan_perez",
///     Email = "juan@ejemplo.com",
///     PasswordHash = BCrypt.Net.BCrypt.HashPassword("contraseña123"),
///     Role = UserRoles.USER,
///     Avatar = null
/// };
/// </code>
/// 
/// Verificar contraseña:
/// <code>
/// bool valido = BCrypt.Net.BCrypt.Verify("contraseña123", usuario.PasswordHash);
/// </code>
/// </example>
public class User : ITimestamped
{
    /// <summary>
    /// URL de avatar por defecto para usuarios sin imagen personalizada.
    /// </summary>
    public const string AVATAR_DEFAULT = "https://via.placeholder.com/150";

    /// <summary>
    /// Prefijo de ruta para avatares locales almacenados en el servidor.
    /// 
    /// <para>
    /// Las imágenes de avatar cargadas por usuarios se almacenan en
    /// /storage/images/usuarios/ y se referencian con este prefijo.
    /// </para>
    /// </summary>
    public const string AVATAR_LOCAL_PREFIX = "/storage/images/usuarios/";

    /// <summary>
    /// Identificador único del usuario (clave primaria).
    /// 
    /// <para>
    /// Se genera automáticamente en la base de datos PostgreSQL.
    /// Es el identificador usado en URLs, referencias externas y JWT claims (sub).
    /// </para>
    /// <remarks>
    /// Valor ejemplo: 1, 2, 3, ... (números positivos)
    /// </remarks>
    public long Id { get; set; }

    /// <summary>
    /// Nombre de usuario único para identificación en el sistema.
    /// 
    /// <para>
    /// Campo obligatorio que identifica al usuario de forma pública.
    /// Se muestra en comentarios, pedidos y otras interacciones.
    /// </para>
    /// <remarks>
    /// Longitud típica: 3-50 caracteres, único en el sistema.
    /// </remarks>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Correo electrónico del usuario.
    /// 
    /// <para>
    /// Campo obligatorio usado para autenticación, recuperación de contraseña
    /// y notificaciones del sistema.
    /// </para>
    /// <remarks>
    /// Debe ser único por usuario activo (no soft-deleted).
    /// </remarks>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Hash de la contraseña del usuario generado con BCrypt.
    /// 
    /// <para>
    /// <b>Seguridad:</b> Las contraseñas nunca se almacenan en texto plano.
    /// Se utiliza el algoritmo BCrypt que incluye:
    /// <list type="bullet">
    ///   <item><description>Hashing adaptativo (work factor configurable).</description></item>
    ///   <item><description>Salt aleatorio incluido en el hash.</description></item>
    ///   <item><description>Resistencia a ataques de tabla rainbow.</description></item>
    ///   <item><description>Protección contra fuerza bruta (tiempo de verificación).</description></item>
    /// </list>
    /// </para>
    /// <remarks>
    /// Formato BCrypt: $2y$12$... (aproximadamente 60 caracteres)
    /// </remarks>
    /// <example>
    /// Generar hash: BCrypt.Net.BCrypt.HashPassword("miContraseña")
    /// Verificar: BCrypt.Net.BCrypt.Verify("miContraseña", hash)
    /// </example>
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>
    /// URL o ruta del avatar del usuario.
    /// 
    /// <para>
    /// Puede ser de tres tipos:
    /// <list type="bullet">
    ///   <item><description>URL externa (http://, https://): Imágenes de CDNs o servicios.</description></item>
    ///   <item><description>Ruta local (/storage/images/usuarios/...): Imágenes cargadas.</description></item>
    ///   <item><description>Nulo o AVATAR_DEFAULT: Sin imagen personalizada.</description></item>
    /// </list>
    /// </para>
    /// </summary>
    /// <example>
    /// Valores válidos:
    /// "https://cdn.example.com/avatar.jpg"
    /// "/storage/images/usuarios/avatar_123.jpg"
    /// null (usa AVATAR_DEFAULT)
    /// </example>
    public string? Avatar { get; set; }

    /// <summary>
    /// Rol del usuario que determina sus permisos en el sistema.
    /// 
    /// <para>
    /// <b>Roles disponibles:</b>
    /// <list type="bullet">
    ///   <item><term>USER</term>: Usuario estándar. Puede ver productos, crear pedidos y gestionar su perfil.</description></item>
    ///   <item><term>ADMIN</term>: Administrador. Tiene acceso total incluyendo gestión de productos, categorías y pedidos.</description></item>
    /// </list>
    /// </para>
    /// <remarks>
    /// Valor por defecto: UserRoles.USER
    /// </remarks>
    /// <example>
    /// Verificar si es administrador:
    /// <code>
    /// if (usuario.Role == UserRoles.ADMIN)
    ///     // Acceso apanel de administración
    /// </code>
    /// </example>
    public string Role { get; set; } = UserRoles.USER;

    /// <summary>
    /// Indica si el usuario ha sido eliminado (soft-delete).
    /// 
    /// <para>
    /// En lugar de eliminar físicamente el registro, se marca este campo
    /// como true para mantener la integridad de datos históricos y permitir
    /// auditoría de usuarios eliminados.
    /// </para>
    /// <para>
    /// Las consultas de autenticación filtran automáticamente por IsDeleted = false,
    /// por lo que los usuarios eliminados no pueden iniciar sesión.
    /// </para>
    /// <remarks>
    /// <list type="bullet">
    ///   <item><term>false</term>: Usuario activo (puede autenticarse).</item>
    ///   <item><term>true</term>: Usuario eliminado (soft-delete).</item>
    /// </list>
    /// </remarks>
    public bool IsDeleted { get; set; }

    /// <summary>
    /// Fecha y hora UTC de creación del registro.
    /// 
    /// <para>
    /// Se asigna automáticamente al crear el usuario.
    /// Se usa para auditoría y ordenación por antigüedad.
    /// </para>
    /// <remarks>
    /// Formato: DateTime en UTC (ej: 2024-01-15T10:30:00Z)
    /// </remarks>
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Fecha y hora UTC de la última modificación.
    /// 
    /// <para>
    /// Se actualiza automáticamente cada vez que se modifica el registro.
    /// Si el usuario nunca ha sido modificado, coincide con CreatedAt.
    /// </para>
    /// <remarks>
    /// Importante para auditoría y sincronización de datos.
    /// </remarks>
    public DateTime UpdatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Determina si el avatar del usuario es local (almacenado en el servidor).
    /// 
    /// <para>
    /// Los avatares locales requieren manejo especial para servir archivos
    /// estáticos y limpieza al eliminar el usuario.
    /// </para>
    /// <returns>
    /// <see langword="true"/> si Avatar comienza con "/storage" (case-insensitive),
    /// <see langword="false"/> si es URL externa o no tiene avatar.
    /// </returns>
    /// <example>
    /// Uso típico:
    /// <code>
    /// if (user.IsLocalAvatar())
    ///     await storageService.DeleteFileAsync(user.Avatar);
    /// </code>
    /// </example>
    public bool IsLocalAvatar() => !string.IsNullOrEmpty(Avatar) && Avatar.StartsWith("/storage", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Determina si el usuario usa el avatar por defecto.
    /// 
    /// <para>
    /// Útil para mostrar un placeholder o solicitar carga de avatar
    /// en la interfaz de usuario.
    /// </para>
    /// <returns>
    /// <see langword="true"/> si Avatar es null, vacío o igual a AVATAR_DEFAULT.
    /// </returns>
    public bool HasDefaultAvatar() => string.IsNullOrEmpty(Avatar) || Avatar == AVATAR_DEFAULT;

    /// <summary>
    /// Obtiene la URL completa del avatar lista para mostrar en navegador.
    /// 
    /// <para>
    /// Normaliza diferentes formatos de entrada:
    /// <list type="number">
    ///   <item><description>URLs externas (http/https): retornadas sin modificación.</description></item>
    ///   <item><description>Rutas con /storage: retornadas tal cual.</description></item>
    ///   <item><description>Rutas relativas (/images/...): prepend /storage.</description></item>
    ///   <item><description>Nombres de archivo: prepend AVATAR_LOCAL_PREFIX.</description></item>
    ///   <item><description>Sin imagen: retorna AVATAR_DEFAULT.</description></item>
    /// </list>
    /// </para>
    /// <returns>URL absoluta o relativa lista para usar en etiquetas &lt;img src="..."&gt;.</returns>
    /// <example>
    /// Uso en HTML:
    /// <code>
    /// &lt;img src="@user.GetAvatarUrl()" alt="@user.Username" /&gt;
    /// </code>
    /// </example>
    public string GetAvatarUrl()
    {
        if (string.IsNullOrEmpty(Avatar))
            return AVATAR_DEFAULT;

        if (Avatar.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            Avatar.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            return Avatar;

        if (Avatar.StartsWith("/storage", StringComparison.OrdinalIgnoreCase))
            return Avatar;

        if (Avatar.StartsWith("/"))
            return $"/storage{Avatar}";

        return $"{AVATAR_LOCAL_PREFIX}{Avatar}";
    }
}

/// <summary>
/// Constantes para los roles de usuario disponibles en el sistema.
/// 
/// <para>
/// Define los niveles de autorización para control de acceso a funcionalidades.
/// Los roles determinan qué operaciones puede realizar cada usuario.
/// </para>
/// </summary>
public static class UserRoles
{
    /// <summary>
    /// Rol de usuario estándar con permisos básicos.
    /// 
    /// <para>
    /// Puede realizar operaciones comunes como:
    /// <list type="bullet">
    ///   <item><description>Ver catálogo de productos.</description></item>
    ///   <item><description>Crear y gestionar sus propios pedidos.</description></item>
    ///   <item><description>Editar su perfil de usuario.</description></item>
    ///   <item><description>Ver historial de compras.</description></item>
    /// </list>
    /// </para>
    /// </summary>
    public const string ADMIN = "ADMIN";

    /// <summary>
    /// Rol de administrador con acceso total al sistema.
    /// 
    /// <para>
    /// Tiene todos los permisos del rol USER más:
    /// <list type="bullet">
    ///   <item><description>Gestionar productos (crear, editar, eliminar).</description></item>
    ///   <item><description>Gestionar categorías.</description></item>
    ///   <item><description>Ver y gestionar todos los pedidos.</description></item>
    ///   <item><description>Ver estadísticas y reportes.</description></item>
    ///   <item><description>Gestionar usuarios.</description></item>
    /// </list>
    /// </para>
    /// </summary>
    public const string USER = "USER";
}

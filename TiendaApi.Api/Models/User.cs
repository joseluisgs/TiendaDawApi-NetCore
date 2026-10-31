using Microsoft.AspNetCore.Identity;
using TiendaApi.Api.Data.Abstractions;

namespace TiendaApi.Api.Models;

/// <summary>
/// Entidad de usuario que representa un usuario.
/// Hereda de IdentityUser&lt;long&gt; para soportar ASP.NET Core Identity.
/// Autenticación con email/password (BCrypt en modo Custom, PBKDF2 en modo Identity).
/// Roles: USER, ADMIN.
/// </summary>
public class User : IdentityUser<long>, ITimestamped
{
    /// <summary>URL de avatar por defecto cuando no hay imagen.</summary>
    public const string AVATAR_DEFAULT = "https://via.placeholder.com/150";

    /// <summary>Prefijo para avatares locales (/storage/images/usuarios/).</summary>
    public const string AVATAR_LOCAL_PREFIX = "/storage/images/usuarios/";

    // ── Heredados de IdentityUser<long> (NO redeclarar aquí) ──
    // public long Id              ← ya lo tenemos de IdentityUser<long>
    // public string Email          ← ya lo tenemos de IdentityUser<long>
    // public string PasswordHash   ← ya lo tenemos de IdentityUser<long> (nosotros usamos BCrypt encima)
    // public string UserName       ← ya lo tenemos de IdentityUser<long>

    // ── Propiedades nuevas que Identity añade automáticamente ──
    // EmailConfirmed, PhoneNumber, PhoneNumberConfirmed, TwoFactorEnabled,
    // LockoutEnd, LockoutEnabled, AccessFailedCount, SecurityStamp, ConcurrencyStamp
    // (no las tocamos, las gestiona UserManager)

    // ⚠️ NO redeclarar PasswordHash con `new`: UserStore (genérico) escribe en la
    // propiedad heredada de IdentityUser y si la ocultamos EF mapea la nuestra →
    // el hash real se pierde y nadie puede hacer login. PasswordHash es nullable
    // (los usuarios OAuth no tienen contraseña).

    /// <summary>URL o ruta del avatar (null = usa AVATAR_DEFAULT).</summary>
    public string? Avatar { get; set; }  // ← custom nuestro

    /// <summary>Rol del usuario (USER o ADMIN).</summary>
    public string Role { get; set; } = UserRoles.USER;  // ← custom nuestro (Identity usa AspNetUserRoles)

    /// <summary>Indica si el usuario está eliminado (soft-delete).</summary>
    public bool IsDeleted { get; set; }  // ← custom nuestro

    /// <summary>Fecha de creación en UTC.</summary>
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;  // ← custom nuestro (ITimestamped)

    /// <summary>Fecha de última modificación en UTC.</summary>
    public DateTime UpdatedAt { get; init; } = DateTime.UtcNow;  // ← custom nuestro

    /// <summary>
    /// Determina si el avatar es local (almacenado en el servidor).
    /// </summary>
    /// <returns>true si Avatar comienza con "/storage".</returns>
    public bool IsLocalAvatar() => !string.IsNullOrEmpty(Avatar) && Avatar.StartsWith("/storage", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Determina si el usuario usa el avatar por defecto.
    /// </summary>
    /// <returns>true si Avatar es null, vacío o igual a AVATAR_DEFAULT.</returns>
    public bool HasDefaultAvatar() => string.IsNullOrEmpty(Avatar) || Avatar == AVATAR_DEFAULT;

    /// <summary>
    /// Obtiene la URL completa del avatar normalizada.
    /// </summary>
    /// <returns>URL lista para usar en &lt;img src&gt;.</returns>
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
/// Constantes para los roles de usuario.
/// </summary>
public static class UserRoles
{
    /// <summary>Rol de administrador con acceso total al sistema.</summary>
    public const string ADMIN = "ADMIN";

    /// <summary>Rol de usuario estándar con permisos básicos.</summary>
    public const string USER = "USER";
}

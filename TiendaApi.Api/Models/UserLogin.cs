using TiendaApi.Api.Data.Abstractions;

namespace TiendaApi.Api.Models;

/// <summary>
/// Vinculación entre un usuario local y un login externo (OAuth 2.0).
/// Un usuario puede tener múltiples logins externos (Google + GitHub, por ejemplo).
/// La unicidad es (LoginProvider, ProviderKey): un proveedor externo solo puede
/// estar vinculado a un único usuario local.
/// </summary>
public class UserLogin : ITimestamped
{
    /// <summary>ID único del registro (PK en PostgreSQL).</summary>
    public long Id { get; set; }

    /// <summary>ID del usuario local al que está vinculado.</summary>
    public long UserId { get; set; }

    /// <summary>Nombre del proveedor externo ("Google", "GitHub", "Microsoft").</summary>
    public string LoginProvider { get; set; } = string.Empty;

    /// <summary>ID único del usuario en el proveedor externo (ProviderKey).</summary>
    public string ProviderKey { get; set; } = string.Empty;

    /// <summary>Nombre visible del proveedor para mostrar al usuario.</summary>
    public string? ProviderDisplayName { get; set; }

    /// <summary>Usuario local al que está vinculado.</summary>
    public User User { get; set; } = null!;

    /// <summary>Fecha de creación del registro en UTC.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Fecha de última modificación en UTC.</summary>
    public DateTime UpdatedAt { get; set; }
}

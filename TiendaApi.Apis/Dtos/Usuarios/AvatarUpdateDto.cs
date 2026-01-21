namespace TiendaApi.Apis.Dtos.Usuarios;

/// <summary>
/// DTO para actualizar el avatar de un usuario.
/// </summary>
public record AvatarUpdateDto
{
    /// <summary>URL del nuevo avatar.</summary>
    /// <value>URL absoluta (https://) o ruta local (/storage/).</value>
    /// <example>https://example.com/avatar.jpg</example>
    public string AvatarUrl { get; init; } = string.Empty;
}

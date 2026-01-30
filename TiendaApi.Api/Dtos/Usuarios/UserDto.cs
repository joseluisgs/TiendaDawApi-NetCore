namespace TiendaApi.Api.Dtos.Usuarios;

/// <summary>
/// Respuesta detallada de un usuario.
/// </summary>
public record UserDto
{
    /// <summary>Identificador numérico.</summary>
    public long Id { get; init; }

    /// <summary>Nombre de usuario.</summary>
    public string Username { get; init; } = string.Empty;

    /// <summary>Correo electrónico.</summary>
    public string Email { get; init; } = string.Empty;

    /// <summary>URL del avatar.</summary>
    public string Avatar { get; init; } = string.Empty;

    /// <summary>Rol del sistema (ADMIN/USER).</summary>
    public string Role { get; init; } = string.Empty;

    /// <summary>Fecha de alta.</summary>
    public DateTime CreatedAt { get; init; }
}

/// <summary>
/// DTO para la respuesta de autenticación exitosa.
/// </summary>
public record AuthResponseDto
{
    /// <summary>Token JWT emitido.</summary>
    public string Token { get; init; } = string.Empty;

    /// <summary>Información del usuario autenticado.</summary>
    public UserDto User { get; init; } = default!;
}

/// <summary>
/// Credenciales para el inicio de sesión.
/// </summary>
public record LoginDto
{
    /// <summary>Email o nombre de usuario.</summary>
    public string Username { get; init; } = string.Empty;

    /// <summary>Contraseña en texto plano.</summary>
    public string Password { get; init; } = string.Empty;
}
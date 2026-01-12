namespace TiendaApi.Apis.Dtos.Usuarios;

/// <summary>
/// DTO de usuario para respuestas de API (sin contraseña).
/// </summary>
public record UserDto
{
    /// <summary>
    /// Identificador único del usuario.
    /// </summary>
    public long Id { get; init; }

    /// <summary>
    /// Nombre de usuario único.
    /// </summary>
    public string Username { get; init; } = string.Empty;

    /// <summary>
    /// Correo electrónico del usuario.
    /// </summary>
    public string Email { get; init; } = string.Empty;

    /// <summary>
    /// Rol del usuario en el sistema.
    /// </summary>
    public string Role { get; init; } = string.Empty;

    /// <summary>
    /// Fecha de creación del registro.
    /// </summary>
    public DateTime CreatedAt { get; init; }
}

/// <summary>
/// DTO para el registro de nuevos usuarios.
/// </summary>
public record RegisterDto
{
    /// <summary>
    /// Nombre de usuario único.
    /// </summary>
    public string Username { get; init; } = string.Empty;

    /// <summary>
    /// Correo electrónico del usuario.
    /// </summary>
    public string Email { get; init; } = string.Empty;

    /// <summary>
    /// Contraseña del usuario.
    /// </summary>
    public string Password { get; init; } = string.Empty;
}

/// <summary>
/// DTO para el inicio de sesión de usuarios.
/// </summary>
public record LoginDto
{
    /// <summary>
    /// Nombre de usuario.
    /// </summary>
    public string Username { get; init; } = string.Empty;

    /// <summary>
    /// Contraseña del usuario.
    /// </summary>
    public string Password { get; init; } = string.Empty;
}

/// <summary>
/// DTO de respuesta de autenticación con JWT.
/// </summary>
public record AuthResponseDto
{
    /// <summary>
    /// Token JWT de autenticación.
    /// </summary>
    public string Token { get; init; } = string.Empty;

    /// <summary>
    /// Datos del usuario autenticado.
    /// </summary>
    public UserDto User { get; init; } = null!;
}

/// <summary>
/// DTO para actualizar datos de usuario.
/// </summary>
public record UserUpdateDto
{
    /// <summary>
    /// Nuevo correo electrónico del usuario.
    /// </summary>
    public string? Email { get; init; }

    /// <summary>
    /// Nueva contraseña del usuario.
    /// </summary>
    public string? Password { get; init; }
}

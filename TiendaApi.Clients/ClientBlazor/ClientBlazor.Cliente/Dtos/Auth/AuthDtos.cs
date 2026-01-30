namespace ClientBlazor.Cliente.DTOs.Auth;

/// <summary>
/// DTO para el inicio de sesión.
/// Copia exacta del LoginDto de la API.
/// </summary>
public record LoginDto
{
    public string Username { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
}

/// <summary>
/// DTO de respuesta de autenticación.
/// Copia exacta del AuthResponseDto de la API.
/// </summary>
public record AuthResponseDto(
    string Token,
    UserDto User
);

/// <summary>
/// DTO de usuario.
/// Copia exacta del UserDto de la API.
/// </summary>
public record UserDto(
    long Id,
    string Username,
    string Email,
    string Avatar,
    string Role,
    DateTime CreatedAt
);
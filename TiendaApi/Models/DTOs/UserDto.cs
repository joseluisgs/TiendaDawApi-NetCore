namespace TiendaApi.Models.DTOs;

/// <summary>
/// DTO for User responses (without password)
/// </summary>
public record UserDto
{
    public long Id { get; init; }
    public string Username { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Role { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
}

/// <summary>
/// DTO for user registration
/// </summary>
public record RegisterDto
{
    public string Username { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
}

/// <summary>
/// DTO for user login
/// </summary>
public record LoginDto
{
    public string Username { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
}

/// <summary>
/// DTO for authentication response with JWT
/// </summary>
public record AuthResponseDto
{
    public string Token { get; init; } = string.Empty;
    public UserDto User { get; init; } = null!;
}

/// <summary>
/// DTO for updating user
/// </summary>
public record UserUpdateDto
{
    public string? Email { get; init; }
    public string? Password { get; init; }
}

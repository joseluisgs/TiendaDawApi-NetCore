namespace ClientBlazor.Cliente.DTOs.Auth;

public record LoginDto
{
    public string Username { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
}

public record AuthResponseDto
{
    public string Token { get; init; } = string.Empty;
    public UserDto User { get; init; } = default!;
}

public record UserDto
{
    public long Id { get; init; }
    public string Username { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Avatar { get; init; } = string.Empty;
    public string Role { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
}

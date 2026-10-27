namespace TiendaApi.Api.Dtos.Auth;

/// <summary>
/// Información del login externo (OAuth 2.0) extraída del callback.
/// Equivalente a ExternalLoginInfo de Identity, pero sin dependencia de Identity.
/// </summary>
public record ExternalLoginDto(
    string LoginProvider,
    string ProviderKey,
    string? Email,
    string? Name,
    string? Avatar
);

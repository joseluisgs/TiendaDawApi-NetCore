namespace TiendaApi.Models.DTOs;

/// <summary>
/// DTO for Categoria responses
/// Separates internal entity from API contract
/// 
/// Java equivalent: Response DTO without @Entity annotations
/// Spring Boot: Similar to ResponseEntity body classes
/// </summary>
public record CategoriaDto
{
    public long Id { get; init; }
    public string Nombre { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}

/// <summary>
/// DTO for creating/updating Categoria
/// Java equivalent: Request DTO with @Valid annotations
/// </summary>
public record CategoriaRequestDto
{
    public string Nombre { get; init; } = string.Empty;
}

namespace TiendaApi.Apis.Dtos.Common;

/// <summary>
/// DTO para filtrar y paginar usuarios.
/// </summary>
public record UserFilterDto(
    string? Username,
    string? Email,
    bool? IsDeleted,
    int Page = 0,
    int Size = 10,
    string SortBy = "id",
    string Direction = "asc"
);

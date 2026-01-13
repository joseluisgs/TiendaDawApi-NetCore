namespace TiendaApi.Apis.Dtos.Common;

public record CategoriaFilterDto
{
    public string? Nombre { get; init; }
    public bool? IsDeleted { get; init; }
    public int Page { get; init; } = 0;
    public int Size { get; init; } = 10;
    public string SortBy { get; init; } = "id";
    public string Direction { get; init; } = "asc";
}

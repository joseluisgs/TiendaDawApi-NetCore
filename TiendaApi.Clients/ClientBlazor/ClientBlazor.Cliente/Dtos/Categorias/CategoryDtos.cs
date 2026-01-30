namespace ClientBlazor.Cliente.DTOs.Categorias;

public record CategoriaDto
{
    public long Id { get; init; }
    public string Nombre { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}

public record CategoriaRequestDto
{
    public string Nombre { get; init; } = string.Empty;
}

public record CategoriaFilterDto
{
    public string? Nombre { get; init; }
    public int Page { get; init; } = 0;
    public int Size { get; init; } = 10;
    public string SortBy { get; init; } = "id";
    public string Direction { get; init; } = "asc";
}

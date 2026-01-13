namespace TiendaApi.Apis.Dtos.Common;

public record ProductoFilterDto
{
    public string? Nombre { get; init; }
    public string? Categoria { get; init; }
    public bool? IsDeleted { get; init; }
    public decimal? PrecioMax { get; init; }
    public int? StockMin { get; init; }
    public int Page { get; init; } = 0;
    public int Size { get; init; } = 10;
    public string SortBy { get; init; } = "id";
    public string Direction { get; init; } = "asc";
}

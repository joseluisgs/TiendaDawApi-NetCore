namespace TiendaApi.Apis.Dtos.Common;

public record ProductoFilterDto(
    string? Nombre,
    string? Categoria,
    bool? IsDeleted,
    decimal? PrecioMax,
    int? StockMin,
    int Page = 0,
    int Size = 10,
    string SortBy = "id",
    string Direction = "asc"
);

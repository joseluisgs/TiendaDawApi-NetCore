namespace TiendaApi.Apis.Dtos.Common;

/// <summary>
/// DTO para filtrar y paginar pedidos.
/// </summary>
public record PedidoFilterDto(
    long? UserId,
    string? Estado,
    DateTime? FechaDesde,
    DateTime? FechaHasta,
    decimal? TotalMin,
    decimal? TotalMax,
    int Page = 0,
    int Size = 10,
    string SortBy = "createdAt",
    string Direction = "desc"
);

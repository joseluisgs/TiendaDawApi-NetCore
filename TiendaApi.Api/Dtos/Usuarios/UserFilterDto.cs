namespace TiendaApi.Api.Dtos.Usuarios;

/// <summary>
/// DTO para filtrar y paginar usuarios.
/// Permite búsquedas flexibles por diferentes criterios con soporte para paginación.
///
/// <remarks>
/// Uso típico:
/// - GET /api/users?username=juan&amp;page=0&amp;size=10
/// - Búsquedas administrativas de usuarios
/// - Reportes filtrados por estado
/// </remarks>
public record UserFilterDto
(
    /// <summary>
    /// Filtrar por nombre de usuario (búsqueda parcial).
    /// </summary>
    /// <example>juan</example>
    string? Username,

    /// <summary>
    /// Filtrar por correo electrónico (búsqueda exacta).
    /// </summary>
    /// <example>juan@example.com</example>
    string? Email,

    /// <summary>
    /// Filtrar por estado de eliminación lógica.
    /// </summary>
    /// <example>false</example>
    bool? IsDeleted,

    /// <summary>
    /// Número de página (base 0).
    /// </summary>
    /// <default>0</default>
    int Page = 0,

    /// <summary>
    /// Cantidad de elementos por página.
    /// </summary>
    /// <default>10</default>
    int Size = 10,

    /// <summary>
    /// Campo para ordenar resultados.
    /// </summary>
    /// <default>id</default>
    string SortBy = "id",

    /// <summary>
    /// Dirección de ordenamiento (asc/desc).
    /// </summary>
    /// <default>asc</default>
    string Direction = "asc"
);

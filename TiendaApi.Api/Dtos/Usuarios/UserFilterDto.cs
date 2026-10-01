namespace TiendaApi.Api.Dtos.Usuarios;

/// <summary>
/// DTO para filtrar y paginar usuarios.
/// Permite búsquedas flexibles por diferentes criterios con soporte para paginación.
/// </summary>
///
/// <remarks>
/// Uso típico:
/// - GET /api/users?username=juan&amp;page=0&amp;size=10
/// - Búsquedas administrativas de usuarios
/// - Reportes filtrados por estado
/// </remarks>
public record UserFilterDto
(
    // <summary>
    // Filtrar por nombre de usuario (búsqueda parcial).
    // </summary>
    // <example>juan</example>
    string? Username,

    // <summary>
    // Filtrar por correo electrónico (búsqueda exacta).
    // </summary>
    // <example>juan@example.com</example>
    string? Email,

    // <summary>
    // Filtrar por estado de eliminación lógica.
    // </summary>
    // <example>false</example>
    bool? IsDeleted,

    // <summary>
    // Número de página (base 0).
    // </summary>
    // <default>0</default>
    [System.ComponentModel.DataAnnotations.Range(0, int.MaxValue, ErrorMessage = "La página no puede ser negativa")]
    int Page = 0,

    // <summary>
    // Cantidad de elementos por página.
    // </summary>
    // <default>10</default>
    [System.ComponentModel.DataAnnotations.Range(1, 100, ErrorMessage = "El tamaño de página debe estar entre 1 y 100")]
    int Size = 10,

    // <summary>
    // Campo para ordenar resultados.
    // </summary>
    // <default>id</default>
    string SortBy = "id",

    // <summary>
    // Dirección de ordenamiento (asc/desc).
    // </summary>
    // <default>asc</default>
    string Direction = "asc"
);

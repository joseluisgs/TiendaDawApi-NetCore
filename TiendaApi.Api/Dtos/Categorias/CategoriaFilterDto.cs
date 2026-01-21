namespace TiendaApi.Api.Dtos.Categorias;

/// <summary>
/// DTO de filtros para paginación y búsqueda de categorías.
/// Permite filtrar resultados por nombre y estado de eliminación,
/// con soporte para paginación y ordenamiento configurable.
///
/// <remarks>
/// Uso típico:
/// - GET /api/categorias?nombre=Elec&amp;page=0&amp;size=10&amp;sortBy=nombre&amp;direction=desc
/// - Consultas con múltiples criterios combinados
/// </remarks>
/// </summary>
public record CategoriaFilterDto
{
    /// <summary>
    /// Filtrar categorías por nombre (búsqueda parcial, sensible a mayúsculas/minúsculas).
    /// Retorna categorías cuyo nombre contiene el texto especificado.
    /// </summary>
    /// <remarks>
    /// Comportamiento:
    /// - Valor null: sin filtro de nombre
    /// - Valor vacío: retorna todas las categorías
    /// - Valor con texto: busca coincidencias parciales
    /// </remarks>
    /// <example>Elec</example>
    public string? Nombre { get; init; }

    /// <summary>
    /// Filtrar por estado de eliminación lógica.
    /// Permite ver solo categorías activas, eliminadas o todas.
    /// </summary>
    /// <remarks>
    /// Valores posibles:
    /// - null: todas las categorías (por defecto)
    /// - false: solo categorías activas
    /// - true: solo categorías eliminadas
    /// </remarks>
    /// <example>false</example>
    public bool? IsDeleted { get; init; }

    /// <summary>
    /// Número de página para paginación (base 0).
    /// Primera página es 0, segunda es 1, etc.
    /// </summary>
    /// <value>0 por defecto</value>
    /// <example>0</example>
    public int Page { get; init; } = 0;

    /// <summary>
    /// Cantidad de elementos por página.
    /// Controla el tamaño de cada página de resultados.
    /// </summary>
    /// <value>10 por defecto</value>
    /// <remarks>
    /// Recomendaciones:
    /// - Valores muy altos pueden afectar rendimiento
    /// - Valor máximo recomendado: 100
    /// </remarks>
    /// <example>10</example>
    public int Size { get; init; } = 10;

    /// <summary>
    /// Campo por el cual ordenar los resultados.
    /// Determina el criterio de ordenamiento.
    /// </summary>
    /// <value>"id" por defecto</value>
    /// <remarks>
    /// Campos disponibles para ordenamiento:
    /// - "id": orden por identificador
    /// - "nombre": orden alfabético por nombre
    /// - "createdAt": orden por fecha de creación
    /// </remarks>
    /// <example>nombre</example>
    public string SortBy { get; init; } = "id";

    /// <summary>
    /// Dirección de ordenamiento: ascendente o descendente.
    /// </summary>
    /// <value>"asc" por defecto (ascendente)</value>
    /// <remarks>
    /// Valores posibles:
    /// - "asc": orden ascendente (A-Z, 1-9)
    /// - "desc": orden descendente (Z-A, 9-1)
    /// </remarks>
    /// <example>asc</example>
    public string Direction { get; init; } = "asc";
}

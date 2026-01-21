namespace TiendaApi.Api.Dtos.Common;

/// <summary>
/// DTO genérico para respuestas paginadas.
/// Este patrón de diseño permite devolver conjuntos de datos extensos
/// de manera fragmentada, mejorando el rendimiento y la experiencia de usuario.
///
/// <typeparam name="T">Tipo de elemento contenido en la paginación.</typeparam>
/// Implementa un modelo de paginación basado en cursor de página numérico.
///
/// <remarks>
/// Ventajas de la paginación:
/// - Reduce carga en servidor y base de datos
/// - Mejora tiempos de respuesta para grandes conjuntos de datos
/// - Facilita la navegación incremental para el usuario
/// - Permite procesamiento lazy de grandes volúmenes
///
/// Cálculos automáticos:
/// - TotalPages: Redondeo hacia arriba de TotalCount / PageSize
/// - HasNextPage: Indica si existen páginas después de la actual
/// - HasPreviousPage: Indica si existen páginas antes de la actual
/// </remarks>
/// </summary>
/// <example>
/// Ejemplo de uso con productos:
/// <code>
/// var resultado = new PagedResult&lt;ProductoDto&gt;
/// {
///     Items = productosPaginaActual,
///     TotalCount = 150,
///     Page = 2,
///     PageSize = 20
/// };
/// Resultado: 8 páginas totales (150/20 = 7.5 → 8)
/// </code>
///
/// <example>
/// JSON típico devuelto:
/// <code>
/// {
///   "items": [...20 productos...],
///   "totalCount": 150,
///   "page": 2,
///   "pageSize": 20,
///   "totalPages": 8,
///   "hasNextPage": true,
///   "hasPreviousPage": true
/// }
/// </code>
/// </example>
public record PagedResult<T>
{
    /// <summary>
    /// Elementos de la página actual.
    /// Colección de objetos del tipo especificado que pertenecen
    /// a la página solicitada según los parámetros de paginación.
    /// </summary>
    /// <remarks>
    /// Esta propiedad nunca es null. Si no hay elementos, devuelve una colección vacía.
    /// El número de elementos coincide con PageSize (excepto en la última página).
    /// </remarks>
    /// <example>
    /// <code>new List&lt;ProductoDto&gt; { prod1, prod2, prod3 }</code>
    /// </example>
    public IEnumerable<T> Items { get; init; } = Enumerable.Empty<T>();

    /// <summary>
    /// Número total de elementos en el conjunto completo.
    /// Cantidad total de registros que coinciden con los filtros aplicados,
    /// sin considerar la paginación.
    /// </summary>
    /// <remarks>
    /// Utilizado para:
    /// - Calcular número total de páginas
    /// - Mostrar información del tipo "Mostrando 1-20 de 150 elementos"
    /// - Determinar si hay más datos disponibles
    /// </remarks>
    /// <example>150</example>
    public int TotalCount { get; init; }

    /// <summary>
    /// Número de página actual (basado en 1).
    /// Índice de la página devuelta, comenzando desde 1.
    /// Valor 1 representa la primera página del conjunto de resultados.
    /// </summary>
    /// <remarks>
    /// Comportamiento según valor:
    /// - Page = 1: Primera página (siempre tiene HasPreviousPage = false)
    /// - Page > 1: Páginas intermedias o finales
    /// - Page > TotalPages: Devuelve página vacía
    /// </remarks>
    /// <example>2</example>
    public int Page { get; init; }

    /// <summary>
    /// Tamaño de página.
    /// Número máximo de elementos devueltos por página.
    /// Configurable por el cliente según sus necesidades.
    /// </summary>
    /// <remarks>
    /// Valores típicos:
    /// - 10: Vistas detalladas con mucha información
    /// - 20: Balance entre información y rendimiento
    /// - 50-100: Listados densos o exports
    ///
    /// Límites recomendados:
    /// - Mínimo: 1 elemento
    /// - Máximo: 100 elementos (para evitar problemas de rendimiento)
    /// </remarks>
    /// <example>20</example>
    public int PageSize { get; init; }

    /// <summary>
    /// Número total de páginas.
    /// Calculado automáticamente como el techo de TotalCount dividido por PageSize.
    /// Indica cuántas páginas existen en total para el conjunto de resultados.
    /// </summary>
    /// <remarks>
    /// Fórmula: Math.Ceiling(TotalCount / PageSize)
    /// Casos especiales:
    /// - PageSize = 0: Retorna 0 para evitar división por cero
    /// - TotalCount = 0: Retorna 0 (no hay páginas)
    /// </remarks>
    /// <example>
    /// TotalCount = 150, PageSize = 20 → TotalPages = 8
    /// (porque 150/20 = 7.5, y ceil(7.5) = 8)
    /// </example>
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;

    /// <summary>
    /// Indica si hay una página siguiente.
    /// Propiedad calculada que evalúa si la página actual es la última.
    /// True cuando Page es menor que TotalPages.
    /// </summary>
    /// <remarks>
    /// Uso típico en UI:
    /// - Habilitar botón "Siguiente" cuando HasNextPage = true
    /// - Deshabilitar cuando HasNextPage = false
    /// - Mostrar indicador visual "Página X de Y"
    /// </remarks>
    /// <example>
    /// Page = 2, TotalPages = 8 → HasNextPage = true
    /// Page = 8, TotalPages = 8 → HasNextPage = false
    /// </example>
    public bool HasNextPage => Page < TotalPages;

    /// <summary>
    /// Indica si hay una página anterior.
    /// Propiedad calculada que evalúa si la página actual es la primera.
    /// True cuando Page es mayor que 1.
    /// </summary>
    /// <remarks>
    /// Uso típico en UI:
    /// - Habilitar botón "Anterior" cuando HasPreviousPage = true
    /// - Deshabilitar cuando HasPreviousPage = false
    /// - Ocultar completamente el botón en primera página
    /// </remarks>
    /// <example>
    /// Page = 1, TotalPages = 8 → HasPreviousPage = false
    /// Page = 2, TotalPages = 8 → HasPreviousPage = true
    /// </example>
    public bool HasPreviousPage => Page > 1;
}

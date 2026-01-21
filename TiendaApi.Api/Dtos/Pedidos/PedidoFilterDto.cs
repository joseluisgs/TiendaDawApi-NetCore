using System.ComponentModel.DataAnnotations;

namespace TiendaApi.Api.Dtos.Pedidos;

/// <summary>
/// DTO para filtrar y paginar resultados de pedidos.
/// Proporciona criterios de búsqueda opcionales para optimizar consultas.
///
/// <remarks>
/// Uso típico:
/// - Panel de administración: filtrar por estado, fecha
/// - Historial de usuario: filtrar por período
/// - Búsqueda específica: por ID de pedido
/// </remarks>
/// </summary>
public class PedidoFilterDto
{
    /// <summary>
    /// Número de página a consultar (1-indexed).
    /// Valor por defecto: 1
    /// </summary>
    /// <example>1</example>
    [Range(1, int.MaxValue, ErrorMessage = "La página debe ser mayor a 0")]
    public int Page { get; init; } = 1;

    /// <summary>
    /// Cantidad de pedidos por página.
    /// Valor por defecto: 10
    /// Máximo permitido: 100
    /// </summary>
    /// <example>10</example>
    [Range(1, 100, ErrorMessage = "El tamaño de página debe estar entre 1 y 100")]
    public int PageSize { get; init; } = 10;

    /// <summary>
    /// Filtrar por estado del pedido.
    /// Si es null o vacío, no se aplica filtro por estado.
    /// </summary>
    /// <example> Pendiente</example>
    public string? Estado { get; init; }

    /// <summary>
    /// Filtrar por ID de usuario (solo admin).
    /// Si es null, se retornan pedidos de todos los usuarios.
    /// </summary>
    /// <example>1</example>
    public long? UserId { get; init; }

    /// <summary>
    /// Filtrar por fecha de inicio (inclusive).
    /// Si es null, no hay límite inferior de fecha.
    /// </summary>
    /// <example>2024-01-01</example>
    public DateTime? FechaInicio { get; init; }

    /// <summary>
    /// Filtrar por fecha de fin (inclusive).
    /// Si es null, no hay límite superior de fecha.
    /// </summary>
    /// <example>2024-12-31</example>
    public DateTime? FechaFin { get; init; }

    /// <summary>
    /// Texto de búsqueda en ID de pedido.
    /// Búsqueda parcial (contains).
    /// </summary>
    /// <example>PED-2024</example>
    public string? Busqueda { get; init; }
}

/// <summary>
/// DTO para filtrar pedidos propios del usuario.
/// Simplificación de PedidoFilterDto para endpoints de usuario.
/// </summary>
public class MyPedidoFilterDto
{
    /// <summary>
    /// Número de página a consultar (1-indexed).
    /// Valor por defecto: 1
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "La página debe ser mayor a 0")]
    public int Page { get; init; } = 1;

    /// <summary>
    /// Cantidad de pedidos por página.
    /// Valor por defecto: 10
    /// </summary>
    [Range(1, 100, ErrorMessage = "El tamaño de página debe estar entre 1 y 100")]
    public int PageSize { get; init; } = 10;

    /// <summary>
    /// Filtrar por estado del pedido.
    /// Si es null, no se aplica filtro.
    /// </summary>
    public string? Estado { get; init; }

    /// <summary>
    /// Filtrar por fecha de inicio.
    /// </summary>
    public DateTime? FechaInicio { get; init; }

    /// <summary>
    /// Filtrar por fecha de fin.
    /// </summary>
    public DateTime? FechaFin { get; init; }
}

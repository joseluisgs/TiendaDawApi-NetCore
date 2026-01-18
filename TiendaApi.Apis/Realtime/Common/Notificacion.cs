using System.Text.Json.Serialization;

namespace TiendaApi.Apis.Realtime.Common;

/// <summary>
/// DTO genérico de notificación para broadcasts en tiempo real.
/// Puede ser utilizado para cualquier tipo de entidad.
/// </summary>
/// <remarks>
/// <para><b>Uso:</b></para>
/// <code>
/// var notificacion = Notificacion&lt;ProductoDto&gt;.Create(
///     "productos",
///     Notificacion&lt;ProductoDto&gt;.Tipo.CREATE,
///     productoDto
/// );
/// </code>
/// 
/// <para><b>Serialización JSON:</b></para>
/// <code>
/// {
///   "entity": "productos",
///   "type": "CREATE",
///   "data": { ... },
///   "createdAt": "2025-01-18T10:30:00Z"
/// }
/// </code>
/// </remarks>
/// <typeparam name="T">Tipo de datos de la notificación.</typeparam>
public record Notificacion<T>
{
    /// <summary>
    /// Nombre de la entidad afectada.
    /// </summary>
    [JsonPropertyName("entity")]
    public string Entity { get; init; } = string.Empty;

    /// <summary>
    /// Tipo de operación realizada.
    /// </summary>
    [JsonPropertyName("type")]
    public Tipo Type { get; init; }

    /// <summary>
    /// Datos de la notificación.
    /// </summary>
    [JsonPropertyName("data")]
    public T Data { get; init; } = default!;

    /// <summary>
    /// Timestamp de creación en formato ISO 8601.
    /// </summary>
    [JsonPropertyName("createdAt")]
    public string CreatedAt { get; init; } = string.Empty;

    /// <summary>
    /// Tipos de operación soportados para notificaciones.
    /// </summary>
    public enum Tipo
    {
        CREATE,
        UPDATE,
        DELETE
    }

    /// <summary>
    /// Crea una nueva notificación con los datos proporcionados.
    /// </summary>
    /// <param name="entity">Nombre de la entidad.</param>
    /// <param name="type">Tipo de operación.</param>
    /// <param name="data">Datos de la notificación.</param>
    /// <returns>Nueva instancia de Notificacion.</returns>
    public static Notificacion<T> Create(string entity, Tipo type, T data)
    {
        return new Notificacion<T>
        {
            Entity = entity,
            Type = type,
            Data = data,
            CreatedAt = DateTime.UtcNow.ToString("o")
        };
    }
}

using System.Text.Json.Serialization;

namespace TiendaApi.Api.Realtime.Common;

/// <summary>
/// DTO genérico de notificación para broadcasts en tiempo real.
/// </summary>
/// <example>
/// var notificacion = Notificacion&lt;ProductoDto&gt;.Create("productos", Tipo.CREATE, productoDto);
/// // Serialización: {"entity":"productos","type":"CREATE","data":{...},"createdAt":"2025-01-18T10:30:00Z"}
/// </example>
/// <typeparam name="T">Tipo de datos.</typeparam>
public record Notificacion<T>
{
    /// <summary>Nombre de la entidad afectada.</summary>
    [JsonPropertyName("entity")]
    public string Entity { get; init; } = string.Empty;

    /// <summary>Tipo de operación.</summary>
    [JsonPropertyName("type")]
    public Tipo Type { get; init; }

    /// <summary>Datos de la notificación.</summary>
    [JsonPropertyName("data")]
    public T Data { get; init; } = default!;

    /// <summary>Timestamp en formato ISO 8601.</summary>
    [JsonPropertyName("createdAt")]
    public string CreatedAt { get; init; } = string.Empty;

    /// <summary>Tipos de operación soportados.</summary>
    public enum Tipo { CREATE, UPDATE, DELETE }

    /// <summary>Crea una nueva notificación.</summary>
    /// <param name="entity">Nombre de la entidad.</param>
    /// <param name="type">Tipo de operación.</param>
    /// <param name="data">Datos de la notificación.</param>
    /// <returns>Nueva instancia.</returns>
    public static Notificacion<T> Create(string entity, Tipo type, T data) => new()
    {
        Entity = entity,
        Type = type,
        Data = data,
        CreatedAt = DateTime.UtcNow.ToString("o")
    };
}

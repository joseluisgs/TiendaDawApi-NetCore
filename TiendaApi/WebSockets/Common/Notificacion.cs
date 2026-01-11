using System.Text.Json.Serialization;

namespace TiendaApi.WebSockets;

/// <summary>
/// Generic notification DTO for WebSocket broadcasts
/// Can be used for any entity type
/// </summary>
public record Notificacion<T>
{
    [JsonPropertyName("entity")]
    public string Entity { get; init; } = string.Empty;
    
    [JsonPropertyName("type")]
    public Tipo Type { get; init; }
    
    [JsonPropertyName("data")]
    public T Data { get; init; } = default!;
    
    [JsonPropertyName("createdAt")]
    public string CreatedAt { get; init; } = string.Empty;

    public enum Tipo
    {
        CREATE,
        UPDATE,
        DELETE
    }

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

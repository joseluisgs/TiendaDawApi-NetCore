namespace TiendaApi.Api.GraphQL.Publishers;

/// <summary>
/// Interfaz para publicar eventos en el bus de eventos de GraphQL.
/// </summary>
public interface IEventPublisher
{
    /// <summary>
    /// Publica un evento en el bus de eventos.
    /// </summary>
    /// <typeparam name="T">Tipo del payload del evento</typeparam>
    /// <param name="topic">Nombre del topic</param>
    /// <param name="payload">Payload del evento</param>
    Task PublishAsync<T>(string topic, T payload);
}

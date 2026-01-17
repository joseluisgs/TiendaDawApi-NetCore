using HotChocolate;
using HotChocolate.Execution;
using HotChocolate.Resolvers;
using HotChocolate.Subscriptions;

namespace TiendaApi.Apis.GraphQL.Publishers;

/// <summary>
/// Implementación de IEventPublisher que usa HotChocolate Pub/Sub.
/// </summary>
public class EventPublisher : IEventPublisher
{
    private readonly ITopicEventSender _eventSender;

    public EventPublisher(ITopicEventSender eventSender)
    {
        _eventSender = eventSender;
    }

    /// <inheritdoc />
    public async Task PublishAsync<T>(string topic, T payload)
    {
        await _eventSender.SendAsync(topic, payload);
    }
}

/// <summary>
/// Extensiones para registrar el EventPublisher en el contenedor DI.
/// </summary>
public static class EventPublisherExtensions
{
    /// <summary>
    /// Registra los servicios de Pub/Sub de GraphQL.
    /// </summary>
    public static IServiceCollection AddGraphQLPubSub(this IServiceCollection services)
    {
        services.AddSingleton<IEventPublisher, EventPublisher>();
        return services;
    }
}

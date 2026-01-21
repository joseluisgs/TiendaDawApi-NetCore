using HotChocolate;
using HotChocolate.Execution;
using HotChocolate.Resolvers;
using HotChocolate.Subscriptions;

namespace TiendaApi.Api.GraphQL.Publishers;

/// <summary>
/// Implementación de IEventPublisher usando HotChocolate Pub/Sub.
/// </summary>
public class EventPublisher : IEventPublisher
{
    private readonly ITopicEventSender _eventSender;

    public EventPublisher(ITopicEventSender eventSender) => _eventSender = eventSender;

    /// <inheritdoc/>
    public async Task PublishAsync<T>(string topic, T payload) =>
        await _eventSender.SendAsync(topic, payload);
}

/// <summary>
/// Extensiones para registro de Pub/Sub en DI.
/// </summary>
public static class EventPublisherExtensions
{
    /// <summary>Registra servicios de Pub/Sub de GraphQL.</summary>
    public static IServiceCollection AddGraphQLPubSub(this IServiceCollection services)
    {
        services.AddSingleton<IEventPublisher, EventPublisher>();
        return services;
    }
}

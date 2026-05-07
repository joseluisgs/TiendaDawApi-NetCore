using HotChocolate;
using HotChocolate.Subscriptions;
using Microsoft.Extensions.DependencyInjection;

namespace TiendaApi.Api.GraphQL.Publishers;

public class EventPublisher : IEventPublisher
{
    private readonly IServiceScopeFactory _scopeFactory;

    public EventPublisher(IServiceScopeFactory scopeFactory) => _scopeFactory = scopeFactory;

    public async Task PublishAsync<T>(string topic, T payload)
    {
        using var scope = _scopeFactory.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ITopicEventSender>();
        await sender.SendAsync(topic, payload);
    }
}

public static class EventPublisherExtensions
{
    public static IServiceCollection AddGraphQLPubSub(this IServiceCollection services)
    {
        services.AddSingleton<IEventPublisher, EventPublisher>();
        return services;
    }
}
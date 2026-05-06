using HotChocolate;
using HotChocolate.Subscriptions;
using Microsoft.Extensions.DependencyInjection;

namespace TiendaApi.Api.GraphQL.Publishers;

public class EventPublisher : IEventPublisher
{
    private readonly IServiceProvider _serviceProvider;

    public EventPublisher(IServiceProvider serviceProvider) => _serviceProvider = serviceProvider;

    public async Task PublishAsync<T>(string topic, T payload)
    {
        var sender = _serviceProvider.GetRequiredService<ITopicEventSender>();
        await sender.SendAsync(topic, payload);
    }
}

public static class EventPublisherExtensions
{
    public static IServiceCollection AddGraphQLPubSub(this IServiceCollection services)
    {
        services.AddScoped<IEventPublisher, EventPublisher>();
        return services;
    }
}
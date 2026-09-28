using HotChocolate;
using HotChocolate.Subscriptions;
using Microsoft.Extensions.DependencyInjection;

namespace TiendaApi.Api.GraphQL.Publishers;

/// <summary>
/// Publicador de eventos del bus de pub/sub de GraphQL (HotChocolate).
/// Resuelve un ámbito de servicios por publicación para usar el remitente de tópicos.
/// </summary>
public class EventPublisher : IEventPublisher
{
    private readonly IServiceScopeFactory _scopeFactory;

    /// <summary>
    /// Crea una instancia del publicador de eventos.
    /// </summary>
    /// <param name="scopeFactory">Fábrica de ámbitos de servicios para resolver el remitente.</param>
    public EventPublisher(IServiceScopeFactory scopeFactory) => _scopeFactory = scopeFactory;

    /// <summary>
    /// Publica un payload en el tópico indicado del bus de suscripciones.
    /// </summary>
    /// <typeparam name="T">Tipo del evento publicado.</typeparam>
    /// <param name="topic">Nombre del tópico de suscripción.</param>
    /// <param name="payload">Datos del evento.</param>
    public async Task PublishAsync<T>(string topic, T payload)
    {
        using var scope = _scopeFactory.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ITopicEventSender>();
        await sender.SendAsync(topic, payload);
    }
}

/// <summary>
/// Extensiones de registro del publicador de eventos GraphQL.
/// </summary>
public static class EventPublisherExtensions
{
    /// <summary>
    /// Registra el publicador de eventos de GraphQL en el contenedor de dependencias.
    /// </summary>
    /// <param name="services">Colección de servicios de la aplicación.</param>
    /// <returns>La misma colección de servicios para encadenar registros.</returns>
    public static IServiceCollection AddGraphQLPubSub(this IServiceCollection services)
    {
        services.AddSingleton<IEventPublisher, EventPublisher>();
        return services;
    }
}

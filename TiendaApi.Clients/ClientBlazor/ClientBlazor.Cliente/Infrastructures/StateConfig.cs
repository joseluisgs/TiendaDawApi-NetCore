using ClientBlazor.Cliente.State.Auth;
using ClientBlazor.Cliente.State.Notifications;

namespace ClientBlazor.Cliente.Infrastructures;

/// <summary>
/// Define métodos de extensión para el registro de los almacenes de estado reactivo.
/// </summary>
public static class StateConfig
{
    /// <summary>
    /// Registra los almacenes de estado globales como Singletons.
    /// </summary>
    /// <param name="services">Contenedor de dependencias.</param>
    /// <returns>Contenedor de dependencias actualizado.</returns>
    public static IServiceCollection AddStateStores(this IServiceCollection services)
    {
        services.AddSingleton<IAuthStore, AuthStore>();
        services.AddSingleton<INotificationStore, NotificationStore>();
        return services;
    }
}

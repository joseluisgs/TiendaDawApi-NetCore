using ClientBlazor.Cliente.Services.Storage;
using ClientBlazor.Cliente.State.Auth;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

namespace ClientBlazor.Cliente.Infrastructures;

/// <summary>
/// Proporciona métodos de extensión para la inicialización controlada de la aplicación durante el arranque.
/// </summary>
public static class AppInitializationExtensions
{
    private const string AuthStorageKey = "auth_session";

    /// <summary>
    /// Carga el estado inicial de la aplicación desde el almacenamiento persistente.
    /// Debe invocarse antes del arranque del host.
    /// </summary>
    /// <param name="host">Host de la aplicación WebAssembly.</param>
    public static async Task InitializeAppStateAsync(this WebAssemblyHost host)
    {
        var storage = host.Services.GetRequiredService<ILocalStorageService>();
        var authStore = host.Services.GetRequiredService<IAuthStore>();

        var savedSession = await storage.GetItemAsync<AuthStore.AuthState>(AuthStorageKey);

        if (savedSession != null && !string.IsNullOrEmpty(savedSession.Token))
        {
            authStore.SetAuth(
                savedSession.Token, 
                savedSession.Email, 
                savedSession.Nombre, 
                savedSession.Role
            );
        }
    }

    /// <summary>
    /// Guarda el estado de autenticación actual en el LocalStorage.
    /// </summary>
    /// <param name="storage">Instancia del servicio de almacenamiento.</param>
    /// <param name="state">Estado a persistir.</param>
    public static async Task SaveAuthSessionAsync(this ILocalStorageService storage, AuthStore.AuthState state)
    {
        await storage.SetItemAsync(AuthStorageKey, state);
    }

    /// <summary>
    /// Elimina los datos de sesión guardados en el dispositivo.
    /// </summary>
    /// <param name="storage">Instancia del servicio de almacenamiento.</param>
    public static async Task ClearAuthSessionAsync(this ILocalStorageService storage)
    {
        await storage.RemoveItemAsync(AuthStorageKey);
    }
}
using System.Net;
using System.Net.Http.Headers;
using ClientBlazor.Cliente.State.Auth;
using ClientBlazor.Cliente.State.Notifications;
using ClientBlazor.Cliente.Services.Storage;

namespace ClientBlazor.Cliente.Infrastructures.Handlers;

/// <summary>
/// Interceptor de mensajes HTTP que actúa como middleware de cliente.
/// </summary>
/// <remarks>
/// 1. Inyecta automáticamente el token JWT en la cabecera 'Authorization' si existe en el store.
/// 2. Monitoriza las respuestas con código 401 (Unauthorized) para gestionar sesiones expiradas de forma proactiva.
/// </remarks>
public class AuthHeaderHandler(
    IAuthStore authStore, 
    INotificationStore notificationStore,
    ILocalStorageService storage) : DelegatingHandler
{
    private const string AuthStorageKey = "auth_session";

    /// <summary>
    /// Procesa la petición HTTP saliente y la respuesta entrante de forma asíncrona.
    /// </summary>
    /// <param name="request">La instancia de <see cref="HttpRequestMessage"/> que se va a enviar.</param>
    /// <param name="cancellationToken">Token para cancelar la operación asíncrona.</param>
    /// <returns>La instancia de <see cref="HttpResponseMessage"/> recibida del servidor.</returns>
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var authState = authStore.GetState();
        if (!string.IsNullOrEmpty(authState.Token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authState.Token);
        }

        var response = await base.SendAsync(request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized && authState.IsAuthenticated)
        {
            await HandleExpiredSession();
        }

        return response;
    }

    /// <summary>
    /// Gestiona el flujo de cierre de sesión cuando el servidor detecta un token inválido o expirado.
    /// </summary>
    /// <returns>Una tarea que representa la operación de limpieza.</returns>
    private async Task HandleExpiredSession()
    {
        authStore.Logout();
        await storage.RemoveItemAsync(AuthStorageKey);
        notificationStore.Error(
            "Su sesión ha expirado o el token no es válido. Por favor, inicie sesión de nuevo.", 
            "Sesión Expirada"
        );
    }
}
using ClientBlazor.Cliente.Domain.Errors;
using ClientBlazor.Cliente.State.Auth;
using ClientBlazor.Cliente.State.Notifications;
using ClientBlazor.Cliente.DTOs.Auth;
using ClientBlazor.Cliente.Clients;
using ClientBlazor.Cliente.Infrastructures;
using ClientBlazor.Cliente.Services.Storage;
using CSharpFunctionalExtensions;
using Refit;
using System.Net;

namespace ClientBlazor.Cliente.Services.Rest;

/// <inheritdoc cref="IAuthService" />
public class AuthService(
    ITiendaRestClient client,
    IAuthStore authStore,
    INotificationStore notificationStore,
    ILocalStorageService storage) : IAuthService
{
    /// <inheritdoc cref="IAuthService.LoginAsync(string, string)" />
    public async Task<Result<AuthResponseDto, DomainError>> LoginAsync(string email, string password)
    {
        try
        {
            ValidateCredentials(email, password);
            var loginDto = new LoginDto { Username = email, Password = password };
            var response = await client.LoginAsync(loginDto);

            authStore.SetAuth(
                token: response.Token,
                email: response.User.Email,
                nombre: response.User.Username,
                role: response.User.Role
            );

            await storage.SaveAuthSessionAsync(authStore.GetState());

            notificationStore.Success($"Bienvenido de nuevo, {response.User.Username}!", "Login Correcto");
            return Result.Success<AuthResponseDto, DomainError>(response);
        }
        catch (ApiException ex)
        {
            var error = MapExceptionToDomainError(ex);
            notificationStore.Error(error.Message, "Error de Autenticación");
            return Result.Failure<AuthResponseDto, DomainError>(error);
        }
        catch (DomainError dex)
        {
            return Result.Failure<AuthResponseDto, DomainError>(dex);
        }
        catch (Exception)
        {
            return Result.Failure<AuthResponseDto, DomainError>(GeneralErrors.Unexpected);
        }
    }

    /// <inheritdoc cref="IAuthService.LogoutAsync" />
    public async Task LogoutAsync()
    {
        authStore.Logout();
        await storage.ClearAuthSessionAsync();
    }

    /// <summary>
    /// Mapea códigos de estado HTTP a errores de dominio de seguridad.
    /// </summary>
    /// <param name="ex">La excepción de Refit capturada.</param>
    /// <returns>Una instancia de <see cref="DomainError"/> correspondiente.</returns>
    private static DomainError MapExceptionToDomainError(ApiException ex)
    {
        return ex.StatusCode switch
        {
            HttpStatusCode.Unauthorized => AuthErrors.InvalidCredentials,
            HttpStatusCode.Forbidden => AuthErrors.InsufficientPermissions,
            HttpStatusCode.NotFound => AuthErrors.UserNotFound,
            HttpStatusCode.BadRequest => NetworkErrors.NotFound,
            HttpStatusCode.InternalServerError => NetworkErrors.ServerError,
            HttpStatusCode.ServiceUnavailable => NetworkErrors.ConnectionFailed,
            _ => NetworkErrors.ConnectionFailed
        };
    }

    /// <summary>
    /// Valida que las credenciales no estén vacías y tengan un formato básico correcto.
    /// </summary>
    /// <param name="email">Email a validar.</param>
    /// <param name="password">Contraseña a validar.</param>
    /// <exception cref="DomainError">Se lanza si la validación falla.</exception>
    private static void ValidateCredentials(string email, string password)
    {
        if (string.IsNullOrWhiteSpace(email)) throw ValidationErrors.EmptyField("email");
        if (string.IsNullOrWhiteSpace(password)) throw ValidationErrors.EmptyField("password");
    }
}
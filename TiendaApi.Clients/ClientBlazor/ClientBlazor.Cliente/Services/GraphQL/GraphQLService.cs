using GraphQL;
using GraphQL.Client.Http;
using CSharpFunctionalExtensions;
using ClientBlazor.Cliente.Domain.Errors;
using ClientBlazor.Cliente.State.Auth;
using ClientBlazor.Cliente.State.Notifications;
using System.Reactive.Linq;
using System.Text.Json;

namespace ClientBlazor.Cliente.Services.GraphQL;

/// <inheritdoc cref="IGraphQLService" />
public class GraphQLService(
    GraphQLHttpClient client,
    IAuthStore authStore,
    INotificationStore notificationStore) : IGraphQLService
{
    /// <inheritdoc cref="IGraphQLService.ExecuteQueryAsync{T}(string, object?)" />
    public async Task<Result<T, DomainError>> ExecuteQueryAsync<T>(string query, object? variables = null)
    {
        try
        {
            var request = new GraphQLRequest { Query = query, Variables = variables };
            var response = await client.SendQueryAsync<T>(request);

            if (response.Errors != null && response.Errors.Any())
            {
                var errorMsg = response.Errors[0].Message;
                notificationStore.Error(errorMsg, "Error en GraphQL Query");
                return Result.Failure<T, DomainError>(new GraphQLError("GRAPHQL_QUERY_ERROR", errorMsg));
            }

            return Result.Success<T, DomainError>(response.Data);
        }
        catch (Exception)
        {
            notificationStore.Error("No se pudo conectar con el servidor GraphQL", "Error de Red");
            return Result.Failure<T, DomainError>(NetworkErrors.ConnectionFailed);
        }
    }

    /// <inheritdoc cref="IGraphQLService.ExecuteMutationAsync{T}(string, object?)" />
    public async Task<Result<T, DomainError>> ExecuteMutationAsync<T>(string mutation, object? variables = null)
    {
        var authState = authStore.GetState();
        if (!authState.IsAuthenticated)
            return Result.Failure<T, DomainError>(AuthErrors.LoginRequired);

        try
        {
            var request = new GraphQLRequest { Query = mutation, Variables = variables };
            var response = await client.SendMutationAsync<T>(request);

            if (response.Errors != null && response.Errors.Any())
            {
                var errorMsg = response.Errors[0].Message;
                notificationStore.Error(errorMsg, "Error en GraphQL Mutation");
                return Result.Failure<T, DomainError>(new GraphQLError("GRAPHQL_MUTATION_ERROR", errorMsg));
            }

            return Result.Success<T, DomainError>(response.Data);
        }
        catch (Exception)
        {
            notificationStore.Error("Error de conexion al ejecutar mutacion", "Error de Red");
            return Result.Failure<T, DomainError>(NetworkErrors.ConnectionFailed);
        }
    }

    /// <inheritdoc cref="IGraphQLService.SubscribeAsync{T}(string)" />
    public IObservable<T> SubscribeAsync<T>(string query)
    {
        var request = new GraphQLRequest { Query = query };
        
        Console.WriteLine($"[GraphQL] Creating subscription: {query}");
        
        var stream = client.CreateSubscriptionStream<T>(request);
        
        return stream
            .Do(response =>
            {
                Console.WriteLine($"[GraphQL] Raw response: {response}");
                if (response.Errors?.Any() == true)
                {
                    foreach (var err in response.Errors)
                    {
                        Console.WriteLine($"[GraphQL] Error: {err.Message}");
                    }
                }
            })
            .Where(response => response.Errors == null || !response.Errors.Any())
            .Select(response =>
            {
                if (response.Data == null)
                {
                    Console.WriteLine("[GraphQL] Data is null");
                    return default!;
                }
                
                Console.WriteLine($"[GraphQL] Data: {JsonSerializer.Serialize(response.Data)}");
                return ExtractSubscriptionData<T>(response.Data);
            })
            .Where(data => data != null)!;
    }

    private static T ExtractSubscriptionData<T>(object data)
    {
        try
        {
            if (data == null)
                return default!;
            
            var json = JsonSerializer.Serialize(data);
            return JsonSerializer.Deserialize<T>(json) ?? default!;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[GraphQL] Deserialize error: {ex.Message}");
            
            try
            {
                return (T)data;
            }
            catch
            {
                return default!;
            }
        }
    }

    /// <summary>
    /// Representa un error especifico devuelto por el motor GraphQL.
    /// </summary>
    private class GraphQLError(string code, string message) : DomainError(code, message);
}

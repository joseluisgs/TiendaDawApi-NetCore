using GraphQL;
using GraphQL.Client.Http;
using CSharpFunctionalExtensions;
using ClientBlazor.Cliente.Domain.Errors;
using ClientBlazor.Cliente.State.Auth;
using ClientBlazor.Cliente.State.Notifications;
using System.Reactive.Linq;

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
            notificationStore.Error("Error de conexión al ejecutar mutación", "Error de Red");
            return Result.Failure<T, DomainError>(NetworkErrors.ConnectionFailed);
        }
    }

    /// <inheritdoc cref="IGraphQLService.SubscribeAsync{T}(string)" />
    public IObservable<T> SubscribeAsync<T>(string query)
    {
        var request = new GraphQLRequest { Query = query };
        return client.CreateSubscriptionStream<T>(request)
            .Select(response => response.Data);
    }

    /// <summary>
    /// Representa un error específico devuelto por el motor GraphQL.
    /// </summary>
    private class GraphQLError(string code, string message) : DomainError(code, message);
}

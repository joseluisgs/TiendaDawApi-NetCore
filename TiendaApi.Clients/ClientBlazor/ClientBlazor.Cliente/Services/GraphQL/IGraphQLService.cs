using CSharpFunctionalExtensions;
using ClientBlazor.Cliente.Domain.Errors;

namespace ClientBlazor.Cliente.Services.GraphQL;

/// <summary>
/// Define el contrato para el servicio de comunicación GraphQL.
/// Soporta operaciones de lectura (Queries), escritura (Mutations) y tiempo real (Subscriptions).
/// </summary>
public interface IGraphQLService
{
    /// <summary>
    /// Ejecuta una consulta de lectura en el grafo de la API.
    /// </summary>
    /// <typeparam name="T">Tipo del objeto de respuesta esperado.</typeparam>
    /// <param name="query">La cadena de consulta GraphQL.</param>
    /// <param name="variables">Diccionario opcional de variables para la consulta.</param>
    /// <returns>Resultado con los datos o error de dominio/red.</returns>
    Task<Result<T, DomainError>> ExecuteQueryAsync<T>(string query, object? variables = null);

    /// <summary>
    /// Ejecuta una operación de escritura o modificación en la API.
    /// Requiere que el usuario esté autenticado.
    /// </summary>
    /// <typeparam name="T">Tipo del objeto resultante.</typeparam>
    /// <param name="mutation">La cadena de mutación GraphQL.</param>
    /// <param name="variables">Diccionario opcional de variables.</param>
    /// <returns>Resultado con la confirmación o error de dominio.</returns>
    Task<Result<T, DomainError>> ExecuteMutationAsync<T>(string mutation, object? variables = null);

    /// <summary>
    /// Inicia una suscripción persistente para recibir eventos en tiempo real.
    /// </summary>
    /// <typeparam name="T">Tipo del evento esperado.</typeparam>
    /// <param name="query">La cadena de suscripción GraphQL.</param>
    /// <returns>Un observable que emite los eventos conforme llegan del servidor.</returns>
    IObservable<T> SubscribeAsync<T>(string query);
}

namespace ClientBlazor.Cliente.Services.Storage;

/// <summary>
/// Define el contrato para el almacenamiento persistente en el cliente.
/// Proporciona una abstracción sobre el LocalStorage del navegador.
/// </summary>
public interface ILocalStorageService
{
    /// <summary>
    /// Guarda un objeto serializado en el almacenamiento local.
    /// </summary>
    /// <typeparam name="T">Tipo del objeto a guardar.</typeparam>
    /// <param name="key">Clave única del elemento.</param>
    /// <param name="value">Valor del objeto.</param>
    Task SetItemAsync<T>(string key, T value);

    /// <summary>
    /// Recupera y deserializa un objeto del almacenamiento local.
    /// </summary>
    /// <typeparam name="T">Tipo del objeto esperado.</typeparam>
    /// <param name="key">Clave del elemento.</param>
    /// <returns>El objeto recuperado o null si no existe.</returns>
    Task<T?> GetItemAsync<T>(string key);

    /// <summary>
    /// Elimina un elemento específico del almacenamiento local.
    /// </summary>
    /// <param name="key">Clave del elemento a borrar.</param>
    Task RemoveItemAsync(string key);

    /// <summary>
    /// Limpia todos los elementos guardados en el almacenamiento local.
    /// </summary>
    Task ClearAsync();
}
using Microsoft.JSInterop;
using System.Text.Json;

namespace ClientBlazor.Cliente.Services.Storage;

/// <inheritdoc cref="ILocalStorageService" />
public class LocalStorageService(IJSRuntime jsRuntime) : ILocalStorageService
{
    /// <inheritdoc cref="ILocalStorageService.SetItemAsync{T}(string, T)" />
    public async Task SetItemAsync<T>(string key, T value)
    {
        var json = JsonSerializer.Serialize(value);
        await jsRuntime.InvokeVoidAsync("localStorage.setItem", key, json);
    }

    /// <inheritdoc cref="ILocalStorageService.GetItemAsync{T}(string)" />
    public async Task<T?> GetItemAsync<T>(string key)
    {
        var json = await jsRuntime.InvokeAsync<string?>("localStorage.getItem", key);
        if (string.IsNullOrEmpty(json)) return default;
        
        try 
        {
            return JsonSerializer.Deserialize<T>(json);
        }
        catch 
        {
            return default;
        }
    }

    /// <inheritdoc cref="ILocalStorageService.RemoveItemAsync(string)" />
    public async Task RemoveItemAsync(string key)
    {
        await jsRuntime.InvokeVoidAsync("localStorage.removeItem", key);
    }

    /// <inheritdoc cref="ILocalStorageService.ClearAsync" />
    public async Task ClearAsync()
    {
        await jsRuntime.InvokeVoidAsync("localStorage.clear");
    }
}

using ClientBlazor.Cliente.Services.Storage;
using Microsoft.JSInterop;
using Moq;
using NUnit.Framework;

namespace ClientBlazor.Tests.Services;

/// <summary>
/// Pruebas para el servicio de persistencia local.
/// Objetivo: Validar la interoperabilidad con la API LocalStorage del navegador mediante JSInterop.
/// </summary>
[TestFixture]
public class LocalStorageServiceTests
{
    private Mock<IJSRuntime> _jsRuntimeMock = null!;
    private LocalStorageService _storageService = null!;

    /// <summary>
    /// Configura el mock del entorno de ejecución de JavaScript.
    /// </summary>
    [SetUp]
    public void Setup()
    {
        _jsRuntimeMock = new Mock<IJSRuntime>();
        _storageService = new LocalStorageService(_jsRuntimeMock.Object);
    }

    /// <summary>
    /// Verifica que al llamar a SetItem, el servicio invoque el método 'localStorage.setItem' de JS.
    /// </summary>
    [Test]
    public async Task SetItemAsync_Should_Call_JS_LocalStorage_SetItem()
    {
        // Act
        await _storageService.SetItemAsync("key", "value");

        // Assert
        _jsRuntimeMock.Verify(js => js.InvokeAsync<object>(
            "localStorage.setItem", 
            It.Is<object[]>(args => args[0].ToString() == "key")), 
            Times.Once);
    }

    /// <summary>
    /// Verifica que al llamar a RemoveItem, el servicio invoque el método 'localStorage.removeItem' de JS.
    /// </summary>
    [Test]
    public async Task RemoveItemAsync_Should_Call_JS_LocalStorage_RemoveItem()
    {
        // Act
        await _storageService.RemoveItemAsync("key");

        // Assert
        _jsRuntimeMock.Verify(js => js.InvokeAsync<object>(
            "localStorage.removeItem", 
            It.Is<object[]>(args => args[0].ToString() == "key")), 
            Times.Once);
    }
}
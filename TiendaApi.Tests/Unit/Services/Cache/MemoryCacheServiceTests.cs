using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using TiendaApi.Api.Services.Cache;

namespace TiendaApi.Tests.Unit.Services.Cache;

/// <summary>
///     Tests unitarios para MemoryCacheService.
/// </summary>
public class MemoryCacheServiceTests {
    private MemoryCacheService _cacheService = null!;
    private IMemoryCache _memoryCache = null!;
    private Mock<ILogger<MemoryCacheService>> _mockLogger = null!;

    [SetUp]
    public void Setup() {
        _memoryCache = new MemoryCache(new MemoryCacheOptions());
        _mockLogger = new Mock<ILogger<MemoryCacheService>>();
        _cacheService = new MemoryCacheService(_memoryCache, _mockLogger.Object);
    }

    [TearDown]
    public void TearDown() {
        _memoryCache.Dispose();
    }

    #region GetAsync Tests

    [Test]
    public async Task GetAsync_ClaveExistente_RetornaValor() {
        string key = "test-key";
        string expectedValue = "test-value";

        _memoryCache.Set(key, expectedValue);

        string? result = await _cacheService.GetAsync<string>(key);

        result.Should().Be(expectedValue);
    }

    [Test]
    public async Task GetAsync_ClaveNoExistente_RetornaNull() {
        string? result = await _cacheService.GetAsync<string>("non-existent-key");

        result.Should().BeNull();
    }

    [Test]
    public async Task GetAsync_ObjetoComplejo_RetornaObjeto() {
        string key = "complex-key";
        var expectedObject = new TestCacheObject { Id = 1, Name = "Test" };

        _memoryCache.Set(key, expectedObject);

        TestCacheObject? result = await _cacheService.GetAsync<TestCacheObject>(key);

        result.Should().NotBeNull();
        result!.Id.Should().Be(1);
        result.Name.Should().Be("Test");
    }

    #endregion

    #region SetAsync Tests

    [Test]
    public async Task SetAsync_ValorValido_GuardaEnCache() {
        string key = "new-key";
        string value = "new-value";

        Func<Task> act = async () => await _cacheService.SetAsync(key, value);

        await act.Should().NotThrowAsync();

        string? result = _memoryCache.Get<string>(key);
        result.Should().Be(value);
    }

    [Test]
    public async Task SetAsync_ConExpiracion_AplicaExpiracion() {
        string key = "expiring-key";
        string value = "expiring-value";
        var expiration = TimeSpan.FromSeconds(1);

        await _cacheService.SetAsync(key, value, expiration);

        string? result = _memoryCache.Get<string>(key);
        result.Should().Be(value);

        await Task.Delay(1100);

        string? expiredResult = _memoryCache.Get<string>(key);
        expiredResult.Should().BeNull();
    }

    [Test]
    public async Task SetAsync_SinExpiracion_UsaExpiracionPorDefecto() {
        string key = "default-expiry-key";
        string value = "value";

        await _cacheService.SetAsync(key, value);

        string? result = _memoryCache.Get<string>(key);
        result.Should().Be(value);
    }

    [Test]
    public async Task SetAsync_ObjetoComplejo_SerializaCorrectamente() {
        string key = "complex-object-key";
        var value = new TestCacheObject { Id = 42, Name = "Complex Test" };

        Func<Task> act = async () => await _cacheService.SetAsync(key, value);

        await act.Should().NotThrowAsync();

        TestCacheObject? result = _memoryCache.Get<TestCacheObject>(key);
        result.Should().NotBeNull();
        result!.Id.Should().Be(42);
        result.Name.Should().Be("Complex Test");
    }

    #endregion

    #region RemoveAsync Tests

    [Test]
    public async Task RemoveAsync_ClaveExistente_Elimina() {
        string key = "key-to-remove";
        _memoryCache.Set(key, "value");

        Func<Task> act = async () => await _cacheService.RemoveAsync(key);

        await act.Should().NotThrowAsync();

        string? result = _memoryCache.Get<string>(key);
        result.Should().BeNull();
    }

    [Test]
    public async Task RemoveAsync_ClaveNoExistente_NoLanzaExcepcion() {
        Func<Task> act = async () => await _cacheService.RemoveAsync("non-existent-key");

        await act.Should().NotThrowAsync();
    }

    #endregion
}

/// <summary>
///     Clase de prueba para serialización.
/// </summary>
public class TestCacheObject {
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

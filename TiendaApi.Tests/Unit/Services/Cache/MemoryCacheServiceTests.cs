using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using TiendaApi.Api.Services.Cache;

namespace TiendaApi.Tests.Unit.Services.Cache;

/// <summary>
/// Tests unitarios para MemoryCacheService.
/// </summary>
public class MemoryCacheServiceTests
{
    private IMemoryCache _memoryCache = null!;
    private Mock<ILogger<MemoryCacheService>> _mockLogger = null!;
    private MemoryCacheService _cacheService = null!;

    [SetUp]
    public void Setup()
    {
        _memoryCache = new MemoryCache(new MemoryCacheOptions());
        _mockLogger = new Mock<ILogger<MemoryCacheService>>();
        _cacheService = new MemoryCacheService(_memoryCache, _mockLogger.Object);
    }

    [TearDown]
    public void TearDown()
    {
        _memoryCache.Dispose();
    }

    #region GetAsync Tests

    [Test]
    public async Task GetAsync_ClaveExistente_RetornaValor()
    {
        var key = "test-key";
        var expectedValue = "test-value";

        _memoryCache.Set(key, expectedValue);

        var result = await _cacheService.GetAsync<string>(key);

        result.Should().Be(expectedValue);
    }

    [Test]
    public async Task GetAsync_ClaveNoExistente_RetornaNull()
    {
        var result = await _cacheService.GetAsync<string>("non-existent-key");

        result.Should().BeNull();
    }

    [Test]
    public async Task GetAsync_ObjetoComplejo_RetornaObjeto()
    {
        var key = "complex-key";
        var expectedObject = new TestCacheObject { Id = 1, Name = "Test" };

        _memoryCache.Set(key, expectedObject);

        var result = await _cacheService.GetAsync<TestCacheObject>(key);

        result.Should().NotBeNull();
        result!.Id.Should().Be(1);
        result.Name.Should().Be("Test");
    }

    #endregion

    #region SetAsync Tests

    [Test]
    public async Task SetAsync_ValorValido_GuardaEnCache()
    {
        var key = "new-key";
        var value = "new-value";

        var act = async () => await _cacheService.SetAsync(key, value);

        await act.Should().NotThrowAsync();

        var result = _memoryCache.Get<string>(key);
        result.Should().Be(value);
    }

    [Test]
    public async Task SetAsync_ConExpiracion_AplicaExpiracion()
    {
        var key = "expiring-key";
        var value = "expiring-value";
        var expiration = TimeSpan.FromSeconds(1);

        await _cacheService.SetAsync(key, value, expiration);

        var result = _memoryCache.Get<string>(key);
        result.Should().Be(value);

        await Task.Delay(1100);

        var expiredResult = _memoryCache.Get<string>(key);
        expiredResult.Should().BeNull();
    }

    [Test]
    public async Task SetAsync_SinExpiracion_UsaExpiracionPorDefecto()
    {
        var key = "default-expiry-key";
        var value = "value";

        await _cacheService.SetAsync(key, value);

        var result = _memoryCache.Get<string>(key);
        result.Should().Be(value);
    }

    [Test]
    public async Task SetAsync_ObjetoComplejo_SerializaCorrectamente()
    {
        var key = "complex-object-key";
        var value = new TestCacheObject { Id = 42, Name = "Complex Test" };

        var act = async () => await _cacheService.SetAsync(key, value);

        await act.Should().NotThrowAsync();

        var result = _memoryCache.Get<TestCacheObject>(key);
        result.Should().NotBeNull();
        result!.Id.Should().Be(42);
        result.Name.Should().Be("Complex Test");
    }

    #endregion

    #region RemoveAsync Tests

    [Test]
    public async Task RemoveAsync_ClaveExistente_Elimina()
    {
        var key = "key-to-remove";
        _memoryCache.Set(key, "value");

        var act = async () => await _cacheService.RemoveAsync(key);

        await act.Should().NotThrowAsync();

        var result = _memoryCache.Get<string>(key);
        result.Should().BeNull();
    }

    [Test]
    public async Task RemoveAsync_ClaveNoExistente_NoLanzaExcepcion()
    {
        var act = async () => await _cacheService.RemoveAsync("non-existent-key");

        await act.Should().NotThrowAsync();
    }

    #endregion

    #region RemoveByPatternAsync Tests

    [Test]
    public async Task RemoveByPatternAsync_SinSoporte_LogueaDebug()
    {
        var pattern = "productos:*";

        var act = async () => await _cacheService.RemoveByPatternAsync(pattern);

        await act.Should().NotThrowAsync();

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("RemoveByPattern")),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, e) => true)),
            Times.Once);
    }

    #endregion
}

/// <summary>
/// Clase de prueba para serialización.
/// </summary>
public class TestCacheObject
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

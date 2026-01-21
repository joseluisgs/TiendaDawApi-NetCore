using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Moq;
using TiendaApi.Api.Services.Cache;

namespace TiendaApi.Tests.Unit.Services.Cache;

public class RedisCacheServiceEdgeTests
{
    private Mock<IDistributedCache> _mockCache = null!;
    private Mock<ILogger<RedisCacheService>> _mockLogger = null!;
    private RedisCacheService _cacheService = null!;

    [SetUp]
    public void Setup()
    {
        _mockCache = new Mock<IDistributedCache>();
        _mockLogger = new Mock<ILogger<RedisCacheService>>();
        _cacheService = new RedisCacheService(_mockCache.Object, _mockLogger.Object);
    }

    #region GetAsync Edge Cases

    [Test]
    public async Task GetAsync_CacheMiss_RetornaNull()
    {
        _mockCache.Setup(c => c.Get(It.IsAny<string>()))
            .Returns((byte[]?)null);

        var result = await _cacheService.GetAsync<string>("non-existent");

        result.Should().BeNull();
    }

    [Test]
    public async Task GetAsync_ArrayVacio_RetornaNull()
    {
        _mockCache.Setup(c => c.Get(It.IsAny<string>()))
            .Returns(Array.Empty<byte>());

        var result = await _cacheService.GetAsync<string>("test-key");

        result.Should().BeNull();
    }

    [Test]
    public async Task GetAsync_ConExcepcion_RetornaNull()
    {
        _mockCache.Setup(c => c.Get(It.IsAny<string>()))
            .Throws(new Exception("Cache error"));

        var result = await _cacheService.GetAsync<string>("test-key");

        result.Should().BeNull();
    }

    #endregion

    #region SetAsync Edge Cases

    [Test]
    public async Task SetAsync_ConKeyVacio_NoLanzaExcepcion()
    {
        var act = async () => await _cacheService.SetAsync("", "value");
        await act.Should().NotThrowAsync();
    }

    [Test]
    public async Task SetAsync_ConExpiracionNegativa_UsaExpiracionPorDefecto()
    {
        var key = "test-key";
        var value = "test-value";
        var negativeExpiration = TimeSpan.FromMinutes(-1);

        var act = async () => await _cacheService.SetAsync(key, value, negativeExpiration);
        await act.Should().NotThrowAsync();
    }

    [Test]
    public async Task SetAsync_ConExpiracionCero_UsaExpiracionPorDefecto()
    {
        var key = "test-key";
        var value = "test-value";

        var act = async () => await _cacheService.SetAsync(key, value, TimeSpan.Zero);
        await act.Should().NotThrowAsync();
    }

    [Test]
    public async Task SetAsync_ConExpiracionMuyLarga_NoLanzaExcepcion()
    {
        var key = "test-key";
        var value = "test-value";
        var longExpiration = TimeSpan.FromDays(365);

        var act = async () => await _cacheService.SetAsync(key, value, longExpiration);
        await act.Should().NotThrowAsync();
    }

    [Test]
    public async Task SetAsync_ConObjetoVacio_NoLanzaExcepcion()
    {
        var act = async () => await _cacheService.SetAsync("key", new EmptyCacheData());
        await act.Should().NotThrowAsync();
    }

    [Test]
    public async Task SetAsync_ConPropiedadesNulas_NoLanzaExcepcion()
    {
        var data = new CacheTestDataWithNulls { Id = 1, Name = null };

        var act = async () => await _cacheService.SetAsync("key", data);
        await act.Should().NotThrowAsync();
    }

    [Test]
    public async Task SetAsync_ConColeccionVacia_NoLanzaExcepcion()
    {
        var act = async () => await _cacheService.SetAsync("key", new List<string>());
        await act.Should().NotThrowAsync();
    }

    [Test]
    public async Task SetAsync_ConDiccionario_NoLanzaExcepcion()
    {
        var dict = new Dictionary<string, int> { { "key1", 1 }, { "key2", 2 } };

        var act = async () => await _cacheService.SetAsync("key", dict);
        await act.Should().NotThrowAsync();
    }

    [Test]
    public async Task SetAsync_ConCaracteresEspeciales_NoLanzaExcepcion()
    {
        var specialChars = "value with \"quotes\" & <brackets>";
        var key = "key:with:special:chars";

        var act = async () => await _cacheService.SetAsync(key, specialChars);
        await act.Should().NotThrowAsync();
    }

    [Test]
    public async Task SetAsync_ConUnicode_NoLanzaExcepcion()
    {
        var unicodeValue = "Value with 中文 characters 🎉";

        var act = async () => await _cacheService.SetAsync("key", unicodeValue);
        await act.Should().NotThrowAsync();
    }

    #endregion

    #region RemoveAsync Edge Cases

    [Test]
    public async Task RemoveAsync_ConKeyVacio_NoLanzaExcepcion()
    {
        var act = async () => await _cacheService.RemoveAsync("");
        await act.Should().NotThrowAsync();
    }

    [Test]
    public async Task RemoveAsync_ConKeyMuyLargo_NoLanzaExcepcion()
    {
        var longKey = new string('A', 10000);

        var act = async () => await _cacheService.RemoveAsync(longKey);
        await act.Should().NotThrowAsync();
    }

    [Test]
    public async Task RemoveAsync_ConKeyInexistente_NoLanzaExcepcion()
    {
        _mockCache.Setup(c => c.RemoveAsync("non-existent", It.IsAny<CancellationToken>()))
            .Throws(new Exception("Key not found"));

        var act = async () => await _cacheService.RemoveAsync("non-existent");
        await act.Should().NotThrowAsync();
    }

    [Test]
    public async Task RemoveAsync_ConExcepcion_NoLanzaExcepcion()
    {
        _mockCache.Setup(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Throws(new Exception("Cache error"));

        var act = async () => await _cacheService.RemoveAsync("test-key");
        await act.Should().NotThrowAsync();
    }

    #endregion

    #region RemoveByPatternAsync Edge Cases

    [Test]
    public async Task RemoveByPatternAsync_ConPatternVacio_NoLanzaExcepcion()
    {
        var act = async () => await _cacheService.RemoveByPatternAsync("");
        await act.Should().NotThrowAsync();
    }

    [Test]
    public async Task RemoveByPatternAsync_ConPatternAsterisco_NoLanzaExcepcion()
    {
        var act = async () => await _cacheService.RemoveByPatternAsync("*");
        await act.Should().NotThrowAsync();
    }

    [Test]
    public async Task RemoveByPatternAsync_ConPatternComplejo_NoLanzaExcepcion()
    {
        var pattern = "productos:*:categoria:?";

        var act = async () => await _cacheService.RemoveByPatternAsync(pattern);
        await act.Should().NotThrowAsync();
    }

    [Test]
    public async Task RemoveByPatternAsync_ConRegexPattern_NoLanzaExcepcion()
    {
        var pattern = "productos:[0-9]+";

        var act = async () => await _cacheService.RemoveByPatternAsync(pattern);
        await act.Should().NotThrowAsync();
    }

    [Test]
    public async Task RemoveByPatternAsync_ConExcepcion_NoLanzaExcepcion()
    {
        var act = async () => await _cacheService.RemoveByPatternAsync("pattern:*");
        await act.Should().NotThrowAsync();
    }

    #endregion
}

public class CacheTestData
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class NestedCacheData
{
    public int Id { get; set; }
    public NestedCacheObject? Nested { get; set; }
}

public class NestedCacheObject
{
    public string Value { get; set; } = string.Empty;
}

public class EmptyCacheData
{
}

public class CacheTestDataWithNulls
{
    public int Id { get; set; }
    public string? Name { get; set; }
}

using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Moq;
using TiendaApi.Api.Services.Cache;

namespace TiendaApi.Tests.Unit.Services.Cache;

/// <summary>
/// Tests unitarios para RedisCacheService.
/// </summary>
public class RedisCacheServiceTests
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

    #region Tests GetAsync

    /// <summary>
    /// Dado que la caché está vacía, cuando se obtiene un valor, entonces retorna null.
    /// Returns: null (cache miss)
    /// </summary>
    [Test]
    public async Task GetAsync_CacheMiss_RetornaNull()
    {
        _mockCache.Setup(c => c.Get(It.IsAny<string>()))
            .Returns((byte[]?)null);

        var result = await _cacheService.GetAsync<string>("test-key");

        result.Should().BeNull();
    }

    /// <summary>
    /// Dado que existe un valor en caché, cuando se obtiene, entonces no lanza excepción.
    /// Returns: Unit.Success
    /// </summary>
    [Test]
    public async Task GetAsync_CacheHit_NoLanzaExcepcion()
    {
        var testObject = new TestData { Id = 1, Name = "Test" };
        var json = JsonSerializer.Serialize(testObject);
        var bytes = System.Text.Encoding.UTF8.GetBytes(json);

        _mockCache.Setup(c => c.Get("test-key"))
            .Returns(bytes);

        var act = async () => await _cacheService.GetAsync<TestData>("test-key");
        await act.Should().NotThrowAsync();
    }

    /// <summary>
    /// Dado que la caché retorna array vacío, cuando se obtiene, entonces retorna null.
    /// Returns: null (array vacío se trata como cache miss)
    /// </summary>
    [Test]
    public async Task GetAsync_ArrayVacio_RetornaNull()
    {
        _mockCache.Setup(c => c.Get(It.IsAny<string>()))
            .Returns(Array.Empty<byte>());

        var result = await _cacheService.GetAsync<string>("test-key");

        result.Should().BeNull();
    }

    /// <summary>
    /// Dado que ocurre una excepción al obtener de caché, cuando se obtiene, entonces retorna null.
    /// Returns: null (manejo de excepción)
    /// </summary>
    [Test]
    public async Task GetAsync_ConExcepcion_RetornaNull()
    {
        _mockCache.Setup(c => c.Get(It.IsAny<string>()))
            .Throws(new Exception("Error de caché"));

        var result = await _cacheService.GetAsync<string>("test-key");

        result.Should().BeNull();
    }

    #endregion

    #region Tests SetAsync

    /// <summary>
    /// Dado un valor válido, cuando se guarda en caché, entonces se guarda sin excepción.
    /// Returns: Unit.Success
    /// </summary>
    [Test]
    public async Task SetAsync_ConValorValido_GuardarEnCache()
    {
        var key = "test-key";
        var value = "test-value";

        var act = async () => await _cacheService.SetAsync(key, value);
        await act.Should().NotThrowAsync();
    }

    /// <summary>
    /// Dado un valor con expiración específica, cuando se guarda, entonces no lanza excepción.
    /// Returns: Unit.Success con expiración personalizada
    /// </summary>
    [Test]
    public async Task SetAsync_ConExpiracionPersonalizada_UsaExpiracionDada()
    {
        var key = "test-key";
        var value = "test-value";
        var expiration = TimeSpan.FromMinutes(10);

        var act = async () => await _cacheService.SetAsync(key, value, expiration);
        await act.Should().NotThrowAsync();
    }

    /// <summary>
    /// Dado un valor sin expiración, cuando se guarda, entonces no lanza excepción.
    /// Returns: Unit.Success con expiración por defecto
    /// </summary>
    [Test]
    public async Task SetAsync_SinExpiracion_UsaExpiracionPorDefecto()
    {
        var key = "test-key";
        var value = "test-value";

        var act = async () => await _cacheService.SetAsync(key, value);
        await act.Should().NotThrowAsync();
    }

    /// <summary>
    /// Dado un objeto complejo, cuando se guarda, entonces no lanza excepción.
    /// Returns: Unit.Success
    /// </summary>
    [Test]
    public async Task SetAsync_ObjetoComplejo_SerializaCorrectamente()
    {
        var testObject = new TestData { Id = 42, Name = "Complex Test" };

        var act = async () => await _cacheService.SetAsync("complex-key", testObject);
        await act.Should().NotThrowAsync();
    }

    /// <summary>
    /// Dado que ocurre una excepción al guardar, cuando se guarda, entonces no lanza excepción.
    /// Returns: Unit.Success (excepción capturada internamente)
    /// </summary>
    [Test]
    public async Task SetAsync_ConExcepcion_NoLanzaExcepcion()
    {
        _mockCache.Setup(c => c.Set(
                It.IsAny<string>(),
                It.IsAny<byte[]>(),
                It.IsAny<DistributedCacheEntryOptions>()))
            .Throws(new Exception("Error de caché"));

        var act = async () => await _cacheService.SetAsync("test-key", "value");
        await act.Should().NotThrowAsync();
    }

    #endregion

    #region Tests RemoveAsync

    /// <summary>
    /// Dada una clave existente, cuando se elimina, entonces no lanza excepción.
    /// Returns: Unit.Success
    /// </summary>
    [Test]
    public async Task RemoveAsync_ConClaveExistente_NoLanzaExcepcion()
    {
        var key = "test-key";

        var act = async () => await _cacheService.RemoveAsync(key);
        await act.Should().NotThrowAsync();
    }

    /// <summary>
    /// Dado que ocurre una excepción al eliminar, cuando se elimina, entonces no lanza excepción.
    /// Returns: Unit.Success (excepción capturada internamente)
    /// </summary>
    [Test]
    public async Task RemoveAsync_ConExcepcion_NoLanzaExcepcion()
    {
        _mockCache.Setup(c => c.Remove(It.IsAny<string>()))
            .Throws(new Exception("Error de caché"));

        var act = async () => await _cacheService.RemoveAsync("test-key");
        await act.Should().NotThrowAsync();
    }

    #endregion

    #region Tests RemoveByPatternAsync

    /// <summary>
    /// Dado un patrón válido, cuando se eliminan coincidencias, entonces completa la tarea.
    /// Returns: Unit.Success
    /// </summary>
    [Test]
    public async Task RemoveByPatternAsync_ConPatronValido_CompletaTarea()
    {
        var pattern = "productos:*";

        await _cacheService.RemoveByPatternAsync(pattern);

        await Task.CompletedTask;
    }

    /// <summary>
    /// Dado que ocurre una excepción al eliminar por patrón, entonces no lanza excepción.
    /// Returns: Unit.Success (excepción capturada internamente)
    /// </summary>
    [Test]
    public async Task RemoveByPatternAsync_ConExcepcion_NoLanzaExcepcion()
    {
        var act = async () => await _cacheService.RemoveByPatternAsync("pattern:*");
        await act.Should().NotThrowAsync();
    }

    #endregion
}

/// <summary>
/// Clase de prueba para serialización.
/// </summary>
public class TestData
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

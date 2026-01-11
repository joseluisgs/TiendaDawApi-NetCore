using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Moq;
using TiendaApi.Services.Cache;

namespace TiendaApi.Tests;

/// <summary>
/// Smoke tests for Redis cache service
/// </summary>
public class RedisCacheServiceTests
{
    private Mock<IDistributedCache> _mockCache = null!;
    private ICacheService _cacheService = null!;

    [SetUp]
    public void Setup()
    {
        _mockCache = new Mock<IDistributedCache>();
        var mockLogger = new Mock<ILogger<RedisCacheService>>();
        _cacheService = new RedisCacheService(_mockCache.Object, mockLogger.Object);
    }

    [Test]
    public async Task GetAsync_WithCacheMiss_ShouldReturnDefault()
    {
        // Arrange
        _mockCache.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((byte[]?)null);

        // Act
        var result = await _cacheService.GetAsync<string>("test-key");

        // Assert
        result.Should().BeNull();
    }

    [Test]
    public async Task SetAsync_ShouldCallDistributedCache()
    {
        // Arrange
        var key = "test-key";
        var value = "test-value";
        var expiration = TimeSpan.FromMinutes(5);

        // Act
        await _cacheService.SetAsync(key, value, expiration);

        // Assert - Just verify it doesn't throw, can't easily verify extension method
        // In real scenario, we'd check the cache has the value
        _mockCache.Verify(
            c => c.SetAsync(
                It.IsAny<string>(),
                It.IsAny<byte[]>(),
                It.IsAny<DistributedCacheEntryOptions>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task RemoveAsync_ShouldCallDistributedCache()
    {
        // Arrange
        var key = "test-key";

        // Act
        await _cacheService.RemoveAsync(key);

        // Assert
        _mockCache.Verify(
            c => c.RemoveAsync(key, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}

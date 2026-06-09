using System.Text;
using System.Text.Json;
using BackendAssignment.Infrastructure.ExternalServices.Redis;
using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Moq;

namespace BackendAssignment.Tests.Infrastructure.ExternalServices;

public class RedisCacheServiceTests
{
    private readonly Mock<IDistributedCache> _cacheMock = new();
    private readonly Mock<ILogger<RedisCacheService>> _loggerMock = new();
    private readonly RedisCacheService _service;

    public RedisCacheServiceTests()
    {
        // Inject mock dependencies into RedisCacheService
        _service = new RedisCacheService(_cacheMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task GetAsync_ShouldReturnDeserializedObject_WhenCacheHit()
    {
        // Should return deserialized object when value exists in cache
        var key = "book:123";
        var expected = new TestBook { Title = "C# 101", Pages = 321 };
        var json = JsonSerializer.Serialize(expected);
        var bytes = Encoding.UTF8.GetBytes(json);

        _cacheMock.Setup(c => c.GetAsync(key, default)).ReturnsAsync(bytes);

        var result = await _service.GetAsync<TestBook>(key);

        result.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task GetAsync_ShouldReturnDefault_WhenCacheMiss()
    {
        // Should return null when key is not in cache
        var key = "missing-key";
        _cacheMock.Setup(c => c.GetAsync(key, default)).ReturnsAsync((byte[]?)null);

        var result = await _service.GetAsync<TestBook>(key);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAsync_ShouldReturnDefault_WhenDeserializationFails()
    {
        // Should return null when cached value is not valid JSON
        var key = "bad-json";
        var badBytes = Encoding.UTF8.GetBytes("not-json");

        _cacheMock.Setup(c => c.GetAsync(key, default)).ReturnsAsync(badBytes);

        var result = await _service.GetAsync<TestBook>(key);

        result.Should().BeNull();
    }

    [Fact]
    public async Task SetAsync_ShouldSerializeAndStoreObject()
    {
        // Should serialize object and call SetAsync on cache
        var key = "book:save";
        var book = new TestBook { Title = "Unit Testing", Pages = 101 };

        await _service.SetAsync(key, book);

        _cacheMock.Verify(c =>
            c.SetAsync(
                key,
                It.Is<byte[]>(bytes => Encoding.UTF8.GetString(bytes).Contains("Unit Testing")),
                It.IsAny<DistributedCacheEntryOptions>(),
                default),
            Times.Once);
    }

    [Fact]
    public async Task RemoveAsync_ShouldCallRemoveFromCache()
    {
        // Should call RemoveAsync on cache with the given key
        var key = "book:delete";

        await _service.RemoveAsync(key);

        _cacheMock.Verify(c => c.RemoveAsync(key, default), Times.Once);
    }

    [Fact]
    public async Task SetAsync_ShouldHandleException_AndNotThrow()
    {
        // Should silently handle exceptions thrown by cache on SetAsync
        var key = "book:error";
        var book = new TestBook { Title = "Oops", Pages = 1 };

        _cacheMock
            .Setup(c => c.SetAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>(), default))
            .ThrowsAsync(new Exception("Redis write error"));

        var act = () => _service.SetAsync(key, book);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task RemoveAsync_ShouldHandleException_AndNotThrow()
    {
        // Should silently handle exceptions thrown by cache on RemoveAsync
        var key = "book:fail-remove";

        _cacheMock
            .Setup(c => c.RemoveAsync(key, default))
            .ThrowsAsync(new Exception("Redis remove error"));

        var act = () => _service.RemoveAsync(key);

        await act.Should().NotThrowAsync();
    }

    // A simple POCO for testing purposes
    private class TestBook
    {
        public string Title { get; set; } = default!;
        public int Pages { get; set; }
    }
}

using System.Text;
using System.Text.Json;
using BackendAssignment.Application.Interfaces.Services;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace BackendAssignment.Infrastructure.ExternalServices.Redis;

/// <summary>
/// Redis-based implementation of <see cref="ICacheService"/> using <see cref="IDistributedCache"/>.
/// Provides caching functionality with expiration policies and logging.
/// </summary>
/// <remarks>
/// Design Decision:
/// - This service abstracts Redis and centralizes caching logic.
/// - Uses both absolute and sliding expiration to balance performance and freshness.
/// </remarks>
public class RedisCacheService(IDistributedCache cache, ILogger<RedisCacheService> logger) : ICacheService
{
    // Cache policy: 30-minute absolute expiration, 10-minute sliding expiration.
    private static readonly DistributedCacheEntryOptions CacheOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30),
        SlidingExpiration = TimeSpan.FromMinutes(10)
    };

    /// <summary>
    /// Retrieves a cached value by key and deserializes it to the specified type.
    /// </summary>
    /// <typeparam name="T">The type of the cached object</typeparam>
    /// <param name="key">The cache key</param>
    /// <returns>The cached object, or default if not found or an error occurs</returns>
    public async Task<T?> GetAsync<T>(string key)
    {
        try
        {
            var bytes = await cache.GetAsync(key);
            if (bytes is null)
            {
                logger.LogInformation("Cache miss for key: {CacheKey}", key);
                return default;
            }

            // Convert cached byte array back to JSON and then deserialize to T
            var json = Encoding.UTF8.GetString(bytes);
            logger.LogInformation("Cache hit for key: {CacheKey}", key);
            return JsonSerializer.Deserialize<T>(json);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get value from cache for key: {CacheKey}", key);
            return default;
        }
    }

    /// <summary>
    /// Serializes and stores a value in cache with a specified key and expiration options.
    /// </summary>
    /// <typeparam name="T">The type of the value to cache</typeparam>
    /// <param name="key">The cache key</param>
    /// <param name="value">The value to store</param>
    public async Task SetAsync<T>(string key, T value)
    {
        try
        {
            var json = JsonSerializer.Serialize(value);
            var bytes = Encoding.UTF8.GetBytes(json);

            await cache.SetAsync(key, bytes, CacheOptions);
            logger.LogInformation("Cached value set for key: {CacheKey}", key);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to set value in cache for key: {CacheKey}", key);
        }
    }

    /// <summary>
    /// Removes a cache entry based on the given key.
    /// </summary>
    /// <param name="key">The cache key to remove</param>
    public async Task RemoveAsync(string key)
    {
        try
        {
            await cache.RemoveAsync(key);
            logger.LogInformation("Removed cache entry for key: {CacheKey}", key);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to remove cache key: {CacheKey}", key);
        }
    }
}

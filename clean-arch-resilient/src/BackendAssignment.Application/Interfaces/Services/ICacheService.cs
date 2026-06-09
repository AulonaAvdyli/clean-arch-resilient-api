namespace BackendAssignment.Application.Interfaces.Services;

/// <summary>
/// Provides basic operations for interacting with the distributed cache (Redis).
/// </summary>
public interface ICacheService
{
    /// <summary>
    /// Retrieves a value from the cache by key.
    /// </summary>
    Task<T?> GetAsync<T>(string key);

    /// <summary>
    /// Stores a value in the cache with the specified key.
    /// </summary>
    Task SetAsync<T>(string key, T value);

    /// <summary>
    /// Removes a value from the cache by key.
    /// </summary>
    Task RemoveAsync(string key);
}
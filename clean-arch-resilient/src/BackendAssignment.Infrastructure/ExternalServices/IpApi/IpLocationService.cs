using BackendAssignment.Application.Interfaces.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BackendAssignment.Infrastructure.ExternalServices.IpApi;

/// <summary>
/// Service for resolving the country code of a given IP address using ipapi.co.
/// Includes caching to minimize external API calls and improve performance.
/// </summary>
/// <remarks>
/// Design Decision:
/// - Uses in-app caching via ICacheService to reduce external API usage and latency.
/// - Handles local IPs separately to simulate access from Greece during development.
/// </remarks>
public class IpLocationService : IIpLocationService
{
    private readonly HttpClient _httpClient;
    private readonly ICacheService _cache;
    private readonly ILogger<IpLocationService> _logger;

    /// <summary>
    /// Constructs the IpLocationService with required dependencies.
    /// </summary>
    public IpLocationService(HttpClient httpClient, ICacheService cache, ILogger<IpLocationService> logger)
    {
        _httpClient = httpClient;
        _cache = cache;
        _logger = logger;
    }

    /// <summary>
    /// Returns the ISO country code (e.g., "GR") for a given IP address.
    /// </summary>
    /// <param name="ip">IP address to resolve</param>
    /// <returns>Two-letter country code or null if unavailable</returns>
    public async Task<string?> GetCountryCodeAsync(string ip)
    {
        var cacheKey = $"ip:{ip}";
        var cachedCountry = await _cache.GetAsync<string>(cacheKey);

        if (cachedCountry is not null)
        {
            _logger.LogInformation("Cache hit for IP {Ip}: Country = {Country}", ip, cachedCountry);
            return cachedCountry;
        }

        // Simulate country for local IPs
        if (string.IsNullOrWhiteSpace(ip) || ip == "::1" || ip == "127.0.0.1")
        {
            _logger.LogInformation("Local IP detected: {Ip}. Assuming access from Greece for local testing.", ip);
            return "GR"; // simulate Greece
        }

        try
        {
            var url = $"https://ipapi.co/{ip}/country/";
            var countryCode = (await _httpClient.GetStringAsync(url)).Trim();

            if (string.IsNullOrWhiteSpace(countryCode))
            {
                _logger.LogWarning("API call returned empty country code for IP: {Ip}", ip);
                return null;
            }

            _logger.LogInformation("Fetched country code '{Country}' for IP {Ip}", countryCode, ip);
            await _cache.SetAsync(cacheKey, countryCode); // Cache the result for future calls

            return countryCode;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to get country code for IP: {Ip}", ip);
            return null;
        }
    }
}

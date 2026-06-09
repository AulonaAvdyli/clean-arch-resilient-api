namespace BackendAssignment.Application.Interfaces.Services;

/// <summary>
/// Service to retrieve country code based on an IP address.
/// </summary>
public interface IIpLocationService
{
    /// <summary>
    /// Resolves the country code for the given IP address.
    /// </summary>
    Task<string?> GetCountryCodeAsync(string ipAddress);
}
namespace BackendAssignment.Application.Interfaces.Services;

/// <summary>
/// Interface Service for authentication and token generation.
/// </summary>
public interface IAuthService
{
    Task<string> GenerateTokenAsync(string username, string password);
}
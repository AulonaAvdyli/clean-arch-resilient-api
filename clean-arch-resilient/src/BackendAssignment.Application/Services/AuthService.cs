using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BackendAssignment.Application.Interfaces.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace BackendAssignment.Application.Services;

/// <summary>
/// Service responsible for generating JWT tokens for valid users.
/// </summary>
public class AuthService : IAuthService
{
    private readonly IConfiguration _config;

    public AuthService(IConfiguration config)
    {
        _config = config;
    }

    /// <summary>
    /// Generates a JWT token if the user credentials are valid.
    /// </summary>
    /// <param name="username">Username to authenticate</param>
    /// <param name="password">Password to authenticate</param>
    /// <returns>JWT token string</returns>
    /// <exception cref="UnauthorizedAccessException">Thrown if credentials are invalid</exception>
    public Task<string> GenerateTokenAsync(string username, string password)
    {
        var user = GetValidUser(username, password);
        if (user == null)
            throw new UnauthorizedAccessException("Invalid credentials");

        // Prepare claims for the token
        var claims = new List<Claim>
        {
            new("name", user.Value.Username),
            new("role", user.Value.Role)
        };

        // Build the JWT security token
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["JwtSettings:Secret"]));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            _config["JwtSettings:Issuer"],
            _config["JwtSettings:Audience"],
            claims,
            expires: DateTime.UtcNow.AddMinutes(120),
            signingCredentials: creds
        );

        return Task.FromResult(new JwtSecurityTokenHandler().WriteToken(token));
    }

    /// <summary>
    /// Simulates user validation for local testing.
    /// </summary>
    /// <remarks>
    /// Design Note: This method provides static user-role pairs for local development.
    /// </remarks>
    private (string Username, string Role)? GetValidUser(string username, string password)
    {
        if (username == "dev" && password == "dev")
            return ("dev", "Developer");

        if (username == "user" && password == "user")
            return ("user", "User");

        return null;
    }
}

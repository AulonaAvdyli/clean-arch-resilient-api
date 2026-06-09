using System.IdentityModel.Tokens.Jwt;
using BackendAssignment.Application.Interfaces.Services;
using BackendAssignment.Application.Services;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;

namespace BackendAssignment.Tests.Application.Services;

public class AuthServiceTests
{
    private readonly Mock<IConfiguration> _configMock;
    private readonly IAuthService _authService;

    public AuthServiceTests()
    {
        _configMock = new Mock<IConfiguration>();

        // Setup mocked configuration for JWT values
        _configMock.Setup(c => c["JwtSettings:Secret"]).Returns("supersecretkey12345678901234567890");
        _configMock.Setup(c => c["JwtSettings:Issuer"]).Returns("test-issuer");
        _configMock.Setup(c => c["JwtSettings:Audience"]).Returns("test-audience");

        // Inject the mock config into AuthService
        _authService = new AuthService(_configMock.Object);
    }

    [Theory]
    [InlineData("dev", "dev")]
    [InlineData("user", "user")]
    public async Task GenerateTokenAsync_ShouldReturnToken_WhenCredentialsAreValid(string username, string password)
    {
        // Act: Generate token using valid credentials
        var token = await _authService.GenerateTokenAsync(username, password);

        // Assert: Ensure token is returned and readable
        token.Should().NotBeNullOrEmpty();

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);

        // Check issuer and audience in token
        jwt.Issuer.Should().Be("test-issuer");
        jwt.Audiences.Should().Contain("test-audience");

        // Check if 'name' claim matches the username
        var nameClaim = jwt.Claims.FirstOrDefault(c => c.Type == "name")?.Value;
        nameClaim.Should().Be(username);
    }

    [Fact]
    public async Task GenerateTokenAsync_ShouldThrowUnauthorized_WhenCredentialsAreInvalid()
    {
        // Act: Try with invalid credentials
        var act = async () => await _authService.GenerateTokenAsync("wrong", "wrong");

        // Assert: Expect UnauthorizedAccessException with specific message
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Invalid credentials");
    }
}


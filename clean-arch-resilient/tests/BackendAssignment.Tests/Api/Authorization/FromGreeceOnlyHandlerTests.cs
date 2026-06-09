using System.Net;
using System.Security.Claims;
using BackendAssignment.Api.Authorization;
using BackendAssignment.Application.Interfaces.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Moq;

namespace BackendAssignment.Tests.Api.Authorization;

public class FromGreeceOnlyHandlerTests
{
    private readonly Mock<IIpLocationService> _ipServiceMock = new();
    private readonly DefaultHttpContext _httpContext;
    private readonly AuthorizationHandlerContext _authContext;
    private readonly FromGreeceOnlyHandler _handler;

    public FromGreeceOnlyHandlerTests()
    {
        // Mock the context accessor to simulate the current request
        var contextAccessor = new Mock<IHttpContextAccessor>();
        _httpContext = new DefaultHttpContext();
        contextAccessor.Setup(c => c.HttpContext).Returns(_httpContext);

        // Build a fake authorization context for testing
        _authContext = new AuthorizationHandlerContext(
            new[] { new FromGreeceOnlyRequirement() },
            new ClaimsPrincipal(),
            null);

        // Create the handler with mocked dependencies
        _handler = new FromGreeceOnlyHandler(contextAccessor.Object, _ipServiceMock.Object);
    }

    [Fact]
    public async Task ShouldFail_WhenIpIsMissing()
    {
        // Arrange: Simulate missing IP address
        _httpContext.Connection.RemoteIpAddress = null;

        // Act
        await _handler.HandleAsync(_authContext);

        // Assert: Authorization should fail and reason should be recorded
        _authContext.HasSucceeded.Should().BeFalse();
        _httpContext.Items["AuthorizationFailureReason"].Should().Be("IP address not available.");
    }

    [Fact]
    public async Task ShouldFail_WhenCountryCodeIsNull()
    {
        // Arrange: Simulate valid IP but null country from location service
        _httpContext.Connection.RemoteIpAddress = IPAddress.Parse("1.2.3.4");
        _ipServiceMock.Setup(s => s.GetCountryCodeAsync("1.2.3.4")).ReturnsAsync((string?)null);

        // Act
        await _handler.HandleAsync(_authContext);

        // Assert: Authorization should fail with correct reason
        _authContext.HasSucceeded.Should().BeFalse();
        _httpContext.Items["AuthorizationFailureReason"]
            .Should().Be("Access denied: this operation is only allowed for users located in Greece.");
    }

    [Fact]
    public async Task ShouldFail_WhenCountryCodeIsNotGR()
    {
        // Arrange: Simulate IP resolving to a non-GR country (e.g., US)
        _httpContext.Connection.RemoteIpAddress = IPAddress.Parse("1.2.3.4");
        _ipServiceMock.Setup(s => s.GetCountryCodeAsync("1.2.3.4")).ReturnsAsync("US");

        // Act
        await _handler.HandleAsync(_authContext);

        // Assert: Still forbidden because country is not Greece
        _authContext.HasSucceeded.Should().BeFalse();
        _httpContext.Items["AuthorizationFailureReason"]
            .Should().Be("Access denied: this operation is only allowed for users located in Greece.");
    }

    [Fact]
    public async Task ShouldSucceed_WhenCountryCodeIsGR()
    {
        // Arrange: Simulate IP resolving to Greece
        _httpContext.Connection.RemoteIpAddress = IPAddress.Parse("1.2.3.4");
        _ipServiceMock.Setup(s => s.GetCountryCodeAsync("1.2.3.4")).ReturnsAsync("GR");

        // Act
        await _handler.HandleAsync(_authContext);

        // Assert: Authorization should pass, no failure reason expected
        _authContext.HasSucceeded.Should().BeTrue();
        _httpContext.Items.Should().NotContainKey("AuthorizationFailureReason");
    }
}

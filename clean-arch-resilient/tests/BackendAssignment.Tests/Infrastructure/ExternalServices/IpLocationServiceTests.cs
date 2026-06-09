using System.Net;
using BackendAssignment.Application.Interfaces.Services;
using BackendAssignment.Infrastructure.ExternalServices.IpApi;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;

namespace BackendAssignment.Tests.Infrastructure.ExternalServices;


public class IpLocationServiceTests
{
    private readonly Mock<ICacheService> _cacheMock = new();
    private readonly Mock<ILogger<IpLocationService>> _loggerMock = new();

    // Helper to create a fake HttpClient that returns a predefined response
    private HttpClient CreateHttpClient(string response)
    {
        var handlerMock = new Mock<HttpMessageHandler>();

        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(response)
            });

        return new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri("https://ipapi.co/")
        };
    }

    [Fact]
    public async Task GetCountryCodeAsync_ShouldReturnCachedValue_WhenExists()
    {
        // Arrange: Should return country code directly from cache if available
        var ip = "1.2.3.4";
        _cacheMock.Setup(c => c.GetAsync<string>($"ip:{ip}")).ReturnsAsync("GR");

        var service = new IpLocationService(new HttpClient(), _cacheMock.Object, _loggerMock.Object);

        // Act
        var result = await service.GetCountryCodeAsync(ip);

        // Assert
        result.Should().Be("GR");
        _cacheMock.Verify(c => c.GetAsync<string>($"ip:{ip}"), Times.Once);
    }

    [Theory]
    [InlineData("127.0.0.1")]
    [InlineData("::1")]
    [InlineData("")]
    public async Task GetCountryCodeAsync_ShouldReturnGR_ForLocalhostAndEmpty(string ip)
    {
        // Should return "GR" for localhost or empty IPs
        var service = new IpLocationService(new HttpClient(), _cacheMock.Object, _loggerMock.Object);

        var result = await service.GetCountryCodeAsync(ip);

        result.Should().Be("GR");
    }

    [Fact]
    public async Task GetCountryCodeAsync_ShouldFetchFromApi_AndCacheResult()
    {
        // Arrange: Should fetch country code from API and cache the result
        var ip = "5.6.7.8";
        _cacheMock.Setup(c => c.GetAsync<string>($"ip:{ip}")).ReturnsAsync((string)null);

        var httpClient = CreateHttpClient("US");
        var service = new IpLocationService(httpClient, _cacheMock.Object, _loggerMock.Object);

        // Act
        var result = await service.GetCountryCodeAsync(ip);

        // Assert
        result.Should().Be("US");
        _cacheMock.Verify(c => c.SetAsync($"ip:{ip}", "US"), Times.Once);
    }

    [Fact]
    public async Task GetCountryCodeAsync_ShouldReturnNull_WhenApiReturnsEmpty()
    {
        // Should return null if API returns an empty response
        var ip = "9.9.9.9";
        _cacheMock.Setup(c => c.GetAsync<string>($"ip:{ip}")).ReturnsAsync((string)null);

        var httpClient = CreateHttpClient(""); // Empty response
        var service = new IpLocationService(httpClient, _cacheMock.Object, _loggerMock.Object);

        var result = await service.GetCountryCodeAsync(ip);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetCountryCodeAsync_ShouldReturnNull_WhenHttpClientThrows()
    {
        // Should handle HttpClient exceptions gracefully and return null
        var ip = "8.8.8.8";
        _cacheMock.Setup(c => c.GetAsync<string>($"ip:{ip}")).ReturnsAsync((string)null);

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Network error"));

        var httpClient = new HttpClient(handlerMock.Object);
        var service = new IpLocationService(httpClient, _cacheMock.Object, _loggerMock.Object);

        var result = await service.GetCountryCodeAsync(ip);

        result.Should().BeNull();
    }
}

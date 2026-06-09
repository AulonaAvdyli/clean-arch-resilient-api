using BackendAssignment.Api.Middlewares;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Polly.RateLimit;

namespace BackendAssignment.Tests.Api.Middlewares;

public class PollyRateLimitingMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_ShouldReturn429_WhenRateLimitExceeded()
    {
        // Arrange: create a fake context with response body
        var httpContext = new DefaultHttpContext();
        var responseStream = new MemoryStream();
        httpContext.Response.Body = responseStream;

        // Simulate a rate-limited pipeline by throwing RateLimitRejectedException
        var middleware = new PollyRateLimitingMiddleware(context =>
        {
            throw new RateLimitRejectedException("Rate limit exceeded.");
        });

        // Act: invoke the middleware
        await middleware.InvokeAsync(httpContext);
        responseStream.Seek(0, SeekOrigin.Begin);
        var responseText = await new StreamReader(responseStream).ReadToEndAsync();

        // Assert: should return 429 with error message
        httpContext.Response.StatusCode.Should().Be(429);
        responseText.Should().Be("Too many requests. Please try again later.");
    }

    [Fact]
    public async Task InvokeAsync_ShouldCallNext_WhenWithinLimit()
    {
        // Arrange: track whether the next middleware is executed
        var called = false;
        var context = new DefaultHttpContext();

        var middleware = new PollyRateLimitingMiddleware(_ =>
        {
            called = true;
            return Task.CompletedTask;
        });

        // Act: invoke the middleware
        await middleware.InvokeAsync(context);

        // Assert: request was allowed and next delegate was called
        called.Should().BeTrue();
    }
}
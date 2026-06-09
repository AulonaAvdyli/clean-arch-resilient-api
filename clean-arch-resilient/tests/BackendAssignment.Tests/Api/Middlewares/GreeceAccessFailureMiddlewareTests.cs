using BackendAssignment.Api.Middlewares;
using FluentAssertions;
using Microsoft.AspNetCore.Http;

namespace BackendAssignment.Tests.Api.Middlewares;

public class GreeceAccessFailureMiddlewareTests
{
    // Helper method to create middleware with a mocked request delegate
    private static GreeceAccessFailureMiddleware CreateMiddlewareWithStatus(int statusCode, bool responseStarted, bool setReason)
    {
        var middleware = new GreeceAccessFailureMiddleware(async context =>
        {
            // Simulate status code
            context.Response.StatusCode = statusCode;

            // Simulate setting a failure reason
            if (setReason)
            {
                context.Items["AuthorizationFailureReason"] = "Test reason";
            }

            // Simulate that response has already started
            if (responseStarted)
            {
                await context.Response.StartAsync();
            }
        });

        return middleware;
    }

    [Fact]
    public async Task InvokeAsync_ShouldDoNothing_WhenStatusIsNot403()
    {
        // Arrange: status is 200, reason is set
        var context = new DefaultHttpContext();
        var middleware = CreateMiddlewareWithStatus(200, false, true);

        var stream = new MemoryStream();
        context.Response.Body = stream;

        // Act
        await middleware.InvokeAsync(context);

        // Assert: nothing should be written
        stream.Length.Should().Be(0);
    }

    [Fact]
    public async Task InvokeAsync_ShouldDoNothing_WhenReasonIsNotSet()
    {
        // Arrange: status is 403, but no failure reason in context
        var context = new DefaultHttpContext();
        var middleware = CreateMiddlewareWithStatus(403, false, false);

        var stream = new MemoryStream();
        context.Response.Body = stream;

        // Act
        await middleware.InvokeAsync(context);

        // Assert: nothing should be written
        stream.Length.Should().Be(0);
    }

    [Fact]
    public async Task InvokeAsync_ShouldWriteJson_When403AndReasonAndNotStarted()
    {
        // Arrange: status is 403, reason is set, response has not started
        var context = new DefaultHttpContext();
        var middleware = CreateMiddlewareWithStatus(403, false, true);

        var stream = new MemoryStream();
        context.Response.Body = stream;

        // Act
        await middleware.InvokeAsync(context);

        // Assert: middleware should write custom error JSON
        stream.Seek(0, SeekOrigin.Begin);
        var body = new StreamReader(stream).ReadToEnd();
        body.Should().Be("{\"error\": \"Test reason\"}");

        context.Response.ContentType.Should().Be("application/json");
    }
}

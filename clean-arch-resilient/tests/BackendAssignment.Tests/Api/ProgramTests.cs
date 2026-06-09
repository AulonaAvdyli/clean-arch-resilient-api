using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace BackendAssignment.Tests.Api;

// This test ensures the ASP.NET Core app starts successfully without throwing exceptions.
// It uses WebApplicationFactory which mimics the real app hosting environment.
public class ProgramTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ProgramTests(WebApplicationFactory<Program> factory)
    {
        // Override the environment to "Testing" so we prevent development-only logic from running,
        // such as auto-opening the Swagger UI in a browser.
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
        });
    }

    [Fact]
    public void Application_Should_Start_WithoutErrors()
    {
        // Act: Attempt to create the test client, which triggers app startup
        var exception = Record.Exception(() =>
        {
            var client = _factory.CreateClient(); // This builds and starts the application
        });

        // Assert: If no exception occurred, the app started successfully
        exception.Should().BeNull();
    }
}
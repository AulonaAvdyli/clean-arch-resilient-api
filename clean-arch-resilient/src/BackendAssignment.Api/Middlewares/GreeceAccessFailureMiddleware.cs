namespace BackendAssignment.Api.Middlewares;

/// <summary>
/// Middleware that intercepts 403 Forbidden responses and replaces them with a detailed JSON error
/// if a custom authorization failure reason is available (e.g. blocked due to IP geolocation).
/// </summary>
public class GreeceAccessFailureMiddleware
{
    private readonly RequestDelegate _next;

    public GreeceAccessFailureMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Call the next middleware in the pipeline
        await _next(context);

        // If response was a 403 and a reason was set by an authorization handler
        if (context.Response.StatusCode == StatusCodes.Status403Forbidden &&
            context.Items.TryGetValue("AuthorizationFailureReason", out var reason))
        {
            // Ensure response has not started before modifying it
            if (!context.Response.HasStarted)
            {
                context.Response.ContentType = "application/json";

                // Write custom error message as JSON
                await context.Response.WriteAsync($"{{\"error\": \"{reason}\"}}");
            }
        }
    }
}
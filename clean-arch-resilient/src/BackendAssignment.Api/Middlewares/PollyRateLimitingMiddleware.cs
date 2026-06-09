using Polly;
using Polly.RateLimit;

namespace BackendAssignment.Api.Middlewares;

/// <summary>
/// Middleware that applies basic rate limiting using Polly's in-memory policy.
/// </summary>
/// <remarks>
/// Design: Using Polly for local rate limiting aligns with retry/fallback patterns used across the app,
/// and avoids bringing in heavier external libraries for development-only needs.
/// </remarks>
public class PollyRateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly AsyncRateLimitPolicy _rateLimitPolicy;

    public PollyRateLimitingMiddleware(RequestDelegate next)
    {
        _next = next;
        _rateLimitPolicy = Policy.RateLimitAsync(20, TimeSpan.FromSeconds(2));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            // Try to execute the next middleware under the rate limit constraint
            await _rateLimitPolicy.ExecuteAsync(() => _next(context));
        }
        catch (RateLimitRejectedException)
        {
            // If the rate limit is exceeded, respond with 429 (Too Many Requests)
            Console.WriteLine($"Rate limit exceeded for {context.Request.Path} at {DateTime.UtcNow}");
            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            await context.Response.WriteAsync("Too many requests. Please try again later.");
        }
    }
}
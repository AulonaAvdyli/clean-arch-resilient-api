using System.Net;
using System.Text;
using Polly;
using Polly.CircuitBreaker;
using Polly.Extensions.Http;

namespace BackendAssignment.Infrastructure.Policies;

/// <summary>
/// Contains predefined Polly policies for resilience:
/// Retry, Circuit Breaker, and Fallback.
/// </summary>
/// <remarks>
/// Design Decisions:
/// - Encapsulates all resilience logic in one place for reusability.
/// - Policies are layered to ensure graceful degradation and protection from cascading failures.
/// </remarks>
public static class PollyPolicyHelper
{
    /// <summary>
    /// Retry policy that retries transient HTTP failures (5xx and timeouts).
    /// Uses exponential backoff with jitter to avoid retry storms.
    /// </summary>
    public static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
    {
        Random jitter = new();

        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(
                retryCount: 2,
                sleepDurationProvider: retryAttempt =>
                    TimeSpan.FromSeconds(retryAttempt * 5) + TimeSpan.FromMilliseconds(jitter.Next(0, 200)),
                onRetry: (outcome, timespan, retryAttempt, context) =>
                {
                    Console.WriteLine(
                        $"[Retry] Attempt {retryAttempt} after {timespan.TotalSeconds}s due to: {outcome.Exception?.Message ?? outcome.Result.StatusCode.ToString()}");
                });
    }

    /// <summary>
    /// Circuit breaker policy that opens the circuit after 3 consecutive transient failures.
    /// While the circuit is open, all requests fail fast without reaching the external service.
    /// After a cooldown period (default 60s), the circuit goes to half-open mode to test if recovery is possible.
    /// </summary>
    /// <param name="breakDuration">Optional custom duration for how long the circuit should stay open</param>
    /// <returns>An asynchronous circuit breaker policy</returns>
    /// <remarks>
    /// Why Circuit Breaker?
    /// - Prevents overloading an already failing or throttled external service.
    /// - Improves application stability and response times under failure conditions.
    /// - Avoids cascading failures across systems.
    ///
    /// </remarks>
    public static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy(TimeSpan? breakDuration = null)
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(msg => msg.StatusCode == HttpStatusCode.TooManyRequests)
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: 3,
                durationOfBreak: breakDuration ?? TimeSpan.FromSeconds(60),
                onBreak: (outcome, timespan) =>
                {
                    Console.WriteLine(
                        $"[Circuit Breaker] Opened for {timespan.TotalSeconds}s due to: {outcome.Exception?.Message ?? outcome.Result.StatusCode.ToString()}");
                },
                onReset: () => { Console.WriteLine("[Circuit Breaker] Reset"); },
                onHalfOpen: () => { Console.WriteLine("[Circuit Breaker] Half-Open: Next call is a trial."); });
    }

    /// <summary>
    /// Fallback policy to handle situations when the circuit is open or an unexpected error occurs.
    /// Returns a generic service-unavailable response without making any external calls.
    /// </summary>
    public static IAsyncPolicy<HttpResponseMessage> GetFallbackPolicy()
    {
        return Policy<HttpResponseMessage>
            .Handle<BrokenCircuitException>()
            .Or<HttpRequestException>()
            .FallbackAsync(
                fallbackAction: (ct) =>
                {
                    Console.WriteLine("[Fallback] Circuit is open or HTTP failure. Returning fallback response.");
                    var fallbackResponse = new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
                    {
                        Content = new StringContent(
                            "{\"error\": \"External service unavailable. Please try later.\"}",
                            Encoding.UTF8,
                            "application/json")
                    };
                    return Task.FromResult(fallbackResponse);
                });
    }

    /// <summary>
    /// Combines fallback, retry, and circuit breaker policies into a single composite policy.
    /// Execution order: Fallback → Retry → Circuit Breaker.
    /// </summary>
    public static IAsyncPolicy<HttpResponseMessage> WrapResiliencePolicies()
    {
        return Policy.WrapAsync(
            GetFallbackPolicy(),      // Outer: handles final failure
            GetRetryPolicy(),         // Middle: handles retry
            GetCircuitBreakerPolicy() // Inner: guards external service
        );
    }
}

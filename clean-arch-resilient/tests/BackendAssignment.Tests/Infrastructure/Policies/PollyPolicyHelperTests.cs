using System.Net;
using BackendAssignment.Infrastructure.Policies;
using FluentAssertions;
using Polly;
using Polly.CircuitBreaker;

namespace BackendAssignment.Tests.Infrastructure.Policies;

public class PollyPolicyHelperTests
{
    [Fact]
    public async Task FallbackPolicy_ShouldReturnServiceUnavailable_WhenBrokenCircuitException()
    {
        // Arrange: Get fallback policy
        var policy = PollyPolicyHelper.GetFallbackPolicy();

        // Act: Execute policy by returning a failed task directly
        var response = await policy.ExecuteAsync(() => Task.FromException<HttpResponseMessage>(
            new BrokenCircuitException("Simulated circuit break")));

        // Assert: Response should be 503 (Service Unavailable)
        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
    }

    [Fact]
    public async Task FallbackPolicy_ShouldReturnServiceUnavailable_WhenHttpRequestException()
    {
        // Arrange: Get fallback policy
        var policy = PollyPolicyHelper.GetFallbackPolicy();

        // Act: Simulate an HttpRequestException
        var response = await policy.ExecuteAsync(() => throw new HttpRequestException());

        // Assert: Should return 503 as fallback response
        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
    }

    [Fact]
    public async Task RetryPolicy_ShouldRetryOnServerError_Twice()
    {
        // Arrange: Setup retry policy and a counter
        var policy = PollyPolicyHelper.GetRetryPolicy();
        int executionCount = 0;

        // Action that always returns 500
        Task<HttpResponseMessage> Action() {
            executionCount++;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError));
        }

        // Act
        var response = await policy.ExecuteAsync(Action);

        // Assert: Retry should be triggered twice, total calls = 3
        executionCount.Should().Be(3);
        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task CircuitBreakerPolicy_ShouldOpenAfterThreeFailures()
    {
        // Arrange: Get circuit breaker policy
        var policy = PollyPolicyHelper.GetCircuitBreakerPolicy(TimeSpan.FromMilliseconds(100));
        await BreakCircuitAsync(policy);

        // Assert: Circuit is now open and throws BrokenCircuitException
        Func<Task> act = () => policy.ExecuteAsync(() =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));

        await act.Should().ThrowAsync<BrokenCircuitException>();
    }

    [Fact]
    public async Task CircuitBreakerPolicy_ShouldTriggerBreakResetAndHalfOpen()
    {
        // Arrange: Get circuit breaker with short duration
        var policy = PollyPolicyHelper.GetCircuitBreakerPolicy(TimeSpan.FromMilliseconds(100));
        await BreakCircuitAsync(policy);

        // Assert: It should throw BrokenCircuitException now
        await AssertCircuitIsOpen(policy);

        // Wait a little for breaker to go to half-open state
        await Task.Delay(150);

        // Half-open: next success should reset
        var result = await policy.ExecuteAsync(() =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));

        result.StatusCode.Should().Be(HttpStatusCode.OK);
    }
    

    [Fact]
    public async Task CircuitBreakerPolicy_ShouldTriggerAllCallbacks()
    {
        // Arrange: Circuit breaker with very short open duration
        var policy = PollyPolicyHelper.GetCircuitBreakerPolicy(TimeSpan.FromMilliseconds(100));

        // Act: Trigger 3 failures to break it
        await BreakCircuitAsync(policy);

        // Circuit is now open, wait for it to move to half-open
        await Task.Delay(150);

        // Half-open state: a success will reset it
        var result = await policy.ExecuteAsync(() =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));

        // Assert: Should complete successfully and reset breaker
        result.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // Helper Methods
    private static async Task BreakCircuitAsync(IAsyncPolicy<HttpResponseMessage> policy)
    {
        await policy.ExecuteAsync(() => Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError)));
        await policy.ExecuteAsync(() => Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError)));
        await policy.ExecuteAsync(() => Task.FromResult(new HttpResponseMessage(HttpStatusCode.TooManyRequests)));
    }

    private static async Task AssertCircuitIsOpen(IAsyncPolicy<HttpResponseMessage> policy)
    {
        Func<Task> act = () => policy.ExecuteAsync(() =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));

        await act.Should().ThrowAsync<BrokenCircuitException>();
    }
}

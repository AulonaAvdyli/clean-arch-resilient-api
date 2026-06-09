using System.Linq.Expressions;
using Hangfire;
using BackendAssignment.Application.Interfaces.Services;

namespace BackendAssignment.Application.Services;

/// <summary>
/// Wrapper service around Hangfire's IBackgroundJobClient for decoupling background job scheduling logic.
/// </summary>
/// <remarks>
/// Design Decision:
/// This abstraction allows for easier testing and replaces direct Hangfire usage with an injectable interface,
/// improving maintainability and supporting inversion of control (IoC).
/// </remarks>
public class JobScheduler : IJobScheduler
{
    private readonly IBackgroundJobClient _client;

    /// <summary>
    /// Constructs the JobScheduler with a Hangfire background job client.
    /// </summary>
    /// <param name="client">Hangfire IBackgroundJobClient used to enqueue jobs</param>
    public JobScheduler(IBackgroundJobClient client)
    {
        _client = client;
    }

    /// <summary>
    /// Enqueues a background job using a method expression.
    /// </summary>
    /// <typeparam name="T">The type whose method is being enqueued</typeparam>
    /// <param name="methodCall">The method call expression to schedule</param>
    /// <returns>Hangfire Job ID</returns>
    public string Enqueue<T>(Expression<Action<T>> methodCall)
    {
        // Delegate scheduling to Hangfire’s client and return the job ID for tracking
        return _client.Enqueue(methodCall);
    }
}
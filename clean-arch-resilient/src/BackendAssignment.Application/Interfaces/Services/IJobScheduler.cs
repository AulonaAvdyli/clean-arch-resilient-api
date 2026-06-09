using System.Linq.Expressions;

namespace BackendAssignment.Application.Interfaces.Services;

/// <summary>
/// Background job scheduling interface using Hangfire.
/// </summary>
public interface IJobScheduler
{
    /// <summary>
    /// Enqueues a job for background execution.
    /// </summary>
    /// <typeparam name="T">The job class type.</typeparam>
    /// <param name="methodCall">The method to execute.</param>
    /// <returns>Job ID as a string.</returns>
    string Enqueue<T>(Expression<Action<T>> methodCall);
}
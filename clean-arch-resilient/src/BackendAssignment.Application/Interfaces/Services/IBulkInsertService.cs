using BackendAssignment.Application.DTOs;

namespace BackendAssignment.Application.Interfaces.Services;

/// <summary>
/// Handles bulk book insert operations and job tracking.
/// </summary>
public interface IBulkInsertService
{
    Task<Guid> BulkInsertBooksAsync(BulkInsertBooksDto bulkRequest);
    Task<object> GetJobStatusAsync(Guid jobId);
}
namespace BackendAssignment.Application.DTOs;

/// <summary>
/// Represents the response returned after queuing a bulk insert job.
/// </summary>
public record BulkInsertJobResponse(Guid JobId, string Message);
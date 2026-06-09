using BackendAssignment.Application.DTOs;
using BackendAssignment.Application.Interfaces.Repositories;
using BackendAssignment.Application.Interfaces.Services;
using BackendAssignment.Domain.Entities;
using Hangfire;
using Microsoft.Extensions.Logging;

namespace BackendAssignment.Application.Services;

/// <summary>
/// Service responsible for handling bulk insertion of books using Hangfire for background processing.
/// Provides job status tracking and caching for performance and user feedback.
/// </summary>
public class BulkInsertService(
    IBulkInsertRepository bulkInsertRepository,
    ICacheService cache,
    ILogger<BulkInsertService> logger,
    IJobScheduler _scheduler
) : IBulkInsertService
{
    
    /// <summary>
    /// Queues a bulk insert job and returns a unique job ID for tracking.
    /// </summary>
    /// <param name="bulkRequest">DTO containing a list of books to insert</param>
    /// <returns>Job ID (GUID) used to track the job status</returns>
    /// <exception cref="ArgumentException">Thrown if the request is null or contains no books</exception>
    public async Task<Guid> BulkInsertBooksAsync(BulkInsertBooksDto bulkRequest)
    {
        if (bulkRequest == null || bulkRequest.Books.Count == 0)
            throw new ArgumentException("Book list cannot be empty.");

            // Generate a unique ID to represent this job for client-side tracking
            var jobId = Guid.NewGuid();
            logger.LogInformation("[Job {JobId}] Queued for bulk insert processing.", jobId);

            // Enqueue the Hangfire background job, using dependency injection for self-reference
            var hangfireJobId = _scheduler.Enqueue<BulkInsertService>(s => s.ProcessBulkInsertAsync(jobId, bulkRequest));

            // Store the mapping in cache so clients can later retrieve job status
            await cache.SetAsync($"job:{jobId}", hangfireJobId);

        return jobId;
    }

    /// <summary>
    /// Retrieves the status of a background bulk insert job.
    /// </summary>
    /// <param name="jobId">The unique ID of the job</param>
    /// <returns>An object containing the job ID and its current status</returns>
    public async Task<object> GetJobStatusAsync(Guid jobId)
    {
        try
        {
            var jobIdString = await cache.GetAsync<string>($"job:{jobId}");
            if (string.IsNullOrEmpty(jobIdString))
                return new { jobId, status = "Not Found" };

            var monitoringApi = JobStorage.Current.GetMonitoringApi();
            string status;

            if (monitoringApi.ProcessingJobs(0, int.MaxValue).Any(j => j.Key == jobIdString))
                status = "Processing";
            else if (monitoringApi.SucceededJobs(0, int.MaxValue).Any(j => j.Key == jobIdString))
                status = "Completed";
            else if (monitoringApi.FailedJobs(0, int.MaxValue).Any(j => j.Key == jobIdString))
                status = "Failed";
            else if (monitoringApi.DeletedJobs(0, int.MaxValue).Any(j => j.Key == jobIdString))
                status = "Deleted";
            else
                status = "Not Found";

            return new { jobId, status };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while retrieving the job status.");
            throw new ApplicationException("Failed to retrieve job status.", ex);
        }
    }

    /// <summary>
    /// Processes the actual insertion of books in batches asynchronously.
    /// Intended to be executed as a background job by Hangfire.
    /// </summary>
    /// <param name="jobId">Job ID for logging and tracking</param>
    /// <param name="bulkRequest">DTO containing the book data</param>
    /// <returns>Task representing the asynchronous operation</returns>
    public async Task ProcessBulkInsertAsync(Guid jobId, BulkInsertBooksDto bulkRequest)
    {
        try
        {
            logger.LogInformation("Processing Bulk Insert Job {JobId}...", jobId);

            const int batchSize = 10;
            var totalBooks = bulkRequest.Books.Count;

            var validAuthorIds = await bulkInsertRepository.GetAllAuthorIdsAsync();
            var validCategoryIds = await bulkInsertRepository.GetAllCategoryIdsAsync();

            var invalidBooks = bulkRequest.Books
                .Where(b =>
                    !b.AuthorId.HasValue || !validAuthorIds.Contains(b.AuthorId.Value) ||
                    !b.CategoryId.HasValue || !validCategoryIds.Contains(b.CategoryId.Value)
                )
                .ToList();

            if (invalidBooks.Any())
            {
                logger.LogWarning("[Job {JobId}] Skipping {Count} books due to invalid foreign key references.", jobId, invalidBooks.Count);
            }

            var validBooks = bulkRequest.Books
                .Where(b =>
                    b.AuthorId.HasValue && validAuthorIds.Contains(b.AuthorId.Value) &&
                    b.CategoryId.HasValue && validCategoryIds.Contains(b.CategoryId.Value)
                )
                .ToList();

            if (!validBooks.Any())
            {
                logger.LogWarning("[Job {JobId}] No valid books to process after validation.", jobId);
                return;
            }

            var batchNumber = 1;

            for (var i = 0; i < validBooks.Count; i += batchSize)
            {
                var batch = validBooks
                    .Skip(i)
                    .Take(batchSize)
                    .Select(b => new Book
                    {
                        Title = b.Title,
                        PublicationDate = b.PublicationDate ?? DateTime.UtcNow,
                        CategoryId = b.CategoryId,
                        AuthorId = b.AuthorId,
                        Pages = b.Pages
                    })
                    .ToList();

                try
                {
                    await bulkInsertRepository.InsertBooksBatchAsync(batch);
                    logger.LogInformation("[Job {JobId}] Batch {Batch} of {TotalBatches} completed.", jobId, batchNumber, Math.Ceiling((double)validBooks.Count / batchSize));
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "[Job {JobId}] Batch {Batch} failed", jobId, batchNumber);
                }

                batchNumber++;
            }

            await cache.RemoveAsync("book:all");

            logger.LogInformation("Bulk Insert Job {JobId} Completed.", jobId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred during bulk insert processing.");
            throw new ApplicationException("Error during bulk insert processing.", ex);
        }
    }
}

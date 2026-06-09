using BackendAssignment.Application.DTOs;
using BackendAssignment.Application.Interfaces.Repositories;
using BackendAssignment.Application.Interfaces.Services;
using BackendAssignment.Application.Services;
using BackendAssignment.Domain.Entities;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System.Linq.Expressions;

namespace BackendAssignment.Tests.Application.Services;

public class BulkInsertServiceTests
{
    private readonly Mock<IBulkInsertRepository> _repo = new();
    private readonly Mock<ICacheService> _cache = new();
    private readonly Mock<ILogger<BulkInsertService>> _logger = new();
    private readonly Mock<IJobScheduler> _scheduler = new();
    private readonly BulkInsertService _service;

    public BulkInsertServiceTests()
    {
        // Instantiate the service with all mocked dependencies
        _service = new BulkInsertService(
            _repo.Object,
            _cache.Object,
            _logger.Object,
            _scheduler.Object
        );
    }

    [Fact]
    public async Task BulkInsertBooksAsync_ShouldThrow_WhenListIsEmpty()
    {
        // This test ensures the service throws when the book list is empty
        var request = new BulkInsertBooksDto { Books = [] };

        var act = async () => await _service.BulkInsertBooksAsync(request);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Book list cannot be empty.");
    }

    [Fact]
    public async Task BulkInsertBooksAsync_ShouldGenerateJobId_AndSetCache()
    {
        // Should create a job, generate a GUID, and store job mapping in cache
        var books = new List<RequestBookDto> { new() { Title = "Book A", Pages = 1 } };
        var request = new BulkInsertBooksDto { Books = books };

        _scheduler.Setup(j => j.Enqueue<BulkInsertService>(It.IsAny<Expression<Action<BulkInsertService>>>())).Returns("job-123");
        
        var jobId = await _service.BulkInsertBooksAsync(request);

        jobId.Should().NotBeEmpty();
        _cache.Verify(c => c.SetAsync(It.Is<string>(key => key.StartsWith("job:")), "job-123"), Times.Once);
    }

    [Fact]
    public async Task GetJobStatusAsync_ShouldReturnNotFound_WhenNoCache()
    {
        // Should return 'Not Found' if no job status is found in cache
        _cache.Setup(c => c.GetAsync<string>("job:123")).ReturnsAsync((string)null!);

        var result = await _service.GetJobStatusAsync(Guid.Parse("00000000-0000-0000-0000-000000000123"));

        result.Should().BeEquivalentTo(new { jobId = Guid.Parse("00000000-0000-0000-0000-000000000123"), status = "Not Found" });
    }

    [Fact]
    public async Task ProcessBulkInsertAsync_ShouldInsertInBatches()
    {
        _repo.Setup(r => r.GetAllAuthorIdsAsync()).ReturnsAsync(new List<int> { 1 });
        _repo.Setup(r => r.GetAllCategoryIdsAsync()).ReturnsAsync(new List<int> { 1 });

        // Verifies that large inserts are broken down into smaller batches (size 10)
        var books = Enumerable.Range(0, 25).Select(i => new RequestBookDto
        {
            Title = $"Book {i}",
            Pages = 100,
            AuthorId = 1,
            CategoryId = 1
        }).ToList();

        var dto = new BulkInsertBooksDto { Books = books };

        await _service.ProcessBulkInsertAsync(Guid.NewGuid(), dto);

        // 25 books should result in 3 batch inserts (10 + 10 + 5)
        _repo.Verify(r => r.InsertBooksBatchAsync(It.IsAny<List<Book>>()), Times.Exactly(3));
    }

    [Fact]
    public async Task ProcessBulkInsertAsync_ShouldRemoveBookCache()
    {
        _repo.Setup(r => r.GetAllAuthorIdsAsync()).ReturnsAsync(new List<int> { 1 });
        _repo.Setup(r => r.GetAllCategoryIdsAsync()).ReturnsAsync(new List<int> { 1 });

        var books = new List<RequestBookDto>
        {
            new() { Title = "X", Pages = 1, AuthorId = 1, CategoryId = 1 }
        };

        var dto = new BulkInsertBooksDto { Books = books };

        await _service.ProcessBulkInsertAsync(Guid.NewGuid(), dto);

        _cache.Verify(c => c.RemoveAsync("book:all"), Times.Once);
    }

    [Fact]
    public async Task ProcessBulkInsertAsync_ShouldContinue_WhenInsertFails()
    {
        _repo.Setup(r => r.GetAllAuthorIdsAsync()).ReturnsAsync(new List<int> { 1 });
        _repo.Setup(r => r.GetAllCategoryIdsAsync()).ReturnsAsync(new List<int> { 1 });

        var books = Enumerable.Range(0, 15).Select(i => new RequestBookDto
        {
            Title = $"Book {i}",
            Pages = 1,
            AuthorId = 1,
            CategoryId = 1
        }).ToList();

        _repo.SetupSequence(r => r.InsertBooksBatchAsync(It.IsAny<List<Book>>()))
            .ThrowsAsync(new Exception("fail"))
            .Returns(Task.CompletedTask);

        var dto = new BulkInsertBooksDto { Books = books };

        await _service.ProcessBulkInsertAsync(Guid.NewGuid(), dto);

        _repo.Verify(r => r.InsertBooksBatchAsync(It.IsAny<List<Book>>()), Times.Exactly(2));
    }
    
    [Fact]
    public async Task BulkInsertBooksAsync_ShouldThrow_WhenDtoIsNull()
    {
        BulkInsertBooksDto? request = null;

        var act = async () => await _service.BulkInsertBooksAsync(request!);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Book list cannot be empty.");
    }

    [Fact]
    public async Task ProcessBulkInsertAsync_ShouldSkip_WhenNoValidBooks()
    {
        _repo.Setup(r => r.GetAllAuthorIdsAsync()).ReturnsAsync([]);
        _repo.Setup(r => r.GetAllCategoryIdsAsync()).ReturnsAsync([]);

        var books = new List<RequestBookDto>
        {
            new() { Title = "Bad Book", AuthorId = 999, CategoryId = 999 }
        };

        var dto = new BulkInsertBooksDto { Books = books };

        await _service.ProcessBulkInsertAsync(Guid.NewGuid(), dto);

        _repo.Verify(r => r.InsertBooksBatchAsync(It.IsAny<List<Book>>()), Times.Never);
    }
}

using BackendAssignment.Domain.Entities;
using BackendAssignment.Infrastructure.Helpers;
using BackendAssignment.Infrastructure.Persistence.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace BackendAssignment.Tests.Infrastructure.Repositories;

public class BulkInsertRepositoryTests
{
    private readonly Mock<IDatabaseHelper> _dbHelperMock = new();
    private readonly Mock<ILogger<BulkInsertRepository>> _loggerMock = new();
    private readonly Mock<IFakeDbConnection> _fakeConnectionMock = new();

    private readonly BulkInsertRepository _repository;

    public BulkInsertRepositoryTests()
    {
        _dbHelperMock.Setup(d => d.CreateConnection()).Returns(_fakeConnectionMock.Object);
        _repository = new BulkInsertRepository(_dbHelperMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task InsertBooksBatchAsync_ShouldReturn_WhenListIsEmpty()
    {
        // Act
        var act = async () => await _repository.InsertBooksBatchAsync(new List<Book>());

        // Assert
        await act.Should().NotThrowAsync();
    }
    
}

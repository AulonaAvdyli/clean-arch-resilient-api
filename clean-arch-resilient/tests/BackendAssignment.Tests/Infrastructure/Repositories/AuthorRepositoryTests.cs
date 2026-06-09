using BackendAssignment.Domain.Entities;
using BackendAssignment.Domain.Exceptions;
using BackendAssignment.Infrastructure.Helpers;
using BackendAssignment.Infrastructure.Persistence.Repositories;
using FluentAssertions;
using Moq;

namespace BackendAssignment.Tests.Infrastructure.Repositories;

public class AuthorRepositoryTests
{
    private readonly Mock<IDatabaseHelper> _databaseHelperMock;
    private readonly Mock<IFakeDbConnection> _fakeDbConnectionMock;
    private readonly AuthorRepository _authorRepository;

    public AuthorRepositoryTests()
    {
        _databaseHelperMock = new Mock<IDatabaseHelper>();
        _fakeDbConnectionMock = new Mock<IFakeDbConnection>();

        _databaseHelperMock.Setup(d => d.CreateConnection())
            .Returns(_fakeDbConnectionMock.Object);

        _authorRepository = new AuthorRepository(_databaseHelperMock.Object);
    }
    
    [Fact]
    public async Task CreateAuthorAsync_ShouldThrowRepositoryException_WhenExceptionOccurs()
    {
        // Arrange
        var author = new Author { FirstName = "John", LastName = "Doe", Country = "USA", BooksPublished = 10 };

        _fakeDbConnectionMock.Setup(d =>
                d.ExecuteScalarAsync<int>(It.IsAny<string>(), It.IsAny<object>()))
            .ThrowsAsync(new Exception("SQL broke"));

        // Act
        var act = async () => await _authorRepository.CreateAuthorAsync(author);

        // Assert
        await act.Should().ThrowAsync<RepositoryException>()
            .WithMessage("An unexpected error occurred while creating the author*");
    }
    
    [Fact]
    public async Task GetAuthorByIdAsync_ShouldThrowRepositoryException_WhenExceptionOccurs()
    {
        _fakeDbConnectionMock.Setup(d =>
                d.QueryFirstOrDefaultAsync<Author>(It.IsAny<string>(), It.IsAny<object>()))
            .ThrowsAsync(new Exception("DB error"));

        var act = async () => await _authorRepository.GetAuthorByIdAsync(1);

        await act.Should().ThrowAsync<RepositoryException>()
            .WithMessage("An unexpected error occurred while retrieving author with ID 1*");
    }
    
    [Fact]
    public async Task UpdateAuthorAsync_ShouldThrowRepositoryException_WhenExceptionOccurs()
    {
        var author = new Author { FirstName = "John", LastName = "Doe", Country = "USA", BooksPublished = 10 };

        _fakeDbConnectionMock.Setup(d =>
                d.ExecuteAsync(It.IsAny<string>(), It.IsAny<object>()))
            .ThrowsAsync(new Exception("update broke"));

        var act = async () => await _authorRepository.UpdateAuthorAsync(1, author);

        await act.Should().ThrowAsync<RepositoryException>()
            .WithMessage("An unexpected error occurred while updating author with ID 1*");
    }

    [Fact]
    public async Task DeleteAuthorAsync_ShouldThrowRepositoryException_WhenExceptionOccurs()
    {
        _fakeDbConnectionMock.Setup(d =>
                d.ExecuteAsync(It.IsAny<string>(), It.IsAny<object>()))
            .ThrowsAsync(new Exception("delete fail"));

        var act = async () => await _authorRepository.DeleteAuthorAsync(99);

        await act.Should().ThrowAsync<RepositoryException>()
            .WithMessage("An unexpected error occurred while deleting author with ID 99*");
    }

    [Fact]
    public async Task CheckAuthorExists_ShouldThrowRepositoryException_WhenExceptionOccurs()
    {
        _fakeDbConnectionMock.Setup(d =>
                d.ExecuteScalarAsync<int>(It.IsAny<string>(), It.IsAny<object>()))
            .ThrowsAsync(new Exception("check fail"));

        var act = async () => await _authorRepository.CheckAuthorExists(42);

        await act.Should().ThrowAsync<RepositoryException>()
            .WithMessage("An unexpected error occurred while checking if author with ID 42 exists*");
    }

    [Fact]
    public async Task GetAuthorByDetailsAsync_ShouldThrowRepositoryException_WhenExceptionOccurs()
    {
        _fakeDbConnectionMock.Setup(d =>
                d.QueryFirstOrDefaultAsync<Author>(It.IsAny<string>(), It.IsAny<object>()))
            .ThrowsAsync(new Exception("get by details fail"));

        var act = async () => await _authorRepository.GetAuthorByDetailsAsync("John", "Doe", "USA", 5);

        await act.Should().ThrowAsync<RepositoryException>()
            .WithMessage("An unexpected error occurred while retrieving author by details*");
    }
}

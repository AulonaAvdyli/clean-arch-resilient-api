using BackendAssignment.Domain.Entities;
using BackendAssignment.Domain.Exceptions;
using BackendAssignment.Infrastructure.Helpers;
using BackendAssignment.Infrastructure.Persistence.Repositories;
using FluentAssertions;
using Moq;

namespace BackendAssignment.Tests.Infrastructure.Repositories;

public class BookRepositoryTests
{
    private readonly Mock<IDatabaseHelper> _databaseHelperMock;
    private readonly Mock<IFakeDbConnection> _fakeDbConnectionMock;
    private readonly BookRepository _bookRepository;

    public BookRepositoryTests()
    {
        _databaseHelperMock = new Mock<IDatabaseHelper>();
        _fakeDbConnectionMock = new Mock<IFakeDbConnection>();

        _databaseHelperMock.Setup(d => d.CreateConnection())
            .Returns(_fakeDbConnectionMock.Object);

        _bookRepository = new BookRepository(_databaseHelperMock.Object);
    }

    [Fact]
    public async Task CreateBookAsync_ShouldThrowRepositoryException_WhenExceptionOccurs()
    {
        var book = new Book { Title = "Broken Book" };

        _fakeDbConnectionMock.Setup(d =>
                d.ExecuteScalarAsync<int>(It.IsAny<string>(), It.IsAny<object>()))
            .ThrowsAsync(new Exception("db error"));

        var act = async () => await _bookRepository.CreateBookAsync(book);

        await act.Should().ThrowAsync<RepositoryException>()
            .WithMessage("An unexpected error occurred while creating the book*");
    }
    
    [Fact]
    public async Task CheckBookExists_ShouldThrowRepositoryException_WhenExceptionOccurs()
    {
        _fakeDbConnectionMock.Setup(d =>
                d.ExecuteScalarAsync<int>(It.IsAny<string>(), It.IsAny<object>()))
            .ThrowsAsync(new Exception("exist check fail"));

        var act = async () => await _bookRepository.CheckBookExists(99);

        await act.Should().ThrowAsync<RepositoryException>()
            .WithMessage("An unexpected error occurred while checking if book with ID 99 exists*");
    }

    [Fact]
    public async Task CheckAuthorExists_ShouldThrowRepositoryException_WhenExceptionOccurs()
    {
        _fakeDbConnectionMock.Setup(d =>
                d.ExecuteScalarAsync<int>(It.IsAny<string>(), It.IsAny<object>()))
            .ThrowsAsync(new Exception("author check fail"));

        var act = async () => await _bookRepository.CheckAuthorExists(77);

        await act.Should().ThrowAsync<RepositoryException>()
            .WithMessage("An unexpected error occurred while checking if author with ID 77 exists*");
    }
    
    [Fact]
    public async Task UpdateBookAsync_ShouldThrowRepositoryException_WhenExceptionOccurs()
    {
        var book = new Book { Title = "Update Me" };

        _fakeDbConnectionMock.Setup(d =>
                d.ExecuteAsync(It.IsAny<string>(), It.IsAny<object>()))
            .ThrowsAsync(new Exception("update broke"));

        var act = async () => await _bookRepository.UpdateBookAsync(1, book);

        await act.Should().ThrowAsync<RepositoryException>()
            .WithMessage("An unexpected error occurred while updating book with ID 1*");
    }

    [Fact]
    public async Task DeleteBookAsync_ShouldThrowRepositoryException_WhenExceptionOccurs()
    {
        _fakeDbConnectionMock.Setup(d =>
                d.ExecuteAsync(It.IsAny<string>(), It.IsAny<object>()))
            .ThrowsAsync(new Exception("delete failed"));

        var act = async () => await _bookRepository.DeleteBookAsync(42);

        await act.Should().ThrowAsync<RepositoryException>()
            .WithMessage("An unexpected error occurred while deleting book with ID 42*");
    }
    
    
    [Fact]
    public async Task GetBookByTitleAndAuthorAsync_ShouldThrowRepositoryException_WhenConnectionFails()
    {
        // Arrange: simulate failure before Dapper
        _databaseHelperMock.Setup(d => d.CreateConnection())
            .Throws(new Exception("lookup failed"));

        // Act
        var act = async () => await _bookRepository.GetBookByTitleAndAuthorAsync("Book", 1);

        // Assert
        await act.Should().ThrowAsync<Exception>()
            .WithMessage("lookup failed");
    }

    [Fact]
    public async Task LinkBookToAuthorAsync_ShouldThrowException_WhenConnectionFails()
    {
        // Arrange
        _databaseHelperMock.Setup(d => d.CreateConnection())
            .Throws(new Exception("link author fail"));

        // Act
        var act = async () => await _bookRepository.LinkBookToAuthorAsync(1, 2);

        // Assert
        await act.Should().ThrowAsync<Exception>()
            .WithMessage("link author fail");
    }
    
    
    [Fact]
    public async Task LinkBookToCategoryAsync_ShouldThrowException_WhenConnectionFails()
    {
        // Arrange
        _databaseHelperMock.Setup(d => d.CreateConnection())
            .Throws(new Exception("link category fail"));

        // Act
        var act = async () => await _bookRepository.LinkBookToCategoryAsync(1, 3);

        // Assert
        await act.Should().ThrowAsync<Exception>()
            .WithMessage("link category fail");
    }

    
    [Fact]
    public async Task GetBookByIdAsync_ShouldThrowRepositoryException_WhenConnectionFails()
    {
        // Arrange: make the helper throw before even hitting Dapper
        _databaseHelperMock.Setup(d => d.CreateConnection())
            .Throws(new Exception("DB connection error"));

        var act = async () => await _bookRepository.GetBookByIdAsync(1);

        // Assert
        await act.Should().ThrowAsync<RepositoryException>()
            .WithMessage("An unexpected error occurred while retrieving book with ID 1*");
    }

    [Fact]
    public async Task GetAllBooksAsync_ShouldThrowRepositoryException_WhenConnectionFails()
    {
        _databaseHelperMock.Setup(d => d.CreateConnection())
            .Throws(new Exception("connection fail"));

        var act = async () => await _bookRepository.GetAllBooksAsync();

        await act.Should().ThrowAsync<RepositoryException>()
            .WithMessage("An unexpected error occurred while retrieving books*");
    }
    
}

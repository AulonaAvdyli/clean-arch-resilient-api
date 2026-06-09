using BackendAssignment.Application.DTOs;
using BackendAssignment.Application.Interfaces.Repositories;
using BackendAssignment.Application.Interfaces.Services;
using BackendAssignment.Application.Services;
using BackendAssignment.Domain.Entities;
using BackendAssignment.Domain.Exceptions;
using FluentAssertions;
using Moq;

namespace BackendAssignment.Tests.Application.Services;

public class AuthorServiceTests
{
    private readonly Mock<IAuthorRepository> _authorRepositoryMock = new();
    private readonly Mock<ICacheService> _cacheMock = new();
    private readonly IAuthorService _authorService;

    public AuthorServiceTests()
    {
        // Inject mocked dependencies into the service
        _authorService = new AuthorService(_authorRepositoryMock.Object, _cacheMock.Object);
    }

    [Fact]
    public async Task CreateAuthorAsync_Should_CreateAuthor_When_ValidRequest()
    {
        // Arrange: simulate new author
        var request = new RequestAuthorDto { FirstName = "Test", LastName = "Author", Country = "UK", BooksPublished = 5 };

        _authorRepositoryMock.Setup(r => r.GetAuthorByDetailsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>())).ReturnsAsync((Author)null!);
        _authorRepositoryMock.Setup(r => r.CreateAuthorAsync(It.IsAny<Author>())).ReturnsAsync(1);
        _authorRepositoryMock.Setup(r => r.GetAuthorByIdAsync(1)).ReturnsAsync(new Author { Id = 1, FirstName = "Test", LastName = "Author", Country = "UK", BooksPublished = 5 });

        // Act
        var result = await _authorService.CreateAuthorAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(1);
        result.FirstName.Should().Be("Test");
        result.LastName.Should().Be("Author");
        result.Country.Should().Be("UK");
        result.BooksPublished.Should().Be(5);
    }

    [Fact]
    public async Task CreateAuthorAsync_Should_ThrowConflictException_When_AuthorExists()
    {
        // Arrange: simulate duplicate author
        var request = new RequestAuthorDto { FirstName = "Existing", LastName = "Author", Country = "UK", BooksPublished = 1 };
        _authorRepositoryMock.Setup(r => r.GetAuthorByDetailsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>())).ReturnsAsync(new Author());

        // Act + Assert: should throw ConflictException
        var act = async () => await _authorService.CreateAuthorAsync(request);
        await act.Should().ThrowAsync<ConflictException>().WithMessage("*already exists*");
    }

    [Fact]
    public async Task GetAuthorByIdAsync_Should_ReturnAuthor_When_Cached()
    {
        // Arrange: simulate author cached
        var cachedDto = new ResponseAuthorDto { Id = 1, FirstName = "Cached", LastName = "Author", Country = "UK", BooksPublished = 3 };
        _cacheMock.Setup(c => c.GetAsync<ResponseAuthorDto>("author:1")).ReturnsAsync(cachedDto);

        var result = await _authorService.GetAuthorByIdAsync(1);

        result.Should().BeEquivalentTo(cachedDto);
        _authorRepositoryMock.Verify(r => r.GetAuthorByIdAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task GetAuthorByIdAsync_Should_ThrowNotFound_When_NotExists()
    {
        // Arrange: cache + db both miss
        _cacheMock.Setup(c => c.GetAsync<ResponseAuthorDto>("author:1")).ReturnsAsync((ResponseAuthorDto)null!);
        _authorRepositoryMock.Setup(r => r.GetAuthorByIdAsync(1)).ReturnsAsync((Author)null!);

        // Act + Assert
        var act = async () => await _authorService.GetAuthorByIdAsync(1);
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task DeleteAuthorAsync_Should_RemoveFromCache_When_Successful()
    {
        _authorRepositoryMock.Setup(r => r.CheckAuthorExists(1)).ReturnsAsync(true);
        _authorRepositoryMock.Setup(r => r.DeleteAuthorAsync(1)).ReturnsAsync(true);

        var result = await _authorService.DeleteAuthorAsync(1);

        result.Should().BeTrue();
        _cacheMock.Verify(c => c.RemoveAsync("author:1"), Times.Once);
        _cacheMock.Verify(c => c.RemoveAsync("author:all"), Times.Once);
    }

    [Fact]
    public async Task UpdateAuthorAsync_Should_InvalidateCache_When_Successful()
    {
        // Arrange: Simulate an existing author and an update request
        var existing = new Author { Id = 1, FirstName = "John", LastName = "Smith", Country = "GR", BooksPublished = 3 };
        var request = new RequestAuthorDto { FirstName = "John", LastName = "Smith", Country = "GR", BooksPublished = 4 };
        var updatedAuthor = new ResponseAuthorDto { Id = 1, FirstName = "John", LastName = "Smith", Country = "GR", BooksPublished = 4 };

        // Mock repository methods to simulate database interactions
        _authorRepositoryMock.Setup(r => r.GetAuthorByIdAsync(1)).ReturnsAsync(existing);
        _authorRepositoryMock.Setup(r => r.UpdateAuthorAsync(1, It.IsAny<Author>())).ReturnsAsync(true); 

        // Act: Call the UpdateAuthorAsync method
        var result = await _authorService.UpdateAuthorAsync(1, request);

        // Assert: Ensure the result is of the correct type
        var updatedResponse = Assert.IsType<ResponseAuthorDto>(result);
        Assert.Equal("John", updatedResponse.FirstName);
        Assert.Equal(4, updatedResponse.BooksPublished);

        // Verify cache invalidation calls
        _cacheMock.Verify(c => c.RemoveAsync("author:1"), Times.Once);  
        _cacheMock.Verify(c => c.RemoveAsync("author:all"), Times.Once);
    }
    
    [Fact]
    public async Task GetAllAuthorsAsync_Should_ReturnFromCache_When_CacheExists()
    {
        // Arrange: simulate cache hit
        var cachedAuthors = new List<ResponseAuthorDto>
        {
            new() { Id = 1, FirstName = "John", LastName = "Doe", Country = "Greece", BooksPublished = 3 }
        };

        _cacheMock
            .Setup(c => c.GetAsync<IEnumerable<ResponseAuthorDto>>("author:all"))
            .ReturnsAsync(cachedAuthors);

        // Act
        var result = await _authorService.GetAllAuthorsAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        _authorRepositoryMock.Verify(r => r.GetAllAuthorsAsync(), Times.Never);
        _cacheMock.Verify(c => c.SetAsync(It.IsAny<string>(), It.IsAny<object>()), Times.Never);
    }


    [Fact]
    public async Task GetAllAuthorsAsync_Should_ReturnFromRepository_And_Cache_When_CacheIsEmpty()
    {
        // Arrange: cache miss, db hit
        _cacheMock
            .Setup(c => c.GetAsync<IEnumerable<ResponseAuthorDto>>("author:all"))
            .ReturnsAsync((IEnumerable<ResponseAuthorDto>?)null);

        var authorsFromRepo = new List<Author>
        {
            new() { Id = 1, FirstName = "Jane", LastName = "Smith", Country = "Greece", BooksPublished = 5 }
        };

        _authorRepositoryMock
            .Setup(r => r.GetAllAuthorsAsync())
            .ReturnsAsync(authorsFromRepo);

        // Act
        var result = await _authorService.GetAllAuthorsAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result.First().FirstName.Should().Be("Jane");

        _authorRepositoryMock.Verify(r => r.GetAllAuthorsAsync(), Times.Once);
        _cacheMock.Verify(c => c.SetAsync("author:all", It.IsAny<object>()), Times.Once);
    }
    
    [Fact]
    public async Task CreateAuthorAsync_Should_ThrowBadRequest_When_NameInvalid()
    {
        var request = new RequestAuthorDto { FirstName = "", LastName = "", Country = "UK", BooksPublished = 1 };
        var act = async () => await _authorService.CreateAuthorAsync(request);
        await act.Should().ThrowAsync<BadRequestException>();
    }

    [Fact]
    public async Task GetAuthorByIdAsync_Should_Return_When_NotCached()
    {
        _cacheMock.Setup(c => c.GetAsync<ResponseAuthorDto>("author:1")).ReturnsAsync((ResponseAuthorDto)null);

        var author = new Author { Id = 1, FirstName = "Test", LastName = "Test", Country = "UK", BooksPublished = 1 };
        _authorRepositoryMock.Setup(r => r.GetAuthorByIdAsync(1)).ReturnsAsync(author);
        _cacheMock.Setup(c => c.SetAsync("author:1", It.IsAny<ResponseAuthorDto>()));

        var result = await _authorService.GetAuthorByIdAsync(1);

        result.Should().NotBeNull();
        result.Id.Should().Be(1);
    }
    
    [Fact]
    public async Task CreateAuthorAsync_ShouldThrow_WhenCreationFails()
    {
        var request = new RequestAuthorDto
        {
            FirstName = "Fail",
            LastName = "Case",
            Country = "Nowhere",
            BooksPublished = 1
        };

        _authorRepositoryMock.Setup(r => r.GetAuthorByDetailsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync((Author?)null);
        _authorRepositoryMock.Setup(r => r.CreateAuthorAsync(It.IsAny<Author>())).ReturnsAsync(123);
        _authorRepositoryMock.Setup(r => r.GetAuthorByIdAsync(123)).ReturnsAsync((Author?)null);

        var act = async () => await _authorService.CreateAuthorAsync(request);

        await act.Should().ThrowAsync<Exception>().WithMessage("*Author creation failed*");
    }

    [Fact]
    public async Task DeleteAuthorAsync_ShouldReturnFalse_WhenDeleteFails()
    {
        _authorRepositoryMock.Setup(r => r.CheckAuthorExists(1)).ReturnsAsync(true);
        _authorRepositoryMock.Setup(r => r.DeleteAuthorAsync(1)).ReturnsAsync(false);

        var result = await _authorService.DeleteAuthorAsync(1);

        result.Should().BeFalse();
        _cacheMock.Verify(c => c.RemoveAsync(It.IsAny<string>()), Times.Never);
    }
    
    [Fact]
    public async Task UpdateAuthorAsync_ShouldThrowNotFoundException_WhenAuthorDoesNotExist()
    {
        _authorRepositoryMock.Setup(r => r.GetAuthorByIdAsync(1))
            .ReturnsAsync((Author?)null);

        var request = new RequestAuthorDto { FirstName = "New", LastName = "Author" };

        var act = async () => await _authorService.UpdateAuthorAsync(1, request);

        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("Author with ID 1 was not found.");
    }

    [Fact]
    public async Task UpdateAuthorAsync_ShouldThrowApplicationException_WhenRepositoryFails()
    {
        var existing = new Author { Id = 1, FirstName = "Old", LastName = "Author", Country = "USA", BooksPublished = 1 };

        _authorRepositoryMock.Setup(r => r.GetAuthorByIdAsync(1)).ReturnsAsync(existing);
        _authorRepositoryMock.Setup(r => r.UpdateAuthorAsync(1, It.IsAny<Author>())).ReturnsAsync(false);

        var request = new RequestAuthorDto { FirstName = "New", LastName = "Author" };

        var act = async () => await _authorService.UpdateAuthorAsync(1, request);

        await act.Should().ThrowAsync<ApplicationException>()
            .WithMessage("An error occurred while updating author with ID 1.*");
    }

    [Fact]
    public async Task DeleteAuthorAsync_ShouldThrowNotFoundException_WhenAuthorDoesNotExist()
    {
        _authorRepositoryMock.Setup(r => r.CheckAuthorExists(99)).ReturnsAsync(false);

        var act = async () => await _authorService.DeleteAuthorAsync(99);

        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("Author with ID 99 was not found.");
    }

    [Fact]
    public async Task GetAuthorByIdAsync_ShouldThrowApplicationException_WhenOtherErrorOccurs()
    {
        _cacheMock.Setup(c => c.GetAsync<ResponseAuthorDto>("author:1")).ReturnsAsync((ResponseAuthorDto)null);
        _authorRepositoryMock.Setup(r => r.GetAuthorByIdAsync(1)).ThrowsAsync(new Exception("DB fail"));

        var act = async () => await _authorService.GetAuthorByIdAsync(1);

        await act.Should().ThrowAsync<ApplicationException>()
            .WithMessage("An error occurred while retrieving author with ID 1.*");
    }

    [Fact]
    public async Task GetAllAuthorsAsync_ShouldThrowApplicationException_WhenRepositoryFails()
    {
        _cacheMock.Setup(c => c.GetAsync<IEnumerable<ResponseAuthorDto>>("author:all"))
            .ReturnsAsync((IEnumerable<ResponseAuthorDto>?)null);

        _authorRepositoryMock.Setup(r => r.GetAllAuthorsAsync()).ThrowsAsync(new Exception("fail"));

        var act = async () => await _authorService.GetAllAuthorsAsync();

        await act.Should().ThrowAsync<ApplicationException>()
            .WithMessage("An error occurred while retrieving authors*");
    }

}

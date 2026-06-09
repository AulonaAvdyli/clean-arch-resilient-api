using BackendAssignment.Application.DTOs;
using BackendAssignment.Application.Interfaces.Repositories;
using BackendAssignment.Application.Interfaces.Services;
using BackendAssignment.Application.Services;
using BackendAssignment.Domain.Entities;
using BackendAssignment.Domain.Exceptions;
using FluentAssertions;
using Moq;

namespace BackendAssignment.Tests.Application.Services;

public class BookServiceTests
{
    private readonly Mock<IBookRepository> _bookRepo = new();
    private readonly Mock<IAuthorRepository> _authorRepo = new();
    private readonly Mock<ICategoryRepository> _categoryRepo = new();
    private readonly Mock<ICacheService> _cache = new();
    private readonly IBookService _service;

    public BookServiceTests()
    {
        // Inject mocked dependencies into the service
        _service = new BookService(_bookRepo.Object, _authorRepo.Object, _categoryRepo.Object, _cache.Object);
    }

    [Fact]
    public async Task CreateBookAsync_ShouldCreateBook_WhenRequestIsValid()
    {
        // Tests successful book creation flow with valid input
        var request = new RequestBookDto { Title = "Test", Pages = 100, AuthorId = 1 };

        _bookRepo.Setup(r => r.CheckAuthorExists(1)).ReturnsAsync(true);
        _bookRepo.Setup(r => r.GetBookByTitleAndAuthorAsync("Test", 1)).ReturnsAsync((Book)null!);
        _bookRepo.Setup(r => r.CreateBookAsync(It.IsAny<Book>())).ReturnsAsync(1);
        _bookRepo.Setup(r => r.GetBookByIdAsync(1)).ReturnsAsync(new Book
        {
            Id = 1,
            Title = "Test",
            Pages = 100,
            Category = new Category(),
            Author = new Author()
        });
        
        var result = await _service.CreateBookAsync(request);

        result.Should().NotBeNull();
        result.Id.Should().Be(1);
    }
    
    [Fact]
    public async Task CreateBookAsync_ShouldThrowConflict_WhenBookExists()
    {
        // Should throw conflict if a book with same title and author exists
        var request = new RequestBookDto
        {
            Title = "Duplicate",
            AuthorId = 1,
            Pages = 100
        };

        _bookRepo.Setup(r => r.CheckAuthorExists(1)).ReturnsAsync(true);
        _bookRepo.Setup(r => r.GetBookByTitleAndAuthorAsync("Duplicate", 1)).ReturnsAsync(new Book());

        var act = async () => await _service.CreateBookAsync(request);

        await act.Should().ThrowAsync<ConflictException>();
    }
    
    [Fact]
    public async Task GetAllBooksAsync_ShouldReturnFromCache()
    {
        // Returns cached books instead of querying repository
        var expected = new List<ResponseBookDto> { new() { Id = 1 } };
        _cache.Setup(c => c.GetAsync<IEnumerable<ResponseBookDto>>("book:all")).ReturnsAsync(expected);

        var result = await _service.GetAllBooksAsync();

        result.Should().BeEquivalentTo(expected);
        _bookRepo.Verify(r => r.GetAllBooksAsync(), Times.Never);
    }

    [Fact]
    public async Task GetBookByIdAsync_ShouldThrow_WhenNotExists()
    {
        // Throws not found when book doesn't exist in cache or DB
        _cache.Setup(c => c.GetAsync<ResponseBookDto>("book:2")).ReturnsAsync((ResponseBookDto)null!);
        _bookRepo.Setup(r => r.GetBookByIdAsync(2)).ReturnsAsync((Book)null!);

        var act = async () => await _service.GetBookByIdAsync(2);
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task DeleteBookAsync_ShouldRemove_WhenExists()
    {
        // Deletes book and invalidates cache
        _bookRepo.Setup(r => r.CheckBookExists(1)).ReturnsAsync(true);
        _bookRepo.Setup(r => r.DeleteBookAsync(1)).ReturnsAsync(true);

        var result = await _service.DeleteBookAsync(1);

        result.Should().BeTrue();
        _cache.Verify(c => c.RemoveAsync("book:1"), Times.Once);
    }

    [Fact]
    public async Task LinkBookToAuthorAsync_ShouldLink_WhenValid()
    {
        // Successfully links a book to an author
        _bookRepo.Setup(r => r.GetBookByIdAsync(1)).ReturnsAsync(new Book());
        _authorRepo.Setup(r => r.GetAuthorByIdAsync(2)).ReturnsAsync(new Author());
        _bookRepo.Setup(r => r.LinkBookToAuthorAsync(1, 2)).Returns(Task.CompletedTask);

        await _service.LinkBookToAuthorAsync(1, 2);
        _bookRepo.Verify(r => r.LinkBookToAuthorAsync(1, 2), Times.Once);
    }

    [Fact]
    public async Task LinkBookToAuthorAsync_ShouldThrow_WhenAuthorNotFound()
    {
        // Throws if the author does not exist
        _bookRepo.Setup(r => r.GetBookByIdAsync(1)).ReturnsAsync(new Book());
        _authorRepo.Setup(r => r.GetAuthorByIdAsync(2)).ReturnsAsync((Author?)null);

        var act = async () => await _service.LinkBookToAuthorAsync(1, 2);
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task LinkBookToAuthorAsync_ShouldThrow_WhenAlreadyLinked()
    {
        // Throws if book already has an author assigned
        _bookRepo.Setup(r => r.GetBookByIdAsync(1)).ReturnsAsync(new Book { AuthorId = 2 });
        _authorRepo.Setup(r => r.GetAuthorByIdAsync(3)).ReturnsAsync(new Author());

        var act = async () => await _service.LinkBookToAuthorAsync(1, 3);
        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task SearchBooksAsync_ShouldReturn_WhenMatched()
    {
        // Searches by title and returns match
        var books = new List<Book> { new() { Id = 1, Title = "Searchable", Category = new Category(), Author = new Author() } };
        _bookRepo.Setup(r => r.SearchBooksAsync("Searchable", null, null, null)).ReturnsAsync(books);

        var result = await _service.SearchBooksAsync("Searchable", null, null, null);

        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetBookByIdAsync_ShouldReturnCached_WhenExists()
    {
        // Returns book from cache
        var dto = new ResponseBookDto { Id = 1, Title = "Cached Book" };
        _cache.Setup(c => c.GetAsync<ResponseBookDto>("book:1")).ReturnsAsync(dto);

        var result = await _service.GetBookByIdAsync(1);

        result.Should().BeEquivalentTo(dto);
    }

    [Fact]
    public async Task GetBookByIdAsync_ShouldReturnFromRepoAndSetCache_WhenCacheMiss()
    {
        // Loads from DB and sets cache when not cached
        _cache.Setup(c => c.GetAsync<ResponseBookDto>("book:1")).ReturnsAsync((ResponseBookDto?)null);
        _bookRepo.Setup(r => r.GetBookByIdAsync(1)).ReturnsAsync(new Book { Category = new Category(), Author = new Author() });

        var result = await _service.GetBookByIdAsync(1);

        result.Should().NotBeNull();
        _cache.Verify(c => c.SetAsync("book:1", It.IsAny<ResponseBookDto>()), Times.Once);
    }

    [Fact]
    public async Task GetBookByIdAsync_ShouldThrowNotFound_WhenNotExists()
    {
        // Throws not found when book is not cached or in DB
        _cache.Setup(c => c.GetAsync<ResponseBookDto>("book:1")).ReturnsAsync((ResponseBookDto?)null);
        _bookRepo.Setup(r => r.GetBookByIdAsync(1)).ReturnsAsync((Book?)null);

        var act = async () => await _service.GetBookByIdAsync(1);
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task DeleteBookAsync_ShouldThrow_WhenBookNotFound()
    {
        // Throws not found if book doesn't exist
        _bookRepo.Setup(r => r.CheckBookExists(99)).ReturnsAsync(false);

        var act = async () => await _service.DeleteBookAsync(99);
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task DeleteBookAsync_ShouldRemoveCache_WhenDeleted()
    {
        // Invalidates cache after deletion
        _bookRepo.Setup(r => r.CheckBookExists(1)).ReturnsAsync(true);
        _bookRepo.Setup(r => r.DeleteBookAsync(1)).ReturnsAsync(true);

        var result = await _service.DeleteBookAsync(1);

        result.Should().BeTrue();
        _cache.Verify(c => c.RemoveAsync("book:1"), Times.Once);
        _cache.Verify(c => c.RemoveAsync("book:all"), Times.Once);
    }

    [Fact]
    public async Task SearchBooksAsync_ShouldReturnResults()
    {
        // Basic search returns a match
        var books = new List<Book> { new() { Id = 1, Title = "Clean Code" } };
        _bookRepo.Setup(r => r.SearchBooksAsync("Clean Code", null, null, null)).ReturnsAsync(books);

        var result = await _service.SearchBooksAsync("Clean Code", null, null, null);

        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task LinkBookToCategoryAsync_ShouldThrow_WhenAlreadyLinked()
    {
        // Throws if book is already assigned a category
        _bookRepo.Setup(r => r.GetBookByIdAsync(1)).ReturnsAsync(new Book { CategoryId = 5 });
        _categoryRepo.Setup(r => r.GetCategoryByIdAsync(5)).ReturnsAsync(new Category());

        var act = async () => await _service.LinkBookToCategoryAsync(1, 5);
        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task LinkBookToCategoryAsync_ShouldThrow_WhenBookNotFound()
    {
        // Throws if book doesn't exist
        _bookRepo.Setup(r => r.GetBookByIdAsync(1)).ReturnsAsync((Book?)null);

        var act = async () => await _service.LinkBookToCategoryAsync(1, 1);
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task LinkBookToCategoryAsync_ShouldThrow_WhenCategoryNotFound()
    {
        // Throws if category doesn't exist
        _bookRepo.Setup(r => r.GetBookByIdAsync(1)).ReturnsAsync(new Book());
        _categoryRepo.Setup(r => r.GetCategoryByIdAsync(1)).ReturnsAsync((Category?)null);

        var act = async () => await _service.LinkBookToCategoryAsync(1, 1);
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task LinkBookToCategoryAsync_ShouldLink_WhenValid()
    {
        // Successfully links category
        _bookRepo.Setup(r => r.GetBookByIdAsync(1)).ReturnsAsync(new Book());
        _categoryRepo.Setup(r => r.GetCategoryByIdAsync(1)).ReturnsAsync(new Category());

        await _service.LinkBookToCategoryAsync(1, 1);
        _bookRepo.Verify(r => r.LinkBookToCategoryAsync(1, 1), Times.Once);
    }

    [Fact]
    public async Task UpdateBookAsync_ShouldUpdate_WhenValid()
    {
        // Successfully updates book
        var existing = new Book
        {
            Id = 1,
            Title = "Old",
            Pages = 100,
            AuthorId = 1,
            CategoryId = 1,
            Author = new Author { Id = 1, FirstName = "John", LastName = "Doe" }
        };

        var updated = new Book
        {
            Id = 1,
            Title = "Updated",
            Pages = 200,
            AuthorId = 1,
            CategoryId = 1,
            Author = new Author { Id = 1, FirstName = "John", LastName = "Doe" }
        };

        var req = new RequestBookDto { Title = "Updated", Pages = 200, AuthorId = 1 };

        _bookRepo.SetupSequence(r => r.GetBookByIdAsync(1))
            .ReturnsAsync(existing) 
            .ReturnsAsync(updated);

        _bookRepo.Setup(r => r.CheckAuthorExists(1)).ReturnsAsync(true);
        _bookRepo.Setup(r => r.UpdateBookAsync(1, It.IsAny<Book>())).ReturnsAsync(true);

        // Act
        var result = await _service.UpdateBookAsync(1, req);

        // Assert
        var updatedBook = Assert.IsType<ResponseBookDto>(result);
        Assert.Equal("Updated", updatedBook.Title);
        Assert.Equal(200, updatedBook.Pages);
        Assert.Equal(1, updatedBook.Author.Id);

        _bookRepo.Verify();
    }


    [Fact]
    public async Task CreateBookAsync_ShouldThrowBadRequest_WhenTitleMissing()
    {
        // Title is required
        var request = new RequestBookDto { Title = "", Pages = 100 };
        var act = async () => await _service.CreateBookAsync(request);

        await act.Should().ThrowAsync<BadRequestException>();
    }

    [Fact]
    public async Task CreateBookAsync_ShouldThrowBadRequest_WhenPagesInvalid()
    {
        // Page number must be positive
        var request = new RequestBookDto { Title = "Valid", Pages = 0 };
        var act = async () => await _service.CreateBookAsync(request);

        await act.Should().ThrowAsync<BadRequestException>();
    }

    [Fact]
    public async Task CreateBookAsync_ShouldThrowNotFound_WhenAuthorDoesNotExist()
    {
        // Throws if AuthorId is invalid
        var request = new RequestBookDto { Title = "New", Pages = 100, AuthorId = 10 };
        _bookRepo.Setup(r => r.CheckAuthorExists(10)).ReturnsAsync(false);

        var act = async () => await _service.CreateBookAsync(request);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task CreateBookAsync_ShouldThrowException_WhenRetrievalFails()
    {
        // Simulates book creation but retrieval fails
        var request = new RequestBookDto { Title = "Valid", Pages = 123, AuthorId = 1 };

        _bookRepo.Setup(r => r.CheckAuthorExists(1)).ReturnsAsync(true);
        _bookRepo.Setup(r => r.GetBookByTitleAndAuthorAsync("Valid", 1)).ReturnsAsync((Book?)null);
        _bookRepo.Setup(r => r.CreateBookAsync(It.IsAny<Book>())).ReturnsAsync(1);
        _bookRepo.Setup(r => r.GetBookByIdAsync(1)).ReturnsAsync((Book?)null);

        var act = async () => await _service.CreateBookAsync(request);
        await act.Should().ThrowAsync<Exception>().WithMessage("*An unexpected error occurred while creating the book*");
    }

    [Fact]
    public async Task UpdateBookAsync_ShouldThrowNotFound_WhenBookDoesNotExist()
    {
        _bookRepo.Setup(r => r.GetBookByIdAsync(99)).ReturnsAsync((Book?)null);
        var act = async () => await _service.UpdateBookAsync(99, new RequestBookDto());

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task UpdateBookAsync_ShouldThrowNotFound_WhenAuthorInvalid()
    {
        var existing = new Book { Id = 1, AuthorId = 1 };
        _bookRepo.Setup(r => r.GetBookByIdAsync(1)).ReturnsAsync(existing);
        _bookRepo.Setup(r => r.CheckAuthorExists(99)).ReturnsAsync(false);

        var req = new RequestBookDto { AuthorId = 99 };
        var act = async () => await _service.UpdateBookAsync(1, req);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GetAllBooksAsync_ShouldFallbackToRepo_WhenCacheMiss()
    {
        // Cache miss fallback to DB
        _cache.Setup(c => c.GetAsync<IEnumerable<ResponseBookDto>>("book:all")).ReturnsAsync((IEnumerable<ResponseBookDto>?)null);
        _bookRepo.Setup(r => r.GetAllBooksAsync()).ReturnsAsync(new List<Book> { new Book { Id = 1, Title = "From DB", Category = new Category(), Author = new Author() } });

        var result = await _service.GetAllBooksAsync();

        result.Should().HaveCount(1);
        result.First().Title.Should().Be("From DB");
    }
    
    [Fact]
    public async Task SearchBooksAsync_ShouldThrowApplicationException_WhenRepoFails()
    {
        _bookRepo.Setup(r => r.SearchBooksAsync(It.IsAny<string>(), null, null, null))
            .ThrowsAsync(new Exception("DB failure"));

        var act = async () => await _service.SearchBooksAsync("fail", null, null, null);

        await act.Should().ThrowAsync<ApplicationException>()
            .WithMessage("An error occurred while searching books.");
    }

    [Fact]
    public async Task DeleteBookAsync_ShouldReturnFalse_WhenDeletionFails()
    {
        _bookRepo.Setup(r => r.CheckBookExists(1)).ReturnsAsync(true);
        _bookRepo.Setup(r => r.DeleteBookAsync(1)).ReturnsAsync(false);

        var result = await _service.DeleteBookAsync(1);

        result.Should().BeFalse();
        _cache.Verify(c => c.RemoveAsync(It.IsAny<string>()), Times.Never);
    }
}


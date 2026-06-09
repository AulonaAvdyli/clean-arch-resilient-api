using BackendAssignment.Application.DTOs;
using BackendAssignment.Application.Interfaces.Repositories;
using BackendAssignment.Application.Interfaces.Services;
using BackendAssignment.Domain.Entities;
using BackendAssignment.Domain.Exceptions;

namespace BackendAssignment.Application.Services;

/// <summary>
/// Service responsible for managing book-related operations including creation, retrieval, update, linking, and deletion.
/// Implements caching for performance and follows clean architecture.
/// </summary>
public class BookService : IBookService
{
    private readonly IAuthorRepository _authorRepository;
    private readonly IBookRepository _bookRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly ICacheService _cache;

    /// <summary>
    /// Constructor that injects repositories and cache service.
    /// </summary>
    public BookService(
        IBookRepository bookRepository,
        IAuthorRepository authorRepository,
        ICategoryRepository categoryRepository,
        ICacheService cache)
    {
        _bookRepository = bookRepository;
        _authorRepository = authorRepository;
        _categoryRepository = categoryRepository;
        _cache = cache;
    }

    /// <summary>
    /// Creates a new book with validation and stores it in the database.
    /// </summary>
    /// <param name="requestBook">DTO containing the book data</param>
    /// <returns>Created book as a DTO</returns>
    /// <exception cref="BadRequestException">Thrown if book data is invalid</exception>
    /// <exception cref="ConflictException">Thrown if a duplicate book exists</exception>
    /// <exception cref="NotFoundException">Thrown if referenced author does not exist</exception>
    public async Task<ResponseBookDto> CreateBookAsync(RequestBookDto requestBook)
    {
        try
        {
            // Validate book data before processing
            if (string.IsNullOrWhiteSpace(requestBook.Title) || requestBook.Pages <= 0)
                throw new BadRequestException("Invalid book data.");

            // Check if provided Author exists
            if (requestBook.AuthorId.HasValue)
            {
                var authorExists = await _bookRepository.CheckAuthorExists(requestBook.AuthorId.Value);
                if (!authorExists)
                    throw new NotFoundException($"Author with ID {requestBook.AuthorId} does not exist.");
            }

            // Avoid creating duplicates based on title and author
            if (requestBook.AuthorId.HasValue)
            {
                var existingBook = await _bookRepository
                    .GetBookByTitleAndAuthorAsync(requestBook.Title, requestBook.AuthorId.Value);

                if (existingBook != null)
                    throw new ConflictException("A book with the same title and author already exists.");
            }

            var newBook = new Book
            {
                Title = requestBook.Title,
                PublicationDate = requestBook.PublicationDate ?? DateTime.UtcNow,
                CategoryId = requestBook.CategoryId,
                AuthorId = requestBook.AuthorId,
                Pages = requestBook.Pages
            };

            var bookId = await _bookRepository.CreateBookAsync(newBook);
            var createdBook = await _bookRepository.GetBookByIdAsync(bookId);
            if (createdBook == null)
                throw new Exception("Book creation failed.");

            await _cache.RemoveAsync("book:all");
            return MapToResponseDto(createdBook);
        }
        catch (Exception ex) when (!(ex is BadRequestException || ex is NotFoundException || ex is ConflictException))
        {
            throw new ApplicationException("An unexpected error occurred while creating the book.", ex);
        }
    }

    /// <summary>
    /// Retrieves all books with caching applied for performance.
    /// </summary>
    /// <returns>List of all books as DTOs</returns>
    public async Task<IEnumerable<ResponseBookDto>> GetAllBooksAsync()
    {
        const string cacheKey = "book:all";
        try
        {
            var cached = await _cache.GetAsync<IEnumerable<ResponseBookDto>>(cacheKey);
            if (cached is not null)
                return cached;

            var books = await _bookRepository.GetAllBooksAsync();
            var result = books.Select(MapToResponseDto).ToList();
            await _cache.SetAsync(cacheKey, result);
            return result;
        }
        catch (Exception ex)
        {
            throw new ApplicationException("An error occurred while retrieving all books.", ex);
        }
    }

    /// <summary>
    /// Retrieves a book by ID and caches the result.
    /// </summary>
    /// <param name="id">ID of the book</param>
    /// <returns>Book DTO if found</returns>
    /// <exception cref="NotFoundException">If book does not exist</exception>
    public async Task<ResponseBookDto?> GetBookByIdAsync(int id)
    {
        try
        {
            var cacheKey = $"book:{id}";
            var cached = await _cache.GetAsync<ResponseBookDto>(cacheKey);
            if (cached is not null)
                return cached;

            var book = await _bookRepository.GetBookByIdAsync(id);
            if (book == null)
                throw new NotFoundException($"Book with ID {id} was not found.");

            var dto = MapToResponseDto(book);
            await _cache.SetAsync(cacheKey, dto);
            return dto;
        }
        catch (Exception ex) when (!(ex is NotFoundException))
        {
            throw new ApplicationException("An unexpected error occurred while getting the book.", ex);
        }
    }

    /// <summary>
    /// Updates an existing book record.
    /// </summary>
    /// <param name="id">Book ID</param>
    /// <param name="requestBook">Book data to update</param>
    /// <returns>Updated book data as ResponseBookDto</returns>
    /// <exception cref="NotFoundException">If the book or author is not found</exception>
    public async Task<ResponseBookDto> UpdateBookAsync(int id, RequestBookDto requestBook)
    {
        try
        {
            // Retrieve the existing book from the repository
            var existingBook = await _bookRepository.GetBookByIdAsync(id);
            if (existingBook == null)
                throw new NotFoundException($"Book with ID {id} was not found.");

            // Check if the provided author ID exists
            if (requestBook.AuthorId.HasValue)
            {
                var authorExists = await _bookRepository.CheckAuthorExists(requestBook.AuthorId.Value);
                if (!authorExists)
                    throw new NotFoundException($"Author with ID {requestBook.AuthorId.Value} does not exist.");
            }

            // Create the updated book based on the provided data
            var updatedBook = new Book
            {
                Id = id,
                Title = requestBook.Title ?? existingBook.Title,
                PublicationDate = requestBook.PublicationDate ?? existingBook.PublicationDate,
                CategoryId = requestBook.CategoryId ?? existingBook.CategoryId,
                AuthorId = requestBook.AuthorId ?? existingBook.AuthorId,
                Pages = requestBook.Pages > 0 ? requestBook.Pages : existingBook.Pages
            };

            // Update the book in the repository
            var success = await _bookRepository.UpdateBookAsync(id, updatedBook);

            if (success)
            {
                // Invalidate the cache
                await _cache.RemoveAsync($"book:{id}");
                await _cache.RemoveAsync("book:all");
            }

            var updatedBookWithDetails = await _bookRepository.GetBookByIdAsync(id);

            // Return the updated book as a ResponseBookDto
            return MapToResponseDto(updatedBookWithDetails);
        }
        catch (Exception ex) when (!(ex is BadRequestException || ex is NotFoundException || ex is ConflictException))
        {
            throw new ApplicationException("An unexpected error occurred while updating the book.", ex);
        }
    }

    /// <summary>
    /// Links a book to a specific author if not already linked.
    /// </summary>
    /// <param name="bookId">Book ID</param>
    /// <param name="authorId">Author ID</param>
    /// <exception cref="NotFoundException">If book or author is not found</exception>
    /// <exception cref="ConflictException">If book is already linked</exception>
    public async Task LinkBookToAuthorAsync(int bookId, int authorId)
    {
        try
        {
            var book = await _bookRepository.GetBookByIdAsync(bookId);
            if (book == null)
                throw new NotFoundException($"Book with ID {bookId} was not found.");

            var author = await _authorRepository.GetAuthorByIdAsync(authorId);
            if (author == null)
                throw new NotFoundException($"Author with ID {authorId} was not found.");

            if (book.AuthorId != 0 && book.AuthorId != null)
                throw new ConflictException($"Book with ID {bookId} is already linked to an author.");

            await _bookRepository.LinkBookToAuthorAsync(bookId, authorId);
            await _cache.RemoveAsync($"book:{bookId}");
            await _cache.RemoveAsync("book:all");
        }
        catch (Exception ex) when (!(ex is BadRequestException || ex is NotFoundException || ex is ConflictException))
        {
            throw new ApplicationException("An unexpected error occurred while linking the book.", ex);
        }
    }

    /// <summary>
    /// Links a book to a category if not already linked.
    /// </summary>
    /// <param name="bookId">Book ID</param>
    /// <param name="categoryId">Category ID</param>
    /// <exception cref="NotFoundException">If book or category is not found</exception>
    /// <exception cref="ConflictException">If already linked</exception>
    public async Task LinkBookToCategoryAsync(int bookId, int categoryId)
    {
        try
        {
            var book = await _bookRepository.GetBookByIdAsync(bookId);
            if (book == null)
                throw new NotFoundException($"Book with ID {bookId} was not found.");

            var category = await _categoryRepository.GetCategoryByIdAsync(categoryId);
            if (category == null)
                throw new NotFoundException($"Category with ID {categoryId} was not found.");

            if (book.CategoryId != 0 && book.CategoryId != null)
                throw new ConflictException($"Book with ID {bookId} is already linked to a category.");

            await _bookRepository.LinkBookToCategoryAsync(bookId, categoryId);
            await _cache.RemoveAsync($"book:{bookId}");
            await _cache.RemoveAsync("book:all");
        }
        catch (Exception ex) when (!(ex is BadRequestException || ex is NotFoundException || ex is ConflictException))
        {
            throw new ApplicationException("An unexpected error occurred while linking the book.", ex);
        }
    }

    /// <summary>
    /// Deletes a book and removes its cache.
    /// </summary>
    /// <param name="id">Book ID</param>
    /// <returns>True if deletion was successful</returns>
    /// <exception cref="NotFoundException">If book does not exist</exception>
    public async Task<bool> DeleteBookAsync(int id)
    {
        try
        {
            var bookExists = await _bookRepository.CheckBookExists(id);
            if (!bookExists)
                throw new NotFoundException($"Book with ID {id} was not found.");

            var success = await _bookRepository.DeleteBookAsync(id);
            if (success)
            {
                await _cache.RemoveAsync($"book:{id}");
                await _cache.RemoveAsync("book:all");
            }

            return success;
        }
        catch (Exception ex) when (!(ex is NotFoundException))
        {
            throw new ApplicationException("An unexpected error occurred while deleting the book.", ex);
        }
    }

    /// <summary>
    /// Searches books by various filters: title, category, publication date, author.
    /// </summary>
    /// <param name="title">Title to search</param>
    /// <param name="category">Category name</param>
    /// <param name="publicationDate">Publication date filter</param>
    /// <param name="author">Author name</param>
    /// <returns>Matching books as DTOs</returns>
    public async Task<IEnumerable<ResponseBookDto>> SearchBooksAsync(string? title, string? category,
        DateTime? publicationDate, string? author)
    {
        try
        {
            var books = await _bookRepository.SearchBooksAsync(title, category, publicationDate, author);
            return books.Select(MapToResponseDto);
        }
        catch (Exception ex)
        {
            throw new ApplicationException("An error occurred while searching books.", ex);
        }
    }

    /// <summary>
    /// Converts a Book domain model to ResponseBookDto for output.
    /// </summary>
    /// <param name="book">Book domain model</param>
    /// <returns>Mapped ResponseBookDto</returns>
    private static ResponseBookDto MapToResponseDto(Book book)
    {
        return new ResponseBookDto
        {
            Id = book.Id,
            Title = book.Title,
            PublicationDate = book.PublicationDate,
            CategoryId = book.CategoryId,
            CategoryName = book.Category?.Name ?? "Unknown",
            Pages = book.Pages,
            Author = book.Author != null 
                ? new ResponseAuthorDto
                {
                    Id = book.Author.Id,
                    FirstName = book.Author.FirstName,
                    LastName = book.Author.LastName,
                    Country = book.Author.Country,
                    BooksPublished = book.Author.BooksPublished
                }
                : null
        };
    }
}

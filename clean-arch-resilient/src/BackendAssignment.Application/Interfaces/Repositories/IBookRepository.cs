using BackendAssignment.Domain.Entities;

namespace BackendAssignment.Application.Interfaces.Repositories;

/// <summary>
/// Repository interface for managing books in the data store.
/// </summary>
public interface IBookRepository
{
    Task<int> CreateBookAsync(Book book);
    Task<IEnumerable<Book>> GetAllBooksAsync();
    Task<Book?> GetBookByIdAsync(int id);
    Task<bool> UpdateBookAsync(int id, Book book);
    Task LinkBookToAuthorAsync(int bookId, int authorId);
    Task LinkBookToCategoryAsync(int bookId, int categoryId);
    Task<bool> DeleteBookAsync(int id);
    Task<bool> CheckBookExists(int bookId);
    Task<bool> CheckAuthorExists(int authorId);

    /// <summary>
    /// Search for books using optional filters.
    /// </summary>
    Task<IEnumerable<Book>> SearchBooksAsync(string? title, string? category, DateTime? publicationDate, string? author);

    /// <summary>
    /// Finds a book by its title and author ID.
    /// </summary>
    Task<Book?> GetBookByTitleAndAuthorAsync(string title, int authorId);
}
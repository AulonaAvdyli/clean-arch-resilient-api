using BackendAssignment.Application.DTOs;

namespace BackendAssignment.Application.Interfaces.Services;

/// <summary>
/// Interface Service contract for managing books.
/// </summary>
public interface IBookService
{
    Task<ResponseBookDto> CreateBookAsync(RequestBookDto requestBook);
    Task<IEnumerable<ResponseBookDto>> GetAllBooksAsync();
    Task<ResponseBookDto?> GetBookByIdAsync(int id);
    Task<ResponseBookDto> UpdateBookAsync(int id, RequestBookDto book);
    Task LinkBookToAuthorAsync(int bookId, int authorId);
    Task LinkBookToCategoryAsync(int bookId, int authorId);
    Task<bool> DeleteBookAsync(int id);

    /// <summary>
    /// Searches books with optional filters.
    /// </summary>
    Task<IEnumerable<ResponseBookDto>> SearchBooksAsync(string? title, string? category, DateTime? publicationDate, string? author);
}
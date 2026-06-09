using BackendAssignment.Domain.Entities;

namespace BackendAssignment.Application.Interfaces.Repositories;

/// <summary>
/// Repository interface for author-related data access.
/// </summary>
public interface IAuthorRepository
{
    Task<int> CreateAuthorAsync(Author author);
    Task<IEnumerable<Author>> GetAllAuthorsAsync();
    Task<Author?> GetAuthorByIdAsync(int id);
    Task<bool> UpdateAuthorAsync(int id, Author author);
    Task<bool> DeleteAuthorAsync(int id);
    Task<bool> CheckAuthorExists(int authorId);

    /// <summary>
    /// Finds an author by matching personal details.
    /// </summary>
    Task<Author?> GetAuthorByDetailsAsync(string firstName, string lastName, string country, int? booksPublished);
}
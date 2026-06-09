using BackendAssignment.Domain.Entities;

namespace BackendAssignment.Application.Interfaces.Repositories;

/// <summary>
/// Repository interface for batch inserting books.
/// </summary>
public interface IBulkInsertRepository
{
    Task InsertBooksBatchAsync(List<Book> books);
    Task<List<int>> GetAllAuthorIdsAsync();
    Task<List<int>> GetAllCategoryIdsAsync();
}
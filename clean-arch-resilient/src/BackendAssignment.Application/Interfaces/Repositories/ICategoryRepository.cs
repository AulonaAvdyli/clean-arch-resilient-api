using BackendAssignment.Domain.Entities;

namespace BackendAssignment.Application.Interfaces.Repositories;

/// <summary>
/// Repository interface for managing book categories.
/// </summary>
public interface ICategoryRepository
{
    Task<int> CreateCategoryAsync(Category category);
    Task<IEnumerable<Category>> GetAllCategoriesAsync();
    Task<Category> GetCategoryByIdAsync(int? id);
    Task<bool> UpdateCategoryAsync(int id, Category category);
    Task<bool> DeleteCategoryAsync(int id);
    Task<bool> CheckCategoryExists(int categoryId);

    /// <summary>
    /// Finds a category by its name.
    /// </summary>
    Task<Category?> GetCategoryByNameAsync(string name);
}
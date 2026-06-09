using BackendAssignment.Application.Interfaces.Repositories;
using BackendAssignment.Domain.Entities;
using BackendAssignment.Infrastructure.Helpers;
using Dapper;
using Microsoft.Extensions.Logging;
using System.Data.SqlClient;
using BackendAssignment.Domain.Exceptions;

namespace BackendAssignment.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository responsible for managing CRUD operations related to Category entities using Dapper.
/// </summary>
/// <remarks>
/// Design Decision:
/// - Uses Dapper for efficient data access with full SQL control.
/// - Returns default category when invalid ID is provided to ensure null safety.
/// </remarks>
public class CategoryRepository : ICategoryRepository
{
    private readonly IDatabaseHelper _databaseHelper;
    private readonly ILogger<CategoryRepository> _logger;

    public CategoryRepository(IDatabaseHelper databaseHelper, ILogger<CategoryRepository> logger)
    {
        _databaseHelper = databaseHelper;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new category in the database and returns its generated ID.
    /// </summary>
    public async Task<int> CreateCategoryAsync(Category category)
    {
        try
        {
            const string insertQuery = @"
            INSERT INTO library.categories (name)
            VALUES (@Name)
            RETURNING id;";

            using var connection = _databaseHelper.CreateConnection();
            return await connection.ExecuteScalarAsync<int>(insertQuery, category);
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "Failed to create category due to a database error.");
            throw new RepositoryException("Failed to create category due to a database error.", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred while creating the category.");
            throw new RepositoryException("An unexpected error occurred while creating the category.", ex);
        }
    }

    /// <summary>
    /// Retrieves all categories from the database.
    /// </summary>
    public async Task<IEnumerable<Category>> GetAllCategoriesAsync()
    {
        try
        {
            const string query = "SELECT id, name FROM library.categories;";

            using var connection = _databaseHelper.CreateConnection();
            return await connection.QueryAsync<Category>(query);
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "Failed to retrieve categories due to a database error.");
            throw new RepositoryException("Failed to retrieve categories due to a database error.", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred while retrieving categories.");
            throw new RepositoryException("An unexpected error occurred while retrieving categories.", ex);
        }
    }

    /// <summary>
    /// Retrieves a category by its ID. Returns a default category if ID is invalid or not found.
    /// </summary>
    public async Task<Category> GetCategoryByIdAsync(int? id)
    {
        // Handle invalid category ID early
        if (id == null || id <= 0)
            return new Category { Name = "Unknown" }; // Fallback/default

        try
        {
            const string query = "SELECT id, name FROM library.categories WHERE id = @Id;";

            using var connection = _databaseHelper.CreateConnection();
            var category = await connection.QueryFirstOrDefaultAsync<Category>(query, new { Id = id });

            return category ?? new Category { Name = "Unknown" };
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "Failed to retrieve category by ID due to a database error.");
            throw new RepositoryException($"Failed to retrieve category with ID {id} due to a database error.", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred while retrieving category by ID.");
            throw new RepositoryException($"An unexpected error occurred while retrieving category with ID {id}.", ex);
        }
    }

    /// <summary>
    /// Updates a category name by ID. Skips update if name is null.
    /// </summary>
    public async Task<bool> UpdateCategoryAsync(int id, Category category)
    {
        try
        {
            const string query = @"
            UPDATE library.categories
            SET name = COALESCE(@Name, name)
            WHERE id = @Id;";

            using var connection = _databaseHelper.CreateConnection();
            var rowsAffected = await connection.ExecuteAsync(query, new { Id = id, category.Name });

            return rowsAffected > 0;
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "Failed to update category due to a database error.");
            throw new RepositoryException($"Failed to update category with ID {id} due to a database error.", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred while updating the category.");
            throw new RepositoryException($"An unexpected error occurred while updating category with ID {id}.", ex);
        }
    }

    /// <summary>
    /// Deletes a category from the database by its ID.
    /// </summary>
    public async Task<bool> DeleteCategoryAsync(int id)
    {
        try
        {
            const string query = "DELETE FROM library.categories WHERE id = @Id;";

            using var connection = _databaseHelper.CreateConnection();
            var rowsAffected = await connection.ExecuteAsync(query, new { Id = id });

            return rowsAffected > 0;
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "Failed to delete category due to a database error.");
            throw new RepositoryException($"Failed to delete category with ID {id} due to a database error.", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred while deleting the category.");
            throw new RepositoryException($"An unexpected error occurred while deleting category with ID {id}.", ex);
        }
    }

    /// <summary>
    /// Checks if a category exists by ID.
    /// </summary>
    public async Task<bool> CheckCategoryExists(int categoryId)
    {
        try
        {
            const string query = "SELECT COUNT(*) FROM library.categories WHERE id = @CategoryId;";

            using var connection = _databaseHelper.CreateConnection();
            var exists = await connection.ExecuteScalarAsync<int>(query, new { CategoryId = categoryId });

            return exists > 0;
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "Failed to check if category exists due to a database error.");
            throw new RepositoryException($"Failed to check if category with ID {categoryId} exists due to a database error.", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred while checking if category exists.");
            throw new RepositoryException($"An unexpected error occurred while checking if category with ID {categoryId} exists.", ex);
        }
    }

    /// <summary>
    /// Checks if a category with the specified name already exists.
    /// </summary>
    public async Task<Category?> GetCategoryByNameAsync(string name)
    {
        try
        {
            const string query = "SELECT * FROM library.categories WHERE name = @Name LIMIT 1;";

            using var connection = _databaseHelper.CreateConnection();
            return await connection.QueryFirstOrDefaultAsync<Category>(query, new { Name = name });
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "Failed to retrieve category by name due to a database error.");
            throw new RepositoryException("Failed to retrieve category by name due to a database error.", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred while retrieving category by name.");
            throw new RepositoryException("An unexpected error occurred while retrieving category by name.", ex);
        }
    }
}

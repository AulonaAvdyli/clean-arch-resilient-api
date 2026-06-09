using System.Data.SqlClient;
using BackendAssignment.Application.Interfaces.Repositories;
using BackendAssignment.Domain.Entities;
using BackendAssignment.Domain.Exceptions;
using BackendAssignment.Infrastructure.Helpers;
using Dapper;

namespace BackendAssignment.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository for managing CRUD operations for Author entities using Dapper and raw SQL.
/// </summary>
/// <remarks>
/// Design Decision:
/// - Uses Dapper for performance and full SQL control.
/// - Follows the repository pattern to abstract data access.
/// </remarks>
public class AuthorRepository : IAuthorRepository
{
    private readonly IDatabaseHelper _databaseHelper;

    public AuthorRepository(IDatabaseHelper databaseHelper)
    {
        _databaseHelper = databaseHelper;
    }

    /// <summary>
    /// Inserts a new author into the database and returns the generated ID.
    /// </summary>
    public async Task<int> CreateAuthorAsync(Author author)
    {
        try
        {
            const string insertQuery = @"
        INSERT INTO library.authors (first_name, last_name, country, books_published)
        VALUES (@FirstName, @LastName, @Country, @BooksPublished)
        RETURNING id;";

            using var connection = _databaseHelper.CreateConnection();
            return await connection.ExecuteScalarAsync<int>(insertQuery, author);
        }
        catch (SqlException ex)
        {
            throw new RepositoryException("Failed to create author due to a database error.", ex);
        }
        catch (Exception ex)
        {
            throw new RepositoryException("An unexpected error occurred while creating the author.", ex);
        }
    }


    /// <summary>
    /// Retrieves all authors from the database.
    /// </summary>
    public async Task<IEnumerable<Author>> GetAllAuthorsAsync()
    {
        try
        {
            const string query = @"
            SELECT id, first_name, last_name, country, books_published
            FROM library.authors;";

            using var connection = _databaseHelper.CreateConnection();
            return await connection.QueryAsync<Author>(query);
        }
        catch (SqlException ex)
        {
            throw new RepositoryException("Failed to retrieve authors due to a database error.", ex);
        }
        catch (Exception ex)
        {
            throw new RepositoryException("An unexpected error occurred while retrieving authors.", ex);
        }
    }

    /// <summary>
    /// Retrieves an author by ID.
    /// </summary>
    public async Task<Author?> GetAuthorByIdAsync(int id)
    {
        try
        {
            const string query = @"
            SELECT id, first_name, last_name, country, books_published
            FROM library.authors 
            WHERE id = @Id;";

            using var connection = _databaseHelper.CreateConnection();
            return await connection.QueryFirstOrDefaultAsync<Author>(query, new { Id = id });
        }
        catch (SqlException ex)
        {
            throw new RepositoryException($"Failed to retrieve author with ID {id} due to a database error.", ex);
        }
        catch (Exception ex)
        {
            throw new RepositoryException($"An unexpected error occurred while retrieving author with ID {id}.", ex);
        }
    }

    /// <summary>
    /// Updates an author. Uses COALESCE to keep existing values if new ones are null.
    /// </summary>
    public async Task<bool> UpdateAuthorAsync(int id, Author author)
    {
        try
        {
            const string updateQuery = @"
            UPDATE library.authors
            SET first_name = COALESCE(@FirstName, first_name), 
                last_name = COALESCE(@LastName, last_name), 
                country = COALESCE(@Country, country), 
                books_published = COALESCE(@BooksPublished, books_published)
            WHERE id = @Id;";

            using var connection = _databaseHelper.CreateConnection();
            var rowsAffected = await connection.ExecuteAsync(updateQuery,
                new { Id = id, author.FirstName, author.LastName, author.Country, author.BooksPublished });

            return rowsAffected > 0;
        }
        catch (SqlException ex)
        {
            throw new RepositoryException($"Failed to update author with ID {id} due to a database error.", ex);
        }
        catch (Exception ex)
        {
            throw new RepositoryException($"An unexpected error occurred while updating author with ID {id}.", ex);
        }
    }

    /// <summary>
    /// Deletes an author by ID.
    /// </summary>
    public async Task<bool> DeleteAuthorAsync(int id)
    {
        try
        {
            const string deleteQuery = "DELETE FROM library.authors WHERE id = @Id;";

            using var connection = _databaseHelper.CreateConnection();
            var rowsAffected = await connection.ExecuteAsync(deleteQuery, new { Id = id });

            return rowsAffected > 0;
        }
        catch (SqlException ex)
        {
            throw new RepositoryException($"Failed to delete author with ID {id} due to a database error.", ex);
        }
        catch (Exception ex)
        {
            throw new RepositoryException($"An unexpected error occurred while deleting author with ID {id}.", ex);
        }
    }

    /// <summary>
    /// Checks whether an author exists by ID.
    /// </summary>
    public async Task<bool> CheckAuthorExists(int authorId)
    {
        try
        {
            const string query = "SELECT COUNT(*) FROM library.authors WHERE id = @AuthorId;";

            using var connection = _databaseHelper.CreateConnection();
            var exists = await connection.ExecuteScalarAsync<int>(query, new { AuthorId = authorId });

            return exists > 0;
        }
        catch (SqlException ex)
        {
            throw new RepositoryException($"Failed to check if author with ID {authorId} exists due to a database error.", ex);
        }
        catch (Exception ex)
        {
            throw new RepositoryException($"An unexpected error occurred while checking if author with ID {authorId} exists.", ex);
        }
    }

    /// <summary>
    /// Retrieves an author by first name, last name, country, and books published.
    /// </summary>
    public async Task<Author?> GetAuthorByDetailsAsync(string firstName, string lastName, string country, int? booksPublished)
    {
        try
        {
            const string query = @"
            SELECT * FROM library.authors 
            WHERE first_name = @FirstName 
            AND last_name = @LastName 
            AND country = @Country
            AND books_published = @BooksPublished
            LIMIT 1;";

            using var connection = _databaseHelper.CreateConnection();
            return await connection.QueryFirstOrDefaultAsync<Author>(query, new
            {
                FirstName = firstName,
                LastName = lastName,
                Country = country,
                BooksPublished = booksPublished
            });
        }
        catch (SqlException ex)
        {
            throw new RepositoryException("Failed to retrieve author by details due to a database error.", ex);
        }
        catch (Exception ex)
        {
            throw new RepositoryException("An unexpected error occurred while retrieving author by details.", ex);
        }
    }
}

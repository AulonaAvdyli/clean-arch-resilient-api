using BackendAssignment.Application.Interfaces.Repositories;
using BackendAssignment.Domain.Entities;
using BackendAssignment.Domain.Exceptions;
using BackendAssignment.Infrastructure.Helpers;
using Dapper;
using Microsoft.Extensions.Logging;
using Npgsql;
using NpgsqlTypes;

namespace BackendAssignment.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository responsible for high-performance bulk insert and update operations on Book entities.
/// </summary>
/// <remarks>
/// Design Decisions:
/// - Uses PostgreSQL's COPY command for efficient bulk inserts.
/// - Falls back to batched UPDATEs for duplicate (Title + AuthorId) records.
/// - Runs inside a transaction to ensure atomicity.
/// </remarks>
public class BulkInsertRepository : IBulkInsertRepository
{
    private readonly IDatabaseHelper _databaseHelper;
    private readonly ILogger<BulkInsertRepository> _logger;

    public BulkInsertRepository(IDatabaseHelper databaseHelper, ILogger<BulkInsertRepository> logger)
    {
        _databaseHelper = databaseHelper;
        _logger = logger;
    }

    /// <summary>
    /// Performs bulk insert and update of a batch of books.
    /// - Inserts new books using PostgreSQL's COPY for performance.
    /// - Updates existing books that match by title and author.
    /// </summary>
    /// <param name="books">List of books to process</param>
    public async Task InsertBooksBatchAsync(List<Book> books)
    {
        if (books == null || !books.Any())
        {
            _logger.LogWarning("No books to insert. The batch is empty.");
            return;
        }

        // Open a typed PostgreSQL connection and begin a transaction
        await using var connection = (NpgsqlConnection)_databaseHelper.CreateConnection();
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();

        try
        {
            // Query existing books to detect duplicates
            const string existingBooksQuery = @"
                SELECT title, author_id FROM library.books
                WHERE title = ANY(@Titles) AND author_id = ANY(@AuthorIds)";

            var existingBooks = await connection.QueryAsync<(string Title, int AuthorId)>(
                existingBooksQuery,
                new
                {
                    Titles = books.Select(b => b.Title).ToArray(),
                    AuthorIds = books.Select(b => b.AuthorId).ToArray()
                },
                transaction
            );

            var existingBooksSet = new HashSet<(string, int)>(existingBooks);

            // Split into new and existing books for appropriate action
            var newBooks = books
                .Where(b => b.AuthorId.HasValue && !existingBooksSet.Contains((b.Title, b.AuthorId.Value)))
                .ToList();

            var updateBooks = books
                .Where(b => b.AuthorId.HasValue && existingBooksSet.Contains((b.Title, b.AuthorId.Value)))
                .ToList();

            // Use PostgreSQL COPY for fast bulk insert
            if (newBooks.Any())
            {
                try
                {
                    using var writer = await connection.BeginBinaryImportAsync(
                        "COPY library.books (title, publication_date, category_id, author_id, pages) FROM STDIN (FORMAT BINARY)");

                    foreach (var book in newBooks)
                    {
                        await writer.StartRowAsync();
                        await writer.WriteAsync(book.Title, NpgsqlDbType.Text);
                        await writer.WriteAsync(book.PublicationDate, NpgsqlDbType.Date);
                        await writer.WriteAsync(book.CategoryId ?? (object)DBNull.Value, NpgsqlDbType.Integer);
                        await writer.WriteAsync(book.AuthorId, NpgsqlDbType.Integer);
                        await writer.WriteAsync(book.Pages, NpgsqlDbType.Integer);
                    }

                    await writer.CompleteAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred during bulk insert using COPY.");
                    throw new RepositoryException("Error during bulk insert.", ex);
                }
            }

            // Batch update for existing books (e.g., updated pages or category)
            if (updateBooks.Any())
            {
                const string updateQuery = @"
                    UPDATE library.books 
                    SET publication_date = @PublicationDate, 
                        category_id = @CategoryId, 
                        pages = @Pages
                    WHERE title = @Title AND author_id = @AuthorId";

                try
                {
                    foreach (var book in updateBooks)
                    {
                        await connection.ExecuteAsync(updateQuery, book, transaction);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred during batch update for existing books.");
                    throw new RepositoryException("Error during batch update.", ex);
                }
            }

            await transaction.CommitAsync();
            _logger.LogInformation("Successfully processed batch of {BookCount} books.", books.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inserting/updating book batch");
            await transaction.RollbackAsync();
            throw new RepositoryException("Failed to insert or update books.", ex);
        }
    }

    public async Task<List<int>> GetAllAuthorIdsAsync()
    {
        try
        {
            await using var connection = (NpgsqlConnection)_databaseHelper.CreateConnection();
            var query = "SELECT id FROM library.authors";
            var result = await connection.QueryAsync<int>(query);
            return result.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while retrieving all author IDs.");
            throw new RepositoryException("Failed to retrieve author IDs.", ex);
        }
    }

    public async Task<List<int>> GetAllCategoryIdsAsync()
    {
        try
        {
            await using var connection = (NpgsqlConnection)_databaseHelper.CreateConnection();
            var query = "SELECT id FROM library.categories";
            var result = await connection.QueryAsync<int>(query);
            return result.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while retrieving all category IDs.");
            throw new RepositoryException("Failed to retrieve category IDs.", ex);
        }
    }
}

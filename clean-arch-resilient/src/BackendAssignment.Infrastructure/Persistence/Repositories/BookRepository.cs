using System.Data.SqlClient;
using System.Text;
using BackendAssignment.Application.Interfaces.Repositories;
using BackendAssignment.Domain.Entities;
using BackendAssignment.Domain.Exceptions;
using BackendAssignment.Infrastructure.Helpers;
using Dapper;

namespace BackendAssignment.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository responsible for data access related to Books using raw SQL and Dapper.
/// </summary>
/// <remarks>
/// Design Decision:
/// - Uses Dapper for performance and full SQL control.
/// - Handles complex joins with authors and categories using multi-mapping.
/// </remarks>
public class BookRepository : IBookRepository
{
    private readonly IDatabaseHelper _databaseHelper;

    public BookRepository(IDatabaseHelper databaseHelper)
    {
        _databaseHelper = databaseHelper;
    }

    /// <summary>
    /// Inserts a new book and returns the generated ID.
    /// </summary>
    public async Task<int> CreateBookAsync(Book book)
    {
        try
        {
            const string insertQuery = @"
            INSERT INTO library.books (title, publication_date, category_id, author_id, pages)
            VALUES (@Title, @PublicationDate, @CategoryId, @AuthorId, @Pages)
            RETURNING id;";

            using var connection = _databaseHelper.CreateConnection();
            return await connection.ExecuteScalarAsync<int>(insertQuery, book);
        }
        catch (SqlException ex)
        {
            throw new RepositoryException("Failed to create book due to a database error.", ex);
        }
        catch (Exception ex)
        {
            throw new RepositoryException("An unexpected error occurred while creating the book.", ex);
        }
    }

    /// <summary>
    /// Retrieves all books with author and category info using LEFT JOINs.
    /// </summary>
    public async Task<IEnumerable<Book>> GetAllBooksAsync()
    {
        try
        {
            const string query = @"
            SELECT 
                b.id AS Id, 
                b.title AS Title, 
                b.publication_date AS PublicationDate, 
                b.category_id AS CategoryId, 
                b.pages AS Pages,
                a.id AS Id, 
                a.first_name AS FirstName, 
                a.last_name AS LastName, 
                a.country AS Country, 
                a.books_published AS BooksPublished,
                c.id AS Id, 
                c.name AS Name
            FROM library.books b
            LEFT JOIN library.authors a ON b.author_id = a.id
            LEFT JOIN library.categories c ON b.category_id = c.id;";

            using var connection = _databaseHelper.CreateConnection();
            var bookDictionary = new Dictionary<int, Book>();

            // Use Dapper multi-mapping to map Book + Author + Category
            var books = await connection.QueryAsync<Book, Author, Category, Book>(
                query,
                (book, author, category) =>
                {
                    if (!bookDictionary.TryGetValue(book.Id, out var existingBook))
                    {
                        existingBook = book;
                        existingBook.Author = author;
                        existingBook.Category = category ?? new Category();
                        bookDictionary.Add(existingBook.Id, existingBook);
                    }
                    return existingBook;
                },
                splitOn: "Id,Id" // Splits by Author.Id and Category.Id
            );

            return books.Distinct().ToList();
        }
        catch (SqlException ex)
        {
            throw new RepositoryException("Failed to retrieve books due to a database error.", ex);
        }
        catch (Exception ex)
        {
            throw new RepositoryException("An unexpected error occurred while retrieving books.", ex);
        }
    }

    /// <summary>
    /// Retrieves a book by ID, including its author and category info.
    /// </summary>
    public async Task<Book?> GetBookByIdAsync(int id)
    {
        try
        {
            const string query = @"
            SELECT 
                b.id AS Id, 
                b.title AS Title, 
                b.publication_date AS PublicationDate, 
                b.category_id AS CategoryId, 
                b.pages AS Pages,
                a.id AS Id, 
                a.first_name AS FirstName, 
                a.last_name AS LastName, 
                a.country AS Country, 
                a.books_published AS BooksPublished,
                c.id AS Id, 
                c.name AS Name
            FROM library.books b
            LEFT JOIN library.authors a ON b.author_id = a.id
            LEFT JOIN library.categories c ON b.category_id = c.id
            WHERE b.id = @Id;";

            using var connection = _databaseHelper.CreateConnection();
            var bookDictionary = new Dictionary<int, Book>();

            var books = await connection.QueryAsync<Book, Author, Category, Book>(
                query,
                (book, author, category) =>
                {
                    if (!bookDictionary.TryGetValue(book.Id, out var existingBook))
                    {
                        existingBook = book;
                        existingBook.Author = author;
                        existingBook.Category = category ?? new Category();
                        bookDictionary.Add(existingBook.Id, existingBook);
                    }
                    return existingBook;
                },
                new { Id = id },
                splitOn: "Id,Id"
            );

            return books.FirstOrDefault();
        }
        catch (SqlException ex)
        {
            throw new RepositoryException($"Failed to retrieve book with ID {id} due to a database error.", ex);
        }
        catch (Exception ex)
        {
            throw new RepositoryException($"An unexpected error occurred while retrieving book with ID {id}.", ex);
        }
    }

    /// <summary>
    /// Updates a book record. Fields are optional and retain existing values using COALESCE.
    /// </summary>
    public async Task<bool> UpdateBookAsync(int id, Book book)
    {
        try
        {
            const string query = @"
            UPDATE library.books
            SET 
                title = COALESCE(@Title, title),
                publication_date = COALESCE(@PublicationDate, publication_date),
                category_id = COALESCE(@CategoryId, category_id),
                author_id = COALESCE(@AuthorId, author_id),
                pages = COALESCE(@Pages, pages),
                updated_at = NOW()
            WHERE id = @Id;";

            using var connection = _databaseHelper.CreateConnection();
            var rowsAffected = await connection.ExecuteAsync(query, new
            {
                Id = id,
                book.Title,
                book.PublicationDate,
                book.CategoryId,
                book.AuthorId,
                book.Pages
            });

            return rowsAffected > 0;
        }
        catch (SqlException ex)
        {
            throw new RepositoryException($"Failed to update book with ID {id} due to a database error.", ex);
        }
        catch (Exception ex)
        {
            throw new RepositoryException($"An unexpected error occurred while updating book with ID {id}.", ex);
        }
    }

    /// <summary>
    /// Deletes a book by ID.
    /// </summary>
    public async Task<bool> DeleteBookAsync(int id)
    {
        try
        {
            const string query = "DELETE FROM library.books WHERE id = @Id;";
            using var connection = _databaseHelper.CreateConnection();
            var rowsAffected = await connection.ExecuteAsync(query, new { Id = id });

            return rowsAffected > 0;
        }
        catch (SqlException ex)
        {
            throw new RepositoryException($"Failed to delete book with ID {id} due to a database error.", ex);
        }
        catch (Exception ex)
        {
            throw new RepositoryException($"An unexpected error occurred while deleting book with ID {id}.", ex);
        }
    }

    /// <summary>
    /// Checks if a book exists by its ID.
    /// </summary>
    public async Task<bool> CheckBookExists(int bookId)
    {
        try
        {
            const string query = "SELECT COUNT(*) FROM library.books WHERE id = @Id;";
            using var connection = _databaseHelper.CreateConnection();
            var exists = await connection.ExecuteScalarAsync<int>(query, new { Id = bookId });

            return exists > 0;
        }
        catch (SqlException ex)
        {
            throw new RepositoryException($"Failed to check if book with ID {bookId} exists due to a database error.", ex);
        }
        catch (Exception ex)
        {
            throw new RepositoryException($"An unexpected error occurred while checking if book with ID {bookId} exists.", ex);
        }
    }

    /// <summary>
    /// Checks if an author exists by their ID.
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
    /// Searches for books using optional filters: title, category, author name, and publication date.
    /// </summary>
    public async Task<IEnumerable<Book>> SearchBooksAsync(string? title, string? category, DateTime? publicationDate, string? author)
    {
        var (query, parameters) = SearchQuery(title, category, publicationDate, author);

        using var connection = _databaseHelper.CreateConnection();
        var books = await connection.QueryAsync<Book, Author, Category, Book>(
            query,
            (book, author, category) =>
            {
                book.Author = author;
                book.Category = category ?? new Category();
                return book;
            },
            parameters,
            splitOn: "Id,Id"
        );

        return books.Distinct().ToList();
    }

    /// <summary>
    /// Retrieves a book by title and author ID.
    /// </summary>
    public async Task<Book?> GetBookByTitleAndAuthorAsync(string title, int authorId)
    {
        const string query = @"
        SELECT 
            id, 
            title, 
            publication_date, 
            category_id, 
            author_id, 
            pages
        FROM library.books
        WHERE title = @Title AND author_id = @AuthorId
        LIMIT 1;";

        using var connection = _databaseHelper.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<Book>(query, new { Title = title, AuthorId = authorId });
    }

    /// <summary>
    /// Links a book to a specific author by updating the author_id field.
    /// </summary>
    public async Task LinkBookToAuthorAsync(int bookId, int authorId)
    {
        using var connection = _databaseHelper.CreateConnection();
        const string sql = @"UPDATE library.books SET author_id = @AuthorId WHERE id = @BookId";
        await connection.ExecuteAsync(sql, new { BookId = bookId, AuthorId = authorId });
    }

    /// <summary>
    /// Links a book to a specific category by updating the category_id field.
    /// </summary>
    public async Task LinkBookToCategoryAsync(int bookId, int categoryId)
    {
        using var connection = _databaseHelper.CreateConnection();
        const string sql = @"UPDATE library.books SET category_id = @CategoryId WHERE id = @BookId";
        await connection.ExecuteAsync(sql, new { BookId = bookId, CategoryId = categoryId });
    }

    /// <summary>
    /// Dynamically builds the SQL query and parameters for searching books.
    /// </summary>
    private static (string, DynamicParameters) SearchQuery(string? title, string? category, DateTime? publicationDate, string? author)
    {
        var query = new StringBuilder(@"
        SELECT 
            b.id AS Id, 
            b.title AS Title, 
            b.publication_date AS PublicationDate, 
            b.category_id AS CategoryId, 
            b.pages AS Pages,
            a.id AS Id, 
            a.first_name AS FirstName, 
            a.last_name AS LastName, 
            a.country AS Country, 
            a.books_published AS BooksPublished,
            c.id AS Id, 
            c.name AS Name
        FROM library.books b
        JOIN library.authors a ON b.author_id = a.id
        LEFT JOIN library.categories c ON b.category_id = c.id
        WHERE 1=1 ");

        var parameters = new DynamicParameters();

        if (!string.IsNullOrWhiteSpace(title))
        {
            query.Append(" AND b.title ILIKE '%' || @Title || '%'");
            parameters.Add("Title", title);
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            query.Append(" AND c.name ILIKE '%' || @Category || '%'");
            parameters.Add("Category", category);
        }

        if (publicationDate.HasValue)
        {
            query.Append(" AND b.publication_date = @PublicationDate");
            parameters.Add("PublicationDate", publicationDate.Value);
        }

        if (!string.IsNullOrWhiteSpace(author))
        {
            query.Append(" AND (a.first_name ILIKE '%' || @Author || '%' OR a.last_name ILIKE '%' || @Author || '%')");
            parameters.Add("Author", author);
        }

        return (query.ToString(), parameters);
    }
}

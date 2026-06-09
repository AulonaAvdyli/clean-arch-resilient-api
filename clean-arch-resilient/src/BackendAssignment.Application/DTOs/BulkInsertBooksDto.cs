namespace BackendAssignment.Application.DTOs;

/// <summary>
/// Represents a request for bulk insertion of books.
/// </summary>
public class BulkInsertBooksDto
{
    /// <summary>
    /// List of books to insert.
    /// </summary>
    public List<RequestBookDto> Books { get; set; } = new(); // Default to avoid null during model binding
}
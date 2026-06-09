namespace BackendAssignment.Application.DTOs;

/// <summary>
/// Represents a book returned in API responses.
/// </summary>
public class ResponseBookDto
{
    /// <summary>
    /// Unique identifier for the book.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Title of the book.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Publication date of the book.
    /// </summary>
    public DateTime PublicationDate { get; set; }

    /// <summary>
    /// Optional ID of the book's category.
    /// </summary>
    public int? CategoryId { get; set; }

    /// <summary>
    /// Name of the book's category.
    /// </summary>
    public string CategoryName { get; set; }

    /// <summary>
    /// Number of pages in the book.
    /// </summary>
    public int Pages { get; set; }

    /// <summary>
    /// Information about the book's author.
    /// </summary>
    public ResponseAuthorDto Author { get; set; } = null!;
}
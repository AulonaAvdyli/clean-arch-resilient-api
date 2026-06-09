namespace BackendAssignment.Application.DTOs;

/// <summary>
/// Represents an author returned in API responses.
/// </summary>
public class ResponseAuthorDto
{
    /// <summary>
    /// Unique identifier for the author.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Author's first name.
    /// </summary>
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// Author's last name.
    /// </summary>
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// Country of origin.
    /// </summary>
    public string Country { get; set; } = string.Empty;

    /// <summary>
    /// Number of books published by this author.
    /// </summary>
    public int BooksPublished { get; set; } = 0;
}
using System.ComponentModel.DataAnnotations;

namespace BackendAssignment.Application.DTOs;

/// <summary>
/// Represents data required to create or update an author.
/// </summary>
public class RequestAuthorDto
{
    /// <summary>
    /// The first name of the author.
    /// </summary>
    [Required(ErrorMessage = "First name is required.")]
    [StringLength(100, ErrorMessage = "First name cannot exceed 100 characters.")]
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// The last name of the author.
    /// </summary>
    [Required(ErrorMessage = "Last name is required.")]
    [StringLength(100, ErrorMessage = "Last name cannot exceed 100 characters.")]
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// The country the author is from.
    /// </summary>
    [StringLength(100, ErrorMessage = "Country name cannot exceed 100 characters.")]
    public string Country { get; set; } = string.Empty;

    /// <summary>
    /// The number of books the author has published.
    /// </summary>
    [Range(0, int.MaxValue, ErrorMessage = "Books published must be a non-negative number.")]
    public int? BooksPublished { get; set; }
}
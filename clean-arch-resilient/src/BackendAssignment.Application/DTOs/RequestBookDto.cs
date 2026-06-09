using System.ComponentModel.DataAnnotations;

namespace BackendAssignment.Application.DTOs;

/// <summary>
/// Represents data required to create or update a book.
/// </summary>
public class RequestBookDto
{
    /// <summary>
    /// The title of the book.
    /// </summary>
    [Required(ErrorMessage = "Title is required.")]
    [StringLength(255, ErrorMessage = "Title cannot exceed 255 characters.")]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// The publication date of the book.
    /// </summary>
    [Required(ErrorMessage = "Publication date is required.")]
    public DateTime? PublicationDate { get; set; }

    /// <summary>
    /// The ID of the category this book belongs to.
    /// Optional during initial creation.
    /// </summary>
    public int? CategoryId { get; set; }

    /// <summary>
    /// The ID of the author of this book.
    /// Optional during initial creation.
    /// </summary>
    public int? AuthorId { get; set; }

    /// <summary>
    /// The total number of pages in the book.
    /// </summary>
    [Required(ErrorMessage = "Page count is required.")]
    [Range(1, int.MaxValue, ErrorMessage = "Pages must be greater than 0.")]
    public int Pages { get; set; }
}
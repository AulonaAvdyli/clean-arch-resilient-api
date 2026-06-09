using System.ComponentModel.DataAnnotations;

namespace BackendAssignment.Application.DTOs;

/// <summary>
/// Represents data required to create or update a book category.
/// </summary>
public class RequestCategoryDto
{
    /// <summary>
    /// The name of the category.
    /// </summary>
    [Required(ErrorMessage = "Category name is required.")]
    [StringLength(100, ErrorMessage = "Category name cannot exceed 100 characters.")]
    public string Name { get; set; } = string.Empty;
}
namespace BackendAssignment.Application.DTOs;

/// <summary>
/// Represents a category returned in API responses.
/// </summary>
public class ResponseCategoryDto
{
    /// <summary>
    /// Unique identifier of the category.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Name of the category.
    /// </summary>
    public required string Name { get; set; }
}
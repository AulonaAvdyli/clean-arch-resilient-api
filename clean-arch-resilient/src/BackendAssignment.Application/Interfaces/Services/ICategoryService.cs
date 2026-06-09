using BackendAssignment.Application.DTOs;

namespace BackendAssignment.Application.Interfaces.Services;

/// <summary>
/// Service contract for category-related operations.
/// </summary>
public interface ICategoryService
{
    Task<ResponseCategoryDto> CreateCategoryAsync(RequestCategoryDto categoryDto);
    Task<IEnumerable<ResponseCategoryDto>> GetAllCategoriesAsync();
    Task<ResponseCategoryDto> GetCategoryByIdAsync(int id);
    Task<ResponseCategoryDto> UpdateCategoryAsync(int id, RequestCategoryDto categoryDto);
    Task<bool> DeleteCategoryAsync(int id);
}
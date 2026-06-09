using BackendAssignment.Application.DTOs;
using BackendAssignment.Application.Interfaces.Repositories;
using BackendAssignment.Application.Interfaces.Services;
using BackendAssignment.Domain.Entities;
using BackendAssignment.Domain.Exceptions;

namespace BackendAssignment.Application.Services;

/// <summary>
/// Service responsible for managing categories, including creation, retrieval, update, and deletion.
/// Applies caching for performance optimization and includes validations to ensure data integrity.
/// </summary>
public class CategoryService : ICategoryService
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly ICacheService _cache;

    public CategoryService(ICategoryRepository categoryRepository, ICacheService cache)
    {
        _categoryRepository = categoryRepository;
        _cache = cache;
    }

    /// <summary>
    /// Creates a new category if it doesn't already exist.
    /// </summary>
    public async Task<ResponseCategoryDto> CreateCategoryAsync(RequestCategoryDto requestCategory)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(requestCategory.Name))
                throw new BadRequestException("Category name cannot be empty.");

            var existingCategory = await _categoryRepository.GetCategoryByNameAsync(requestCategory.Name);
            if (existingCategory != null)
                throw new ConflictException($"Category '{requestCategory.Name}' already exists.");

            var newCategory = new Category { Name = requestCategory.Name };
            var categoryId = await _categoryRepository.CreateCategoryAsync(newCategory);

            var createdCategory = await _categoryRepository.GetCategoryByIdAsync(categoryId);
            if (createdCategory == null)
                throw new Exception("Category creation failed.");

            await _cache.RemoveAsync("category:all");

            return MapToResponseDto(createdCategory);
        }
        catch (Exception ex) when (!(ex is BadRequestException || ex is NotFoundException || ex is ConflictException))
        {
            // Log error and throw a new application-specific exception if needed
            throw new ApplicationException("An error occurred while creating the category.", ex);
        }
    }

    /// <summary>
    /// Retrieves all categories with caching applied for performance.
    /// </summary>
    public async Task<IEnumerable<ResponseCategoryDto>> GetAllCategoriesAsync()
    {
        const string cacheKey = "category:all";

        try
        {
            var cached = await _cache.GetAsync<IEnumerable<ResponseCategoryDto>>(cacheKey);
            if (cached is not null)
                return cached;

            var categories = await _categoryRepository.GetAllCategoriesAsync();
            var result = categories.Select(MapToResponseDto).ToList();

            await _cache.SetAsync(cacheKey, result);
            return result;
        }
        catch (Exception ex)
        {
            throw new ApplicationException("An error occurred while retrieving categories.", ex);
        }
    }

    /// <summary>
    /// Retrieves a single category by ID, with cache lookup for optimization.
    /// </summary>
    public async Task<ResponseCategoryDto> GetCategoryByIdAsync(int id)
    {
        var cacheKey = $"category:{id}";

        try
        {
            var cached = await _cache.GetAsync<ResponseCategoryDto>(cacheKey);
            if (cached is not null)
                return cached;

            var category = await _categoryRepository.GetCategoryByIdAsync(id);
            if (category == null)
                throw new NotFoundException($"Category with ID {id} was not found.");

            var dto = MapToResponseDto(category);
            await _cache.SetAsync(cacheKey, dto);

            return dto;
        }
        catch (Exception ex) when (!(ex is NotFoundException))
        {
            throw new ApplicationException($"An error occurred while retrieving category with ID {id}.", ex);
        }
    }

    /// <param name="id">Category ID</param>
    /// <param name="requestCategory">DTO containing new name</param>
    /// <returns>True if update was successful</returns>
    /// <exception cref="NotFoundException">If category does not exist</exception>
    public async Task<ResponseCategoryDto> UpdateCategoryAsync(int id, RequestCategoryDto requestCategory)
    {
        try
        {
            var existingCategory = await _categoryRepository.GetCategoryByIdAsync(id);
            if (existingCategory == null)
                throw new NotFoundException($"Category with ID {id} was not found.");

            var updatedCategory = new Category
            {
                Id = id,
                Name = requestCategory.Name ?? existingCategory.Name
            };

            var success = await _categoryRepository.UpdateCategoryAsync(id, updatedCategory);
        
            if (!success)
                throw new Exception("Failed to update the category.");

            var category = await _categoryRepository.GetCategoryByIdAsync(id);

            await _cache.RemoveAsync($"category:{id}");
            await _cache.RemoveAsync("category:all");

            return new ResponseCategoryDto
            {
                Id = category.Id,
                Name = category.Name
            };
        }
        catch (Exception ex) when (!(ex is BadRequestException || ex is NotFoundException || ex is ConflictException))
        {
            throw new ApplicationException($"An error occurred while updating category with ID {id}.", ex);
        }
    }

    /// <summary>
    /// Deletes a category by ID.
    /// </summary>
    public async Task<bool> DeleteCategoryAsync(int id)
    {
        try
        {
            var categoryExists = await _categoryRepository.CheckCategoryExists(id);
            if (!categoryExists)
                throw new NotFoundException($"Category with ID {id} was not found.");

            var success = await _categoryRepository.DeleteCategoryAsync(id);

            if (success)
            {
                await _cache.RemoveAsync($"category:{id}");
                await _cache.RemoveAsync("category:all");
            }

            return success;
        }
        catch (Exception ex) when (!(ex is NotFoundException))
        {
            throw new ApplicationException($"An error occurred while deleting category with ID {id}.", ex);
        }
    }

    /// <summary>
    /// Helper method to map the domain Category entity to a DTO.
    /// </summary>
    private static ResponseCategoryDto MapToResponseDto(Category category)
    {
        return new ResponseCategoryDto
        {
            Id = category.Id,
            Name = category.Name
        };
    }
}

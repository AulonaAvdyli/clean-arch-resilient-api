using BackendAssignment.Application.DTOs;
using BackendAssignment.Application.Interfaces.Services;
using BackendAssignment.Domain.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BackendAssignment.Api.Controllers;

/// <summary>
/// API controller for managing book categories.
/// </summary>
[Authorize(Roles = "Developer")]
[Route("api/v1/categories")]
[ApiController]
public class CategoriesController : ControllerBase
{
    private readonly ICategoryService _categoryService;

    public CategoriesController(ICategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    /// <summary>
    /// Creates a new category.
    /// </summary>
    /// <param name="requestResponseCategory">Category data</param>
    /// <returns>The created category</returns>
    /// <response code="201">Category successfully created.</response>
    /// <response code="400">Invalid input.</response>
    /// <response code="401">Unauthorized access.</response>
    [Authorize(Policy = "FromGreeceOnly")]
    [HttpPost]
    [ProducesResponseType(typeof(ResponseCategoryDto), 201)]  
    [ProducesResponseType(typeof(ErrorResponse), 400)]   
    [ProducesResponseType(typeof(ErrorResponse), 401)]   
    [ProducesResponseType(typeof(ErrorResponse), 500)]   
    public async Task<ActionResult<ResponseCategoryDto>> CreateCategory([FromBody] RequestCategoryDto requestResponseCategory)
    {
        if (!ModelState.IsValid)
            return BadRequest(new ErrorResponse("Invalid input.", 400));

        try
        {
            var createdCategory = await _categoryService.CreateCategoryAsync(requestResponseCategory);
            return CreatedAtAction(nameof(GetCategoryById), new { id = createdCategory.Id }, createdCategory);
        }
        catch (BadRequestException ex)
        {
            return BadRequest(new ErrorResponse($"Bad request: {ex.Message}", 400));
        }
        catch (ConflictException ex)
        {
            return Conflict(new ErrorResponse($"Conflict: {ex.Message}", 409){ Type = "Conflict" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ErrorResponse($"Error while creating category: {ex.Message}", 500));
        }
    }

    /// <summary>
    /// Gets all available categories.
    /// </summary>
    /// <response code="200">List of categories returned.</response>
    /// <response code="500">Internal server error.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ResponseCategoryDto>), 200)]  
    [ProducesResponseType(typeof(ErrorResponse), 500)]  
    public async Task<ActionResult<IEnumerable<ResponseCategoryDto>>> GetAllCategories()
    {
        try
        {
            var categories = await _categoryService.GetAllCategoriesAsync();
            return Ok(categories);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ErrorResponse($"Error while retrieving categories: {ex.Message}", 500));
        }
    }

    /// <summary>
    /// Gets a specific category by its ID.
    /// </summary>
    /// <param name="id">Category ID</param>
    /// <response code="200">Category found and returned.</response>
    /// <response code="404">Category not found.</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ResponseCategoryDto), 200)]  
    [ProducesResponseType(typeof(ErrorResponse), 404)]  
    public async Task<ActionResult<ResponseCategoryDto>> GetCategoryById(int id)
    {
        try
        {
            var category = await _categoryService.GetCategoryByIdAsync(id);
            if (category == null)
                return NotFound(new ErrorResponse("Category not found.", 404));

            return Ok(category);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ErrorResponse($"Error while retrieving category by ID: {ex.Message}", 500));
        }
    }

    /// <summary>
    /// Updates a category by ID.
    /// </summary>
    /// <param name="id">Category ID</param>
    /// <param name="responseCategory">Updated category data</param>
    /// <response code="200">Category updated successfully.</response>
    /// <response code="404">Category not found.</response>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ErrorResponse), 404)]   
    [ProducesResponseType(typeof(ResponseCategoryDto), 200)]  // Updated category payload
    public async Task<IActionResult> UpdateCategory(int id, [FromBody] RequestCategoryDto responseCategory)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new ErrorResponse("Invalid input.", 400));
        }
        try
        {
            var updatedCategory = await _categoryService.UpdateCategoryAsync(id, responseCategory);
            if (updatedCategory == null)
                return NotFound(new ErrorResponse("Category not found.", 404));

            return Ok(updatedCategory);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new ErrorResponse(ex.Message, 404));
        }
        catch (BadRequestException ex)
        {
            return BadRequest(new ErrorResponse($"Bad request: {ex.Message}", 400));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ErrorResponse($"Error while updating category: {ex.Message}", 500));
        }
    }

    /// <summary>
    /// Deletes a category by ID.
    /// </summary>
    /// <param name="id">Category ID</param>
    /// <response code="200">Category successfully deleted.</response>
    /// <response code="404">Category not found.</response>
    [Authorize(Policy = "FromGreeceOnly")]
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ErrorResponse), 404)]    
    [ProducesResponseType(typeof(ErrorResponse), 500)]
    public async Task<IActionResult> DeleteCategory(int id)
    {
        try
        {
            var deleted = await _categoryService.DeleteCategoryAsync(id);
            if (!deleted)
                return NotFound(new ErrorResponse("Category not found.", 404));

            return Ok(new { Message = "Category deleted successfully", CategoryId = id });  // Return the deleted ID
        }
        catch (NotFoundException ex)
        {
            return NotFound(new ErrorResponse(ex.Message, 404));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ErrorResponse($"Error while deleting category: {ex.Message}", 500));
        }
    }
}

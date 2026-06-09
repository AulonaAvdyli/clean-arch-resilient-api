using BackendAssignment.Api.Controllers;
using BackendAssignment.Application.DTOs;
using BackendAssignment.Application.Interfaces.Services;
using BackendAssignment.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Newtonsoft.Json.Linq;

namespace BackendAssignment.Tests.Api.Controllers;

public class CategoriesControllerTests
{
    private readonly CategoriesController _controller;
    private readonly Mock<ICategoryService> _serviceMock = new();

    public CategoriesControllerTests()
    {
        // Inject the mocked ICategoryService into the controller
        _controller = new CategoriesController(_serviceMock.Object);
    }

    [Fact]
    public async Task CreateCategory_ValidRequest_ReturnsCreatedResult()
    {
        // Arrange: prepare a valid request and expected response
        var request = new RequestCategoryDto { Name = "Test" };
        var response = new ResponseCategoryDto { Id = 1, Name = "Test" };

        // Set up mock to return the expected result
        _serviceMock.Setup(s => s.CreateCategoryAsync(request))
            .ReturnsAsync(response)
            .Verifiable();

        // Act: call the controller method
        var result = await _controller.CreateCategory(request);

        // Assert: should return 201 Created with correct data
        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(201, created.StatusCode);
        Assert.Equal("GetCategoryById", created.ActionName);
        
        var category = Assert.IsType<ResponseCategoryDto>(created.Value);
        Assert.Equal("Test", category.Name);

        _serviceMock.Verify();
    }

    [Fact]
    public async Task CreateCategory_InvalidModel_ReturnsBadRequest()
    {
        // Arrange: simulate invalid model state
        _controller.ModelState.AddModelError("Name", "Required");

        // Act
        var result = await _controller.CreateCategory(new RequestCategoryDto());

        // Assert: should return 400 BadRequest with model errors
        var badResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        var error = Assert.IsType<ErrorResponse>(badResult.Value);
        Assert.Equal(400, error.Status);
        Assert.Equal("Error", error.Type);
        Assert.Contains("Invalid input.", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateCategory_CategoryAlreadyExists_ReturnsConflict()
    {
        // Arrange: simulate category already exists
        var request = new RequestCategoryDto { Name = "Test" };
        _serviceMock.Setup(s => s.CreateCategoryAsync(request))
            .ThrowsAsync(new ConflictException("Category 'Test' already exists."));

        // Act
        var result = await _controller.CreateCategory(request);

        // Assert: should return 409 Conflict
        var conflictResult = Assert.IsType<ConflictObjectResult>(result.Result);
        var error = Assert.IsType<ErrorResponse>(conflictResult.Value);
        Assert.Equal(409, error.Status);
        Assert.Equal("Conflict", error.Type);
        Assert.Contains("Category 'Test' already exists.", error.Message);
    }
    
    [Fact]
    public async Task CreateCategory_Exception_ReturnsInternalServerError()
    {
        var request = new RequestCategoryDto { Name = "NewCategory" };

        _serviceMock.Setup(s => s.CreateCategoryAsync(request))
            .ThrowsAsync(new Exception("Unexpected failure"));

        var result = await _controller.CreateCategory(request);

        var errorResult = Assert.IsType<ObjectResult>(result.Result);
        var error = Assert.IsType<ErrorResponse>(errorResult.Value);
        Assert.Equal(500, errorResult.StatusCode);
        Assert.Contains("Error while creating category", error.Message);
    }

    [Fact]
    public async Task GetAllCategories_ReturnsOk()
    {
        // Arrange: mock a list of categories
        var categories = new List<ResponseCategoryDto>
        {
            new() { Id = 1, Name = "Tech" },
            new() { Id = 2, Name = "Science" }
        };

        _serviceMock.Setup(s => s.GetAllCategoriesAsync()).ReturnsAsync(categories);

        // Act
        var result = await _controller.GetAllCategories();

        // Assert: should return 200 OK with category list
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var returned = Assert.IsAssignableFrom<IEnumerable<ResponseCategoryDto>>(ok.Value);
        Assert.Equal(2, returned.Count());
    }

    [Fact]
    public async Task GetCategoryById_ReturnsOk()
    {
        // Arrange: mock response for specific category
        var response = new ResponseCategoryDto { Id = 1, Name = "Test" };
        _serviceMock.Setup(s => s.GetCategoryByIdAsync(1)).ReturnsAsync(response);

        // Act
        var result = await _controller.GetCategoryById(1);

        // Assert: should return 200 OK with correct data
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var returned = Assert.IsType<ResponseCategoryDto>(ok.Value);
        Assert.Equal(1, returned.Id);
    }
    
    [Fact]
    public async Task GetCategoryById_CategoryNotFound_ReturnsNotFound()
    {
        // Arrange: mock the service to return null
        _serviceMock.Setup(s => s.GetCategoryByIdAsync(999)).ReturnsAsync((ResponseCategoryDto)null);

        // Act
        var result = await _controller.GetCategoryById(999);

        // Assert: should return 404 NotFound
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        var error = Assert.IsType<ErrorResponse>(notFoundResult.Value);
        Assert.Equal(404, error.Status);
        Assert.Equal("Error", error.Type);
        Assert.Contains("Category not found", error.Message);
    }
    
    [Fact]
    public async Task GetCategoryById_Exception_ReturnsInternalServerError()
    {
        _serviceMock.Setup(s => s.GetCategoryByIdAsync(1))
            .ThrowsAsync(new Exception("Something broke"));

        var result = await _controller.GetCategoryById(1);

        var error = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, error.StatusCode);
        Assert.Contains("Error while retrieving category by ID", ((ErrorResponse)error.Value).Message);
    }
    
    [Fact]
    public async Task GetAllCategories_Exception_ReturnsInternalServerError()
    {
        _serviceMock.Setup(s => s.GetAllCategoriesAsync())
            .ThrowsAsync(new Exception("Boom"));

        var result = await _controller.GetAllCategories();

        var error = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, error.StatusCode);
        Assert.Contains("Error while retrieving categories", ((ErrorResponse)error.Value).Message);
    }

    [Fact]
    public async Task UpdateCategory_ReturnsOk()
    {
        // Arrange: simulate successful update
        var updatedCategory = new ResponseCategoryDto { Id = 1, Name = "Updated" };
        _serviceMock.Setup(s => s.UpdateCategoryAsync(1, It.IsAny<RequestCategoryDto>()))
            .ReturnsAsync(updatedCategory)
            .Verifiable();

        // Act
        var result = await _controller.UpdateCategory(1, new RequestCategoryDto { Name = "Updated" });

        // Assert: should return 200 OK with the updated category data
        var okResult = Assert.IsType<OkObjectResult>(result);
        var category = Assert.IsType<ResponseCategoryDto>(okResult.Value);
        Assert.Equal("Updated", category.Name);
        _serviceMock.Verify();
    }

    [Fact]
    public async Task UpdateCategory_CategoryDoesNotExist_ReturnsNotFound()
    {
        // Arrange: simulate category not found
        _serviceMock.Setup(s => s.UpdateCategoryAsync(999, It.IsAny<RequestCategoryDto>()))
            .ReturnsAsync((ResponseCategoryDto)null);

        // Act
        var result = await _controller.UpdateCategory(999, new RequestCategoryDto());

        // Assert: should return 404 NotFound
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        var error = Assert.IsType<ErrorResponse>(notFoundResult.Value);
        Assert.Equal(404, error.Status);
        Assert.Equal("Error", error.Type);
        Assert.Contains("Category not found", error.Message);
    }
    
    [Fact]
    public async Task UpdateCategory_InvalidModel_ReturnsBadRequest()
    {
        // Arrange: simulate invalid category model
        _controller.ModelState.AddModelError("Name", "Required");

        // Act
        var result = await _controller.UpdateCategory(1, new RequestCategoryDto());

        // Assert: should return 400 BadRequest
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result); 
        var error = Assert.IsType<ErrorResponse>(badRequestResult.Value); 
        Assert.Equal(400, error.Status); 
        Assert.Equal("Error", error.Type); 
        Assert.Contains("Invalid input.", error.Message); 
    }
    
    [Fact]
    public async Task UpdateCategory_InternalServerError_Returns500()
    {
        // Arrange: simulate an internal error (e.g., service exception)
        _serviceMock.Setup(s => s.UpdateCategoryAsync(1, It.IsAny<RequestCategoryDto>()))
            .ThrowsAsync(new Exception("Internal error"));

        // Act
        var result = await _controller.UpdateCategory(1, new RequestCategoryDto { Name = "Updated" });

        // Assert: should return 500 Internal Server Error
        var objectResult = Assert.IsType<ObjectResult>(result);
        var errorResponse = Assert.IsType<ErrorResponse>(objectResult.Value);
        Assert.Equal(500, objectResult.StatusCode);
        Assert.Contains("Error while updating category", errorResponse.Message);
    }


    [Fact]
    public async Task DeleteCategory_ReturnsOk()
    {
        // Arrange: simulate successful deletion
        _serviceMock.Setup(s => s.DeleteCategoryAsync(1))
            .ReturnsAsync(true)
            .Verifiable();

        // Act
        var result = await _controller.DeleteCategory(1);

        // Assert: should return 200 OK with the deleted category info
        var okResult = Assert.IsType<OkObjectResult>(result);
        
        // Parse the response using JObject
        var response = JObject.FromObject(okResult.Value);
        
        // Assert the success message and deleted ID
        Assert.Equal("Category deleted successfully", response["Message"].ToString());
        Assert.Equal(1, (int)response["CategoryId"]);
        
        _serviceMock.Verify();
    }

    [Fact]
    public async Task DeleteCategory_CategoryDoesNotExist_ReturnsNotFound()
    {
        // Arrange: simulate delete failure (category not found)
        _serviceMock.Setup(s => s.DeleteCategoryAsync(999))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.DeleteCategory(999);

        // Assert: should return 404 NotFound
        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        var error = Assert.IsType<ErrorResponse>(notFound.Value);
        Assert.Equal(404, error.Status);
        Assert.Equal("Error", error.Type);
        Assert.Contains("not found", error.Message, StringComparison.OrdinalIgnoreCase);
    }
    
    [Fact]
    public async Task DeleteCategory_InternalServerError_Returns500()
    {
        // Arrange: simulate an internal error (e.g., service exception)
        _serviceMock.Setup(s => s.DeleteCategoryAsync(1))
            .ThrowsAsync(new Exception("Internal error"));

        // Act
        var result = await _controller.DeleteCategory(1);

        // Assert: should return 500 Internal Server Error with error details
        var objectResult = Assert.IsType<ObjectResult>(result); 
        var errorResponse = Assert.IsType<ErrorResponse>(objectResult.Value);
        Assert.Equal(500, objectResult.StatusCode);
        Assert.Contains("Error while deleting category", errorResponse.Message);
    }
    
    [Fact]
    public async Task CreateCategory_ServiceReturnsNull_ThrowsException()
    {
        var request = new RequestCategoryDto { Name = "NullCase" };
        _serviceMock.Setup(s => s.CreateCategoryAsync(request)).ReturnsAsync((ResponseCategoryDto)null!);

        var result = await _controller.CreateCategory(request);

        var errorResult = Assert.IsType<ObjectResult>(result.Result);
        var error = Assert.IsType<ErrorResponse>(errorResult.Value);
        Assert.Equal(500, errorResult.StatusCode);
        Assert.Contains("Error while creating category", error.Message);
    }
    
    [Fact]
    public async Task UpdateCategory_ThrowsNotFoundException_ReturnsNotFound()
    {
        // Arrange
        _serviceMock.Setup(s => s.UpdateCategoryAsync(1, It.IsAny<RequestCategoryDto>()))
            .ThrowsAsync(new NotFoundException("Category not found."));

        // Act
        var result = await _controller.UpdateCategory(1, new RequestCategoryDto());

        // Assert
        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        var error = Assert.IsType<ErrorResponse>(notFound.Value);
        Assert.Equal(404, error.Status);
        Assert.Contains("Category not found", error.Message);
    }

    [Fact]
    public async Task DeleteCategory_ThrowsNotFoundException_ReturnsNotFound()
    {
        _serviceMock.Setup(s => s.DeleteCategoryAsync(1))
            .ThrowsAsync(new NotFoundException("Category not found."));

        var result = await _controller.DeleteCategory(1);

        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        var error = Assert.IsType<ErrorResponse>(notFound.Value);
        Assert.Equal(404, error.Status);
        Assert.Contains("Category not found", error.Message);
    }

}

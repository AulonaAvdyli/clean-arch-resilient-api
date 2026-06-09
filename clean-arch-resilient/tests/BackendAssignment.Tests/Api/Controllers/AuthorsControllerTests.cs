using BackendAssignment.Api.Controllers;
using BackendAssignment.Application.DTOs;
using BackendAssignment.Application.Interfaces.Services;
using BackendAssignment.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Newtonsoft.Json.Linq;

namespace BackendAssignment.Tests.Api.Controllers;

public class AuthorsControllerTests
{
    private readonly AuthorsController _controller;
    private readonly Mock<IAuthorService> _serviceMock = new();

    public AuthorsControllerTests()
    {
        // Inject the mocked service into the controller
        _controller = new AuthorsController(_serviceMock.Object);
    }

    [Fact]
    public async Task CreateAuthor_ValidRequest_ReturnsCreated()
    {
        // Arrange: simulate valid input and mock a successful service response
        var request = new RequestAuthorDto { FirstName = "John", LastName = "Doe", Country = "Greece", BooksPublished = 3 };
        var response = new ResponseAuthorDto { Id = 1, FirstName = "John", LastName = "Doe" };

        _serviceMock.Setup(s => s.CreateAuthorAsync(request))
            .ReturnsAsync(response)
            .Verifiable();

        // Act: call the controller method
        var result = await _controller.CreateAuthor(request);

        // Assert: should return 201 Created with correct data
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(201, createdResult.StatusCode);
        Assert.Equal("GetAuthorById", createdResult.ActionName);

        var returnedAuthor = Assert.IsType<ResponseAuthorDto>(createdResult.Value);
        Assert.Equal("John", returnedAuthor.FirstName);

        _serviceMock.Verify();
    }
    
    [Fact]
    public async Task CreateAuthor_BadRequestException_ReturnsBadRequest()
    {
        var request = new RequestAuthorDto { FirstName = "John" };

        _serviceMock.Setup(s => s.CreateAuthorAsync(request))
            .ThrowsAsync(new BadRequestException("Duplicate name"));

        var result = await _controller.CreateAuthor(request);

        var badResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        var error = Assert.IsType<ErrorResponse>(badResult.Value);
        Assert.Contains("Bad request", error.Message);
        Assert.Equal(400, error.Status);
    }
    
    [Fact]
    public async Task CreateAuthor_InvalidModel_ReturnsBadRequest()
    {
        // Arrange
        _controller.ModelState.AddModelError("FirstName", "Required");

        // Act
        var result = await _controller.CreateAuthor(new RequestAuthorDto());

        // Assert
        var badResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        var error = Assert.IsType<ErrorResponse>(badResult.Value);
        Assert.Equal(400, error.Status);
        Assert.Equal("Error", error.Type);
        Assert.Contains("Invalid input.", error.Message);
    }

    [Fact]
    public async Task CreateAuthor_ConflictException_ReturnsConflict()
    {
        var request = new RequestAuthorDto { FirstName = "John" };

        _serviceMock.Setup(s => s.CreateAuthorAsync(request))
            .ThrowsAsync(new ConflictException("Already exists"));

        var result = await _controller.CreateAuthor(request);

        var conflict = Assert.IsType<ConflictObjectResult>(result.Result);
        var error = Assert.IsType<ErrorResponse>(conflict.Value);
        Assert.Equal(409, error.Status);
        Assert.Contains("Conflict", error.Message);
    }
    
    [Fact]
    public async Task GetAllAuthors_ReturnsOk()
    {
        // Arrange: simulate a list of authors
        var list = new List<ResponseAuthorDto>
        {
            new() { Id = 1, FirstName = "A", LastName = "B" },
            new() { Id = 2, FirstName = "C", LastName = "D" }
        };

        _serviceMock.Setup(s => s.GetAllAuthorsAsync()).ReturnsAsync(list);

        // Act
        var result = await _controller.GetAllAuthors();

        // Assert: should return 200 OK with correct list
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var authors = Assert.IsAssignableFrom<IEnumerable<ResponseAuthorDto>>(okResult.Value);
        Assert.Equal(2, authors.Count());
    }

    [Fact]
    public async Task GetAuthorById_ReturnsOk()
    {
        // Arrange: simulate returning a single author
        var dto = new ResponseAuthorDto { Id = 1 };
        _serviceMock.Setup(s => s.GetAuthorByIdAsync(1)).ReturnsAsync(dto);

        // Act
        var result = await _controller.GetAuthorById(1);

        // Assert: should return 200 OK with correct ID
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var author = Assert.IsType<ResponseAuthorDto>(ok.Value);
        Assert.Equal(1, author.Id);
    }

    [Fact]
    public async Task GetAuthorById_ThrowsNotFoundException_ReturnsNotFound()
    {
        _serviceMock.Setup(s => s.GetAuthorByIdAsync(1))
            .ThrowsAsync(new NotFoundException("Author not found."));

        var result = await _controller.GetAuthorById(1);

        var notFound = Assert.IsType<NotFoundObjectResult>(result.Result);
        var error = Assert.IsType<ErrorResponse>(notFound.Value);
        Assert.Equal(404, error.Status);
        Assert.Contains("Author not found", error.Message);
    }
    
    [Fact]
    public async Task GetAuthorById_InternalError_Returns500()
    {
        _serviceMock.Setup(s => s.GetAuthorByIdAsync(1))
            .ThrowsAsync(new Exception("Boom"));

        var result = await _controller.GetAuthorById(1);

        var obj = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, obj.StatusCode);
        var error = Assert.IsType<ErrorResponse>(obj.Value);
        Assert.Contains("Error while retrieving author by ID", error.Message);
    }
    
    [Fact]
    public async Task UpdateAuthor_ReturnsUpdatedAuthor()
    {
        // Arrange: simulate successful update
        var request = new RequestAuthorDto { FirstName = "Updated", LastName = "Name", Country = "Greece", BooksPublished = 10 };
        var response = new ResponseAuthorDto { Id = 1, FirstName = "Updated", LastName = "Name", Country = "Greece", BooksPublished = 10 };

        // Update the mock setup to return the updated author as a ResponseAuthorDto
        _serviceMock.Setup(s => s.UpdateAuthorAsync(1, request))
            .ReturnsAsync(response)
            .Verifiable();

        // Act
        var result = await _controller.UpdateAuthor(1, request);

        // Assert: should return 200 OK with the updated data
        var okResult = Assert.IsType<OkObjectResult>(result);
        var updatedAuthor = Assert.IsType<ResponseAuthorDto>(okResult.Value); 

        // Assert that the updated author's first name is "Updated"
        Assert.Equal("Updated", updatedAuthor.FirstName);
        Assert.Equal("Name", updatedAuthor.LastName);
        Assert.Equal("Greece", updatedAuthor.Country);
        Assert.Equal(10, updatedAuthor.BooksPublished);

        _serviceMock.Verify();
    }
    
    [Fact]
    public async Task UpdateAuthor_BadRequestException_ReturnsBadRequest()
    {
        var request = new RequestAuthorDto { FirstName = "John" };

        _serviceMock.Setup(s => s.UpdateAuthorAsync(1, request))
            .ThrowsAsync(new BadRequestException("Invalid update"));

        var result = await _controller.UpdateAuthor(1, request);

        var badResult = Assert.IsType<BadRequestObjectResult>(result);
        var error = Assert.IsType<ErrorResponse>(badResult.Value);
        Assert.Equal(400, error.Status);
        Assert.Contains("Bad request", error.Message);
    }
    
    [Fact]
    public async Task UpdateAuthor_NotFoundException_Returns404()
    {
        var request = new RequestAuthorDto { FirstName = "John" };

        _serviceMock.Setup(s => s.UpdateAuthorAsync(1, request))
            .ThrowsAsync(new NotFoundException("Not found"));

        var result = await _controller.UpdateAuthor(1, request);

        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        var error = Assert.IsType<ErrorResponse>(notFound.Value);
        Assert.Equal(404, error.Status);
    }
    
    [Fact]
    public async Task UpdateAuthor_InternalError_Returns500()
    {
        var request = new RequestAuthorDto { FirstName = "John" };

        _serviceMock.Setup(s => s.UpdateAuthorAsync(1, request))
            .ThrowsAsync(new Exception("Oops"));

        var result = await _controller.UpdateAuthor(1, request);

        var serverError = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, serverError.StatusCode);
        var error = Assert.IsType<ErrorResponse>(serverError.Value);
        Assert.Contains("Error while updating author", error.Message);
    }

    [Fact]
    public async Task DeleteAuthor_ReturnsOk()
    {
        // Arrange: simulate successful deletion
        _serviceMock.Setup(s => s.DeleteAuthorAsync(1))
            .ReturnsAsync(true)
            .Verifiable();

        // Act
        var result = await _controller.DeleteAuthor(1);

        // Assert: should return 200 OK with the correct response
        var okResult = Assert.IsType<OkObjectResult>(result);
    
        // Parse the response using JObject
        var response = JObject.FromObject(okResult.Value);

        // Assert the success message and deleted ID
        Assert.Equal("Author deleted successfully", response["Message"].ToString());
        Assert.Equal(1, (int)response["AuthorId"]);

        _serviceMock.Verify();
    }
    
    [Fact]
    public async Task DeleteAuthor_AuthorDoesNotExist_ReturnsNotFound()
    {
        // Arrange: simulate delete failure
        _serviceMock.Setup(s => s.DeleteAuthorAsync(999)).ReturnsAsync(false);

        // Act
        var result = await _controller.DeleteAuthor(999);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        var error = Assert.IsType<ErrorResponse>(notFoundResult.Value);
        Assert.Equal(404, error.Status);
        Assert.Equal("Error", error.Type);
        Assert.Contains("not found", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UpdateAuthor_AuthorDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        var request = new RequestAuthorDto { FirstName = "Updated", LastName = "Name", Country = "Greece", BooksPublished = 10 };
        _serviceMock.Setup(s => s.UpdateAuthorAsync(999, request))
            .ReturnsAsync((ResponseAuthorDto)null);  // Simulating the case where the author does not exist

        // Act
        var result = await _controller.UpdateAuthor(999, request);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result); 
        var error = Assert.IsType<ErrorResponse>(notFoundResult.Value); 
        Assert.Equal(404, error.Status);
        Assert.Equal("Error", error.Type);
        Assert.Contains("not found", error.Message, StringComparison.OrdinalIgnoreCase); 
    }
    
    [Fact]
    public async Task CreateAuthor_InternalServerError_Returns500()
    {
        // Arrange: simulate an exception in service layer
        var request = new RequestAuthorDto { FirstName = "John", LastName = "Doe", Country = "Greece", BooksPublished = 3 };
        _serviceMock.Setup(s => s.CreateAuthorAsync(request))
            .ThrowsAsync(new Exception("Internal server error"));

        // Act
        var result = await _controller.CreateAuthor(request);

        // Assert: should return 500 Internal Server Error
        var statusCodeResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, statusCodeResult.StatusCode);
        var error = Assert.IsType<ErrorResponse>(statusCodeResult.Value);
        Assert.Contains("Error while creating author", error.Message);
    }

    [Fact]
    public async Task GetAllAuthors_InternalServerError_Returns500()
    {
        // Arrange: simulate an exception in the service layer
        _serviceMock.Setup(s => s.GetAllAuthorsAsync())
            .ThrowsAsync(new Exception("Internal server error"));

        // Act
        var result = await _controller.GetAllAuthors();

        // Assert: should return 500 Internal Server Error
        var statusCodeResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, statusCodeResult.StatusCode);
        var error = Assert.IsType<ErrorResponse>(statusCodeResult.Value);
        Assert.Contains("Error while retrieving authors", error.Message);
    }
    
    [Fact]
    public async Task DeleteAuthor_InternalError_Returns500()
    {
        _serviceMock.Setup(s => s.DeleteAuthorAsync(1))
            .ThrowsAsync(new Exception("Oops"));

        var result = await _controller.DeleteAuthor(1);

        var serverError = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, serverError.StatusCode);
        var error = Assert.IsType<ErrorResponse>(serverError.Value);
        Assert.Contains("Error while deleting author", error.Message);
    }
}



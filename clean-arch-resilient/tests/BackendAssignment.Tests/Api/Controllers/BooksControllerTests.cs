using BackendAssignment.Api.Controllers;
using BackendAssignment.Application.DTOs;
using BackendAssignment.Application.Interfaces.Services;
using BackendAssignment.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Newtonsoft.Json.Linq;

namespace BackendAssignment.Tests.Api.Controllers;

public class BooksControllerTests
{
    private readonly BooksController _controller;
    private readonly Mock<IBookService> _bookServiceMock = new();
    private readonly Mock<IBulkInsertService> _bulkInsertServiceMock = new();

    public BooksControllerTests()
    {
        // Inject mock services into the controller
        _controller = new BooksController(_bookServiceMock.Object, _bulkInsertServiceMock.Object);
    }

    [Fact]
    public async Task CreateBook_ValidRequest_ReturnsCreated()
    {
        // Arrange: valid book creation request
        var request = new RequestBookDto { Title = "C# Book" };
        var response = new ResponseBookDto { Id = 1, Title = "C# Book" };

        _bookServiceMock.Setup(s => s.CreateBookAsync(request))
            .ReturnsAsync(response)
            .Verifiable();

        // Act
        var result = await _controller.CreateBook(request);

        // Assert: should return 201 with the created book
        var created = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(201, created.StatusCode);
        var book = Assert.IsType<ResponseBookDto>(created.Value);
        Assert.Equal("C# Book", book.Title);
        _bookServiceMock.Verify();
    }

    [Fact]
    public async Task CreateBook_InvalidModel_ReturnsBadRequest()
    {
        // Arrange
        _controller.ModelState.AddModelError("Title", "Required");

        // Act
        var result = await _controller.CreateBook(new RequestBookDto());

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        var error = Assert.IsType<ErrorResponse>(badRequest.Value);
        Assert.Equal(400, error.Status);
        Assert.Equal("Error", error.Type); 
        Assert.Contains("Invalid input.", error.Message);
    }
    
    [Fact]
    public async Task CreateBook_GenericException_ReturnsInternalServerError()
    {
        var request = new RequestBookDto { Title = "C# Book" };

        _bookServiceMock.Setup(s => s.CreateBookAsync(request))
            .ThrowsAsync(new Exception("Database crash"));

        var result = await _controller.CreateBook(request);

        var errorResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, errorResult.StatusCode);
        var error = Assert.IsType<ErrorResponse>(errorResult.Value);
        Assert.Contains("Error while creating book", error.Message);
    }

    [Fact]
    public async Task GetAllBooks_ReturnsOk()
    {
        // Arrange: mock a list of books
        _bookServiceMock.Setup(s => s.GetAllBooksAsync())
            .ReturnsAsync(new List<ResponseBookDto> { new() { Id = 1 } });

        // Act
        var result = await _controller.GetAllBooks();

        // Assert: should return 200 with the book list
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var books = Assert.IsAssignableFrom<IEnumerable<ResponseBookDto>>(ok.Value);
        Assert.Single(books);
    }

    [Fact]
    public async Task GetAllBooks_Exception_Returns500()
    {
        _bookServiceMock.Setup(s => s.GetAllBooksAsync())
            .ThrowsAsync(new Exception("Boom"));

        var result = await _controller.GetAllBooks();

        var error = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, error.StatusCode);
        Assert.Contains("Error while retrieving books", ((ErrorResponse)error.Value).Message);
    }
    
    [Fact]
    public async Task GetBookById_ReturnsOk()
    {
        // Arrange: mock one book
        _bookServiceMock.Setup(s => s.GetBookByIdAsync(1))
            .ReturnsAsync(new ResponseBookDto { Id = 1 });

        // Act
        var result = await _controller.GetBookById(1);

        // Assert: should return 200 with the book data
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var book = Assert.IsType<ResponseBookDto>(ok.Value);
        Assert.Equal(1, book.Id);
    }
    
    [Fact]
    public async Task GetBookById_ThrowsNotFoundException_Returns404()
    {
        _bookServiceMock.Setup(s => s.GetBookByIdAsync(1))
            .ThrowsAsync(new NotFoundException("Book not found"));

        var result = await _controller.GetBookById(1);

        var notFound = Assert.IsType<NotFoundObjectResult>(result.Result);
        var error = Assert.IsType<ErrorResponse>(notFound.Value);
        Assert.Equal(404, error.Status);
        Assert.Contains("Book not found", error.Message);
    }
    
    [Fact]
    public async Task GetBookById_ThrowsGenericException_Returns500()
    {
        _bookServiceMock.Setup(s => s.GetBookByIdAsync(1))
            .ThrowsAsync(new Exception("Unexpected"));

        var result = await _controller.GetBookById(1);

        var error = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, error.StatusCode);
        var err = Assert.IsType<ErrorResponse>(error.Value);
        Assert.Contains("Error while retrieving book by ID", err.Message);
    }

    [Fact]
    public async Task UpdateBook_ReturnsUpdatedBook()
    {
        // Arrange: simulate successful update
        var request = new RequestBookDto { Title = "Updated Book" };
        var response = new ResponseBookDto { Id = 1, Title = "Updated Book" };

        _bookServiceMock.Setup(s => s.UpdateBookAsync(1, It.IsAny<RequestBookDto>()))
            .ReturnsAsync(response)
            .Verifiable();

        // Act
        var result = await _controller.UpdateBook(1, request);

        // Assert: should return 200 OK with the updated book
        var okResult = Assert.IsType<OkObjectResult>(result);
        var updatedBook = Assert.IsType<ResponseBookDto>(okResult.Value);
        Assert.Equal("Updated Book", updatedBook.Title);
        _bookServiceMock.Verify();
    }

    [Fact]
    public async Task UpdateBook_BadRequestException_Returns400()
    {
        var request = new RequestBookDto { Title = "Invalid" };

        _bookServiceMock.Setup(s => s.UpdateBookAsync(1, request))
            .ThrowsAsync(new BadRequestException("Validation failed"));

        var result = await _controller.UpdateBook(1, request);

        var badResult = Assert.IsType<BadRequestObjectResult>(result);
        var error = Assert.IsType<ErrorResponse>(badResult.Value);
        Assert.Equal(400, error.Status);
        Assert.Contains("Validation failed", error.Message);
    }
    
    [Fact]
    public async Task UpdateBook_BookDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        var request = new RequestBookDto { Title = "Updated Book" };

        _bookServiceMock.Setup(s => s.UpdateBookAsync(999, It.IsAny<RequestBookDto>()))
            .ReturnsAsync(null as ResponseBookDto);

        // Act
        var result = await _controller.UpdateBook(999, request);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        var error = Assert.IsType<ErrorResponse>(notFoundResult.Value);
        Assert.Equal(404, error.Status);
        Assert.Equal("Error", error.Type);
        Assert.Contains("not found", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UpdateBook_NotFoundException_Returns404()
    {
        var request = new RequestBookDto { Title = "Missing" };

        _bookServiceMock.Setup(s => s.UpdateBookAsync(1, request))
            .ThrowsAsync(new NotFoundException("Not found"));

        var result = await _controller.UpdateBook(1, request);

        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        var error = Assert.IsType<ErrorResponse>(notFound.Value);
        Assert.Equal(404, error.Status);
        Assert.Contains("Not found", error.Message);
    }
    
    [Fact]
    public async Task UpdateBook_Exception_Returns500()
    {
        var request = new RequestBookDto { Title = "Something" };

        _bookServiceMock.Setup(s => s.UpdateBookAsync(1, request))
            .ThrowsAsync(new Exception("DB crashed"));

        var result = await _controller.UpdateBook(1, request);

        var serverError = Assert.IsType<ObjectResult>(result);
        var error = Assert.IsType<ErrorResponse>(serverError.Value);
        Assert.Equal(500, serverError.StatusCode);
        Assert.Contains("Error while updating book", error.Message);
    }

    
    [Fact]
    public async Task DeleteBook_ReturnsOK()
    {
        // Arrange: simulate successful deletion
        _bookServiceMock.Setup(s => s.DeleteBookAsync(1))
            .ReturnsAsync(true)
            .Verifiable();

        // Act
        var result = await _controller.DeleteBook(1);
        
        // Assert: should return 200 OK with the correct response
        var okResult = Assert.IsType<OkObjectResult>(result);
    
        // Parse the response using JObject
        var response = JObject.FromObject(okResult.Value);
        
        // Assert the success message and deleted ID
        Assert.Equal("Book deleted successfully", response["Message"].ToString());
        Assert.Equal(1, (int)response["BookId"]);

        _bookServiceMock.Verify();
    }
    
    [Fact]
    public async Task DeleteBook_BookDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        _bookServiceMock.Setup(s => s.DeleteBookAsync(999)).ReturnsAsync(false);

        // Act
        var result = await _controller.DeleteBook(999);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        var error = Assert.IsType<ErrorResponse>(notFoundResult.Value);
        Assert.Equal(404, error.Status);
        Assert.Equal("Error", error.Type);
        Assert.Contains("not found", error.Message, StringComparison.OrdinalIgnoreCase);
    }
    
    [Fact]
    public async Task DeleteBook_ThrowsException_Returns500()
    {
        _bookServiceMock.Setup(s => s.DeleteBookAsync(1))
            .ThrowsAsync(new Exception("Unexpected"));

        var result = await _controller.DeleteBook(1);

        var error = Assert.IsType<ObjectResult>(result);
        var err = Assert.IsType<ErrorResponse>(error.Value);
        Assert.Equal(500, error.StatusCode);
        Assert.Contains("Error while deleting book", err.Message);
    }

    [Fact]
    public async Task SearchBooks_ReturnsOk()
    {
        // Arrange: simulate search result with one book
        _bookServiceMock.Setup(s => s.SearchBooksAsync("C#", null, null, null))
            .ReturnsAsync(new List<ResponseBookDto> { new() { Id = 1, Title = "C# Book" } });

        // Act
        var result = await _controller.SearchBooks("C#", null, null, null);

        // Assert: should return 200 OK with search results
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var books = Assert.IsAssignableFrom<IEnumerable<ResponseBookDto>>(ok.Value);
        Assert.Single(books);
    }

    [Fact]
    public async Task SearchBooks_Exception_Returns500()
    {
        _bookServiceMock.Setup(s => s.SearchBooksAsync(null, null, null, null))
            .ThrowsAsync(new Exception("Unexpected"));

        var result = await _controller.SearchBooks(null, null, null, null);

        var error = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, error.StatusCode);
        var err = Assert.IsType<ErrorResponse>(error.Value);
        Assert.Contains("Error while searching books", err.Message);
    }
    
    [Fact]
    public async Task LinkBookToAuthor_ReturnsOk()
    {
        // Arrange: simulate linking book to author
        _bookServiceMock.Setup(s => s.LinkBookToAuthorAsync(1, 2))
            .Returns(Task.CompletedTask)
            .Verifiable();

        // Act
        var result = await _controller.LinkBookToAuthor(1, 2);

        // Assert: should return 200 OK with the correct response
        var okResult = Assert.IsType<OkObjectResult>(result);

        // Parse the response using JObject
        var response = JObject.FromObject(okResult.Value);

        // Assert the success message and deleted ID
        Assert.Equal("Book linked to author successfully", response["Message"]);

        _bookServiceMock.Verify();
    }
    
    [Fact]
    public async Task LinkBookToAuthor_Exception_Returns500()
    {
        _bookServiceMock.Setup(s => s.LinkBookToAuthorAsync(1, 2))
            .ThrowsAsync(new Exception("Unexpected"));

        var result = await _controller.LinkBookToAuthor(1, 2);

        var error = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, error.StatusCode);
        Assert.Contains("Error while linking book to author", ((ErrorResponse)error.Value).Message);
    }

    [Fact]
    public async Task LinkBookToCategory_ReturnsOK()
    {
        // Arrange: simulate linking book to category
        _bookServiceMock.Setup(s => s.LinkBookToCategoryAsync(1, 3))
            .Returns(Task.CompletedTask)
            .Verifiable();

        // Act
        var result = await _controller.LinkBookToCategory(1, 3);
        
        // Assert: should return 200 OK with the correct response
        var okResult = Assert.IsType<OkObjectResult>(result);

        // Parse the response using JObject
        var response = JObject.FromObject(okResult.Value);

        // Assert the success message and deleted ID
        Assert.Equal("Book linked to category successfully", response["Message"]);

        _bookServiceMock.Verify();
    }

    [Fact]
    public async Task LinkBookToCategory_Exception_Returns500()
    {
        _bookServiceMock.Setup(s => s.LinkBookToCategoryAsync(1, 3))
            .ThrowsAsync(new Exception("Unexpected"));

        var result = await _controller.LinkBookToCategory(1, 3);

        var error = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, error.StatusCode);
        Assert.Contains("Error while linking book to category", ((ErrorResponse)error.Value).Message);
    }
    
    [Fact]
    public async Task BulkInsertBooks_ReturnsAccepted()
    {
        // Arrange: simulate background job created
        var request = new BulkInsertBooksDto { Books = [] };
        var jobId = Guid.NewGuid();

        _bulkInsertServiceMock.Setup(s => s.BulkInsertBooksAsync(request))
            .ReturnsAsync(jobId)
            .Verifiable();

        // Act
        var result = await _controller.BulkInsertBooks(request);

        // Assert: should return 202 Accepted with job info
        var accepted = Assert.IsType<AcceptedResult>(result);
        var body = Assert.IsType<BulkInsertJobResponse>(accepted.Value);

        Assert.Equal(jobId, body.JobId);
        Assert.Equal("Bulk insert job queued. Check Hangfire dashboard for progress.", body.Message);
        _bulkInsertServiceMock.Verify();
    }
    
    [Fact]
    public async Task BulkInsertBooks_Exception_Returns500()
    {
        var request = new BulkInsertBooksDto { Books = [] };

        _bulkInsertServiceMock.Setup(s => s.BulkInsertBooksAsync(request))
            .ThrowsAsync(new Exception("Insert fail"));

        var result = await _controller.BulkInsertBooks(request);

        var error = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, error.StatusCode);
        var err = Assert.IsType<ErrorResponse>(error.Value);
        Assert.Contains("Error while processing bulk insert", err.Message);
    }

    [Fact]
    public async Task GetJobStatus_ReturnsOk()
    {
        // Arrange: simulate job status check
        _bulkInsertServiceMock.Setup(s => s.GetJobStatusAsync(It.IsAny<Guid>()))
            .ReturnsAsync("Completed");

        // Act
        var result = await _controller.GetJobStatus(Guid.NewGuid());

        // Assert: should return 200 OK with status string
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal("Completed", ok.Value);
    }
    
    [Fact]
    public async Task GetJobStatus_Exception_Returns500()
    {
        _bulkInsertServiceMock.Setup(s => s.GetJobStatusAsync(It.IsAny<Guid>()))
            .ThrowsAsync(new Exception("Unknown error"));

        var result = await _controller.GetJobStatus(Guid.NewGuid());

        var error = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, error.StatusCode);
        Assert.Contains("Error while retrieving bulk job status", ((ErrorResponse)error.Value).Message);
    }
    
    [Fact]
    public async Task CreateBook_BadRequestException_ReturnsBadRequest()
    {
        // Arrange
        var request = new RequestBookDto { Title = "C# Book" };

        _bookServiceMock.Setup(s => s.CreateBookAsync(request))
            .ThrowsAsync(new BadRequestException("Invalid book data"))
            .Verifiable();

        // Act
        var result = await _controller.CreateBook(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var error = Assert.IsType<ErrorResponse>(badRequestResult.Value);
        Assert.Equal(400, error.Status);
        Assert.Contains("Bad request", error.Message);
    }
    
    [Fact]
    public async Task CreateBook_ConflictException_ReturnsConflict()
    {
        // Arrange
        var request = new RequestBookDto { Title = "C# Book" };

        _bookServiceMock.Setup(s => s.CreateBookAsync(request))
            .ThrowsAsync(new ConflictException("Book already exists"))
            .Verifiable();

        // Act
        var result = await _controller.CreateBook(request);

        // Assert
        var conflictResult = Assert.IsType<ConflictObjectResult>(result);
        var error = Assert.IsType<ErrorResponse>(conflictResult.Value);
        Assert.Equal(409, error.Status);
        Assert.Contains("Conflict", error.Message);
    }

    [Fact]
    public async Task LinkBookToAuthor_NotFoundException_ReturnsNotFound()
    {
        // Arrange
        _bookServiceMock.Setup(s => s.LinkBookToAuthorAsync(1, 2))
            .ThrowsAsync(new NotFoundException("Author not found"))
            .Verifiable();

        // Act
        var result = await _controller.LinkBookToAuthor(1, 2);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        var error = Assert.IsType<ErrorResponse>(notFoundResult.Value);
        Assert.Equal(404, error.Status);
        Assert.Contains("Author not found", error.Message);
    }

    [Fact]
    public async Task LinkBookToCategory_NotFoundException_ReturnsNotFound()
    {
        // Arrange
        _bookServiceMock.Setup(s => s.LinkBookToCategoryAsync(1, 3))
            .ThrowsAsync(new NotFoundException("Category not found"))
            .Verifiable();

        // Act
        var result = await _controller.LinkBookToCategory(1, 3);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        var error = Assert.IsType<ErrorResponse>(notFoundResult.Value);
        Assert.Equal(404, error.Status);
        Assert.Contains("Category not found", error.Message);
    }

    [Fact]
    public async Task LinkBookToAuthor_ConflictException_ReturnsConflict()
    {
        // Arrange
        _bookServiceMock.Setup(s => s.LinkBookToAuthorAsync(1, 2))
            .ThrowsAsync(new ConflictException("Book is already linked to an author"))
            .Verifiable();

        // Act
        var result = await _controller.LinkBookToAuthor(1, 2);

        // Assert
        var conflictResult = Assert.IsType<ConflictObjectResult>(result);
        var error = Assert.IsType<ErrorResponse>(conflictResult.Value);
        Assert.Equal(409, error.Status);
        Assert.Contains("already linked to an author", error.Message);
    }

    [Fact]
    public async Task LinkBookToCategory_ConflictException_ReturnsConflict()
    {
        // Arrange
        _bookServiceMock.Setup(s => s.LinkBookToCategoryAsync(1, 3))
            .ThrowsAsync(new ConflictException("Book is already linked to a category"))
            .Verifiable();

        // Act
        var result = await _controller.LinkBookToCategory(1, 3);

        // Assert
        var conflictResult = Assert.IsType<ConflictObjectResult>(result);
        var error = Assert.IsType<ErrorResponse>(conflictResult.Value);
        Assert.Equal(409, error.Status);
        Assert.Contains("already linked to a category", error.Message);
    }
}


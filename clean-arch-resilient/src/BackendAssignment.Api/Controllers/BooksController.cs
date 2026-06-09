using BackendAssignment.Application.DTOs;
using BackendAssignment.Application.Interfaces.Services;
using BackendAssignment.Domain.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BackendAssignment.Api.Controllers;

/// <summary>
/// API controller for managing books.
/// </summary>
[Route("api/v1/books")]
[ApiController]
public class BooksController : ControllerBase
{
    private readonly IBookService _bookService;
    private readonly IBulkInsertService _bulkInsertService;

    public BooksController(IBookService bookService, IBulkInsertService bulkInsertService)
    {
        _bookService = bookService;
        _bulkInsertService = bulkInsertService;
    }

    /// <summary>
    /// Creates a new book.
    /// </summary>
    /// <param name="requestBook">Book data</param>
    /// <returns>The created book</returns>
    /// <response code="201">Book successfully created.</response>
    /// <response code="400">Invalid input.</response>
    /// <response code="401">Unauthorized access.</response>
    [HttpPost]
    [Authorize(Roles = "Developer")]
    [Authorize(Policy = "FromGreeceOnly")]
    [ProducesResponseType(typeof(ResponseBookDto), 201)]  
    [ProducesResponseType(typeof(ErrorResponse), 400)]    
    [ProducesResponseType(typeof(ErrorResponse), 401)]    
    [ProducesResponseType(typeof(ErrorResponse), 500)]    
    public async Task<IActionResult> CreateBook([FromBody] RequestBookDto requestBook)
    {
        if (!ModelState.IsValid)
            return BadRequest(new ErrorResponse("Invalid input.", 400));

        try
        {
            var createdBook = await _bookService.CreateBookAsync(requestBook);
            return CreatedAtAction(nameof(GetBookById), new { id = createdBook.Id }, createdBook);
        }
        catch (BadRequestException ex)
        {
            return BadRequest(new ErrorResponse($"Bad request: {ex.Message}", 400));
        }
        catch (ConflictException ex)
        {
            return Conflict(new ErrorResponse($"Conflict: {ex.Message}", 409));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ErrorResponse($"Error while creating book: {ex.Message}", 500));
        }
    }

    /// <summary>
    /// Retrieves all books.
    /// </summary>
    /// <returns>A list of books.</returns>
    /// <response code="200">List of books returned.</response>
    /// <response code="401">Unauthorized access.</response>
    [Authorize(Roles = "User,Developer")]
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ResponseBookDto>), 200)]  
    [ProducesResponseType(typeof(ErrorResponse), 401)]  
    public async Task<ActionResult<IEnumerable<ResponseBookDto>>> GetAllBooks()
    {
        try
        {
            return Ok(await _bookService.GetAllBooksAsync());
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ErrorResponse($"Error while retrieving books: {ex.Message}", 500));
        }
    }

    /// <summary>
    /// Retrieves a book by its ID.
    /// </summary>
    /// <param name="id">Book ID</param>
    /// <response code="200">Book found and returned.</response>
    /// <response code="404">Book not found.</response>
    [Authorize(Roles = "User,Developer")]
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ResponseBookDto), 200)]  
    [ProducesResponseType(typeof(ErrorResponse), 404)]    
    public async Task<ActionResult<ResponseBookDto>> GetBookById(int id)
    {
        try
        {
            var book = await _bookService.GetBookByIdAsync(id);
            if (book == null)
                return NotFound(new ErrorResponse("Book not found.", 404));

            return Ok(book);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new ErrorResponse(ex.Message, 404));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ErrorResponse($"Error while retrieving book by ID: {ex.Message}", 500));
        }
    }

    /// <summary>
    /// Updates an existing book.
    /// </summary>
    /// <param name="id">Book ID</param>
    /// <param name="book">Updated book data</param>
    /// <response code="200">Book updated successfully.</response>
    /// <response code="404">Book not found.</response>
    [Authorize(Roles = "Developer")]
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ErrorResponse), 404)]   
    [ProducesResponseType(typeof(ResponseBookDto), 200)]  // Updated book payload
    public async Task<IActionResult> UpdateBook(int id, [FromBody] RequestBookDto book)
    {
        try
        {
            var updatedBook = await _bookService.UpdateBookAsync(id, book);
            if (updatedBook == null)
                return NotFound(new ErrorResponse("Book not found.", 404));

            return Ok(updatedBook); 
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
            return StatusCode(500, new ErrorResponse($"Error while updating book: {ex.Message}", 500));
        }
    }

    /// <summary>
    /// Deletes a book.
    /// </summary>
    /// <param name="id">Book ID</param>
    /// <response code="200">Book deleted successfully.</response>
    /// <response code="404">Book not found.</response>
    [Authorize(Roles = "Developer")]
    [Authorize(Policy = "FromGreeceOnly")]
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ErrorResponse), 404)]    
    public async Task<IActionResult> DeleteBook(int id)
    {
        try
        {
            var deleted = await _bookService.DeleteBookAsync(id);
            if (!deleted)
                return NotFound(new ErrorResponse("Book not found.", 404));

            return Ok(new { Message = "Book deleted successfully", BookId = id });
        }
        catch (NotFoundException ex)
        {
            return NotFound(new ErrorResponse(ex.Message, 404));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ErrorResponse($"Error while deleting book: {ex.Message}", 500));
        }
    }

    /// <summary>
    /// Searches for books by title, category, publication date, or author.
    /// </summary>
    [Authorize(Roles = "User,Developer")]
    [HttpGet("search")]
    public async Task<ActionResult<IEnumerable<ResponseBookDto>>> SearchBooks(
        [FromQuery] string? title,
        [FromQuery] string? category,
        [FromQuery] DateTime? publicationDate,
        [FromQuery] string? author)
    {
        try
        {
            var books = await _bookService.SearchBooksAsync(title, category, publicationDate, author);
            return Ok(books);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ErrorResponse($"Error while searching books: {ex.Message}", 500));
        }
    }

    /// <summary>
    /// Links a book to an author.
    /// </summary>
    /// <param name="bookId">Book ID</param>
    /// <param name="authorId">Author ID</param>
    [Authorize(Roles = "Developer")]
    [HttpPatch("{bookId}/authors/{authorId}")]
    public async Task<IActionResult> LinkBookToAuthor(int bookId, int authorId)
    {
        try
        {
            await _bookService.LinkBookToAuthorAsync(bookId, authorId);
            return Ok(new { Message = "Book linked to author successfully", BookId = bookId, AuthorId = authorId });
        }
        catch (NotFoundException ex)
        {
            return NotFound(new ErrorResponse(ex.Message, 404));
        }
        catch (ConflictException ex)
        {
            return Conflict(new ErrorResponse(ex.Message, 409));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ErrorResponse($"Error while linking book to author: {ex.Message}", 500));
        }
    }

    /// <summary>
    /// Links a book to a category.
    /// </summary>
    /// <param name="bookId">Book ID</param>
    /// <param name="categoryId">Category ID</param>
    [Authorize(Roles = "Developer")]
    [HttpPatch("{bookId}/category/{categoryId}")]
    public async Task<IActionResult> LinkBookToCategory(int bookId, int categoryId)
    {
        try 
        {
            await _bookService.LinkBookToCategoryAsync(bookId, categoryId);
            return Ok(new { Message = "Book linked to category successfully", BookId = bookId, CategoryId = categoryId });
        }
        catch (NotFoundException ex)
        {
            return NotFound(new ErrorResponse(ex.Message, 404));
        }
        catch (ConflictException ex)
        {
            return Conflict(new ErrorResponse(ex.Message, 409));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ErrorResponse($"Error while linking book to category: {ex.Message}", 500));
        }
    }

    /// <summary>
    /// Submits a background job for bulk book insertion.
    /// </summary>
    /// <param name="bulkRequest">List of books to insert</param>
    /// <response code="202">Bulk insert job successfully queued.</response>
    [Authorize(Roles = "Developer")]
    [HttpPost("bulk-insert")]
    [ProducesResponseType(typeof(BulkInsertJobResponse), 202)]   
    public async Task<IActionResult> BulkInsertBooks([FromBody] BulkInsertBooksDto bulkRequest)
    {
        try
        {
            var jobId = await _bulkInsertService.BulkInsertBooksAsync(bulkRequest);
            return Accepted(new BulkInsertJobResponse(
                jobId,
                "Bulk insert job queued. Check Hangfire dashboard for progress."
            ));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ErrorResponse($"Error while processing bulk insert: {ex.Message}", 500));
        }
    }

    /// <summary>
    /// Gets the status of a bulk insert job.
    /// </summary>
    /// <param name="jobId">Job ID</param>
    [Authorize(Roles = "Developer")]
    [HttpGet("bulk-status/{jobId}")]
    public async Task<IActionResult> GetJobStatus(Guid jobId)
    {
        try
        {
            var status = await _bulkInsertService.GetJobStatusAsync(jobId);
            return Ok(status);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ErrorResponse($"Error while retrieving bulk job status: {ex.Message}", 500));
        }
    }
}

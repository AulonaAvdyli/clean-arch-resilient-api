using BackendAssignment.Application.DTOs;
using BackendAssignment.Application.Interfaces.Services;
using BackendAssignment.Domain.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BackendAssignment.Api.Controllers;

/// <summary>
/// Controller responsible for managing authors.
/// </summary>
[Route("api/v1/authors")]
[ApiController]
public class AuthorsController : ControllerBase
{
    private readonly IAuthorService _authorService;

    public AuthorsController(IAuthorService authorService)
    {
        _authorService = authorService;
    }

    /// <summary>
    /// Creates a new author.
    /// </summary>
    /// <param name="requestAuthor">The author creation payload.</param>
    /// <returns>The created author.</returns>
    /// <remarks>
    /// Example of a successful request body:
    /// 
    /// {
    ///     "firstName": "John",
    ///     "lastName": "Doe",
    ///     "country": "USA",
    ///     "booksPublished": 5
    /// }
    /// </remarks>
    /// <response code="201">Author successfully created.</response>
    /// <response code="400">Invalid input.</response>
    /// <response code="401">Unauthorized access.</response>
    [HttpPost]
    [Authorize(Roles = "Developer")]
    [Authorize(Policy = "FromGreeceOnly")]
    [ProducesResponseType(typeof(ResponseAuthorDto), 201)]  // Success
    [ProducesResponseType(typeof(ErrorResponse), 400)]    // Bad Request
    [ProducesResponseType(typeof(ErrorResponse), 401)]    // Unauthorized
    public async Task<ActionResult<ResponseAuthorDto>> CreateAuthor([FromBody] RequestAuthorDto requestAuthor)
    {
        if (!ModelState.IsValid)
            return BadRequest(new ErrorResponse("Invalid input.", 400));

        try
        {
            var responseAuthorDto = await _authorService.CreateAuthorAsync(requestAuthor);
            return CreatedAtAction(nameof(GetAuthorById), new { id = responseAuthorDto.Id }, responseAuthorDto);
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
            return StatusCode(500, new ErrorResponse($"Error while creating author: {ex.Message}", 500));
        }
    }

    /// <summary>
    /// Retrieves all authors.
    /// </summary>
    /// <returns>A list of authors.</returns>
    /// <response code="200">List of authors returned.</response>
    /// <response code="401">Unauthorized access.</response>
    [Authorize(Roles = "User,Developer")]
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ResponseAuthorDto>), 200)]  // Success
    [ProducesResponseType(typeof(ErrorResponse), 401)]    // Unauthorized
    public async Task<ActionResult<IEnumerable<ResponseAuthorDto>>> GetAllAuthors()
    {
        try
        {
            return Ok(await _authorService.GetAllAuthorsAsync());
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ErrorResponse($"Error while retrieving authors: {ex.Message}", 500));
        }
    }

    /// <summary>
    /// Retrieves a specific author by ID.
    /// </summary>
    /// <param name="id">The ID of the author.</param>
    /// <returns>The author if found.</returns>
    /// <response code="200">Author found and returned.</response>
    /// <response code="404">Author not found.</response>
    [Authorize(Roles = "User,Developer")]
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ResponseAuthorDto), 200)]  // Success
    [ProducesResponseType(typeof(ErrorResponse), 404)]    // Not Found
    public async Task<ActionResult<ResponseAuthorDto>> GetAuthorById(int id)
    {
        try
        {
            var author = await _authorService.GetAuthorByIdAsync(id);
            if (author == null)
                return NotFound(new ErrorResponse("Author not found.", 404));

            return Ok(author);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new ErrorResponse(ex.Message, 404));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ErrorResponse($"Error while retrieving author by ID: {ex.Message}", 500));
        }
    }

    /// <summary>
    /// Updates an existing author.
    /// </summary>
    /// <param name="id">ID of the author to update.</param>
    /// <param name="author">Updated author details.</param>
    /// <returns>No content if successful.</returns>
    /// <response code="200">Author updated successfully.</response>
    /// <response code="404">Author not found.</response>
    [Authorize(Roles = "Developer")]
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ResponseAuthorDto), 200)]    // Return the updated author in case of success
    [ProducesResponseType(typeof(ErrorResponse), 404)]    // Not Found
    [ProducesResponseType(typeof(ErrorResponse), 400)]    // Bad Request
    public async Task<IActionResult> UpdateAuthor(int id, [FromBody] RequestAuthorDto author)
    {
        try
        {
            // Attempt to update the author
            var updatedAuthor = await _authorService.UpdateAuthorAsync(id, author);

            // If no author was found, return NotFound
            if (updatedAuthor == null)
                return NotFound(new ErrorResponse("Author not found.", 404));

            // Return the updated author
            return Ok(updatedAuthor);
        }
        catch (BadRequestException ex)
        {
            // Return BadRequest if there is an issue with the input
            return BadRequest(new ErrorResponse($"Bad request: {ex.Message}", 400));
        }
        catch (NotFoundException ex)
        {
            // Return NotFound if the author or related data isn't found
            return NotFound(new ErrorResponse(ex.Message, 404));
        }
        catch (Exception ex)
        {
            // Return a generic error if something unexpected happens
            return StatusCode(500, new ErrorResponse($"Error while updating author: {ex.Message}", 500));
        }
    }


    /// <summary>
    /// Deletes an author by ID.
    /// </summary>
    /// <param name="id">The ID of the author to delete.</param>
    /// <returns>No content if deletion succeeds.</returns>
    /// <response code="200">Author successfully deleted.</response>
    /// <response code="404">Author not found.</response>
    [Authorize(Roles = "Developer")]
    [Authorize(Policy = "FromGreeceOnly")]
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ErrorResponse), 404)]    // Not Found
    public async Task<IActionResult> DeleteAuthor(int id)
    {
        try
        {
            var deleted = await _authorService.DeleteAuthorAsync(id);
            if (!deleted)
                return NotFound(new ErrorResponse("Author not found.", 404));

            return Ok(new { Message = "Author deleted successfully", AuthorId = id });  // Return the deleted ID
        }
        catch (NotFoundException ex)
        {
            return NotFound(new ErrorResponse(ex.Message, 404));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ErrorResponse($"Error while deleting author: {ex.Message}", 500));
        }
    }
}

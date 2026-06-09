using BackendAssignment.Application.DTOs;
using BackendAssignment.Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace BackendAssignment.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>
    /// Logs a user in and returns a JWT token for authentication.
    /// </summary>
    /// <remarks>
    /// This endpoint allows a user to authenticate by providing their username and password. 
    /// If the credentials are valid, a JWT token is returned for subsequent authenticated requests.
    /// Example of a successful request body:
    /// 
    /// {
    ///     "username": "dev",
    ///     "password": "dev"
    /// }
    /// </remarks>
    /// <param name="request">The login credentials (username and password) for authentication.</param>
    /// <returns>A JWT token in the response body if authentication succeeds.</returns>
    /// <response code="200">A JWT token is returned if the authentication is successful.</response>
    /// <response code="400">If the input model is invalid or missing required fields.</response>
    /// <response code="401">If the authentication fails due to invalid credentials.</response>
    /// <response code="500">If an unexpected error occurs.</response>
    [HttpPost("login")]
    [ProducesResponseType(typeof(TokenResponseDto), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 400)]  
    [ProducesResponseType(typeof(ErrorResponse), 401)]  
    [ProducesResponseType(typeof(ErrorResponse), 500)] 
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
    {
        if (!ModelState.IsValid)
            return BadRequest(new ErrorResponse("Invalid input.", 400));

        try
        {
            var token = await _authService.GenerateTokenAsync(request.Username, request.Password);
            return Ok(new TokenResponseDto(token));
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new ErrorResponse("Invalid username or password.", 401));
        }
        catch (Exception ex) // General exception handling for unexpected errors
        {
            return StatusCode(500, new ErrorResponse(ex.Message, 500));
        }
    }
}

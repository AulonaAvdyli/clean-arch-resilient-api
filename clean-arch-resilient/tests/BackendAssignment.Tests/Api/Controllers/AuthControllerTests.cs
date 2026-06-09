using BackendAssignment.Api.Controllers;
using BackendAssignment.Application.DTOs;
using BackendAssignment.Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace BackendAssignment.Tests.Api.Controllers;

public class AuthControllerTests
{
    private readonly AuthController _controller;
    private readonly Mock<IAuthService> _authServiceMock = new();

    public AuthControllerTests()
    {
        // Inject mocked AuthService into the controller
        _controller = new AuthController(_authServiceMock.Object);
    }

    [Fact]
    public async Task Login_ValidCredentials_ReturnsToken()
    {
        // Arrange: Set up valid credentials and mock token
        var request = new LoginRequestDto { Username = "admin", Password = "pass123" };
        var expectedToken = "mock-jwt-token";

        // Mock the token generation to return a valid token
        _authServiceMock.Setup(s => s.GenerateTokenAsync(request.Username, request.Password))
            .ReturnsAsync(expectedToken)
            .Verifiable();

        // Act: Call the controller method
        var result = await _controller.Login(request);

        // Assert: Should return 200 OK with token in response body
        var ok = Assert.IsType<OkObjectResult>(result);
        var tokenObj = Assert.IsType<TokenResponseDto>(ok.Value);
        Assert.Equal(expectedToken, tokenObj.Token);

        // Verify the method was called with correct arguments
        _authServiceMock.Verify();
    }

    [Fact]
    public async Task Login_InvalidCredentials_ReturnsUnauthorized()
    {
        // Arrange: Set up credentials that simulate failure
        var request = new LoginRequestDto { Username = "admin", Password = "wrongpass" };

        _authServiceMock.Setup(s => s.GenerateTokenAsync(request.Username, request.Password))
            .ThrowsAsync(new UnauthorizedAccessException())
            .Verifiable();

        // Act: Call the controller method
        var result = await _controller.Login(request);

        // Assert: Should return 401 Unauthorized with error message
        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
        var errorResponse = Assert.IsType<ErrorResponse>(unauthorized.Value);
        Assert.Equal("Invalid username or password.", errorResponse.Message);
        Assert.Equal(401, errorResponse.Status);
        Assert.Equal("Error", errorResponse.Type);

        _authServiceMock.Verify();
    }

    
    [Fact]
    public async Task Login_InvalidModelState_ReturnsBadRequest()
    {
        // Arrange
        var controller = new AuthController(_authServiceMock.Object);
        controller.ModelState.AddModelError("Username", "Required");

        var request = new LoginRequestDto(); // missing Username

        // Act
        var result = await controller.Login(request);

        // Assert: Controller should short-circuit to 400
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
    }
    
    [Fact]
    public async Task Login_UnexpectedException_ReturnsInternalServerError()
    {
        // Arrange
        var request = new LoginRequestDto { Username = "admin", Password = "pass123" };

        _authServiceMock.Setup(s => s.GenerateTokenAsync(request.Username, request.Password))
            .ThrowsAsync(new Exception("Something went wrong"))
            .Verifiable();

        // Act
        var result = await _controller.Login(request);

        // Assert
        var serverError = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, serverError.StatusCode);

        var errorResponse = Assert.IsType<ErrorResponse>(serverError.Value);
        Assert.Equal("Something went wrong", errorResponse.Message);
        Assert.Equal(500, errorResponse.Status);
        Assert.Equal("Error", errorResponse.Type);

        _authServiceMock.Verify();
    }
}

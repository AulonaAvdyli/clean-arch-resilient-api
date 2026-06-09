namespace BackendAssignment.Application.DTOs;

/// <summary>
/// Represents the response returned after successful authentication.
/// </summary>
/// <param name="Token">The generated JWT access token.</param>
public record TokenResponseDto(string Token);
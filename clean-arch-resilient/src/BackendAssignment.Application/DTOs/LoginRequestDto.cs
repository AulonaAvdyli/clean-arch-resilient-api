using System.ComponentModel.DataAnnotations;

namespace BackendAssignment.Application.DTOs;

/// <summary>
/// Represents a login request containing credentials for authentication.
/// </summary>
public class LoginRequestDto
{
    /// <summary>
    /// The username of the user attempting to log in.
    /// </summary>
    [Required]
    public string Username { get; set; }

    /// <summary>
    /// The password of the user attempting to log in.
    /// </summary>
    [Required]
    public string Password { get; set; }
}
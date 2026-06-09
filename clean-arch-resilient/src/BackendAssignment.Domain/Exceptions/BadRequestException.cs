namespace BackendAssignment.Domain.Exceptions;

/// <summary>
/// Exception thrown when the request is invalid or malformed.
/// </summary>
public class BadRequestException(string message) : Exception(message);
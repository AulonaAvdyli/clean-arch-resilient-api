namespace BackendAssignment.Domain.Exceptions;

/// <summary>
/// Exception thrown when a conflict occurs, such as a duplicate entity.
/// </summary>
public class ConflictException(string message) : Exception(message);
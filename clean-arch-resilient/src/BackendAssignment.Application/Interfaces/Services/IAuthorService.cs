using BackendAssignment.Application.DTOs;

namespace BackendAssignment.Application.Interfaces.Services;

/// <summary>
/// Interface Service contract for author-related operations.
/// </summary>
public interface IAuthorService
{
    Task<ResponseAuthorDto> CreateAuthorAsync(RequestAuthorDto requestAuthor);
    Task<IEnumerable<ResponseAuthorDto>> GetAllAuthorsAsync();
    Task<ResponseAuthorDto> GetAuthorByIdAsync(int id);
    Task<ResponseAuthorDto> UpdateAuthorAsync(int id, RequestAuthorDto author);
    Task<bool> DeleteAuthorAsync(int id);
}
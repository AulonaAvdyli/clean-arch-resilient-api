using BackendAssignment.Application.DTOs;
using BackendAssignment.Application.Interfaces.Repositories;
using BackendAssignment.Application.Interfaces.Services;
using BackendAssignment.Domain.Entities;
using BackendAssignment.Domain.Exceptions;

namespace BackendAssignment.Application.Services;

/// <summary>
/// Handles business logic for managing authors (create, read, update, delete).
/// Uses caching to reduce database load on frequent read operations.
/// </summary>
public class AuthorService(IAuthorRepository authorRepository, ICacheService cache) : IAuthorService
{
    /// <summary>
    /// Creates a new author if not already existing.
    /// </summary>
    public async Task<ResponseAuthorDto> CreateAuthorAsync(RequestAuthorDto requestAuthor)
    {
        if (string.IsNullOrWhiteSpace(requestAuthor.FirstName) || string.IsNullOrWhiteSpace(requestAuthor.LastName))
            throw new BadRequestException("Author's first and last name cannot be empty.");

        // Check if the author already exists to prevent duplicates.
        var existingAuthor = await authorRepository.GetAuthorByDetailsAsync(
            requestAuthor.FirstName, requestAuthor.LastName, requestAuthor.Country,
            requestAuthor.BooksPublished.GetValueOrDefault());

        if (existingAuthor != null)
            throw new ConflictException(
                $"An author with the name {requestAuthor.FirstName} {requestAuthor.LastName} from {requestAuthor.Country} already exists.");

        // Map DTO to domain model
        var newAuthor = new Author
        {
            FirstName = requestAuthor.FirstName,
            LastName = requestAuthor.LastName,
            Country = requestAuthor.Country,
            BooksPublished = requestAuthor.BooksPublished.GetValueOrDefault(0)
        };
        try
        {
            // Create the new author in the repository
            var authorId = await authorRepository.CreateAuthorAsync(newAuthor);
        
            // Retrieve the created author
            var createdAuthor = await authorRepository.GetAuthorByIdAsync(authorId);

            if (createdAuthor == null)
                throw new Exception("Author creation failed.");

            return MapToResponseDto(createdAuthor);
        }
        catch (Exception ex) when (!(ex is BadRequestException || ex is NotFoundException || ex is ConflictException))
        {
            throw new ApplicationException("Author creation failed.", ex);
        }
    }

    /// <summary>
    /// Retrieves all authors from cache or repository.
    /// </summary>
    public async Task<IEnumerable<ResponseAuthorDto>> GetAllAuthorsAsync()
    {
        const string cacheKey = "author:all";

        try
        {
            var cached = await cache.GetAsync<IEnumerable<ResponseAuthorDto>>(cacheKey);
            if (cached != null)
                return cached;

            var authors = await authorRepository.GetAllAuthorsAsync();
            var dtos = authors.Select(MapToResponseDto).ToList();

            // Cache the result for next time
            await cache.SetAsync(cacheKey, dtos);

            return dtos;
        }
        catch (Exception ex)
        {
            throw new ApplicationException("An error occurred while retrieving authors.", ex);
        }
    }

    /// <summary>
    /// Gets a specific author by ID, with caching.
    /// </summary>
    public async Task<ResponseAuthorDto> GetAuthorByIdAsync(int id)
    {
        string cacheKey = $"author:{id}";

        try
        {
            var cached = await cache.GetAsync<ResponseAuthorDto>(cacheKey);

            if (cached != null)
                return cached;

            var author = await authorRepository.GetAuthorByIdAsync(id);
            if (author == null)
                throw new NotFoundException($"Author with ID {id} was not found.");

            var dto = MapToResponseDto(author);

            // Cache individual author lookup
            await cache.SetAsync(cacheKey, dto);

            return dto;
        }
        catch (Exception ex) when (!(ex is NotFoundException))
        {
            throw new ApplicationException($"An error occurred while retrieving author with ID {id}.", ex);
        }
    }

    /// <summary>
    /// Updates an existing author's info. Invalidates cache if successful.
    /// </summary>
    public async Task<ResponseAuthorDto> UpdateAuthorAsync(int id, RequestAuthorDto requestAuthor)
    {
        try
        {
            var existingAuthor = await authorRepository.GetAuthorByIdAsync(id);
            if (existingAuthor == null)
                throw new NotFoundException($"Author with ID {id} was not found.");

            // Update only fields provided (simple merge strategy)
            var updatedAuthor = new Author
            {
                Id = id, // Retain existing ID when updating
                FirstName = requestAuthor.FirstName ?? existingAuthor.FirstName,
                LastName = requestAuthor.LastName ?? existingAuthor.LastName,
                Country = requestAuthor.Country ?? existingAuthor.Country,
                BooksPublished = requestAuthor.BooksPublished.GetValueOrDefault(existingAuthor.BooksPublished)
            };

            // Update the author in the repository
            var success = await authorRepository.UpdateAuthorAsync(id, updatedAuthor);
            if (!success)
                throw new Exception("Failed to update the author.");

            // Invalidate cache to reflect updated data
            await cache.RemoveAsync($"author:{id}");
            await cache.RemoveAsync("author:all");

            // Return the updated author as a DTO
            return MapToResponseDto(updatedAuthor);
        }
        catch (Exception ex) when (!(ex is BadRequestException || ex is NotFoundException || ex is ConflictException))
        {
            throw new ApplicationException($"An error occurred while updating author with ID {id}.", ex);
        }
    }

    /// <summary>
    /// Deletes an author by ID and clears cache.
    /// </summary>
    public async Task<bool> DeleteAuthorAsync(int id)
    {
        try
        {
            var exists = await authorRepository.CheckAuthorExists(id);
            if (!exists)
                throw new NotFoundException($"Author with ID {id} was not found.");

            var success = await authorRepository.DeleteAuthorAsync(id);

            if (success)
            {
                // Clean up cached data
                await cache.RemoveAsync($"author:{id}");
                await cache.RemoveAsync("author:all");
            }

            return success;
        }
        catch (Exception ex) when (!(ex is NotFoundException))
        {
            throw new ApplicationException($"An error occurred while deleting author with ID {id}.", ex);
        }
    }

    // Maps a domain Author entity to a DTO for returning to API clients
    private static ResponseAuthorDto MapToResponseDto(Author author)
    {
        return new ResponseAuthorDto
        {
            Id = author.Id,
            FirstName = author.FirstName,
            LastName = author.LastName,
            Country = author.Country,
            BooksPublished = author.BooksPublished
        };
    }
}

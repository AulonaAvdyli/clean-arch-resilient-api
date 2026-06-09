using BackendAssignment.Application.DTOs;
using BackendAssignment.Application.Interfaces.Repositories;
using BackendAssignment.Application.Interfaces.Services;
using BackendAssignment.Application.Services;
using BackendAssignment.Domain.Entities;
using BackendAssignment.Domain.Exceptions;
using FluentAssertions;
using Moq;

namespace BackendAssignment.Tests.Application.Services;

public class CategoryServiceTests
{
    private readonly Mock<ICategoryRepository> _categoryRepoMock = new();
    private readonly Mock<ICacheService> _cacheMock = new();
    private readonly ICategoryService _categoryService;

    public CategoryServiceTests()
    {
        // Initialize service with mocked dependencies
        _categoryService = new CategoryService(_categoryRepoMock.Object, _cacheMock.Object);
    }

    [Fact]
    public async Task CreateCategoryAsync_Should_Create_When_ValidRequest()
    {
        // Should create a category successfully when it doesn't already exist
        var request = new RequestCategoryDto { Name = "Fantasy" };
        _categoryRepoMock.Setup(r => r.GetCategoryByNameAsync("Fantasy")).ReturnsAsync((Category)null!);
        _categoryRepoMock.Setup(r => r.CreateCategoryAsync(It.IsAny<Category>())).ReturnsAsync(1);
        _categoryRepoMock.Setup(r => r.GetCategoryByIdAsync(1)).ReturnsAsync(new Category { Id = 1, Name = "Fantasy" });

        var result = await _categoryService.CreateCategoryAsync(request);

        result.Should().NotBeNull();
        result.Name.Should().Be("Fantasy");
    }

    [Fact]
    public async Task CreateCategoryAsync_Should_ThrowConflict_When_Exists()
    {
        // Should throw ConflictException if category with the same name already exists
        var request = new RequestCategoryDto { Name = "Fantasy" };
        _categoryRepoMock.Setup(r => r.GetCategoryByNameAsync("Fantasy")).ReturnsAsync(new Category());

        var act = async () => await _categoryService.CreateCategoryAsync(request);
        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task CreateCategoryAsync_Should_ThrowBadRequest_When_NameMissing()
    {
        // Should throw BadRequestException when category name is invalid
        var request = new RequestCategoryDto { Name = " " };
        var act = async () => await _categoryService.CreateCategoryAsync(request);
        await act.Should().ThrowAsync<BadRequestException>();
    }

    [Fact]
    public async Task GetAllCategoriesAsync_Should_Return_FromCache()
    {
        // Should return categories from cache if available
        var cached = new List<ResponseCategoryDto> { new() { Id = 1, Name = "Horror" } };
        _cacheMock.Setup(c => c.GetAsync<IEnumerable<ResponseCategoryDto>>("category:all")).ReturnsAsync(cached);

        var result = await _categoryService.GetAllCategoriesAsync();

        result.Should().BeEquivalentTo(cached);
    }

    [Fact]
    public async Task GetCategoryByIdAsync_Should_Return_FromCache()
    {
        // Should return single category from cache if available
        var dto = new ResponseCategoryDto { Id = 1, Name = "Romance" };
        _cacheMock.Setup(c => c.GetAsync<ResponseCategoryDto>("category:1")).ReturnsAsync(dto);

        var result = await _categoryService.GetCategoryByIdAsync(1);
        result.Should().Be(dto);
    }

    [Fact]
    public async Task GetCategoryByIdAsync_Should_Throw_When_NotFound()
    {
        // Should throw NotFoundException if category doesn't exist in cache or DB
        _cacheMock.Setup(c => c.GetAsync<ResponseCategoryDto>("category:2")).ReturnsAsync((ResponseCategoryDto)null!);
        _categoryRepoMock.Setup(r => r.GetCategoryByIdAsync(2)).ReturnsAsync((Category)null!);

        var act = async () => await _categoryService.GetCategoryByIdAsync(2);
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task UpdateCategoryAsync_Should_RemoveCache_When_Success()
    {
        // Arrange: simulate successful update
        var request = new RequestCategoryDto { Name = "Drama" };
        var existingCategory = new Category { Id = 1, Name = "Old" };
        var updatedCategory = new Category { Id = 1, Name = "Drama" };

        _categoryRepoMock.Setup(r => r.GetCategoryByIdAsync(1)).ReturnsAsync(existingCategory);
        _categoryRepoMock.Setup(r => r.UpdateCategoryAsync(1, It.IsAny<Category>())).ReturnsAsync(true);
        _categoryRepoMock.Setup(r => r.GetCategoryByIdAsync(1)).ReturnsAsync(updatedCategory);  // Simulating retrieval of updated category

        // Act
        var result = await _categoryService.UpdateCategoryAsync(1, request);

        // Assert: should return the updated category DTO
        result.Should().NotBeNull();
        result.Name.Should().Be("Drama");

        // Assert: cache should be removed for the updated category
        _cacheMock.Verify(c => c.RemoveAsync("category:1"), Times.Once);
    }

    [Fact]
    public async Task UpdateCategoryAsync_Should_Throw_When_NotExists()
    {
        // Arrange: simulate category not found
        _categoryRepoMock.Setup(r => r.GetCategoryByIdAsync(99)).ReturnsAsync((Category)null!);

        // Act & Assert: should throw NotFoundException when trying to update a non-existent category
        var act = async () => await _categoryService.UpdateCategoryAsync(99, new RequestCategoryDto());
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task DeleteCategoryAsync_Should_Throw_When_NotFound()
    {
        // Should throw NotFoundException if category doesn't exist
        _categoryRepoMock.Setup(r => r.CheckCategoryExists(2)).ReturnsAsync(false);

        var act = async () => await _categoryService.DeleteCategoryAsync(2);
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task DeleteCategoryAsync_Should_RemoveCache_When_Success()
    {
        // Should delete category and invalidate cache
        _categoryRepoMock.Setup(r => r.CheckCategoryExists(1)).ReturnsAsync(true);
        _categoryRepoMock.Setup(r => r.DeleteCategoryAsync(1)).ReturnsAsync(true);

        var result = await _categoryService.DeleteCategoryAsync(1);
        result.Should().BeTrue();
        _cacheMock.Verify(c => c.RemoveAsync("category:1"), Times.Once);
    }

    [Fact]
    public async Task GetAllCategoriesAsync_Should_FetchFromRepo_And_Cache_When_MissingInCache()
    {
        // Should fetch from DB and populate cache if cache is empty
        _cacheMock.Setup(c => c.GetAsync<IEnumerable<ResponseCategoryDto>>("category:all")).ReturnsAsync((IEnumerable<ResponseCategoryDto>?)null);
        _categoryRepoMock.Setup(r => r.GetAllCategoriesAsync()).ReturnsAsync(new List<Category> { new() { Id = 1, Name = "History" } });

        var result = await _categoryService.GetAllCategoriesAsync();

        result.Should().HaveCount(1);
        result.First().Name.Should().Be("History");
        _cacheMock.Verify(c => c.SetAsync("category:all", It.IsAny<object>()), Times.Once);
    }

    [Fact]
    public async Task GetCategoryByIdAsync_Should_Return_When_NotCached()
    {
        // Should return category from DB and then set it to cache
        _cacheMock.Setup(c => c.GetAsync<ResponseCategoryDto>("category:1")).ReturnsAsync((ResponseCategoryDto)null);
        _categoryRepoMock.Setup(r => r.GetCategoryByIdAsync(1)).ReturnsAsync(new Category { Id = 1, Name = "Mystery" });

        var result = await _categoryService.GetCategoryByIdAsync(1);

        result.Should().NotBeNull();
        result.Name.Should().Be("Mystery");
        _cacheMock.Verify(c => c.SetAsync("category:1", It.IsAny<ResponseCategoryDto>()), Times.Once);
    }

    [Fact]
    public async Task CreateCategoryAsync_Should_ThrowException_When_CreationFails()
    {
        // Should throw generic exception when DB insertion fails silently
        var request = new RequestCategoryDto { Name = "Thriller" };
        _categoryRepoMock.Setup(r => r.GetCategoryByNameAsync("Thriller")).ReturnsAsync((Category)null!);
        _categoryRepoMock.Setup(r => r.CreateCategoryAsync(It.IsAny<Category>())).ReturnsAsync(1);
        _categoryRepoMock.Setup(r => r.GetCategoryByIdAsync(1)).ReturnsAsync((Category)null!);

        var act = async () => await _categoryService.CreateCategoryAsync(request);
        await act.Should().ThrowAsync<Exception>().WithMessage("*An error occurred while creating the category*");
    }
    
    [Fact]
    public async Task DeleteCategoryAsync_Should_ReturnFalse_WhenDeleteFails()
    {
        _categoryRepoMock.Setup(r => r.CheckCategoryExists(1)).ReturnsAsync(true);
        _categoryRepoMock.Setup(r => r.DeleteCategoryAsync(1)).ReturnsAsync(false);

        var result = await _categoryService.DeleteCategoryAsync(1);

        result.Should().BeFalse();
        _cacheMock.Verify(c => c.RemoveAsync(It.IsAny<string>()), Times.Never);
    }
    
    [Fact]
    public async Task UpdateCategoryAsync_Should_ThrowException_WhenUpdateFails()
    {
        var existing = new Category { Id = 1, Name = "Old" };
        var request = new RequestCategoryDto { Name = "Updated" };

        _categoryRepoMock.Setup(r => r.GetCategoryByIdAsync(1)).ReturnsAsync(existing);
        _categoryRepoMock.Setup(r => r.UpdateCategoryAsync(1, It.IsAny<Category>())).ReturnsAsync(false);

        var act = async () => await _categoryService.UpdateCategoryAsync(1, request);

        await act.Should().ThrowAsync<Exception>().WithMessage("*An error occurred while updating category with ID 1*");
    }
}



using BackendAssignment.Domain.Entities;
using BackendAssignment.Domain.Exceptions;
using BackendAssignment.Infrastructure.Helpers;
using BackendAssignment.Infrastructure.Persistence.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace BackendAssignment.Tests.Infrastructure.Repositories;

public class CategoryRepositoryTests
{
    private readonly Mock<IDatabaseHelper> _databaseHelperMock;
    private readonly Mock<IFakeDbConnection> _fakeDbConnectionMock;
    private readonly Mock<ILogger<CategoryRepository>> _loggerMock;
    private readonly CategoryRepository _categoryRepository;

    public CategoryRepositoryTests()
    {
        _databaseHelperMock = new Mock<IDatabaseHelper>();
        _fakeDbConnectionMock = new Mock<IFakeDbConnection>();
        _loggerMock = new Mock<ILogger<CategoryRepository>>();

        _databaseHelperMock.Setup(d => d.CreateConnection())
            .Returns(_fakeDbConnectionMock.Object);

        _categoryRepository = new CategoryRepository(_databaseHelperMock.Object, _loggerMock.Object);
    }
    
    [Fact]
    public async Task CreateCategoryAsync_ShouldThrowRepositoryException_WhenExceptionOccurs()
    {
        var category = new Category { Name = "Test" };

        _fakeDbConnectionMock
            .Setup(d => d.ExecuteScalarAsync<int>(It.IsAny<string>(), It.IsAny<object>()))
            .ThrowsAsync(new Exception("broken insert"));

        var act = async () => await _categoryRepository.CreateCategoryAsync(category);

        await act.Should().ThrowAsync<RepositoryException>()
            .WithMessage("An unexpected error occurred while creating the category*");
    }
    
    [Fact]
    public async Task CheckCategoryExists_ShouldThrowRepositoryException_WhenExceptionOccurs()
    {
        _fakeDbConnectionMock
            .Setup(d => d.ExecuteScalarAsync<int>(It.IsAny<string>(), It.IsAny<object>()))
            .ThrowsAsync(new Exception("count fail"));

        var act = async () => await _categoryRepository.CheckCategoryExists(10);

        await act.Should().ThrowAsync<RepositoryException>()
            .WithMessage("An unexpected error occurred while checking if category with ID 10 exists*");
    }

    [Fact]
    public async Task GetCategoryByNameAsync_ShouldThrowRepositoryException_WhenExceptionOccurs()
    {
        _fakeDbConnectionMock
            .Setup(d => d.QueryFirstOrDefaultAsync<Category>(It.IsAny<string>(), It.IsAny<object>()))
            .ThrowsAsync(new Exception("select by name fail"));

        var act = async () => await _categoryRepository.GetCategoryByNameAsync("Science");

        await act.Should().ThrowAsync<RepositoryException>()
            .WithMessage("An unexpected error occurred while retrieving category by name*");
    }
    
    [Fact]
    public async Task GetCategoryByIdAsync_ShouldThrowRepositoryException_WhenExceptionOccurs()
    {
        _fakeDbConnectionMock
            .Setup(d => d.QueryFirstOrDefaultAsync<Category>(It.IsAny<string>(), It.IsAny<object>()))
            .ThrowsAsync(new Exception("select by id fail"));

        var act = async () => await _categoryRepository.GetCategoryByIdAsync(5);

        await act.Should().ThrowAsync<RepositoryException>()
            .WithMessage("An unexpected error occurred while retrieving category with ID 5*");
    }
    
    
    [Fact]
    public async Task UpdateCategoryAsync_ShouldThrowRepositoryException_WhenExceptionOccurs()
    {
        var category = new Category { Name = "Updated Name" };

        _fakeDbConnectionMock
            .Setup(d => d.ExecuteAsync(It.IsAny<string>(), It.IsAny<object>()))
            .ThrowsAsync(new Exception("update error"));

        var act = async () => await _categoryRepository.UpdateCategoryAsync(1, category);

        await act.Should().ThrowAsync<RepositoryException>()
            .WithMessage("An unexpected error occurred while updating category with ID 1*");
    }
    
    [Fact]
    public async Task DeleteCategoryAsync_ShouldThrowRepositoryException_WhenExceptionOccurs()
    {
        _fakeDbConnectionMock
            .Setup(d => d.ExecuteAsync(It.IsAny<string>(), It.IsAny<object>()))
            .ThrowsAsync(new Exception("delete error"));

        var act = async () => await _categoryRepository.DeleteCategoryAsync(999);

        await act.Should().ThrowAsync<RepositoryException>()
            .WithMessage("An unexpected error occurred while deleting category with ID 999*");
    }
    
    [Theory]
    [InlineData(null)]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetCategoryByIdAsync_ShouldReturnUnknown_WhenIdIsInvalid(int? id)
    {
        var result = await _categoryRepository.GetCategoryByIdAsync(id);

        result.Should().NotBeNull();
        result.Name.Should().Be("Unknown");
    }
}

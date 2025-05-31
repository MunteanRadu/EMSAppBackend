using EMSApp.Application;
using EMSApp.Domain;
using EMSApp.Domain.Entities;
using EMSApp.Infrastructure;
using Microsoft.VisualBasic;
using Moq;

namespace EMSApp.Tests;

[Trait("Category", "Service")]
public class AssignmentServiceTests
{
    private readonly Mock<IAssignmentRepository> _repo;
    private readonly IAssignmentService _service;
    private CancellationToken _ct = CancellationToken.None;

    public AssignmentServiceTests()
    {
        _repo = new Mock<IAssignmentRepository>();
        _service = new AssignmentService(_repo.Object);
    }

    [Fact]
    public async Task CreateAsync_ValidData_CreatesAndReturnsEntity()
    {
        // Arrange
        var title = "title";
        var description = "description";
        var dueDate = DateTime.UtcNow.AddDays(5);
        var assignedTo = "user-1";

        // Act
        var result = await _service.CreateAsync(title, description, dueDate, assignedTo, _ct);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(title, result.Title);
        Assert.Equal(description, result.Description);
        Assert.Equal(dueDate, result.DueDate);
        Assert.Equal(assignedTo, result.AssignedToId);

        _repo.Verify(r => r.CreateAsync(
            It.Is<Assignment>(a =>
                a.Title == title &&
                a.Description == description &&
                a.DueDate == dueDate &&
                a.AssignedToId == assignedTo),
            _ct),
            Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_RepoReturnsEntity_ServiceReturnsSame()
    {
        // Arrange
        var a = new Assignment("title", "desc", DateTime.UtcNow.AddDays(5), "user-1");
        _repo.Setup(r => r.GetByIdAsync(a.Id, _ct)).ReturnsAsync(a);

        // Act
        var result = await _service.GetByIdAsync(a.Id, _ct);

        // Assert
        Assert.Same(a, result);
        _repo.Verify(r => r.GetByIdAsync(a.Id, _ct), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_NotFound_ReturnsNull()
    {
        // Arrange
        _repo.Setup(r => r.GetByIdAsync("noid", _ct)).ReturnsAsync((Assignment?)null);

        // Act
        var result = await _service.GetByIdAsync("noid", _ct);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task ListByAsigneeAsync_RepoReturnsList_ServiceReturnsSame()
    {
        // Arrange
        var list = new List<Assignment> 
        { 
            new Assignment("t1", "d1", DateTime.UtcNow.AddDays(5), "u1"),
            new Assignment("t2", "d2", DateTime.UtcNow.AddDays(10), "u1")
        };
        _repo.Setup(r => r.ListByAssigneeAsync("u1", _ct)).ReturnsAsync(list);

        // Act
        var result = await _service.ListByAsigneeAsync("u1", _ct);

        // Assert
        Assert.Same(list, result);
        _repo.Verify(r => r.ListByAssigneeAsync("u1", _ct), Times.Once);
    }

    [Fact]
    public async Task ListByOverdueAsync_RepoReturnsList_ServiceReturnsSame()
    {
        // Arrange
        var list = new List<Assignment>
        {
            new Assignment("t1", "d1", DateTime.UtcNow.AddDays(5), "u1"),
            new Assignment("t2", "d2", DateTime.UtcNow.AddDays(5), "u1")
        };
        var asOf = DateTime.UtcNow.AddDays(10);
        _repo.Setup(r => r.ListOverdueAsync(asOf, _ct)).ReturnsAsync(list);

        // Act
        var result = await _service.ListByOverdueAsync(asOf, _ct);

        // Assert
        Assert.Same(list, result);
        _repo.Verify(r => r.ListOverdueAsync(asOf, _ct), Times.Once);
    }

    [Fact]
    public async Task ListByStatusAsync_RepoReturnsList_ServiceReturnsSame()
    {
        // Arrange
        var list = new List<Assignment>
        {
            new Assignment("t1", "d1", DateTime.UtcNow.AddDays(5), "u1"),
            new Assignment("t2", "d2", DateTime.UtcNow.AddDays(10), "u1")
        };
        _repo.Setup(r => r.ListByStatusAsync(AssignmentStatus.Pending, _ct)).ReturnsAsync(list);

        // Act
        var result = await _service.ListByStatusAsync(AssignmentStatus.Pending, _ct);

        // Assert
        Assert.Same(list, result);
        _repo.Verify(r => r.ListByStatusAsync(AssignmentStatus.Pending, _ct), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_CallsRepository()
    {
        // Arrange
        var a = new Assignment("t1", "d1", DateTime.UtcNow.AddDays(5), "u1");

        // Act
        await _service.UpdateAsync(a, _ct);

        // Assert
        _repo.Verify(r => r.UpdateAsync(a, false, _ct), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_CallsRepository()
    {
        // Act
        await _service.DeleteAsync("a1", _ct);
        
        // Assert
        _repo.Verify(r => r.DeleteAsync("a1", _ct), Times.Once);
    }
}

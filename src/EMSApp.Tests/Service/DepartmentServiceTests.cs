using EMSApp.Application;
using EMSApp.Domain;
using EMSApp.Domain.Entities;
using EMSApp.Infrastructure;
using Moq;

namespace EMSApp.Tests;

[Trait("Category", "Service")]
public class DepartmentServiceTests
{
    private readonly Mock<IDepartmentRepository> _repo;
    private readonly IDepartmentService _service;
    private CancellationToken _ct = CancellationToken.None;

    public DepartmentServiceTests()
    {
        _repo = new Mock<IDepartmentRepository>();
        _service = new DepartmentService(_repo.Object);
    }

    [Fact]
    public async Task CreateAsync_ValidData_CreatesAndReturnsEntity()
    {
        // Arrange
        var name = "name";
        var manager = "manager-1";

        // Act
        var result = await _service.CreateAsync(name, manager, _ct);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(name, result.Name);
        Assert.Equal(manager, result.ManagerId);

        _repo.Verify(r => r.CreateAsync(
            It.Is<Department>(d =>
                d.Name == name &&
                d.ManagerId == manager),
            _ct),
            Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_RepoReturnsList_ServiceReturnsSame()
    {
        // Arrange
        var list = new List<Department>
        {
            new Department("n1", "m1"),
            new Department("n2", "m2")
        };
        _repo.Setup(r => r.GetAllAsync(_ct)).ReturnsAsync(list);

        // Act
        var result = await _service.GetAllAsync(_ct);

        // Assert
        Assert.Same(list, result);
        _repo.Verify(r => r.GetAllAsync(_ct), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_RepoReturnsEntiry_ServiceReturnsSame()
    {
        // Arrange
        var d = new Department("n1", "m1");
        _repo.Setup(r => r.GetByIdAsync(d.Id, _ct)).ReturnsAsync(d);

        // Act
        var result = await _service.GetByIdAsync(d.Id, _ct);

        // Assert
        Assert.Same(d, result);
        _repo.Verify(r => r.GetByIdAsync(d.Id, _ct), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_NotFound_ReturnsNull()
    {
        // Arrange
        _repo.Setup(r => r.GetByIdAsync("noid", _ct)).ReturnsAsync((Department?)null);

        // Act
        var result = await _service.GetByIdAsync("noid", _ct);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateAsync_CallsRepository()
    {
        // Arrange
        var d = new Department("n1", "m1");

        // Act
        await _service.UpdateAsync(d, _ct);

        // Assert
        _repo.Verify(r => r.UpdateAsync(d, false, _ct), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_CallsRepository()
    {
        // Act
        await _service.DeleteAsync("dept-1", _ct);

        // Assert
        _repo.Verify(r => r.DeleteAsync("dept-1", _ct), Times.Once);
    }
}

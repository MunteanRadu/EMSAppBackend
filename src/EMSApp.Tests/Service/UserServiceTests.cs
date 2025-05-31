using EMSApp.Application;
using EMSApp.Domain;
using EMSApp.Domain.Entities;
using EMSApp.Infrastructure;
using Moq;

namespace EMSApp.Tests;

[Trait("Category", "Service")]
public class UserServiceTests
{
    private readonly Mock<IUserRepository> _repo;
    private readonly IUserService _service;
    private CancellationToken _ct = CancellationToken.None;

    public UserServiceTests()
    {
        _repo = new Mock<IUserRepository>();
        _service = new UserService(_repo.Object);
    }

    [Fact]
    public async Task CreateAsync_ValidData_CreatesAndReturns()
    {
        // Arrange
        var email = "email@email.email";
        var username = "username";
        var passwordHash = "validpassword123";
        var department = "dept-1";

        // Act
        var result = await _service.CreateAsync(email, username, passwordHash, department, _ct);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(email, result.Email);
        Assert.Equal(username, result.Username);
        Assert.Equal(passwordHash, result.PasswordHash);
        Assert.Equal(department, result.DepartmentId);

        _repo.Verify(r => r.CreateAsync(
            It.Is<User>(u =>
                u.Email == email &&
                u.Username == username &&
                u.PasswordHash == passwordHash &&
                u.DepartmentId == department),
            _ct),
            Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_RepoReturnsEntity_ServiceReturnsSame()
    {
        // Arrange
        var u = new User("email@email.email", "username", "validpassword123", "dept-1");
        _repo.Setup(r => r.GetByIdAsync(u.Id, _ct)).ReturnsAsync(u);

        // Act
        var result = await _service.GetByIdAsync(u.Id, _ct);

        // Assert
        Assert.Same(u, result);
        _repo.Verify(r => r.GetByIdAsync(u.Id, _ct), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_NotFound_ReturnsNull()
    {
        // Arrange
        _repo.Setup(r => r.GetByIdAsync("noid", _ct)).ReturnsAsync((User?)null);

        // Act
        var result = await _service.GetByIdAsync("noid", _ct);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task ListByDepartmentAsync_RepoReturnsList_ServiceReturnsSame()
    {
        // Arrange
        var list = new List<User>
        {
            new User("email@email.email", "username", "validpassword123", "dept-1"),
            new User("email2@email.email", "username2", "validpassword1232", "dept-1"),
        };
        _repo.Setup(r => r.ListByDepartmentAsync("dept-1", _ct)).ReturnsAsync(list);

        // Act
        var result = await _service.ListByDepartmentAsync("dept-1", _ct);

        // Assert
        Assert.Same(list, result);
        _repo.Verify(r => r.ListByDepartmentAsync("dept-1", _ct), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_CallsRepository()
    {
        // Arrange
        var u = new User("email@email.email", "username", "validpassword123", "dept-1");

        // Act
        await _service.UpdateAsync(u, _ct);

        // Assert
        _repo.Verify(r => r.UpdateAsync(u, false, _ct), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_CallsRepository()
    {
        // Act
        await _service.DeleteAsync("u-1", _ct);

        // Assert
        _repo.Verify(r => r.DeleteAsync("u-1", _ct), Times.Once);
    }
}

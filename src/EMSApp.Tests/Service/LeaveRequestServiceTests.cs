using EMSApp.Application;
using EMSApp.Domain;
using EMSApp.Infrastructure;
using Moq;

namespace EMSApp.Tests;

[Trait("Category", "Service")]
public class LeaveRequestServiceTests
{
    private readonly Mock<ILeaveRequestRepository> _repo;
    private readonly ILeaveRequestService _service;
    private CancellationToken _ct = CancellationToken.None;

    public LeaveRequestServiceTests()
    {
        _repo = new Mock<ILeaveRequestRepository>();
        _service = new LeaveRequestService(_repo.Object);
    }

    [Fact]
    public async Task CreateAsync_ValidData_CreatesAndReturnsEntity()
    {
        // Arrange
        var userid = "user-1";
        var type = LeaveType.Paid;
        var start = DateOnly.FromDateTime(DateTime.UtcNow);
        var end = start.AddDays(10);
        var reason = "reason";

        // Act
        var result = await _service.CreateAsync(userid, type, start, end, reason, _ct);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(userid, result.UserId);
        Assert.Equal(type, result.Type);
        Assert.Equal(start, result.StartDate);
        Assert.Equal(end, result.EndDate);
        Assert.Equal(reason, result.Reason);

        _repo.Verify(r => r.CreateAsync(
            It.Is<LeaveRequest>(lr => 
                lr.UserId == userid &&
                lr.Type == type &&
                lr.StartDate == start &&
                lr.EndDate == end &&
                lr.Reason == reason),
            _ct),
            Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_RepoReturnsEntity_ServiceReturnsSame()
    {
        // Arrange
        var start = DateOnly.FromDateTime(DateTime.UtcNow);
        var end = start.AddDays(10);
        var lr = new LeaveRequest("u1", LeaveType.Paid, start, end, "r");
        _repo.Setup(r => r.GetByIdAsync(lr.Id, _ct)).ReturnsAsync(lr);

        // Act
        var result = await _service.GetByIdAsync(lr.Id, _ct);

        // Assert
        Assert.Same(lr, result);
        _repo.Verify(r => r.GetByIdAsync(lr.Id, _ct), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_NotFound_ReturnsNull()
    {
        // Arrange
        _repo.Setup(r => r.GetByIdAsync("noid", _ct)).ReturnsAsync((LeaveRequest?)null);

        // Act
        var result = await _service.GetByIdAsync("noid", _ct);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task ListByManagerAsync_RepoReturnsList_ServiceReturnsSame()
    {
        // Arrange
        var start = DateOnly.FromDateTime(DateTime.UtcNow);
        var end = start.AddDays(10);
        var list = new List<LeaveRequest>
        {
            new LeaveRequest("u1", LeaveType.Paid, start, end, "r"),
            new LeaveRequest("u2", LeaveType.Sick, start, end, "r2")
        };
        list[0].Approve("m1");
        list[1].Approve("m1");
        _repo.Setup(r => r.ListByManagerAsync("m1", _ct)).ReturnsAsync(list);

        // Act
        var result = await _service.ListByManagerAsync("m1", _ct);

        // Assert
        Assert.Same(list, result);
        _repo.Verify(r => r.ListByManagerAsync("m1", _ct), Times.Once);
    }

    [Fact]
    public async Task ListByStatusAsync_RepoReturnsList_ServiceReturnsSame()
    {
        // Arrange
        var start = DateOnly.FromDateTime(DateTime.UtcNow);
        var end = start.AddDays(10);
        var list = new List<LeaveRequest>
        {
            new LeaveRequest("u1", LeaveType.Paid, start, end, "r"),
            new LeaveRequest("u2", LeaveType.Sick, start, end, "r2")
        };
        _repo.Setup(r => r.ListByStatusAsync(LeaveStatus.Pending, _ct)).ReturnsAsync(list);

        // Act
        var result = await _service.ListByStatusAsync(LeaveStatus.Pending, _ct);

        // Assert
        Assert.Same(list, result);
        _repo.Verify(r => r.ListByStatusAsync(LeaveStatus.Pending, _ct), Times.Once);
    }

    [Fact]
    public async Task ListByUserAsync_RepoReturnsList_ServiceReturnsSame()
    {
        // Arrange
        var start = DateOnly.FromDateTime(DateTime.UtcNow);
        var end = start.AddDays(10);
        var list = new List<LeaveRequest>
        {
            new LeaveRequest("u1", LeaveType.Paid, start, end, "r"),
            new LeaveRequest("u1", LeaveType.Sick, start, end, "r2")
        };
        _repo.Setup(r => r.ListByUserAsync("u1", _ct)).ReturnsAsync(list);

        // Act
        var result = await _service.ListByUserAsync("u1", _ct);

        // Assert
        Assert.Same(list, result);
        _repo.Verify(r => r.ListByUserAsync("u1", _ct), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_CallsRepository()
    {
        // Arrange
        var start = DateOnly.FromDateTime(DateTime.UtcNow);
        var end = start.AddDays(10);
        var lr = new LeaveRequest("u1", LeaveType.Paid, start, end, "r");

        // Act
        await _service.UpdateAsync(lr, _ct);

        // Assert
        _repo.Verify(r => r.UpdateAsync(lr, false, _ct), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_CallsRepository()
    {
        // Act
        await _service.DeleteAsync("lr-1", _ct);

        // Assert
        _repo.Verify(r => r.DeleteAsync("lr-1", _ct), Times.Once);
    }
}

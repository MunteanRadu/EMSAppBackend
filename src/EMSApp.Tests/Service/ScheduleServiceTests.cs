using EMSApp.Application;
using EMSApp.Domain;
using EMSApp.Infrastructure;
using Moq;

namespace EMSApp.Tests;

[Trait("Category", "Service")]
public class ScheduleServiceTests
{
    private readonly Mock<IScheduleRepository> _repo;
    private readonly IScheduleService _service;
    private CancellationToken _ct = CancellationToken.None;

    public ScheduleServiceTests()
    {
        _repo = new Mock<IScheduleRepository>();
        _service = new ScheduleService(_repo.Object);
    }

    [Fact]
    public async Task CreateAsync_ValidData_CreatesAndReturns()
    {
        // Arrange
        var departmentId = "dept-1";
        var managerId = "manager-1";
        var day = DayOfWeek.Monday;
        var startTime = TimeOnly.Parse("08:00");
        var endTime = startTime.AddHours(8);
        var isWorkingDay = true;

        // Act
        var result = await _service.CreateAsync(departmentId, managerId, day, startTime, endTime, isWorkingDay, _ct);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(departmentId, result.DepartmentId);
        Assert.Equal(managerId, result.ManagerId);
        Assert.Equal(startTime, result.StartTime);
        Assert.Equal(endTime, result.EndTime);
        Assert.Equal(isWorkingDay, result.IsWorkingDay);

        _repo.Verify(r => r.CreateAsync(
            It.Is<Schedule>(s =>
                s.DepartmentId == departmentId &&
                s.ManagerId == managerId &&
                s.StartTime == startTime &&
                s.EndTime == endTime &&
                s.IsWorkingDay == isWorkingDay),
            _ct),
            Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_RepoReturnsEntity_ServiceReturnsSame()
    {
        // Arrange
        var startTime = TimeOnly.Parse("08:00");
        var endTime = startTime.AddHours(8);
        var s = new Schedule("d1", "m1", DayOfWeek.Monday, startTime, endTime, true);
        _repo.Setup(r => r.GetByIdAsync(s.Id, _ct)).ReturnsAsync(s);

        // Act
        var result = await _service.GetByIdAsync(s.Id, _ct);

        // Assert
        Assert.Same(s, result);
        _repo.Verify(r => r.GetByIdAsync(s.Id, _ct), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_NotFound_ReturnsNull()
    {
        // Arrange
        _repo.Setup(r => r.GetByIdAsync("noid", _ct)).ReturnsAsync((Schedule?)null);

        // Act
        var result = await _service.GetByIdAsync("noid", _ct);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task ListByDepartmentAsync_RepoReturnsList_ServiceReturnsSame()
    {
        // Arrange
        var startTime = TimeOnly.Parse("08:00");
        var endTime = startTime.AddHours(8);
        var list = new List<Schedule>
        {
            new Schedule("d1", "m1", DayOfWeek.Monday, startTime, endTime, true),
            new Schedule("d1", "m2", DayOfWeek.Wednesday, startTime, endTime, true),
        };
        _repo.Setup(r => r.ListByDepartmentAsync("d1", _ct)).ReturnsAsync(list);

        // Act
        var result = await _service.ListByDepartmentAsync("d1", _ct);

        // Assert
        Assert.Same(list, result);
        _repo.Verify(r => r.ListByDepartmentAsync("d1", _ct), Times.Once);
    }

    [Fact]
    public async Task ListByManagerAsync_RepoReturnsList_ServiceReturnsSame()
    {
        // Arrange
        var startTime = TimeOnly.Parse("08:00");
        var endTime = startTime.AddHours(8);
        var list = new List<Schedule>
        {
            new Schedule("d2", "m1", DayOfWeek.Monday, startTime, endTime, true),
            new Schedule("d1", "m1", DayOfWeek.Wednesday, startTime, endTime, true),
        };
        _repo.Setup(r => r.ListByManagerAsync("m1", _ct)).ReturnsAsync(list);

        // Act
        var result = await _service.ListByManagerAsync("m1", _ct);

        // Assert
        Assert.Same(list, result);
        _repo.Verify(r => r.ListByManagerAsync("m1", _ct), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_CallsRepository()
    {
        // Arrange
        var startTime = TimeOnly.Parse("08:00");
        var endTime = startTime.AddHours(8);
        var s = new Schedule("d1", "m1", DayOfWeek.Monday, startTime, endTime, true);

        // Act
        await _service.UpdateAsync(s, _ct);

        // Assert
        _repo.Verify(r => r.UpdateAsync(s, false, _ct), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_CallsRepository()
    {
        // Act
        await _service.DeleteAsync("s-1", _ct);

        // Assert
        _repo.Verify(r => r.DeleteAsync("s-1", _ct), Times.Once);
    }
}

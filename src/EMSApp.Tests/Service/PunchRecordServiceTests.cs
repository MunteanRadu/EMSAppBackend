using EMSApp.Application;
using EMSApp.Domain;
using EMSApp.Infrastructure;
using Moq;
using System.Diagnostics.Contracts;

namespace EMSApp.Tests;

[Trait("Category", "Service")]
public class PunchRecordServiceTests
{
    private readonly Mock<IPunchRecordRepository> _repo;
    private readonly IPunchRecordService _service;
    private CancellationToken _ct = CancellationToken.None;

    public PunchRecordServiceTests()
    {
        _repo = new Mock<IPunchRecordRepository>();
        _service = new PunchRecordService(_repo.Object);
    }

    [Fact]
    public async Task CreateAsync_ValidData_CreatesAndReturns()
    {
        // Arrange
        var userId = "user-1";
        var date = DateOnly.FromDateTime(DateTime.UtcNow);
        var timeIn = TimeOnly.Parse("08:01");

        // Act
        var result = await _service.CreateAsync(userId, date, timeIn, _ct);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(userId, result.UserId);
        Assert.Equal(date, result.Date);
        Assert.Equal(timeIn, result.TimeIn);

        _repo.Verify(r => r.CreateAsync(
            It.Is<PunchRecord>(pr =>
                pr.UserId == userId &&
                pr.Date == date &&
                pr.TimeIn == timeIn),
            _ct),
            Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_RepoReturnsEntity_ServiceReturnsSame()
    {
        // Arrange
        var date = DateOnly.FromDateTime(DateTime.UtcNow);
        var timeIn = TimeOnly.Parse("08:01");
        var pr = new PunchRecord("u1", date, timeIn);
        _repo.Setup(r => r.GetByIdAsync(pr.Id, _ct)).ReturnsAsync(pr);

        // Act
        var result = await _service.GetByIdAsync(pr.Id, _ct);

        // Assert
        Assert.Same(pr, result);
        _repo.Verify(r => r.GetByIdAsync(pr.Id, _ct), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_NotFound_ReturnsNull()
    {
        // Arrange
        _repo.Setup(r => r.GetByIdAsync("noid", _ct)).ReturnsAsync((PunchRecord?)null);

        // Act
        var result = await _service.GetByIdAsync("noid", _ct);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task ListByUserAsync_RepoReturnsList_ServiceReturnsSame()
    {
        // Arrange
        var date = DateOnly.FromDateTime(DateTime.UtcNow);
        var timeIn = TimeOnly.Parse("08:01");
        var list = new List<PunchRecord> 
        {
            new PunchRecord("u1", date, timeIn),
            new PunchRecord("u1", date.AddDays(1), timeIn)
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
        var date = DateOnly.FromDateTime(DateTime.UtcNow);
        var timeIn = TimeOnly.Parse("08:01");
        var pr = new PunchRecord("u1", date, timeIn);

        // Act
        await _service.UpdateAsync(pr, _ct);

        // Assert
        _repo.Verify(r => r.UpdateAsync(pr, false, _ct), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_CallsRepository()
    {
        // Act
        await _service.DeleteAsync("pr-1", _ct);

        // Assert
        _repo.Verify(r => r.DeleteAsync("pr-1", _ct), Times.Once);
    }
}

using EMSApp.Application;
using EMSApp.Domain;
using EMSApp.Infrastructure;
using Moq;
using System.Diagnostics.Contracts;

namespace EMSApp.Tests;

[Trait("Category", "Service")]
public class BreakSessionServiceTests
{
    private readonly Mock<IBreakSessionRepository> _repo;
    private readonly IBreakSessionService _service;
    private CancellationToken _ct = CancellationToken.None;
    
    public BreakSessionServiceTests()
    {
        _repo = new Mock<IBreakSessionRepository>();
        _service = new BreakSessionService(_repo.Object);
    }

    [Fact]
    public async Task CreateAsync_ValidData_CreatesAndReturnsEntity()
    {
        // Arrange
        var punchRecordId = "punch-1";
        var start = TimeOnly.Parse("12:00");

        // Act
        var result = await _service.CreateAsync(punchRecordId, start, _ct);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(punchRecordId, result.PunchRecordId);
        Assert.Equal(start, result.StartTime);

        _repo.Verify(r => r.CreateAsync(
            It.Is<BreakSession>(bs =>
                bs.PunchRecordId == punchRecordId &&
                bs.StartTime == start),
            _ct),
            Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_RepoReturnsEntity_ServiceReturnsSame()
    {
        // Arrange
        var bs = new BreakSession("punch-1", TimeOnly.Parse("12:00"));
        _repo.Setup(r => r.GetByIdAsync(bs.Id, _ct)).ReturnsAsync(bs);

        // Act
        var result = await _service.GetByIdAsync(bs.Id, _ct);

        // Assert
        Assert.Same(bs, result);
        _repo.Verify(r => r.GetByIdAsync(bs.Id, _ct), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_NotFound_ReturnsNull()
    {
        // Arrange
        _repo.Setup(r => r.GetByIdAsync("noid", _ct)).ReturnsAsync((BreakSession?)null);

        // Act
        var result = await _service.GetByIdAsync("noid", _ct);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task ListByPunchRecordAsync_RepoReturnsList_ServiceReturnsSame()
    {
        // Arrange
        var list = new List<BreakSession> 
        {
            new BreakSession("punch-1", TimeOnly.Parse("12:00")),
            new BreakSession("punch-1", TimeOnly.Parse("15:30"))
        };
        _repo.Setup(r => r.ListByPunchRecordAsync("punch-1", _ct)).ReturnsAsync(list);

        // Act
        var result = await _service.ListByPunchRecordAsync("punch-1", _ct);

        // Assert
        Assert.Same(list, result);
        _repo.Verify(r => r.ListByPunchRecordAsync("punch-1", _ct), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_CallsRepository()
    {
        // Arrange
        var bs = new BreakSession("punch-1", TimeOnly.Parse("13:00"));

        // Act
        await _service.UpdateAsync(bs, _ct);

        // Assert
        _repo.Verify(r => r.UpdateAsync(bs, false, _ct), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_CallsRepository()
    {
        // Act
        await _service.DeleteAsync("punch-1", _ct);

        // Assert
        _repo.Verify(r => r.DeleteAsync("punch-1", _ct), Times.Once);
    }
}

using EMSApp.Application;
using EMSApp.Domain;
using EMSApp.Domain.Entities;
using EMSApp.Infrastructure;
using Microsoft.VisualStudio.TestPlatform.Utilities;
using Moq;
using System.ComponentModel;

namespace EMSApp.Tests;

[Trait("Category", "Service")]
public class PolicyServiceTests
{
    private readonly Mock<IPolicyRepository> _repo;
    private readonly IPolicyService _service;
    private CancellationToken _ct = CancellationToken.None;

    public PolicyServiceTests()
    {
        _repo = new Mock<IPolicyRepository>();
        _service = new PolicyService(_repo.Object);
    }

    private static IDictionary<LeaveType, int> GetValidQuotas()
    {
        return Enum.GetValues(typeof(LeaveType))
                   .Cast<LeaveType>()
                   .ToDictionary(lt => lt, lt => 10);
    }

    [Fact]
    public async Task CreateAsync_ValidData_CreatesAndReturns()
    {
        // Arrange
        var year = 2025;
        var workDayStart = TimeOnly.Parse("08:00");
        var workDayEnd = workDayStart.AddHours(8);
        var punchInTolerance = new TimeSpan(0, 15, 0);
        var punchOutTolerance = new TimeSpan(0, 15, 0);
        var maxSingleBreak = new TimeSpan(0, 45, 0);
        var maxTotalBreakPerDay = new TimeSpan(2, 0, 0);
        var overtimeMultiplier = 1.5m;
        var leaveQuotas = GetValidQuotas();

        // Act
        var result = await _service.CreateAsync(
            year,
            workDayStart,
            workDayEnd,
            punchInTolerance,
            punchOutTolerance,
            maxSingleBreak,
            maxTotalBreakPerDay,
            overtimeMultiplier,
            leaveQuotas,
            _ct);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(year, result.Year);
        Assert.Equal(workDayStart, result.WorkDayStart);
        Assert.Equal(workDayEnd, result.WorkDayEnd);
        Assert.Equal(punchInTolerance, result.PunchInTolerance);
        Assert.Equal(punchOutTolerance, result.PunchOutTolerance);
        Assert.Equal(maxSingleBreak, result.MaxSingleBreak);
        Assert.Equal(maxTotalBreakPerDay, result.MaxTotalBreakPerDay);
        Assert.Equal(overtimeMultiplier, result.OvertimeMultiplier);
        Assert.Equal(leaveQuotas, result.LeaveQuotas);

        _repo.Verify(r => r.CreateAsync(
            It.Is<Policy>(p =>
                p.Year == year &&
                p.WorkDayStart == workDayStart &&
                p.WorkDayEnd == workDayEnd &&
                p.PunchInTolerance == punchInTolerance &&
                p.PunchOutTolerance == punchOutTolerance &&
                p.MaxSingleBreak == maxSingleBreak &&
                p.MaxTotalBreakPerDay == maxTotalBreakPerDay &&
                p.OvertimeMultiplier == overtimeMultiplier &&
                leaveQuotas.All(kv => p.LeaveQuotas[kv.Key] == kv.Value)),
            _ct),
            Times.Once);
    }

    [Fact]
    public async Task GetByYearAsync_RepoReturnsEntity_ServiceReturnsSame()
    {
        // Arrange
        var year = 2025;
        var workDayStart = TimeOnly.Parse("08:00");
        var workDayEnd = workDayStart.AddHours(8);
        var punchInTolerance = new TimeSpan(0, 15, 0);
        var punchOutTolerance = new TimeSpan(0, 15, 0);
        var maxSingleBreak = new TimeSpan(0, 45, 0);
        var maxTotalBreakPerDay = new TimeSpan(2, 0, 0);
        var overtimeMultiplier = 1.5m;
        var leaveQuotas = GetValidQuotas();
        var p = new Policy(
            year,
            workDayStart,
            workDayEnd,
            punchInTolerance,
            punchOutTolerance,
            maxSingleBreak,
            maxTotalBreakPerDay,
            overtimeMultiplier,
            leaveQuotas);
        _repo.Setup(r => r.GetByYearAsync(p.Year, _ct)).ReturnsAsync(p);

        // Act
        var result = await _service.GetByYearAsync(p.Year, _ct);

        // Assert
        Assert.Same(p, result);
        _repo.Verify(r => r.GetByYearAsync(p.Year, _ct), Times.Once);
    }

    [Fact]
    public async Task GetByYearAsync_NotFound_ReturnsNull()
    {
        // Arrange
        _repo.Setup(r => r.GetByYearAsync(1, _ct)).ReturnsAsync((Policy?)null);

        // Act
        var result = await _service.GetByYearAsync(1, _ct);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetAllAsync_RepoReturnsList_ServiceReturnsSame()
    {
        // Arrange
        var year = 2025;
        var workDayStart = TimeOnly.Parse("08:00");
        var workDayEnd = workDayStart.AddHours(8);
        var punchInTolerance = new TimeSpan(0, 15, 0);
        var punchOutTolerance = new TimeSpan(0, 15, 0);
        var maxSingleBreak = new TimeSpan(0, 45, 0);
        var maxTotalBreakPerDay = new TimeSpan(2, 0, 0);
        var overtimeMultiplier = 1.5m;
        var leaveQuotas = GetValidQuotas();
        var list = new List<Policy>
        {
            new Policy(
                year,
                workDayStart,
                workDayEnd,
                punchInTolerance,
                punchOutTolerance,
                maxSingleBreak,
                maxTotalBreakPerDay,
                overtimeMultiplier,
                leaveQuotas)
        };
        _repo.Setup(r => r.GetAllAsync(_ct)).ReturnsAsync(list);

        // Act
        var result = await _service.GetAllAsync(_ct);

        // Assert
        Assert.Same(list, result);
        _repo.Verify(r => r.GetAllAsync(_ct), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_CallsRepository()
    {
        // Arrange
        var year = 2025;
        var workDayStart = TimeOnly.Parse("08:00");
        var workDayEnd = workDayStart.AddHours(8);
        var punchInTolerance = new TimeSpan(0, 15, 0);
        var punchOutTolerance = new TimeSpan(0, 15, 0);
        var maxSingleBreak = new TimeSpan(0, 45, 0);
        var maxTotalBreakPerDay = new TimeSpan(2, 0, 0);
        var overtimeMultiplier = 1.5m;
        var leaveQuotas = GetValidQuotas();
        var p = new Policy(
            year,
            workDayStart,
            workDayEnd,
            punchInTolerance,
            punchOutTolerance,
            maxSingleBreak,
            maxTotalBreakPerDay,
            overtimeMultiplier,
            leaveQuotas);

        // Act
        await _service.UpdateAsync(p, _ct);

        // Assert
        _repo.Verify(r => r.UpdateAsync(p, false, _ct), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_CallsRepository()
    {
        // Act
        await _service.DeleteAsync(1, _ct);

        // Assert
        _repo.Verify(r => r.DeleteAsync(1, _ct), Times.Once);
    }
}

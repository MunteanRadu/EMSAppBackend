using EMSApp.Application.Interfaces;
using EMSApp.Infrastructure.Services;
using EMSApp.Domain;
using EMSApp.Domain.Entities;
using Moq;
using System.Text.Json;
using Xunit;
using EMSApp.Application;

[Trait("Category", "Service")]
public class ScheduleGenerationServiceTests
{
    private readonly Mock<IScheduleRepository> _scheduleRepo = new();
    private readonly Mock<IChatBotService> _chatBotService = new();
    private readonly Mock<IDepartmentRepository> _departmentRepo = new();
    private readonly Mock<ILeaveRequestRepository> _leaveRepo = new();
    private readonly Mock<IShiftRuleRepository> _shiftRuleRepo = new();
    private readonly ScheduleGenerationService _svc;

    public ScheduleGenerationServiceTests()
    {
        _svc = new ScheduleGenerationService(
            _scheduleRepo.Object,
            _chatBotService.Object,
            _departmentRepo.Object,
            _leaveRepo.Object,
            _shiftRuleRepo.Object
        );
    }

    [Fact]
    public async Task GetScheduleSuggestionJsonAsync_TrimsMarkdownAndPassesThroughJson()
    {
        // Arrange
        var deptId = "dept-1";
        var weekStart = DateOnly.FromDateTime(DateTime.Today);
        var managerId = "mgr-1";
        var dept = new Department("Engineering");
        dept.AssignManager(managerId);
        dept.AddEmployee("u1");
        dept.AddEmployee("u2");
        _departmentRepo
            .Setup(r => r.GetByIdAsync(deptId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dept);
        var leave = new LeaveRequest("u2", LeaveType.Paid, weekStart.AddDays(2), weekStart.AddDays(3), "vacation");
        leave.Approve(managerId);
        var approvedLeave = new List<LeaveRequest> { leave };
        _leaveRepo.Setup(r => r.GetApprovedLeavesForWeekAsync(It.Is<IEnumerable<string>>(ids => ids.SequenceEqual(dept.Employees)),weekStart, It.IsAny<CancellationToken>())).ReturnsAsync(approvedLeave);

        var schedules = new List<Schedule> {
            new Schedule(deptId, managerId, ShiftType.Shift1, DayOfWeek.Monday,
                TimeOnly.Parse("08:00"), TimeOnly.Parse("16:00"), true)
        };
        _scheduleRepo
            .Setup(r => r.GetByDepartmentAsync(deptId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(schedules);

        var rule = new ShiftRule(deptId, 2, 2, 1, 2, 12.0);
        _shiftRuleRepo
            .Setup(r => r.GetByDepartmentAsync(deptId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(rule);

        var rawResponse = "```json\n[{\"userId\":\"u1\",\"date\":\"2025-06-16\",\"shift\":\"Shift1\",\"startTime\":\"08:00:00\",\"endTime\":\"16:00:00\"}]\n```";
        string capturedPrompt = null!;
        _chatBotService
            .Setup(c => c.GetChatResponseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, CancellationToken>((prompt, ct) => capturedPrompt = prompt)
            .ReturnsAsync(rawResponse);

        // Act
        var result = await _svc.GetScheduleSuggestionJsonAsync(deptId, weekStart, CancellationToken.None);
        var json = result.Trim();

        // Assert
        Assert.Equal(
            "[{\"userId\":\"u1\",\"date\":\"2025-06-16\",\"shift\":\"Shift1\",\"startTime\":\"08:00:00\",\"endTime\":\"16:00:00\"}]",
            json);

        Assert.NotNull(capturedPrompt);
        
        Assert.StartsWith("You are tasked with generating employee shifts for the specified department", capturedPrompt);

        Assert.Contains("\"userId\":\"u1\"", capturedPrompt);
        Assert.Contains("\"userId\":\"u2\"", capturedPrompt);

        Assert.Contains($"minimum {rule.MinPerShift1} employees required", capturedPrompt);
        Assert.Contains($"at least {rule.MinRestHoursBetweenShifts} rest hours", capturedPrompt);
    }
}

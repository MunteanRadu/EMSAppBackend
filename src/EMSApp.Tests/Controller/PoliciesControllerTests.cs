using AutoMapper;
using EMSApp.Api.Controllers;
using EMSApp.Api;
using EMSApp.Application;
using EMSApp.Domain;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace EMSApp.Tests;

[Trait("Category", "Controller")]
public class PoliciesControllerTests
{
    private readonly Mock<IPolicyService> _svc = new();
    private readonly Mock<IMapper> _mapper = new();
    private readonly PoliciesController _ctrl;
    private readonly CancellationToken _ct = CancellationToken.None;

    public PoliciesControllerTests()
        => _ctrl = new PoliciesController(_svc.Object, _mapper.Object);

    [Fact]
    public async Task Create_ReturnsCreatedDto()
    {
        // Arrange
        var req = new CreatePolicyRequest(
            2025,
            TimeOnly.Parse("08:00"),
            TimeOnly.Parse("16:00"),
            TimeSpan.FromMinutes(15),
            TimeSpan.FromMinutes(15),
            TimeSpan.FromMinutes(45),
            TimeSpan.FromHours(2),
            1.5m,
            new Dictionary<LeaveType, int> {
                [LeaveType.Annual] = 20,
                [LeaveType.Compassionate] = 5,
                [LeaveType.Parental] = 15,
                [LeaveType.Paid] = 10,
                [LeaveType.Unpaid] = 15,
                [LeaveType.Sick] = 20,
                [LeaveType.TOIL] = 0,
                [LeaveType.Academic] = 10,
                [LeaveType.Misc] = 0,
            }
        );
        var policy = new Policy(
            req.Year,
            req.WorkDayStart, req.WorkDayEnd,
            req.PunchInTolerance, req.PunchOutTolerance,
            req.MaxSingleBreak, req.MaxTotalBreakPerDay,
            req.OvertimeMultiplier, req.LeaveQuotas
        );
        var dto = new PolicyDto
        {
            Year = policy.Year,
            WorkDayStart = policy.WorkDayStart,
            WorkDayEnd = policy.WorkDayEnd,
            PunchInTolerance = policy.PunchInTolerance,
            PunchOutTolerance = policy.PunchOutTolerance,
            MaxSingleBreak = policy.MaxSingleBreak,
            MaxTotalBreakPerDay = policy.MaxTotalBreakPerDay,
            OvertimeMultiplier = policy.OvertimeMultiplier,
            LeaveQuotas = policy.LeaveQuotas
        };

        _svc.Setup(s => s.CreateAsync(
                req.Year,
                req.WorkDayStart, req.WorkDayEnd,
                req.PunchInTolerance, req.PunchOutTolerance,
                req.MaxSingleBreak, req.MaxTotalBreakPerDay,
                req.OvertimeMultiplier, req.LeaveQuotas, _ct))
            .ReturnsAsync(policy);
        _mapper.Setup(m => m.Map<PolicyDto>(policy)).Returns(dto);

        // Act
        var result = await _ctrl.Create(req, _ct);

        // Assert
        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(dto, created.Value);
        _svc.Verify(s => s.CreateAsync(
            req.Year,
            req.WorkDayStart, req.WorkDayEnd,
            req.PunchInTolerance, req.PunchOutTolerance,
            req.MaxSingleBreak, req.MaxTotalBreakPerDay,
            req.OvertimeMultiplier, req.LeaveQuotas, _ct), Times.Once);
    }

    [Fact]
    public async Task GetByYear_Found_ReturnsDto()
    {
        // Arrange
        var p = new Policy(
            2025,
            TimeOnly.Parse("08:00"), TimeOnly.Parse("16:00"),
            TimeSpan.FromMinutes(15), TimeSpan.FromMinutes(15),
            TimeSpan.FromMinutes(45), TimeSpan.FromHours(2), 1.5m,
            new Dictionary<LeaveType, int>
            {
                [LeaveType.Annual] = 20,
                [LeaveType.Compassionate] = 5,
                [LeaveType.Parental] = 15,
                [LeaveType.Paid] = 10,
                [LeaveType.Unpaid] = 15,
                [LeaveType.Sick] = 20,
                [LeaveType.TOIL] = 0,
                [LeaveType.Academic] = 10,
                [LeaveType.Misc] = 0,
            }
        );
        var dto = new PolicyDto { Year = 2025 };
        _svc.Setup(s => s.GetByYearAsync(2025, _ct)).ReturnsAsync(p);
        _mapper.Setup(m => m.Map<PolicyDto>(p)).Returns(dto);

        // Act
        var result = await _ctrl.GetByYear(2025, _ct);

        // Assert
        Assert.Equal(dto, result.Value);
        _svc.Verify(s => s.GetByYearAsync(2025, _ct), Times.Once);
    }

    [Fact]
    public async Task GetByYear_NotFound_Returns404()
    {
        // Arrange
        _svc.Setup(s => s.GetByYearAsync(1999, _ct)).ReturnsAsync((Policy?)null);

        // Act
        var result = await _ctrl.GetByYear(1999, _ct);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task Update_Existing_CallsService()
    {
        // Arrange
        var p = new Policy(
            2025,
            TimeOnly.Parse("08:00"), TimeOnly.Parse("16:00"),
            TimeSpan.FromMinutes(15), TimeSpan.FromMinutes(15),
            TimeSpan.FromMinutes(45), TimeSpan.FromHours(2), 1.5m,
            new Dictionary<LeaveType, int>
            {
                [LeaveType.Annual] = 20,
                [LeaveType.Compassionate] = 5,
                [LeaveType.Parental] = 15,
                [LeaveType.Paid] = 10,
                [LeaveType.Unpaid] = 15,
                [LeaveType.Sick] = 20,
                [LeaveType.TOIL] = 0,
                [LeaveType.Academic] = 10,
                [LeaveType.Misc] = 0,
            }
        );
        var req = new UpdatePolicyRequest { PunchInTolerance = TimeSpan.FromMinutes(10) };
        _svc.Setup(s => s.GetByYearAsync(2025, _ct)).ReturnsAsync(p);

        // Act
        var result = await _ctrl.Update(2025, req, _ct);

        // Assert
        Assert.IsType<NoContentResult>(result);
        _svc.Verify(s => s.UpdateAsync(p, _ct), Times.Once);
    }

    [Fact]
    public async Task Update_NotFound_Returns404()
    {
        // Arrange
        _svc.Setup(s => s.GetByYearAsync(1999, _ct)).ReturnsAsync((Policy?)null);

        // Act
        var result = await _ctrl.Update(1999, new UpdatePolicyRequest(), _ct);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Delete_CallsService()
    {
        // Act
        var result = await _ctrl.Delete(2025, _ct);

        // Assert
        Assert.IsType<NoContentResult>(result);
        _svc.Verify(s => s.DeleteAsync(2025, _ct), Times.Once);
    }
}

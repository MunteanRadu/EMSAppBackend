using AutoMapper;
using EMSApp.Api.Controllers;
using EMSApp.Api;
using EMSApp.Application;
using EMSApp.Domain;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace EMSApp.Tests;

[Trait("Category", "Controller")]
public class LeaveRequestsControllerTests
{
    private readonly Mock<ILeaveRequestService> _svc = new();
    private readonly Mock<IMapper> _mapper = new();
    private readonly LeaveRequestsController _ctrl;
    private readonly CancellationToken _ct = CancellationToken.None;

    public LeaveRequestsControllerTests()
        => _ctrl = new LeaveRequestsController(_svc.Object, _mapper.Object);

    [Fact]
    public async Task Create_ReturnsCreatedDto()
    {
        // Arrange
        var req = new CreateLeaveRequestRequest("u1", LeaveType.Annual, DateOnly.Parse("2025-06-01"), DateOnly.Parse("2025-06-05"), "vacay");
        var lr = new LeaveRequest("u1", LeaveType.Annual, DateOnly.Parse("2025-06-01"), DateOnly.Parse("2025-06-05"), "vacay");
        var dto = new LeaveRequestDto
        {
            Id = lr.Id,
            UserId = lr.UserId,
            Type = lr.Type,
            StartDate = lr.StartDate,
            EndDate = lr.EndDate,
            Reason = lr.Reason,
            Status = lr.Status,
            ManagerId = null,
            RequestedAt = null
        };
        _svc.Setup(s => s.CreateAsync(req.UserId, req.Type, req.StartDate, req.EndDate, req.Reason, _ct))
            .ReturnsAsync(lr);
        _mapper.Setup(m => m.Map<LeaveRequestDto>(lr)).Returns(dto);

        // Act
        var result = await _ctrl.Create(req, _ct);

        // Assert
        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(dto, created.Value);
        _svc.Verify(s => s.CreateAsync(req.UserId, req.Type, req.StartDate, req.EndDate, req.Reason, _ct), Times.Once);
    }

    [Fact]
    public async Task GetById_Found_ReturnsDto()
    {
        // Arrange
        var lr = new LeaveRequest("u2", LeaveType.Sick, DateOnly.Parse("2025-07-01"), DateOnly.Parse("2025-07-02"), "ill");
        var dto = new LeaveRequestDto
        {
            Id = lr.Id,
            UserId = lr.UserId,
            Type = lr.Type,
            StartDate = lr.StartDate,
            EndDate = lr.EndDate,
            Reason = lr.Reason,
            Status = lr.Status,
            ManagerId = null,
            RequestedAt = null
        };
        _svc.Setup(s => s.GetByIdAsync(lr.Id, _ct)).ReturnsAsync(lr);
        _mapper.Setup(m => m.Map<LeaveRequestDto>(lr)).Returns(dto);

        // Act
        var result = await _ctrl.GetById(lr.Id, _ct);

        // Assert
        Assert.Equal(dto, result.Value);
        _svc.Verify(s => s.GetByIdAsync(lr.Id, _ct), Times.Once);
    }

    [Fact]
    public async Task Update_CallsService()
    {
        // Arrange
        var lr = new LeaveRequest("u3", LeaveType.Sick, DateOnly.Parse("2025-08-01"), DateOnly.Parse("2025-08-02"), "err");
        var req = new UpdateLeaveRequestRequest { Reason = "new reason" };
        _svc.Setup(s => s.GetByIdAsync(lr.Id, _ct)).ReturnsAsync(lr);

        // Act
        var result = await _ctrl.Update(lr.Id, req, _ct);

        // Assert
        Assert.IsType<NoContentResult>(result);
        _svc.Verify(s => s.UpdateAsync(lr, _ct), Times.Once);
    }

    [Fact]
    public async Task Delete_CallsService()
    {
        // Act
        var result = await _ctrl.Delete("x1", _ct);

        // Assert
        Assert.IsType<NoContentResult>(result);
        _svc.Verify(s => s.DeleteAsync("x1", _ct), Times.Once);
    }
}

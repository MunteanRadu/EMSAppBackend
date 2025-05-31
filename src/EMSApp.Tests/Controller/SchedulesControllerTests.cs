using AutoMapper;
using EMSApp.Api.Controllers;
using EMSApp.Api;
using EMSApp.Application;
using EMSApp.Domain;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace EMSApp.Tests;

[Trait("Category", "Controller")]
public class SchedulesControllerTests
{
    private readonly Mock<IScheduleService> _svc = new();
    private readonly Mock<IMapper> _mapper = new();
    private readonly SchedulesController _ctrl;
    private readonly CancellationToken _ct = CancellationToken.None;

    public SchedulesControllerTests()
        => _ctrl = new SchedulesController(_svc.Object, _mapper.Object);

    [Fact]
    public async Task Create_Valid_CreatesAndReturnsDto()
    {
        // Arrange
        var req = new CreateScheduleRequest(
            "dept1", "mgr1", DayOfWeek.Monday,
            TimeOnly.Parse("08:00"), TimeOnly.Parse("16:00"), true);
        var sched = new Schedule("dept1", "mgr1", DayOfWeek.Monday,
                                 req.StartTime, req.EndTime, true);
        var dto = new ScheduleDto
        {
            Id = sched.Id,
            DepartmentId = sched.DepartmentId,
            ManagerId = sched.ManagerId,
            Day = sched.Day,
            StartTime = sched.StartTime,
            EndTime = sched.EndTime,
            IsWorkingDay = sched.IsWorkingDay
        };
        _svc.Setup(s => s.CreateAsync(
                req.DepartmentId, req.ManagerId, req.Day,
                req.StartTime, req.EndTime, req.IsWorkingDay, _ct))
            .ReturnsAsync(sched);
        _mapper.Setup(m => m.Map<ScheduleDto>(sched)).Returns(dto);

        // Act
        var result = await _ctrl.Create(req, _ct);

        // Assert
        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(dto, created.Value);
        _svc.Verify(s => s.CreateAsync(
            req.DepartmentId, req.ManagerId, req.Day,
            req.StartTime, req.EndTime, req.IsWorkingDay, _ct), Times.Once);
    }

    [Fact]
    public async Task GetById_Found_ReturnsDto()
    {
        // Arrange
        var sched = new Schedule("d", "m", DayOfWeek.Tuesday,
                                  TimeOnly.Parse("09:00"), TimeOnly.Parse("17:00"), false);
        var dto = new ScheduleDto
        {
            Id = sched.Id,
            DepartmentId = sched.DepartmentId,
            ManagerId = sched.ManagerId,
            Day = sched.Day,
            StartTime = sched.StartTime,
            EndTime = sched.EndTime,
            IsWorkingDay = sched.IsWorkingDay
        };
        _svc.Setup(s => s.GetByIdAsync(sched.Id, _ct)).ReturnsAsync(sched);
        _mapper.Setup(m => m.Map<ScheduleDto>(sched)).Returns(dto);

        // Act
        var result = await _ctrl.GetById(sched.Id, _ct);

        // Assert
        Assert.Equal(dto, result.Value);
        _svc.Verify(s => s.GetByIdAsync(sched.Id, _ct), Times.Once);
    }

    [Fact]
    public async Task GetById_NotFound_Returns404()
    {
        // Arrange
        _svc.Setup(s => s.GetByIdAsync("nope", _ct)).ReturnsAsync((Schedule?)null);

        // Act
        var result = await _ctrl.GetById("nope", _ct);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task ListAll_ReturnsDtos()
    {
        // Arrange
        var list = new List<Schedule> {
                new Schedule("d","m",DayOfWeek.Wednesday,TimeOnly.Parse("08:00"),TimeOnly.Parse("12:00"),true)
            };
        var dtos = new List<ScheduleDto> {
                new ScheduleDto {
                    Id = list[0].Id,
                    DepartmentId = list[0].DepartmentId,
                    ManagerId = list[0].ManagerId,
                    Day = list[0].Day,
                    StartTime = list[0].StartTime,
                    EndTime = list[0].EndTime,
                    IsWorkingDay = list[0].IsWorkingDay
                }
            };
        _svc.Setup(s => s.GetAllAsync(_ct)).ReturnsAsync(list);
        _mapper.Setup(m => m.Map<IEnumerable<ScheduleDto>>(list)).Returns(dtos);

        // Act
        var result = await _ctrl.List(_ct);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(dtos, ok.Value);
        _svc.Verify(s => s.GetAllAsync(_ct), Times.Once);
    }

    [Fact]
    public async Task Update_Existing_CallsService()
    {
        // Arrange
        var sched = new Schedule("d", "m", DayOfWeek.Friday,
                                  TimeOnly.Parse("08:00"), TimeOnly.Parse("16:00"), true);
        var req = new UpdateScheduleRequest
        {
            StartTime = TimeOnly.Parse("09:00"),
            EndTime = TimeOnly.Parse("17:00"),
            IsWorkingDay = false
        };
        _svc.Setup(s => s.GetByIdAsync(sched.Id, _ct)).ReturnsAsync(sched);

        // Act
        var result = await _ctrl.Update(sched.Id, req, _ct);

        // Assert
        Assert.IsType<NoContentResult>(result);
        _svc.Verify(s => s.UpdateAsync(sched, _ct), Times.Once);
    }

    [Fact]
    public async Task Update_NotFound_Returns404()
    {
        // Arrange
        _svc.Setup(s => s.GetByIdAsync("nope", _ct)).ReturnsAsync((Schedule?)null);

        // Act
        var result = await _ctrl.Update("nope", new UpdateScheduleRequest(), _ct);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Delete_CallsService()
    {
        // Act
        var result = await _ctrl.Delete("id1", _ct);

        // Assert
        Assert.IsType<NoContentResult>(result);
        _svc.Verify(s => s.DeleteAsync("id1", _ct), Times.Once);
    }
}

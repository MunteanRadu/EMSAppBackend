using AutoMapper;
using EMSApp.Api.Controllers;
using EMSApp.Api;
using EMSApp.Application;
using EMSApp.Domain;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace EMSApp.Tests;

[Trait("Category", "Controller")]
public class PunchRecordsControllerTests
{
    private readonly Mock<IPunchRecordService> _svc = new();
    private readonly Mock<IMapper> _mapper = new();
    private readonly PunchRecordsController _ctrl;
    private readonly CancellationToken _ct = CancellationToken.None;

    public PunchRecordsControllerTests()
        => _ctrl = new PunchRecordsController(_svc.Object, _mapper.Object);

    [Fact]
    public async Task Create_ReturnsCreatedDto()
    {
        // Arrange
        var req = new CreatePunchRecordRequest("u1", DateOnly.Parse("2025-05-01"), TimeOnly.Parse("09:00"));
        var pr = new PunchRecord("u1", DateOnly.Parse("2025-05-01"), TimeOnly.Parse("09:00"));
        var dto = new PunchRecordDto
        {
            Id = pr.Id,
            UserId = pr.UserId,
            Date = pr.Date,
            TimeIn = pr.TimeIn,
            TimeOut = null,
            TotalHours = null,
            BreakSessions = new List<BreakSessionDto>()
        };
        _svc.Setup(s => s.CreateAsync(req.UserId, req.Date, req.TimeIn, _ct))
            .ReturnsAsync(pr);
        _mapper.Setup(m => m.Map<PunchRecordDto>(pr)).Returns(dto);

        // Act
        var result = await _ctrl.Create(req, _ct);

        // Assert
        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(dto, created.Value);
        _svc.Verify(s => s.CreateAsync(req.UserId, req.Date, req.TimeIn, _ct), Times.Once);
    }

    [Fact]
    public async Task GetById_Found_ReturnsDto()
    {
        // Arrange
        var pr = new PunchRecord("u2", DateOnly.Parse("2025-05-02"), TimeOnly.Parse("10:00"));
        var dto = new PunchRecordDto
        {
            Id = pr.Id,
            UserId = pr.UserId,
            Date = pr.Date,
            TimeIn = pr.TimeIn,
            TimeOut = null,
            TotalHours = null,
            BreakSessions = new List<BreakSessionDto>()
        };
        _svc.Setup(s => s.GetByIdAsync(pr.Id, _ct)).ReturnsAsync(pr);
        _mapper.Setup(m => m.Map<PunchRecordDto>(pr)).Returns(dto);

        // Act
        var result = await _ctrl.GetById(pr.Id, _ct);

        // Assert
        Assert.Equal(dto, result.Value);
        _svc.Verify(s => s.GetByIdAsync(pr.Id, _ct), Times.Once);
    }

    [Fact]
    public async Task Update_CallsService()
    {
        // Arrange
        var pr = new PunchRecord("u3", DateOnly.Parse("2025-05-03"), TimeOnly.Parse("11:00"));
        var req = new UpdatePunchRecordRequest { TimeOut = TimeOnly.Parse("17:00") };
        _svc.Setup(s => s.GetByIdAsync(pr.Id, _ct)).ReturnsAsync(pr);

        // Act
        var result = await _ctrl.Update(pr.Id, req, _ct);

        // Assert
        Assert.IsType<NoContentResult>(result);
        _svc.Verify(s => s.UpdateAsync(pr, _ct), Times.Once);
    }

    [Fact]
    public async Task Delete_CallsService()
    {
        // Act
        var result = await _ctrl.Delete("pr1", _ct);

        // Assert
        Assert.IsType<NoContentResult>(result);
        _svc.Verify(s => s.DeleteAsync("pr1", _ct), Times.Once);
    }
}
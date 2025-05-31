using AutoMapper;
using EMSApp.Api.Controllers;
using EMSApp.Api;
using EMSApp.Application;
using EMSApp.Domain;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace EMSApp.Tests;

[Trait("Category", "Controller")]
public class BreakSessionsControllerTests
{
    private readonly Mock<IBreakSessionService> _svc = new();
    private readonly Mock<IMapper> _mapper = new();
    private readonly BreakSessionsController _ctrl;
    private readonly CancellationToken _ct = CancellationToken.None;
    private const string PunchId = "pr1";

    public BreakSessionsControllerTests()
        => _ctrl = new BreakSessionsController(_svc.Object, _mapper.Object);

    [Fact]
    public async Task Create_ReturnsCreatedDto()
    {
        // Arrange
        var req = new CreateBreakSessionRequest("p", TimeOnly.Parse("12:00")) { };
        var bs = new BreakSession("pr1", TimeOnly.Parse("12:00"));
        var dto = new BreakSessionDto
        {
            Id = bs.Id,
            PunchRecordId = bs.PunchRecordId,
            StartTime = bs.StartTime,
            EndTime = null,
            Duration = null
        };
        _svc.Setup(s => s.CreateAsync(PunchId, req.StartTime, _ct)).ReturnsAsync(bs);
        _mapper.Setup(m => m.Map<BreakSessionDto>(bs)).Returns(dto);

        // Act
        var result = await _ctrl.Create(PunchId, req, _ct);

        // Assert
        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(dto, created.Value);
        _svc.Verify(s => s.CreateAsync(PunchId, req.StartTime, _ct), Times.Once);
    }

    [Fact]
    public async Task GetById_FoundAndMatchesPunch_ReturnsDto()
    {
        // Arrange
        var bs = new BreakSession("pr1", TimeOnly.Parse("13:00"));
        var dto = new BreakSessionDto
        {
            Id = bs.Id,
            PunchRecordId = bs.PunchRecordId,
            StartTime = bs.StartTime,
            EndTime = null,
            Duration = null
        };
        _svc.Setup(s => s.GetByIdAsync(bs.Id, _ct)).ReturnsAsync(bs);
        _mapper.Setup(m => m.Map<BreakSessionDto>(bs)).Returns(dto);

        // Act
        var result = await _ctrl.GetById(PunchId, bs.Id, _ct);

        // Assert
        Assert.Equal(dto, result.Value);
        _svc.Verify(s => s.GetByIdAsync(bs.Id, _ct), Times.Once);
    }

    [Fact]
    public async Task GetById_MismatchPunch_Returns404()
    {
        // Arrange
        var bs = new BreakSession("other", TimeOnly.Parse("14:00"));
        _svc.Setup(s => s.GetByIdAsync(bs.Id, _ct)).ReturnsAsync(bs);

        // Act
        var result = await _ctrl.GetById(PunchId, bs.Id, _ct);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task Update_CallsService()
    {
        // Arrange
        var bs = new BreakSession(PunchId, TimeOnly.Parse("15:00"));
        var req = new UpdateBreakSessionRequest { EndTime = TimeOnly.Parse("15:30") };
        _svc.Setup(s => s.GetByIdAsync(bs.Id, _ct)).ReturnsAsync(bs);

        // Act
        var result = await _ctrl.Update(PunchId, bs.Id, req, _ct);

        // Assert
        Assert.IsType<NoContentResult>(result);
        _svc.Verify(s => s.UpdateAsync(bs, _ct), Times.Once);
    }

    [Fact]
    public async Task Delete_CallsService()
    {
        // Act
        var result = await _ctrl.Delete(PunchId, "bs1", _ct);

        // Assert
        Assert.IsType<NoContentResult>(result);
        _svc.Verify(s => s.DeleteAsync("bs1", _ct), Times.Once);
    }
}

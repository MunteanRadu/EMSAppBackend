using AutoMapper;
using EMSApp.Api.Controllers;
using EMSApp.Api;
using EMSApp.Application;
using EMSApp.Domain;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace EMSApp.Tests;

[Trait("Category", "Controller")]
public class AssignmentFeedbacksControllerTests
{
    private readonly Mock<IAssignmentFeedbackService> _svc = new();
    private readonly Mock<IMapper> _mapper = new();
    private readonly AssignmentFeedbacksController _ctrl;
    private readonly CancellationToken _ct = CancellationToken.None;

    public AssignmentFeedbacksControllerTests()
        => _ctrl = new AssignmentFeedbacksController(_svc.Object, _mapper.Object);

    [Fact]
    public async Task Create_ReturnsCreatedDto()
    {
        // Arrange
        var req = new CreateAssignmentFeedbackRequest { AssignmentId = "a1", UserId = "u1", Text = "txt", Type = FeedbackType.Manager };
        var af = new AssignmentFeedback("a1", "u1", "txt", FeedbackType.Manager);
        var dto = new AssignmentFeedbackDto
        {
            Id = af.Id,
            AssignmentId = af.AssignmentId,
            UserId = af.UserId,
            Text = af.Text,
            TimeStamp = af.TimeStamp,
            Type = af.Type
        };
        _svc.Setup(s => s.CreateAsync(req.AssignmentId, req.UserId, req.Text, req.Type, _ct))
            .ReturnsAsync(af);
        _mapper.Setup(m => m.Map<AssignmentFeedbackDto>(af)).Returns(dto);

        // Act
        var result = await _ctrl.Create(req, _ct);

        // Assert
        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(dto, created.Value);
        _svc.Verify(s => s.CreateAsync(req.AssignmentId, req.UserId, req.Text, req.Type, _ct), Times.Once);
    }

    [Fact]
    public async Task GetById_Found_ReturnsDto()
    {
        // Arrange
        var af = new AssignmentFeedback("a2", "u2", "t2", FeedbackType.Employee);
        var dto = new AssignmentFeedbackDto
        {
            Id = af.Id,
            AssignmentId = af.AssignmentId,
            UserId = af.UserId,
            Text = af.Text,
            TimeStamp = af.TimeStamp,
            Type = af.Type
        };
        _svc.Setup(s => s.GetByIdAsync(af.Id, _ct)).ReturnsAsync(af);
        _mapper.Setup(m => m.Map<AssignmentFeedbackDto>(af)).Returns(dto);

        // Act
        var result = await _ctrl.GetById(af.Id, _ct);

        // Assert
        Assert.Equal(dto, result.Value);
        _svc.Verify(s => s.GetByIdAsync(af.Id, _ct), Times.Once);
    }

    [Fact]
    public async Task ListByAssignment_ReturnsDtos()
    {
        // Arrange
        var list = new List<AssignmentFeedback>{
                new AssignmentFeedback("a3","u3","t3",FeedbackType.Manager)
            };
        var dtos = new List<AssignmentFeedbackDto>{
                new AssignmentFeedbackDto {
                    Id=list[0].Id,
                    AssignmentId=list[0].AssignmentId,
                    UserId=list[0].UserId,
                    Text=list[0].Text,
                    TimeStamp=list[0].TimeStamp,
                    Type=list[0].Type
                }
            };
        _svc.Setup(s => s.ListByAssignmentAsync("a3", _ct)).ReturnsAsync(list);
        _mapper.Setup(m => m.Map<IEnumerable<AssignmentFeedbackDto>>(list)).Returns(dtos);

        // Act
        var result = await _ctrl.ListByAssignment("a3", _ct);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(dtos, ok.Value);
        _svc.Verify(s => s.ListByAssignmentAsync("a3", _ct), Times.Once);
    }

    [Fact]
    public async Task Update_CallsService()
    {
        // Arrange
        var af = new AssignmentFeedback("a4", "u4", "t4", FeedbackType.Employee);
        var req = new UpdateAssignmentFeedbackRequest { Text = "new" };
        _svc.Setup(s => s.GetByIdAsync(af.Id, _ct)).ReturnsAsync(af);

        // Act
        var result = await _ctrl.Update(af.Id, req, _ct);

        // Assert
        Assert.IsType<NoContentResult>(result);
        _svc.Verify(s => s.UpdateAsync(af, _ct), Times.Once);
    }

    [Fact]
    public async Task Delete_CallsService()
    {
        // Act
        var result = await _ctrl.Delete("idF", _ct);

        // Assert
        Assert.IsType<NoContentResult>(result);
        _svc.Verify(s => s.DeleteAsync("idF", _ct), Times.Once);
    }
}

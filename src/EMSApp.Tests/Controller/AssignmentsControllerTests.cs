using AutoMapper;
using EMSApp.Api;
using EMSApp.Application;
using EMSApp.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Reflection;

namespace EMSApp.Tests;

[Trait("Category", "Controller")]
public class AssignmentsControllerTests
{
    private readonly Mock<IAssignmentService> _svc = new();
    private readonly Mock<IMapper> _mapper = new();
    private readonly AssignmentsController _ctrl;
    private readonly CancellationToken _ct = CancellationToken.None;

    public AssignmentsControllerTests()
        => _ctrl = new AssignmentsController(_svc.Object, _mapper.Object);

    [Fact]
    public async Task Create_ReturnsCreatedDto()
    {
        // Arrange
        var req = new CreateAssignmentRequest { Title = "T", Description = "D", DueDate =  DateTime.Parse("2025-05-20"), DepartmentId = "u1" };
        var a = new Assignment("T", "D", req.DueDate, req.DepartmentId);
        var dto = new AssignmentDto
        {
            Id = a.Id,
            Title = a.Title,
            Description = a.Description,
            DueDate = a.DueDate,
            AssignedToId = a.AssignedToId,
            Status = a.Status.ToString()
        };
        _svc.Setup(s => s.CreateAsync(req.Title, req.Description, req.DueDate, req.DepartmentId, _ct))
            .ReturnsAsync(a);
        _mapper.Setup(m => m.Map<AssignmentDto>(a)).Returns(dto);

        // Act
        var result = await _ctrl.Create(req, _ct);

        // Assert
        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(dto, created.Value);
        _svc.Verify(s => s.CreateAsync(req.Title, req.Description, req.DueDate, req.DepartmentId, _ct), Times.Once);
    }

    [Fact]
    public async Task GetById_Found_ReturnsDto()
    {
        // Arrange
        var a = new Assignment("T2", "D2", DateTime.Parse("2025-05-21"), "u2");
        var dto = new AssignmentDto
        {
            Id = a.Id,
            Title = a.Title,
            Description = a.Description,
            DueDate = a.DueDate,
            AssignedToId = a.AssignedToId,
            Status = a.Status.ToString()
        };
        _svc.Setup(s => s.GetByIdAsync(a.Id, _ct)).ReturnsAsync(a);
        _mapper.Setup(m => m.Map<AssignmentDto>(a)).Returns(dto);

        // Act
        var result = await _ctrl.GetById(a.Id, _ct);

        // Assert
        Assert.Equal(dto, result.Value);
        _svc.Verify(s => s.GetByIdAsync(a.Id, _ct), Times.Once);
    }

    [Fact]
    public async Task ListByAssignee_CallsService()
    {
        // Arrange
        var list = new List<Assignment>
        {
            new Assignment("T","D",DateTime.Now,"u3")
        };

        var dtos = new List<AssignmentDto>

        {
            new AssignmentDto {
                Id = list[0].Id,
                Title = list[0].Title,
                Description = list[0].Description,
                DueDate = list[0].DueDate,
                AssignedToId = list[0].AssignedToId,
                Status = list[0].Status.ToString()
            }
        };

        _svc.Setup(s => s.ListByAsigneeAsync("u3", _ct))
            .ReturnsAsync(list);

        _mapper
          .Setup(m => m.Map<AssignmentDto>(list[0]))
          .Returns(dtos[0]);

        // Act
        var result = await _ctrl.ListBySomething("u3", null, null, _ct);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var returned = Assert.IsAssignableFrom<IEnumerable<AssignmentDto>>(ok.Value);
        // materialize to List to compare
        var returnedList = returned.ToList();
        Assert.Equal(dtos, returnedList);

        _svc.Verify(s => s.ListByAsigneeAsync("u3", _ct), Times.Once);
    }

    [Fact]
    public async Task Update_CallsService()
    {
        // Arrange
        var a = new Assignment("T", "D", DateTime.Now, "u4");
        var req = new UpdateAssignmentRequest { Title = "new" };
        _svc.Setup(s => s.GetByIdAsync(a.Id, _ct)).ReturnsAsync(a);

        // Act
        var result = await _ctrl.Update(a.Id, req, _ct);

        // Assert
        Assert.IsType<NoContentResult>(result);
        _svc.Verify(s => s.UpdateAsync(a, _ct), Times.Once);
    }

    [Fact]
    public async Task Delete_CallsService()
    {
        // Act
        var result = await _ctrl.Delete("idA", _ct);

        // Assert
        Assert.IsType<NoContentResult>(result);
        _svc.Verify(s => s.DeleteAsync("idA", _ct), Times.Once);
    }
}

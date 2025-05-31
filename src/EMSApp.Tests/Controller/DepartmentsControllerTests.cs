using AutoMapper;
using EMSApp.Api.Controllers;
using EMSApp.Api;
using EMSApp.Application;
using EMSApp.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace EMSApp.Tests;

[Trait("Category", "Controller")]
public class DepartmentsControllerTests
{
    private readonly Mock<IDepartmentService> _svc = new();
    private readonly Mock<IMapper> _mapper = new();
    private readonly DepartmentsController _ctrl;
    private readonly CancellationToken _ct = CancellationToken.None;

    public DepartmentsControllerTests()
        => _ctrl = new DepartmentsController(_svc.Object, _mapper.Object);

    [Fact]
    public async Task Create_ReturnsCreatedDto()
    {
        // Arrange
        var req = new CreateDepartmentRequest(Name: "HR", ManagerId: "mgr1");
        var dept = new Department("HR", "mgr1");
        var dto = new DepartmentDto
        {
            Id = dept.Id,
            Name = dept.Name,
            ManagerId = dept.ManagerId,
            Employees = new List<string>()
        };
        _svc.Setup(s => s.CreateAsync(req.Name, req.ManagerId, _ct))
            .ReturnsAsync(dept);
        _mapper.Setup(m => m.Map<DepartmentDto>(dept)).Returns(dto);

        // Act
        var result = await _ctrl.Create(req, _ct);

        // Assert
        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(dto, created.Value);
        _svc.Verify(s => s.CreateAsync(req.Name, req.ManagerId, _ct), Times.Once);
    }

    [Fact]
    public async Task GetById_Found_ReturnsDto()
    {
        // Arrange
        var dept = new Department("IT", "mgr2");
        var dto = new DepartmentDto
        {
            Id = dept.Id,
            Name = dept.Name,
            ManagerId = dept.ManagerId,
            Employees = new List<string>()
        };
        _svc.Setup(s => s.GetByIdAsync(dept.Id, _ct)).ReturnsAsync(dept);
        _mapper.Setup(m => m.Map<DepartmentDto>(dept)).Returns(dto);

        // Act
        var result = await _ctrl.GetById(dept.Id, _ct);

        // Assert
        Assert.Equal(dto, result.Value);
        _svc.Verify(s => s.GetByIdAsync(dept.Id, _ct), Times.Once);
    }

    [Fact]
    public async Task List_ReturnsDtos()
    {
        // Arrange
        var list = new List<Department> { new Department("Ops", "mgr3") };
        var dtos = new List<DepartmentDto> {
                new DepartmentDto {
                    Id=list[0].Id,
                    Name=list[0].Name,
                    ManagerId=list[0].ManagerId,
                    Employees=new List<string>()
                }
            };
        _svc.Setup(s => s.GetAllAsync(_ct)).ReturnsAsync(list);
        _mapper.Setup(m => m.Map<IEnumerable<DepartmentDto>>(list)).Returns(dtos);

        // Act
        var result = await _ctrl.List(_ct);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(dtos, ok.Value);
        _svc.Verify(s => s.GetAllAsync(_ct), Times.Once);
    }

    [Fact]
    public async Task Update_CallsService()
    {
        // Arrange
        var dept = new Department("Sales", "mgr4");
        var req = new UpdateDepartmentRequest { Name = "Sales2", ManagerId = "mgr5" };
        _svc.Setup(s => s.GetByIdAsync(dept.Id, _ct)).ReturnsAsync(dept);

        // Act
        var result = await _ctrl.Update(dept.Id, req, _ct);

        // Assert
        Assert.IsType<NoContentResult>(result);
        _svc.Verify(s => s.UpdateAsync(dept, _ct), Times.Once);
    }

    [Fact]
    public async Task Delete_CallsService()
    {
        // Act
        var result = await _ctrl.Delete("idX", _ct);

        // Assert
        Assert.IsType<NoContentResult>(result);
        _svc.Verify(s => s.DeleteAsync("idX", _ct), Times.Once);
    }

    [Fact]
    public async Task AddEmployee_CallsService()
    {
        // Act
        var result = await _ctrl.AddEmployee("idX", new AddDepartmentEmployeeRequest(UserId: "u1"), _ct);

        // Assert
        Assert.IsType<NoContentResult>(result);
        _svc.Verify(s => s.AddEmployeeAsync("idX", "u1", _ct), Times.Once);
    }

    [Fact]
    public async Task RemoveEmployee_CallsService()
    {
        // Act
        var result = await _ctrl.RemoveEmployee("idX", "u1", _ct);

        // Assert
        Assert.IsType<NoContentResult>(result);
        _svc.Verify(s => s.RemoveEmployeeAsync("idX", "u1", _ct), Times.Once);
    }
}

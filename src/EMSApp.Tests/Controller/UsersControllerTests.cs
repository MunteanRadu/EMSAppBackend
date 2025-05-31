using AutoMapper;
using EMSApp.Api;
using EMSApp.Application;
using EMSApp.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace EMSApp.Tests;

[Trait("Category", "Controller")]
public class UsersControllerTests
{
    private readonly Mock<IUserService> _svc = new();
    private readonly Mock<IMapper> _mapper = new();
    private readonly UsersController _ctrl;
    private readonly CancellationToken _ct = CancellationToken.None;

    public UsersControllerTests()
        => _ctrl = new UsersController(_svc.Object, _mapper.Object);

    [Fact]
    public async Task Create_CallsServiceAndReturnsCreated() 
    {
        // Arrange
        var req = new CreateUserRequest(Email: "email@email.com", Username: "username", PasswordHash: "validpassword", DepartmentId: "d");
        var user = new User("email@email.com", "username", "validpassword", "d");
        var dto = new UserDto { Id = user.Id, Email = user.Email, Username = user.Username, DepartmentId = user.DepartmentId };

        _svc.Setup(s => s.CreateAsync(req.Email, req.Username, req.PasswordHash, req.DepartmentId, _ct))
            .ReturnsAsync(user);
        _mapper.Setup(m => m.Map<UserDto>(user)).Returns(dto);

        // Act
        var result = await _ctrl.Create(req, _ct);

        // Assert
        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(dto, created.Value);
        _svc.Verify(s => s.CreateAsync(req.Email, req.Username, req.PasswordHash, req.DepartmentId, _ct), Times.Once);
    }

    [Fact]
    public async Task GetById_Found_ReturnsDto()
    {
        // Arrange
        var user = new User("email@email.com", "username", "validpassword", "d");
        var dto = new UserDto { Id = user.Id, Email = user.Email, Username = user.Username, DepartmentId = user.DepartmentId };
        _svc.Setup(s => s.GetByIdAsync(user.Id, _ct)).ReturnsAsync(user);
        _mapper.Setup(m => m.Map<UserDto>(user)).Returns(dto);

        // Act
        var result = await _ctrl.GetById(user.Id, _ct);

        // Assert
        Assert.Equal(dto, result.Value);
        _svc.Verify(s => s.GetByIdAsync(user.Id, _ct), Times.Once);
    }

    [Fact]
    public async Task GetById_NotFound_Returns404()
    {
        // Arrange
        _svc.Setup(s => s.GetByIdAsync("nope", _ct)).ReturnsAsync((User?)null);

        // Act & Assert
        var result = await _ctrl.GetById("nope", _ct);
        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task ListByDept_NoDept_CallsListAll()
    {
        // Arrange
        var users = new List<User> { new User("email@email.com", "username", "validpassword", "d") };
        var dtos = new List<UserDto> { new UserDto { Id = "i", Email = "email@email.com", Username = "username", DepartmentId = "d" } };
        _svc.Setup(s => s.GetAllAsync(_ct)).ReturnsAsync(users);
        _mapper.Setup(m => m.Map<IEnumerable<UserDto>>(users)).Returns(dtos);

        // Act
        var result = await _ctrl.ListByDept(null, _ct);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(dtos, ok.Value);
        _svc.Verify(s => s.GetAllAsync(_ct), Times.Once);
    }

    [Fact]
    public async Task ListByDept_WithDept_CallsListByDepartment()
    {
        // Arrange
        var dept = "d1";
        var users = new List<User> { new User("email@email.com", "username", "validpassword", dept) };
        var dtos = new List<UserDto> { new UserDto{ Id = "i", Email = "email@email.com", Username = "username", DepartmentId = dept } };
        _svc.Setup(s => s.ListByDepartmentAsync(dept, _ct)).ReturnsAsync(users);
        _mapper.Setup(m => m.Map<IEnumerable<UserDto>>(users)).Returns(dtos);

        // Act
        var result = await _ctrl.ListByDept(dept, _ct);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(dtos, ok.Value);
        _svc.Verify(s => s.ListByDepartmentAsync(dept, _ct), Times.Once);
    }

    [Fact]
    public async Task Update_ExistingUser_CallsService()
    {
        // Arrange
        var user = new User("email@email.com", "username", "validpassword", "d");
        var req = new UpdateUserRequest { PasswordHash = "validpassword", DepartmentId = "d2" };
        _svc.Setup(s => s.GetByIdAsync(user.Id, _ct)).ReturnsAsync(user);

        // Act
        var result = await _ctrl.Update(user.Id, req, _ct);

        // Assert
        Assert.IsType<NoContentResult>(result);
        _svc.Verify(s => s.UpdateAsync(user, _ct), Times.Once);
    }

    [Fact]
    public async Task Update_NotFound_Returns404()
    {
        // Arrange
        _svc.Setup(s => s.GetByIdAsync("nope", _ct)).ReturnsAsync((User?)null);

        // Act
        var result = await _ctrl.Update("nope", new UpdateUserRequest(), _ct);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Delete_CallsService()
    {
        // Act
        var result = await _ctrl.Delete("i1", _ct);

        // Assert
        Assert.IsType<NoContentResult>(result);
        _svc.Verify(s => s.DeleteAsync("i1", _ct), Times.Once);
    }
}

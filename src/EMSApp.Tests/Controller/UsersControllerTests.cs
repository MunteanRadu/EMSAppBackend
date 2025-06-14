using AutoMapper;
using EMSApp.Api;
using EMSApp.Application;
using EMSApp.Application.Interfaces;
using EMSApp.Domain.Entities;
using Intercom.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using Xunit;
using User = EMSApp.Domain.Entities.User;

namespace EMSApp.Tests;

[Trait("Category", "Controller")]
public class UsersControllerTests
{
    private readonly Mock<IUserService> _svc = new();
    private readonly Mock<IDepartmentService> _deptSvc = new();
    private readonly Mock<IMapper> _mapper = new();
    private readonly UsersController _ctrl;
    private readonly CancellationToken _ct = CancellationToken.None;

    public UsersControllerTests()
    {
        _ctrl = new UsersController(_svc.Object, _deptSvc.Object, _mapper.Object);
    }

    private void SetUserRole(string role, string userId = "caller")
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Role, role),
            new Claim(ClaimTypes.NameIdentifier, userId)
        };
        var identity = new ClaimsIdentity(claims, "test");
        _ctrl.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };
    }

    [Fact]
    public async void Create_Returns_CreatedAtAction()
    {
        var req = new CreateUserRequest("a@b", "alice", "password", "d1");
        var user = new User(req.Email, req.Username, req.PasswordHash, req.DepartmentId);
        var dto = new UserDto(user.Id, user.Email, user.Username, user.DepartmentId, null, null, 0, "");
        _svc.Setup(s => s.CreateAsync(req.Email, req.Username, req.PasswordHash, req.DepartmentId, _ct))
            .ReturnsAsync(user);
        _mapper.Setup(m => m.Map<UserDto>(user)).Returns(dto);

        var action = await _ctrl.Create(req, _ct);
        var created = Assert.IsType<CreatedAtActionResult>(action.Result);
        Assert.Equal(nameof(_ctrl.GetById), created.ActionName);
        Assert.Equal(dto, created.Value);
        _svc.Verify(s => s.CreateAsync(req.Email, req.Username, req.PasswordHash, req.DepartmentId, _ct), Times.Once);
    }

    [Fact]
    public async void GetById_UserExists_ReturnsDto()
    {
        var user = new User("e@x", "bob", "password", "d");
        var dto = new UserDto(user.Id, user.Email, user.Username, user.DepartmentId, null, null, 0, "");
        _svc.Setup(s => s.GetByIdAsync(user.Id, _ct)).ReturnsAsync(user);
        _mapper.Setup(m => m.Map<UserDto>(user)).Returns(dto);

        var result = await _ctrl.GetById(user.Id, _ct);
        Assert.Equal(dto, result.Value);
        _svc.Verify(s => s.GetByIdAsync(user.Id, _ct), Times.Once);
    }

    [Fact]
    public async void GetById_NotFound_Returns404()
    {
        _svc.Setup(s => s.GetByIdAsync("no", _ct)).ReturnsAsync((User?)null);
        var result = await _ctrl.GetById("no", _ct);
        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async void GetProfile_NoUser_Returns404()
    {
        _svc.Setup(s => s.GetByIdAsync("u", _ct)).ReturnsAsync((User?)null);
        var result = await _ctrl.GetProfile("u", _ct);
        Assert.IsType<NotFoundResult>(result);
    }

[Fact]
    public async void GetProfile_WithProfile_ReturnsOk()
    {
        var user = new User("e@a", "user", "password", "d");
        user.UpdateProfile(new UserProfile("Name", 25, "0712345678", "Address 123", "0712345671"));
        var dto = new UserProfileDto { Name = "Name", Age = 25, Phone = "555", Address = "Addr", EmergencyContact = "0712345671" };
        _svc.Setup(s => s.GetByIdAsync("u", _ct)).ReturnsAsync(user);
        _mapper.Setup(m => m.Map<UserProfileDto>(user.Profile!)).Returns(dto);

        var ok = Assert.IsType<OkObjectResult>(await _ctrl.GetProfile("u", _ct));
        Assert.Equal(dto, ok.Value);
    }

    [Fact]
    public async void ListAll_ReturnsOkWithDtos()
    {
        var users = new List<User> { new User("a@a", "user", "password", "d") };
        var dtos = new List<UserDto> { new UserDto("i", "a@a", "user", "d", null, null, 0, "") };
        _svc.Setup(s => s.GetAllAsync(_ct)).ReturnsAsync(users);
        _mapper.Setup(m => m.Map<IEnumerable<UserDto>>(users)).Returns(dtos);

        var ok = Assert.IsType<OkObjectResult>((await _ctrl.ListAll(_ct)).Result);
        Assert.Equal(dtos, ok.Value);
    }

    [Fact]
    public async void ListByDept_WithAndWithoutParam()
    {
        var users = new List<User> { new User("a@a", "user", "password", "d") };
        var dtos = new List<UserDto> { new UserDto("i", "a@a", "user", "d", null, null, 0, "") };

        _svc.Setup(s => s.GetAllAsync(_ct)).ReturnsAsync(users);
        _svc.Setup(s => s.ListByDepartmentAsync("d", _ct)).ReturnsAsync(users);
        _mapper.Setup(m => m.Map<IEnumerable<UserDto>>(users)).Returns(dtos);

        var ok1 = Assert.IsType<OkObjectResult>((await _ctrl.ListByDept(null, _ct)).Result);
        Assert.Equal(dtos, ok1.Value);

        var ok2 = Assert.IsType<OkObjectResult>((await _ctrl.ListByDept("d", _ct)).Result);
        Assert.Equal(dtos, ok2.Value);
    }

    [Fact]
    public async void ListByRole_WithAndWithoutParam()
    {
        var users = new List<User> { new User("a@a", "user", "password", "d") };
        var dtos = new List<UserDto> { new UserDto("i", "a@a", "user", "d", UserRole.Admin, null, 0, "") };

        _svc.Setup(s => s.GetAllAsync(_ct)).ReturnsAsync(users);
        _svc.Setup(s => s.ListByRoleAsync(UserRole.Admin, _ct)).ReturnsAsync(users);
        _mapper.Setup(m => m.Map<IEnumerable<UserDto>>(users)).Returns(dtos);

        var ok1 = Assert.IsType<OkObjectResult>((await _ctrl.ListByRole(null, _ct)).Result);
        Assert.Equal(dtos, ok1.Value);

        var ok2 = Assert.IsType<OkObjectResult>((await _ctrl.ListByRole(UserRole.Admin, _ct)).Result);
        Assert.Equal(dtos, ok2.Value);
    }

    [Fact]
    public async void UpdateSalary_AdminVsManagerBehavior()
    {
        var user = new User("e@a", "user", "password", "d");
        _svc.Setup(s => s.GetByIdAsync("u", _ct)).ReturnsAsync(user);

        SetUserRole("admin", "adminId");
        var req = new UpdateSalaryRequest(Salary: 123m);
        var r1 = await _ctrl.UpdateSalary("u", req, _ct);
        Assert.IsType<NoContentResult>(r1);
        _svc.Verify(s => s.UpdateAsync(user, _ct), Times.Once);

        user.UpdateDepartment("d2");
        SetUserRole("manager", "mgrId");
        var mgr = new User("e2@a", "masd", "password", "d2");
        _svc.Setup(s => s.GetByIdAsync("mgrId", _ct)).ReturnsAsync(mgr);
        user.UpdateDepartment("d2");
        var r2 = await _ctrl.UpdateSalary("u", req, _ct);
        Assert.IsType<NoContentResult>(r2);

        user.UpdateDepartment("other");
        var r3 = await _ctrl.UpdateSalary("u", req, _ct);
        Assert.IsType<ForbidResult>(r3);
    }

    [Fact]
    public async void UpdateJobTitle_SimilarToSalary()
    {
        var user = new User("e@a", "user", "password", "d");
        _svc.Setup(s => s.GetByIdAsync("u", _ct)).ReturnsAsync(user);
        SetUserRole("admin", "a");
        var req = new UpdateJobTitleRequest(JobTitle: "Dev");
        var r1 = await _ctrl.UpdateJobTitle("u", req, _ct);
        Assert.IsType<NoContentResult>(r1);

        SetUserRole("manager", "mId");
        var mgr = new User("e2@a", "mnbv", "password", "d");
        _svc.Setup(s => s.GetByIdAsync("mId", _ct)).ReturnsAsync(mgr);
        var r2 = await _ctrl.UpdateJobTitle("u", req, _ct);
        Assert.IsType<NoContentResult>(r2);
    }

    public async void UpdateDepartment_ManagerAssignment()
    {
        var user = new User("e@a", "user", "password", "dOld");
        user.UpdateRole(UserRole.Manager);
        _svc.Setup(s => s.GetByIdAsync("u", _ct)).ReturnsAsync(user);

        var dept = new Department("new");
        dept.AssignManager("oldMgr");
        _deptSvc.Setup(d => d.GetByIdAsync("new", _ct)).ReturnsAsync(dept);

        SetUserRole("admin", "any");
        var req = new UpdateUserDepartmentRequest(DepartmentId: "new");
        var result = await _ctrl.UpdateDepartment("u", req, _ct);

        Assert.IsType<NoContentResult>(result);
        Assert.Equal("u", dept.ManagerId);
        _deptSvc.Verify(d => d.UpdateAsync(dept, _ct), Times.Once);
    }

[Fact]
    public async void Update_FullPut_AndProfile()
    {
        var user = new User("e@a", "user", "password", "d");
        var dept = new Department("d2");
        dept.AssignManager("x");
        _svc.Setup(s => s.GetByIdAsync("u", _ct)).ReturnsAsync(user);
        _deptSvc.Setup(d => d.GetByIdAsync("d2", _ct)).ReturnsAsync(dept);
        SetUserRole("admin", "any");

        var put = new UpdateUserRequest(
            Email: "E2@a", Username: "User2", PasswordHash: "Password2",
            DepartmentId: "d2", Salary: 50m, JobTitle: "T2",
            Profile: new UserProfileDto { Name = "Name", Age = 30, Phone = "0712345678", Address = "Address 123", EmergencyContact = "0712345671" },
            Role: UserRole.Manager
        );

        var result = await _ctrl.Update("u", put, _ct);
        Assert.IsType<NoContentResult>(result);

        // Profile separately
        _svc.Setup(s => s.GetByIdAsync("u", _ct)).ReturnsAsync(user);
        var upr = new UpdateUserProfileRequest {
            Name = "NewName",
            Age = 28, 
            Phone = "0712345678", 
            Address = "Address 1232", 
            EmergencyContact = "0709876543"
        };
        var r2 = await _ctrl.UpdateProfile("u", upr, _ct);
        Assert.IsType<NoContentResult>(r2);
    }

    [Fact]
    public async void Delete_ReturnsNoContent()
    {
        var r = await _ctrl.Delete("u", _ct);
        Assert.IsType<NoContentResult>(r);
        _svc.Verify(s => s.DeleteAsync("u", _ct), Times.Once);
    }
}

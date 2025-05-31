using AutoMapper;
using EMSApp.Application;
using EMSApp.Domain.Entities;
using EMSApp.Domain.Exceptions;
using EMSApp.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EMSApp.Api;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IUserService _service;
    private readonly IDepartmentService _departmentService;
    private readonly IMapper _mapper;
    public UsersController(IUserService service, IDepartmentService departmentService, IMapper mapper)
    {
        _service = service;
        _departmentService = departmentService;
        _mapper = mapper;
    }

    [Authorize(Roles ="admin,manager")]
    [HttpPost]
    public async Task<ActionResult<UserDto>> Create(
        [FromBody] CreateUserRequest req, 
        CancellationToken ct)
    {
        var user = await _service.CreateAsync(req.Email, req.Username, req.PasswordHash, req.DepartmentId, ct);
        var dto = _mapper.Map<UserDto>(user);
        return CreatedAtAction(
            nameof(GetById), 
            new { id = user.Id }, 
            dto);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<UserDto>> GetById(string id, CancellationToken ct)
    {
        var user = await _service.GetByIdAsync(id, ct);
        if(user is null) return NotFound();

        var dto = _mapper.Map<UserDto>(user);
        return dto;
    }

    [HttpGet("{id}/profile")]
    public async Task<IActionResult> GetProfile(string id, CancellationToken ct)
    {
        var user = await _service.GetByIdAsync(id, ct);
        if (user is null) return NotFound();

        if (user.Profile is null)
            return NotFound();

        var dto = _mapper.Map<UserProfileDto>(user.Profile);
        return Ok(dto);
    }


    [HttpGet]
    public async Task<ActionResult<List<UserDto>>> ListAll(CancellationToken ct)
    {
        var list = await _service.GetAllAsync(ct);
        return Ok(_mapper.Map<IEnumerable<UserDto>>(list));
    }

    [HttpGet("byDepartment")]
    public async Task<ActionResult<List<UserDto>>> ListByDept(
        [FromQuery] string? departmentId,
        CancellationToken ct)
    {
        var users = departmentId is null
            ? await _service.GetAllAsync(ct)
            : await _service.ListByDepartmentAsync(departmentId, ct);

        var dtos = _mapper.Map<IEnumerable<UserDto>>(users);
        return Ok(dtos);
    }

    [HttpGet("listByRole")]
    public async Task<ActionResult<List<UserDto>>> ListByRole(
        [FromQuery] UserRole? role,
        CancellationToken ct)
    {
        var users = role is null
            ? await _service.GetAllAsync(ct)
            : await _service.ListByRoleAsync(role, ct);

        var dtos = _mapper.Map<IEnumerable<UserDto>>(users);
        return Ok(dtos);
    }

    [Authorize(Roles = "admin,manager")]
    [HttpPatch("{id}/salary")]
    public async Task<IActionResult> UpdateSalary(
        string id,
        [FromBody] UpdateSalaryRequest req,
        CancellationToken ct)
    {
        var user = await _service.GetByIdAsync(id, ct);
        if (user is null) return NotFound();

        if (User.IsInRole("manager"))
        {
            var callerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var callerUser = await _service.GetByIdAsync(callerId, ct);
            if (user.DepartmentId != callerUser.DepartmentId)
                return Forbid();
        }

        user.UpdateSalary(req.Salary, User.IsInRole("admin") ? "admin" : "manager");
        await _service.UpdateAsync(user, ct);
        return NoContent();
    }

    [Authorize(Roles = "admin,manager")]
    [HttpPatch("{id}/jobtitle")]
    public async Task<IActionResult> UpdateJobTitle(
        string id,
        [FromBody] UpdateJobTitleRequest req,
        CancellationToken ct)
    {
        var user = await _service.GetByIdAsync(id, ct);
        if (user is null) return NotFound();

        if (User.IsInRole("manager"))
        {
            var callerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var callerUser = await _service.GetByIdAsync(callerId, ct);
            if (user.DepartmentId != callerUser.DepartmentId)
                return Forbid();
        }
        user.UpdateJobTitle(req.JobTitle);
        await _service.UpdateAsync(user, ct);
        return NoContent();
    }

    [Authorize(Roles = "manager,admin")]
    [HttpPatch("{id}/department")]
    public async Task<IActionResult> UpdateDepartment(
    string id,
    [FromBody] UpdateUserDepartmentRequest req,
    CancellationToken ct)
    {
        var user = await _service.GetByIdAsync(id, ct);
        if (user is null) return NotFound();
        user.UpdateDepartment(req.DepartmentId);
        await _service.UpdateAsync(user, ct);
        if (user.Role == UserRole.Manager && req.DepartmentId != null)
        {
            var dept = await _departmentService.GetByIdAsync(req.DepartmentId, ct);
            if (dept != null)
            {
                dept.AssignManager(user.Id);
                await _departmentService.UpdateAsync(dept, ct);
            }
        }
        return NoContent();
    }

    [Authorize(Roles ="admin")]
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateUserRequest req, CancellationToken ct)
    {
        var user = await _service.GetByIdAsync(id, ct);
        if (user is null) return NotFound();

        if (req.Email is not null) user.UpdateEmail(req.Email);
        if (req.Username is not null) user.UpdateUsername(req.Username);
        if (req.PasswordHash is not null) user.UpdatePassword(req.PasswordHash);
        if (req.DepartmentId is not null) user.UpdateDepartment(req.DepartmentId);
        if (req.Salary is not null) user.UpdateSalary((decimal)req.Salary, "admin");
        if(req.JobTitle is not null) user.UpdateJobTitle(req.JobTitle);
        //if (req.Profile is not null) user.UpdateProfile(_mapper.Map<UserProfileDto>(req.Profile));
        if (req.Role is not null) user.UpdateRole((UserRole)req.Role);

        await _service.UpdateAsync(user, ct);
        if (user.Role == UserRole.Manager && req.DepartmentId != null)
        {
            var dept = await _departmentService.GetByIdAsync(req.DepartmentId, ct);
            if (dept != null)
            {
                dept.AssignManager(user.Id);
                await _departmentService.UpdateAsync(dept, ct);
            }
        }
        return NoContent();
    }

    [HttpPut("{id}/profile")]
    public async Task<IActionResult> UpdateProfile(
    string id,
    [FromBody] UpdateUserProfileRequest req,
    CancellationToken ct)
    {
        var user = await _service.GetByIdAsync(id, ct);
        if (user is null) return NotFound();

        if (user.Profile is null)
        {
            var profile = new UserProfile(
                req.Name,
                (int)req.Age,
                req.Phone,
                req.Address,
                req.EmergencyContact
            );
            user.UpdateProfile(profile);
        }
        else
        {
            user.Profile.Update(
                req.Name,
                (int)req.Age,
                req.Phone,
                req.Address,
                req.EmergencyContact
            );
        }

        await _service.UpdateAsync(user, ct);

        return NoContent();
    }

    [Authorize(Roles = "admin,manager")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
    {
        await _service.DeleteAsync(id, ct);
        return NoContent();
    }
}

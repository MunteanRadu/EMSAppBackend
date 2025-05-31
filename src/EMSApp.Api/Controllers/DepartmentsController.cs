using AutoMapper;
using EMSApp.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EMSApp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DepartmentsController : ControllerBase
{
    private readonly IDepartmentService _service;
    private readonly IMapper _mapper;

    public DepartmentsController(IDepartmentService service, IMapper mapper)
    {
        _service = service;
        _mapper = mapper;
    }

    [Authorize(Roles = "admin,manager")]
    [HttpPost]
    public async Task<ActionResult<DepartmentDto>> Create(
        [FromBody] CreateDepartmentRequest req,
        CancellationToken ct)
    {
        var d = await _service.CreateAsync(req.Name, ct);
        return CreatedAtAction(
            nameof(GetById),
            new { id = d.Id },
            _mapper.Map<DepartmentDto>(d));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<DepartmentDto>> GetById(
        string id,
        CancellationToken ct)
    {
        var d = await _service.GetByIdAsync(id, ct);
        if (d is null) return NotFound();
        return _mapper.Map<DepartmentDto>(d);
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<DepartmentDto>>> List(
        CancellationToken ct)
    {
        var list = await _service.GetAllAsync(ct);
        return Ok(_mapper.Map<IEnumerable<DepartmentDto>>(list));
    }

    [Authorize(Roles = "admin,manager")]
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(
        string id,
        [FromBody] UpdateDepartmentRequest req,
        CancellationToken ct)
    {
        var d = await _service.GetByIdAsync(id, ct);
        if (d is null) return NotFound();

        if (req.Name is not null) d.UpdateName(req.Name);
        if (req.ManagerId is not null) d.AssignManager(req.ManagerId);

        await _service.UpdateAsync(d, ct);
        return NoContent();
    }

    [Authorize(Roles = "admin,manager")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(
        string id,
        CancellationToken ct)
    {
        await _service.DeleteAsync(id, ct);
        return NoContent();
    }

    [Authorize(Roles = "admin,manager")]
    [HttpPost("{id}/employees")]
    public async Task<IActionResult> AddEmployee(
        string id,
        [FromBody] AddDepartmentEmployeeRequest req,
        CancellationToken ct)
    {
        await _service.AddEmployeeAsync(id, req.UserId, ct);
        return NoContent();
    }

    [Authorize(Roles = "admin,manager")]
    [HttpDelete("{id}/employees/{userId}")]
    public async Task<IActionResult> RemoveEmployee(
        string id,
        string userId,
        CancellationToken ct)
    {
        await _service.RemoveEmployeeAsync(id, userId, ct);
        return NoContent();
    }
}

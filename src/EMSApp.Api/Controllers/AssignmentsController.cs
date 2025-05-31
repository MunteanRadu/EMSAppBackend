using AutoMapper;
using EMSApp.Application;
using EMSApp.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace EMSApp.Api;

[ApiController]
[Route("api/[controller]")]
public class AssignmentsController : ControllerBase
{
    private readonly IAssignmentService _service;
    private readonly IMapper _mapper;
    public AssignmentsController(IAssignmentService service, IMapper mapper)
    {
        _service = service;
        _mapper = mapper;
    }

    [Authorize(Roles = "admin,manager")]
    [HttpPost]
    public async Task<ActionResult<AssignmentDto>> Create(
        [FromBody] CreateAssignmentRequest req,
        CancellationToken ct)
    {
        var a = await _service.CreateAsync(
            req.Title,
            req.Description,
            req.DueDate,
            req.DepartmentId,
            req.ManagerId,
            ct);

        var dto = _mapper.Map<AssignmentDto>(a);
        return CreatedAtAction(
            nameof(GetById),
            new { id = a.Id },
            dto);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<AssignmentDto>> GetById(string id, CancellationToken ct)
    {
        var a = await _service.GetByIdAsync(id, ct);
        if (a is null) return NotFound();
        return _mapper.Map<AssignmentDto>(a);
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<AssignmentDto>>> ListAll(
        CancellationToken ct)
    {
        var list = await _service.GetAllAsync(ct);
        return Ok(_mapper.Map<IEnumerable<AssignmentDto>>(list));
    }

    [HttpGet("listBySomething")]
    public async Task<ActionResult<IEnumerable<AssignmentDto>>> ListBySomething(
        [FromQuery(Name = "asignee")] string? userId,
        [FromQuery(Name = "overdueAsOf")] DateTime? asOf,
        [FromQuery(Name = "status")] AssignmentStatus? status,
        [FromQuery(Name = "departmentId")] string? departmentId,
        CancellationToken ct)
    {
        IReadOnlyList<Assignment> list;

        if (userId is not null)
            list = await _service.ListAsync(a => a.AssignedToId == userId, ct);
        else if (asOf is not null)
            list = await _service.ListAsync(a => a.DueDate < asOf && (a.Status == AssignmentStatus.Pending || a.Status == AssignmentStatus.InProgress), ct);
        else if (status is not null)
            list = await _service.ListAsync(a => a.Status == AssignmentStatus.Pending, ct);
        else if (departmentId is not null)
            list = await _service.ListAsync(a => a.DepartmentId == departmentId, ct);
        else
            return BadRequest("Must specify assignee, overdueAsOf, status or departmentId");

        var dtos = list.Select(a => _mapper.Map<AssignmentDto>(a));
        return Ok(dtos);
    }

    [HttpPatch("{id}/start")]
    public async Task<IActionResult> StartAssignment(
        string id,
        [FromQuery] string asignee,
        CancellationToken ct)
    {
        var a = await _service.GetByIdAsync(id, ct);
        if (a is null) return NotFound();
        a.Start(asignee);
        await _service.UpdateAsync(a, ct);
        return Ok(_mapper.Map<AssignmentDto>(a));
    }

    [HttpPatch("{id}/complete")]
    public async Task<IActionResult> CompleteAssignment(
        string id,
        CancellationToken ct)
    {
        var a = await _service.GetByIdAsync(id, ct);
        if (a is null) return NotFound();
        a.Complete();
        await _service.UpdateAsync(a, ct);
        return Ok(_mapper.Map<AssignmentDto>(a));
    }

    [Authorize(Roles = "admin,manager")]
    [HttpPatch("{id}/approve")]
    public async Task<IActionResult> ApproveAssignment(
        string id,
        CancellationToken ct)
    {
        var a = await _service.GetByIdAsync(id, ct);
        if (a is null) return NotFound();
        a.Approve();
        await _service.UpdateAsync(a, ct);
        return Ok(_mapper.Map<AssignmentDto>(a));
    }

    [Authorize(Roles = "admin,manager")]
    [HttpPatch("{id}/reject")]
    public async Task<IActionResult> RejectAssignment(
        string id,
        CancellationToken ct)
    {
        var a = await _service.GetByIdAsync(id, ct);
        if (a is null) return NotFound();
        a.Reject();
        await _service.UpdateAsync(a, ct);
        return Ok(_mapper.Map<AssignmentDto>(a));
    }

    [Authorize(Roles = "admin,manager")]
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(
        string id,
        [FromBody] UpdateAssignmentRequest req,
        CancellationToken ct)
    {
        var existing = await _service.GetByIdAsync(id, ct);
        if (existing is null) return NotFound();

        if (req.Title is not null) existing.UpdateTitle(req.Title);
        if (req.Description is not null) existing.UpdateDescription(req.Description);
        if (req.DueDate is not null) existing.UpdateDueDate((DateTime)req.DueDate);
        if(req.AssignedToId is not null) existing.UpdateAssignedToId(req.AssignedToId);
        if (req.Status is not null) existing.UpdateStatus((AssignmentStatus)req.Status);

        await _service.UpdateAsync(existing, ct);
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

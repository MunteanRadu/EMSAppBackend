using AutoMapper;
using EMSApp.Application;
using EMSApp.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EMSApp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LeaveRequestsController : ControllerBase
{
    private readonly ILeaveRequestService _service;
    private readonly IMapper _mapper;

    public LeaveRequestsController(ILeaveRequestService service, IMapper mapper)
    {
        _service = service;
        _mapper = mapper;
    }

    [HttpPost]
    public async Task<ActionResult<LeaveRequestDto>> Create(
        [FromBody] CreateLeaveRequestRequest req,
        CancellationToken ct)
    {
        var lr = await _service.CreateAsync(
            req.UserId,
            req.Type,
            req.StartDate,
            req.EndDate,
            req.Reason,
            ct);

        var dto = _mapper.Map<LeaveRequestDto>(lr);
        return CreatedAtAction(
            nameof(GetById),
            new { id = lr.Id },
            dto);
    }


    [HttpGet("{id}")]
    public async Task<ActionResult<LeaveRequestDto>> GetById(
        string id,
        CancellationToken ct)
    {
        var lr = await _service.GetByIdAsync(id, ct);
        if (lr is null) return NotFound();
        return _mapper.Map<LeaveRequestDto>(lr);
    }

    [Authorize(Roles = "admin,manager")]
    [HttpPost("{id}/approve")]
    public async Task<IActionResult> Approve(
        string id,
        [FromQuery] string managerId,
        CancellationToken ct)
    {
        var lr = await _service.GetByIdAsync(id, ct);
        if (lr is null) return NotFound();
        lr.Approve(managerId);
        await _service.UpdateAsync(lr, ct);
        return Ok(_mapper.Map<LeaveRequestDto>(lr));
    }

    [Authorize(Roles = "admin,manager")]
    [HttpPost("{id}/reject")]
    public async Task<IActionResult> Reject(
        string id,
        [FromQuery] string managerId,
        CancellationToken ct)
    {
        var lr = await _service.GetByIdAsync(id, ct);
        if (lr is null) return NotFound();
        lr.Reject(managerId);
        await _service.UpdateAsync(lr, ct);
        return Ok(_mapper.Map<LeaveRequestDto>(lr));
    }


    [Authorize(Roles = "admin,manager")]
    [HttpGet]
    public async Task<ActionResult<IEnumerable<LeaveRequestDto>>> List(
        CancellationToken ct)
    {
        var list = await _service.GetAllAsync(ct);
        return Ok(_mapper.Map<IEnumerable<LeaveRequestDto>>(list));
    }

    [HttpGet("listBySomething")]
    public async Task<ActionResult<IEnumerable<LeaveRequestDto>>> ListByUser(
        [FromQuery] string? userId,
        [FromQuery] LeaveStatus? status,
        CancellationToken ct)
    {
        var list = userId is not null
            ? await _service.ListByUserAsync(userId, ct)
            : status is not null
                ? await _service.ListByStatusAsync(status.Value, ct)
                : await _service.GetAllAsync(ct);

        return Ok(_mapper.Map<IEnumerable<LeaveRequestDto>>(list));
    }

    [Authorize(Roles = "admin,manager")]
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(
        string id,
        [FromBody] UpdateLeaveRequestRequest req,
        CancellationToken ct)
    {
        var lr = await _service.GetByIdAsync(id, ct);
        if (lr is null) return NotFound();

        if (req.Type is not null) lr.UpdateType(req.Type.Value);
        if (req.StartDate is not null) lr.UpdateStartDate(req.StartDate.Value);
        if (req.EndDate is not null) lr.UpdateEndDate(req.EndDate.Value);
        if (req.Reason is not null) lr.UpdateReason(req.Reason);

        await _service.UpdateAsync(lr, ct);
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
}

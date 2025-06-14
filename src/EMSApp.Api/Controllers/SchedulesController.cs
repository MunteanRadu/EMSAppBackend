using AutoMapper;
using EMSApp.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EMSApp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SchedulesController : ControllerBase
{
    private readonly IScheduleService _service;
    private readonly IMapper _mapper;

    public SchedulesController(IScheduleService service, IMapper mapper)
    {
        _service = service;
        _mapper = mapper;
    }

    [Authorize(Roles = "admin,manager")]
    [HttpPost]
    public async Task<ActionResult<ScheduleDto>> Create(
        [FromBody] CreateScheduleRequest req,
        CancellationToken ct)
    {
        var s = await _service.CreateAsync(
            req.DepartmentId,
            req.ManagerId,
            req.ShiftType,
            req.Day,
            req.StartTime,
            req.EndTime,
            req.IsWorkingDay,
            ct);

        var dto = _mapper.Map<ScheduleDto>(s);
        return CreatedAtAction(
            nameof(GetById),
            new { id = s.Id },
            dto);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ScheduleDto>> GetById(
        string id,
        CancellationToken ct)
    {
        var s = await _service.GetByIdAsync(id, ct);
        if (s is null) return NotFound();
        return _mapper.Map<ScheduleDto>(s);
    }

    [HttpGet("department/{departmentId}")]
    public async Task<ActionResult<IEnumerable<ScheduleDto>>> ListByDepartment(
    string departmentId,
    CancellationToken ct)
    {
        var list = await _service.ListByDepartmentAsync(departmentId, ct);
        return Ok(_mapper.Map<IEnumerable<ScheduleDto>>(list));
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ScheduleDto>>> List(
        CancellationToken ct)
    {
        var list = await _service.GetAllAsync(ct);
        return Ok(_mapper.Map<IEnumerable<ScheduleDto>>(list));
    }

    [Authorize(Roles = "admin,manager")]
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(
        string id,
        [FromBody] UpdateScheduleRequest req,
        CancellationToken ct)
    {
        var s = await _service.GetByIdAsync(id, ct);
        if (s is null) return NotFound();

        if (req.StartTime is not null) s.UpdateShift(s.ShiftType, req.StartTime.Value, s.EndTime, s.IsWorkingDay);
        if (req.EndTime is not null) s.UpdateShift(s.ShiftType, s.StartTime, req.EndTime.Value, s.IsWorkingDay);
        if (!Enum.IsDefined(req.ShiftType)) s.UpdateShift(req.ShiftType, s.StartTime, s.EndTime, s.IsWorkingDay);
        if (req.IsWorkingDay is not null) s.UpdateShift(s.ShiftType, s.StartTime, s.EndTime, req.IsWorkingDay.Value);

        await _service.UpdateAsync(s, ct);
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

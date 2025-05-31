using AutoMapper;
using EMSApp.Application;
using EMSApp.Domain;
using EMSApp.Domain.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EMSApp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PunchRecordsController : ControllerBase
{
    private readonly IPunchRecordService _service;
    private readonly ILeaveRequestService _leaveService;
    private readonly IMapper _mapper;

    public PunchRecordsController(IPunchRecordService service, ILeaveRequestService leaveService, IMapper mapper)
    {
        _service = service;
        _leaveService = leaveService;
        _mapper = mapper;
    }

    [HttpPost]
    public async Task<ActionResult<PunchRecordDto>> Create(
        [FromBody] CreatePunchRecordRequest req,
        CancellationToken ct)
    {
        try
        {
            var date = DateOnly.FromDateTime(DateTime.UtcNow);
            var created = await _service.CreateAsync(req.UserId, date, req.TimeIn, ct);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, _mapper.Map<PunchRecordDto>(created));
        }
        catch (DomainException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("{id}/punchout")]
    public async Task<IActionResult> PunchOut(
    string id,
    [FromBody] UpdatePunchRecordRequest req,
    CancellationToken ct)
    {
        try
        {
            var updated = await _service.PunchOutAsync(id, req.TimeOut, ct);
            return Ok(updated);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (DomainException ex)
        {
            return BadRequest(ex.Message);
        }
    }


    [HttpGet("{id}")]
    public async Task<ActionResult<PunchRecordDto>> GetById(
        string id,
        CancellationToken ct)
    {
        var pr = await _service.GetByIdAsync(id, ct);
        if (pr is null) return NotFound();
        return _mapper.Map<PunchRecordDto>(pr);
    }

    [HttpGet("user/{id}")]
    public async Task<ActionResult<IEnumerable<PunchRecordDto>>> ListByUser(
        [FromQuery] string? userId,
        CancellationToken ct)
    {
        var list = userId is not null
            ? await _service.ListByUserAsync(userId, ct)
            : await _service.GetAllAsync(ct);

        return Ok(_mapper.Map<IEnumerable<PunchRecordDto>>(list));
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<PunchRecordDto>>> ListAll(CancellationToken ct)
    {
        var list = await _service.GetAllAsync(ct);
        return Ok(_mapper.Map<IEnumerable<PunchRecordDto>>(list));
    }

    [HttpGet("summary")]
    public async Task<IActionResult> MonthSummary(
        [FromQuery] string userId,
        [FromQuery] int year,
        [FromQuery] int month,
        CancellationToken ct)
    {
        var list = await _service.GetMonthSummaryAsync(userId, year, month, ct);
        return Ok(list);
    }

    [HttpGet("today")]
    public async Task<ActionResult<IEnumerable<PunchRecordDto>>> ListByDate(
        [FromQuery] string? userId,
        [FromQuery] DateOnly date,
        CancellationToken ct)
    {
        var list = await _service.GetByDateAsync(userId, date, ct);
        return Ok(list);
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

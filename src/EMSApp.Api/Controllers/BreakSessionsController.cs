using AutoMapper;
using EMSApp.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EMSApp.Api.Controllers;

[ApiController]
[Route("api/punchrecords/{punchId}/[controller]")]
public class BreakSessionsController : ControllerBase
{
    private readonly IBreakSessionService _service;
    private readonly IMapper _mapper;

    public BreakSessionsController(
        IBreakSessionService service,
        IMapper mapper)
    {
        _service = service;
        _mapper = mapper;
    }

    [HttpPost]
    public async Task<ActionResult<BreakSessionDto>> Create(string punchId, [FromBody] CreateBreakSessionRequest req, CancellationToken ct)
    {
        var bs = await _service.CreateAsync(punchId, req.StartTime, ct);

        var dto = _mapper.Map<BreakSessionDto>(bs);
        return CreatedAtAction(
            nameof(GetById),
            new { punchId, id = bs.Id },
            dto);
    }

    [HttpPatch("{id}")]
    public async Task<IActionResult> End(
        string punchId,
        string id,
        [FromBody] UpdateBreakSessionRequest req,
        CancellationToken ct)
    {
        var dto = await _service.EndAsync(punchId, id, req.EndTime, ct);
        if (dto is null) return NotFound();
        return Ok(dto);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<BreakSessionDto>> GetById(
        string punchId,
        string id,
        CancellationToken ct)
    {
        var bs = await _service.GetByIdAsync(id, ct);
        if (bs is null || bs.PunchRecordId != punchId) return NotFound();
        return _mapper.Map<BreakSessionDto>(bs);
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<BreakSessionDto>>> List(
        string punchId,
        CancellationToken ct)
    {
        var list = await _service.ListByPunchRecordAsync(punchId, ct);
        return Ok(_mapper.Map<IEnumerable<BreakSessionDto>>(list));
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

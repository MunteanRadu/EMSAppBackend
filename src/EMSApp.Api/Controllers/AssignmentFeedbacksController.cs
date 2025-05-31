using AutoMapper;
using EMSApp.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EMSApp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AssignmentFeedbacksController : ControllerBase
{
    private readonly IAssignmentFeedbackService _service;
    private readonly IMapper _mapper;

    public AssignmentFeedbacksController(
        IAssignmentFeedbackService service,
        IMapper mapper)
    {
        _service = service;
        _mapper = mapper;
    }

    [HttpPost]
    public async Task<ActionResult<AssignmentFeedbackDto>> Create(
        [FromBody] CreateAssignmentFeedbackRequest req,
        CancellationToken ct)
    {
        var af = await _service.CreateAsync(
            req.AssignmentId,
            req.UserId,
            req.Text,
            req.Type,
            ct);

        var dto = _mapper.Map<AssignmentFeedbackDto>(af);
        return CreatedAtAction(
            nameof(GetById),
            new { id = af.Id },
            dto);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<AssignmentFeedbackDto>> GetById(
        string id,
        CancellationToken ct)
    {
        var af = await _service.GetByIdAsync(id, ct);
        if (af is null) return NotFound();
        return _mapper.Map<AssignmentFeedbackDto>(af);
    }

    [Authorize(Roles = "admin,manager")]
    [HttpGet]
    public async Task<ActionResult<IEnumerable<AssignmentFeedbackDto>>> List(
        CancellationToken ct)
    {
        var list = await _service.GetAllAsync(ct);
        return Ok(_mapper.Map<IEnumerable<AssignmentFeedbackDto>>(list));
    }

    [HttpGet("listByAssignment")]
    public async Task<ActionResult<IEnumerable<AssignmentFeedbackDto>>> ListByAssignment(
        [FromQuery] string assignmentId,
        CancellationToken ct)
    {
        var list = await _service.ListByAssignmentAsync(assignmentId, ct);
        return Ok(_mapper.Map<IEnumerable<AssignmentFeedbackDto>>(list));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(
        string id,
        [FromBody] UpdateAssignmentFeedbackRequest req,
        CancellationToken ct)
    {
        var af = await _service.GetByIdAsync(id, ct);
        if (af is null) return NotFound();

        if (req.Text is not null) af.Edit(req.Text);

        await _service.UpdateAsync(af, ct);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(
        string id,
        CancellationToken ct)
    {
        await _service.DeleteAsync(id, ct);
        return NoContent();
    }
}

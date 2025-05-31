using AutoMapper;
using EMSApp.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EMSApp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PoliciesController : ControllerBase
{
    private readonly IPolicyService _svc;
    private readonly IMapper _mapper;

    public PoliciesController(IPolicyService svc, IMapper mapper)
    {
        _svc = svc;
        _mapper = mapper;
    }

    [Authorize(Roles = "admin")]
    [HttpPost]
    public async Task<ActionResult<PolicyDto>> Create(
        [FromBody] CreatePolicyRequest req,
        CancellationToken ct)
    {
        var p = await _svc.CreateAsync(
            req.Year,
            req.WorkDayStart,
            req.WorkDayEnd,
            req.PunchInTolerance,
            req.PunchOutTolerance,
            req.MaxSingleBreak,
            req.MaxTotalBreakPerDay,
            req.OvertimeMultiplier,
            req.LeaveQuotas,
            ct);

        var dto = _mapper.Map<PolicyDto>(p);
        return CreatedAtAction(
            nameof(GetByYear),
            new { year = p.Year },
            dto);
    }

    [HttpGet("{year:int}")]
    public async Task<ActionResult<PolicyDto>> GetByYear(
        int year,
        CancellationToken ct)
    {
        var p = await _svc.GetByYearAsync(year, ct);
        if (p is null) return NotFound();
        return _mapper.Map<PolicyDto>(p);
    }

    [Authorize(Roles ="admin")]
    [HttpPatch("{year}/quotas")]
    public async Task<IActionResult> UpdateLeaveQuotas(
        int year,
        [FromBody] UpdateLeaveQuotasRequest req,
        CancellationToken ct)
    {
        var p = await _svc.GetByYearAsync(year, ct);
        if (p is null) return NotFound();

        if (req.LeaveQuotas is not null) p.SetLeaveQuotas(req.LeaveQuotas);

        await _svc.UpdateAsync(p, ct);
        return NoContent();
    }

    [Authorize(Roles = "admin")]
    [HttpPut("{year}")]
    public async Task<IActionResult> Update(
        int year,
        [FromBody] UpdatePolicyRequest req,
        CancellationToken ct)
    {
        var p = await _svc.GetByYearAsync(year, ct);
        if (p is null) return NotFound();

        if (req.WorkDayStart is not null) p.SetWorkingHours(req.WorkDayStart.Value, p.WorkDayEnd);
        if (req.WorkDayEnd is not null) p.SetWorkingHours(p.WorkDayStart, req.WorkDayEnd.Value);
        if (req.PunchInTolerance is not null) p.SetPunchTolerances(req.PunchInTolerance.Value, p.PunchOutTolerance);
        if (req.PunchOutTolerance is not null) p.SetPunchTolerances(p.PunchInTolerance, req.PunchOutTolerance.Value);
        if (req.MaxSingleBreak is not null) p.SetBreakRules(req.MaxSingleBreak.Value, p.MaxTotalBreakPerDay);
        if (req.MaxTotalBreakPerDay is not null) p.SetBreakRules(p.MaxSingleBreak, req.MaxTotalBreakPerDay.Value);
        if (req.OvertimeMultiplier is not null) p.SetOvertimeMultiplier(req.OvertimeMultiplier.Value);
        //if (req.LeaveQuotas is not null) p.SetLeaveQuotas(req.LeaveQuotas);

        await _svc.UpdateAsync(p, ct);
        return NoContent();
    }

    [Authorize(Roles = "admin")]
    [HttpDelete("{year:int}")]
    public async Task<IActionResult> Delete(int year, CancellationToken ct)
    {
        await _svc.DeleteAsync(year, ct);
        return NoContent();
    }
}

using EMSApp.Application;
using EMSApp.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EMSApp.Api;

[ApiController]
[Route("api/departments/{departmentId}/rules")]
public class ShiftRuleController : ControllerBase
{
    private readonly IShiftRuleService _shiftRuleService;
    private readonly IDepartmentRepository _departmentRepo;

    public ShiftRuleController(
        IShiftRuleService shiftRuleService,
        IDepartmentRepository departmentRepo)
    {
        _shiftRuleService = shiftRuleService;
        _departmentRepo = departmentRepo;
    }

    [HttpGet]
    [Authorize(Roles = "manager,admin")]
    public async Task<IActionResult> GetRule(
        [FromRoute] string departmentId,
        CancellationToken ct)
    {
        var dept = await _departmentRepo.GetByIdAsync(departmentId, ct);
        if (dept == null) return NotFound("Department not found");

        var rule = await _shiftRuleService.GetRuleByDepartmentAsync(departmentId, ct);
        if (rule == null)
            return NotFound("No rules are defined for this department");

        return Ok(rule);
    }

    [HttpPost]
    [Authorize(Roles = "manager,admin")]
    public async Task<IActionResult> UpsertRule(
        [FromRoute] string departmentId,
        [FromBody] ShiftRuleDto dto,
        CancellationToken ct)
    {
        var dept = await _departmentRepo.GetByIdAsync(departmentId, ct);
        if (dept == null) return NotFound("Department not found");

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!User.IsInRole("Admin") && dept.ManagerId != userId)
        {
            Console.WriteLine($"manager: {dept.ManagerId}, userId: {userId}");
            return Forbid();
        }

        var rule = await _shiftRuleService.CreateOrUpdateRuleAsync(
            departmentId,
            dto.MinShift1,
            dto.MinShift2,
            dto.MinNightShift,
            dto.MaxConsecutiveNightShifts,
            dto.MinRestHoursBetweenShifts,
            ct);

        return Ok(rule);
    }

    [HttpDelete]
    [Authorize(Roles = "manager,admin")]
    public async Task<IActionResult> DeleteRule(
        [FromRoute] string departmentId,
        CancellationToken ct)
    {
        var dept = await _departmentRepo.GetByIdAsync(departmentId, ct);
        if (dept == null) return NotFound("Department not found");

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!User.IsInRole("Admin") && dept.ManagerId != userId)
            return Forbid();

        await _shiftRuleService.DeleteRuleByDepartmentAsync(departmentId, ct);
        return NoContent();
    }
}

public class ShiftRuleDto
{
    public int MinShift1 { get; set; }
    public int MinShift2 { get; set; }
    public int MinNightShift { get; set; }
    public int MaxConsecutiveNightShifts { get; set; }
    public double MinRestHoursBetweenShifts {  get; set; }
}

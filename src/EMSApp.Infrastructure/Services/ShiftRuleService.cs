using EMSApp.Application;
using EMSApp.Domain;

namespace EMSApp.Infrastructure;

public class ShiftRuleService : IShiftRuleService
{
    private readonly IShiftRuleRepository _ruleRepo;

    public ShiftRuleService(IShiftRuleRepository ruleRepo)
    {
        _ruleRepo = ruleRepo;
    }

    public async Task<ShiftRule?> GetRuleByDepartmentAsync(string departmentId, CancellationToken ct)
    {
        return await _ruleRepo.GetByDepartmentAsync(departmentId, ct);
    }

    public async Task<ShiftRule> CreateOrUpdateRuleAsync(
        string departmentId,
        int minShift1,
        int minShift2,
        int minNightShift,
        int maxConsecutiveNight,
        double minRestHoursBetweenShifts,
        CancellationToken ct)
    {
        var existingRule = await _ruleRepo.GetByDepartmentAsync(departmentId, ct);
        if (existingRule == null)
        {
            var rule = new ShiftRule(departmentId, minShift1, minShift2, minNightShift, maxConsecutiveNight, minRestHoursBetweenShifts);
            await _ruleRepo.UpsertAsync(rule, ct);
            return rule;
        }
        else
        {
            existingRule.Update(minShift1, minShift2, minNightShift, maxConsecutiveNight, minRestHoursBetweenShifts);
            await _ruleRepo.UpsertAsync(existingRule, ct);
            return existingRule;
        }
    }

    public async Task DeleteRuleByDepartmentAsync(string departmentId, CancellationToken ct)
    {
        await _ruleRepo.DeleteByDepartmentAsync(departmentId, ct);
    }
}

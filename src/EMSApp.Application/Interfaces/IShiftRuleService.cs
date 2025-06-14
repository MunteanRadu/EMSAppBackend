using EMSApp.Domain;

namespace EMSApp.Application;

public interface IShiftRuleService
{
    Task<ShiftRule?> GetRuleByDepartmentAsync(string departmentId, CancellationToken ct);

    Task<ShiftRule> CreateOrUpdateRuleAsync(
        string departmentId,
        int minShift1,
        int minShift2,
        int minNightShift,
        int maxConsecutiveNight,
        double minRestHoursBetweenShifts,
        CancellationToken ct);

    Task DeleteRuleByDepartmentAsync(string departmentId, CancellationToken ct);
}
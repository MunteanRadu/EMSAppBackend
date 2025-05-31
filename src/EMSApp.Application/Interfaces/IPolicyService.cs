using EMSApp.Domain;

namespace EMSApp.Application;

public interface IPolicyService
{
    Task<Policy> CreateAsync(int year, TimeOnly workDayStart, TimeOnly workDayEnd, TimeSpan punchInTolerance, TimeSpan punchOutTolerance, TimeSpan maxSingleBreak, TimeSpan maxTotalBreakPerDay, decimal overtimeMultiplier, IDictionary<LeaveType, int> leaveQuotas, CancellationToken ct);
    Task<Policy?> GetByYearAsync(int year, CancellationToken ct);
    Task<IReadOnlyList<Policy>> GetAllAsync(CancellationToken ct);
    Task UpdateAsync(Policy policy, CancellationToken ct);
    Task DeleteAsync(int year, CancellationToken ct);
}

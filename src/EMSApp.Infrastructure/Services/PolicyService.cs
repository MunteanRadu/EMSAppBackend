using EMSApp.Application;
using EMSApp.Domain;

namespace EMSApp.Infrastructure;

public class PolicyService : IPolicyService
{
    private readonly IPolicyRepository _repo;
    public PolicyService(IPolicyRepository repo)
    {
        _repo = repo;
    }

    public async Task<Policy> CreateAsync(int year, TimeOnly workDayStart, TimeOnly workDayEnd, TimeSpan punchInTolerance, TimeSpan punchOutTolerance, TimeSpan maxSingleBreak, TimeSpan maxTotalBreakPerDay, decimal overtimeMultiplier, IDictionary<LeaveType, int> leaveQuotas, CancellationToken ct)
    {
        var policy = new Policy(
            year,
            workDayStart,
            workDayEnd,
            punchInTolerance,
            punchOutTolerance,
            maxSingleBreak,
            maxTotalBreakPerDay,
            overtimeMultiplier,
            leaveQuotas);
        await _repo.CreateAsync(policy, ct);
        return policy;
    }

    public Task<IReadOnlyList<Policy>> GetAllAsync(CancellationToken ct)
    {
        return _repo.GetAllAsync(ct);
    }

    public Task<Policy?> GetByYearAsync(int year, CancellationToken ct)
    {
        return _repo.GetByYearAsync(year, ct);
    }

    public Task UpdateAsync(Policy policy, CancellationToken ct)
    {
        return _repo.UpdateAsync(policy, false, ct);
    }

    public Task DeleteAsync(int year, CancellationToken ct)
    {
        return _repo.DeleteAsync(year, ct);
    }
}

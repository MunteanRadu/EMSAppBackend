using EMSApp.Domain;

namespace EMSApp.Api;

public record class UpdatePolicyRequest
{
    public TimeOnly? WorkDayStart { get; init; }
    public TimeOnly? WorkDayEnd { get; init; }
    public TimeSpan? PunchInTolerance { get; init; }
    public TimeSpan? PunchOutTolerance { get; init; }
    public TimeSpan? MaxSingleBreak { get; init; }
    public TimeSpan? MaxTotalBreakPerDay { get; init; }
    public decimal? OvertimeMultiplier { get; init; }
    public IDictionary<LeaveType, int>? LeaveQuotas { get; init; }
}

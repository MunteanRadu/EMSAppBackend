using EMSApp.Domain;

namespace EMSApp.Application;

public record PolicyDto
{
    public int Year { get; init; }
    public TimeOnly WorkDayStart { get; init; }
    public TimeOnly WorkDayEnd { get; init; }
    public TimeSpan PunchInTolerance { get; init; }
    public TimeSpan PunchOutTolerance { get; init; }
    public TimeSpan MaxSingleBreak { get; init; }
    public TimeSpan MaxTotalBreakPerDay { get; init; }
    public decimal OvertimeMultiplier { get; init; }
    public IDictionary<LeaveType, int> LeaveQuotas { get; init; } = null!;
}

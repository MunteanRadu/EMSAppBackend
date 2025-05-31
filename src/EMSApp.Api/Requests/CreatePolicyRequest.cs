using EMSApp.Domain;

namespace EMSApp.Api;

public record CreatePolicyRequest(
    int Year,
    TimeOnly WorkDayStart,
    TimeOnly WorkDayEnd,
    TimeSpan PunchInTolerance,
    TimeSpan PunchOutTolerance,
    TimeSpan MaxSingleBreak,
    TimeSpan MaxTotalBreakPerDay,
    decimal OvertimeMultiplier,
    IDictionary<LeaveType, int> LeaveQuotas
);

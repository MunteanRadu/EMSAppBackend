using EMSApp.Domain;

namespace EMSApp.Application;

public record PolicyDto(
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

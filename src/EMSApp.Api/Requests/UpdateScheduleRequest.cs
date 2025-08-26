using EMSApp.Domain;

namespace EMSApp.Api;

public sealed record UpdateScheduleRequest(
    TimeOnly? StartTime,
    TimeOnly? EndTime,
    ShiftType ShiftType,
    bool? IsWorkingDay
);
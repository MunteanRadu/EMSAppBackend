using EMSApp.Domain;

namespace EMSApp.Api;

public record class UpdateScheduleRequest(
    TimeOnly? StartTime,
    TimeOnly? EndTime,
    ShiftType ShiftType,
    bool? IsWorkingDay
);
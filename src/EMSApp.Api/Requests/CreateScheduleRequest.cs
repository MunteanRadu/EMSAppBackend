using EMSApp.Domain;

namespace EMSApp.Api;

public record CreateScheduleRequest(
    string DepartmentId,
    string ManagerId,
    ShiftType ShiftType,
    DayOfWeek Day,
    TimeOnly StartTime,
    TimeOnly EndTime,
    bool IsWorkingDay
);

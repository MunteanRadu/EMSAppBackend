namespace EMSApp.Api;

public record CreateScheduleRequest(
    string DepartmentId,
    string ManagerId,
    DayOfWeek Day,
    TimeOnly StartTime,
    TimeOnly EndTime,
    bool IsWorkingDay
);

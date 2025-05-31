namespace EMSApp.Application;

public sealed record ScheduleDto(
     string Id,
     string DepartmentId,
     string ManagerId,
     DayOfWeek Day,
     TimeOnly StartTime,
     TimeOnly EndTime,
     bool IsWorkingDay,
     HashSet<DateOnly> Exceptions
);

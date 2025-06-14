namespace EMSApp.Application;

public sealed record ScheduleDto(
     string Id,
     string DepartmentId,
     string ManagerId,
     ShiftType ShiftType,
     DayOfWeek Day,
     TimeOnly StartTime,
     TimeOnly EndTime,
     bool IsWorkingDay
);

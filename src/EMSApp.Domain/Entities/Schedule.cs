using EMSApp.Domain.Exceptions;

namespace EMSApp.Domain;

/// <summary>
/// A repeating weekly shift for a department, owned by a manager
/// </summary>
public class Schedule
{
    public string Id { get; private set; }
    public string DepartmentId { get; private set; }
    public string ManagerId { get; private set; }
    public ShiftType ShiftType { get; private set; }
    public DayOfWeek Day { get; private set; }
    public TimeOnly StartTime { get; private set; }
    public TimeOnly EndTime { get; private set; }
    public bool IsWorkingDay { get; private set; }

    public Schedule(
        string departmentId,
        string managerId,
        ShiftType shift,
        DayOfWeek day,
        TimeOnly startTime,
        TimeOnly endTime,
        bool isWorkingDay)
    {
        // DepartmentId validation
        if (string.IsNullOrWhiteSpace(departmentId))
            throw new DomainException("Department Id cannot be empty");

        // ManagerId validation
        if (string.IsNullOrWhiteSpace(managerId))
            throw new DomainException("Manager Id cannot be empty");

        // Day validation
        if (!Enum.IsDefined(typeof(DayOfWeek), day))
            throw new DomainException("Invalid day of week");

        // EndTime validation
        if (endTime == default)
            throw new DomainException("End time must be provided");
        if (endTime <= startTime)
            throw new DomainException("End time must be after start time");

        Id = Guid.NewGuid().ToString();
        DepartmentId = departmentId;
        ManagerId = managerId;
        ShiftType = shift;
        Day = day;
        StartTime = startTime;
        EndTime = endTime;
        IsWorkingDay = isWorkingDay;
    }

    /// <summary>
    /// Updates shift start, end and isWorkingDay parameters
    /// </summary>
    public void UpdateShift(ShiftType newShift, TimeOnly newStart, TimeOnly newEnd, bool isWorkingDay)
    {
        if (string.IsNullOrWhiteSpace(DepartmentId) || string.IsNullOrWhiteSpace(ManagerId))
            throw new DomainException("Invalid IDs");
        if (newEnd <= newStart)
            throw new DomainException("End time must be after start time");

        ShiftType = newShift;
        StartTime = newStart;
        EndTime = newEnd;
        IsWorkingDay = isWorkingDay;
    }
}

using EMSApp.Domain.Exceptions;

public enum ShiftType { Shift1, Shift2, NightShift }

public class ShiftAssignment
{
    public string Id { get; private set; }
    public string UserId { get; private set; }
    public DateOnly Date { get; private set; }
    public ShiftType Shift { get; private set; }
    public TimeOnly StartTime { get; private set; }
    public TimeOnly EndTime { get; private set; }
    public string DepartmentId { get; private set; }
    public string ManagerId { get; private set; }

    public ShiftAssignment(
        string userId,
        DateOnly date,
        ShiftType shift,
        TimeOnly startTime,
        TimeOnly endTime,
        string departmentId,
        string managerId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new DomainException("UserId cannot be empty");
        if (date == default)
            throw new DomainException("Date must be provided");
        if (endTime <= startTime)
            throw new DomainException("End time must be after start time");
        if (string.IsNullOrWhiteSpace(departmentId))
            throw new DomainException("DepartmentId cannot be empty");
        if (string.IsNullOrWhiteSpace(managerId))
            throw new DomainException("ManagerId cannot be empty");

        Id = Guid.NewGuid().ToString();
        UserId = userId;
        Date = date;
        Shift = shift;
        StartTime = startTime;
        EndTime = endTime;
        DepartmentId = departmentId;
        ManagerId = managerId;
    }

    public void UpdateShift(ShiftType newShift, TimeOnly newStart, TimeOnly newEnd)
    {
        if (newEnd <= newStart)
            throw new DomainException("End time must be after start time");

        Shift = newShift;
        StartTime = newStart;
        EndTime = newEnd;
    }
}

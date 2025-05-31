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
    public DayOfWeek Day { get; private set; }
    public TimeOnly StartTime { get; private set; }
    public TimeOnly EndTime { get; private set; }
    public bool IsWorkingDay { get; private set; }
    public List<DateOnly> Exceptions { get; private set; } = new();

    public Schedule(
        string departmentId, 
        string managerId, 
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

        // StartTime validation
        if (startTime == default)
            throw new DomainException("Start time must be provided");

        // EndTime validation
        if (endTime == default)
            throw new DomainException("End time must be provided");
        if (endTime <= startTime)
            throw new DomainException("End time must be after start time");

        Id = Guid.NewGuid().ToString();
        DepartmentId = departmentId;
        ManagerId = managerId;
        Day = day;
        StartTime = startTime;
        EndTime = endTime;
        IsWorkingDay = isWorkingDay;
    }

    /// <summary>
    /// Checks if the given date/time is within this scheduled shift
    /// </summary>
    /// <param name="date"></param>
    /// <param name="time"></param>
    /// <returns></returns>
    public bool IsWithinShift(DateOnly date, TimeOnly time)
        => IsWorkingDay
        && date.DayOfWeek == Day
        && time >= StartTime
        && time <= EndTime
        && !Exceptions.Contains(date);

    /// <summary>
    /// How many hours this shift lasts
    /// </summary>
    public TimeSpan Duration => EndTime - StartTime;

    /// <summary>
    /// Get the next N calendar dates on which this shift occurs.
    /// </summary>
    /// <param name="after"></param>
    /// <param name="count"></param>
    /// <returns></returns>
    public IEnumerable<DateOnly> NextOccurrences(DateOnly after, int count)
    {
        if (count <= 0) yield break;

        var daysUntil = ((int)Day - (int)after.DayOfWeek + 7) % 7;
        var current = after.AddDays(daysUntil);
        var found = 0;

        while (found < count)
        {
            if (IsWorkingDay && !Exceptions.Contains(current))
            {
                yield return current;
                found++;
            }
            current = current.AddDays(7);
        }
    }

    /// <summary>
    /// Updates shift start, end and isWorkingDay parameters
    /// </summary>
    /// <param name="newStart"></param>
    /// <param name="newEnd"></param>
    /// <param name="isWorkingDay"></param>
    /// <exception cref="DomainException"></exception>
    public void UpdateShift(TimeOnly newStart, TimeOnly newEnd, bool isWorkingDay)
    {
        if (newEnd <= newStart)
            throw new DomainException("End time must be after start time");

        StartTime = newStart;
        EndTime = newEnd;
        IsWorkingDay = isWorkingDay;
    }

    /// <summary>
    /// Adding a date in the exceptions collection
    /// </summary>
    /// <param name="date"></param>
    /// <exception cref="DomainException"></exception>
    public void AddException(DateOnly date)
    {
        if (Exceptions.Contains(date))
            throw new DomainException($"Already excepted {date}");
        if(date < DateOnly.FromDateTime(DateTime.UtcNow))
            throw new DomainException("Date must be in the future");
        Exceptions.Add(date);
    }

    /// <summary>
    /// Removing a date from the exceptions collection
    /// </summary>
    /// <param name="date"></param>
    /// <exception cref="DomainException"></exception>
    public void RemoveException(DateOnly date)
    {
        if (!Exceptions.Contains(date))
            throw new DomainException($"No exception found for {date}");
        Exceptions.Remove(date);
    }
}

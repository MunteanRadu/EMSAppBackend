using EMSApp.Domain.Exceptions;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace EMSApp.Domain;

public class PunchRecord
{
    public string Id {  get; private set; }
    public string UserId { get; private set; }
    public DateOnly Date { get; private set; }
    public TimeOnly TimeIn { get; private set; }
    public TimeOnly? TimeOut { get; private set; }
    public TimeSpan? TotalHours { get; private set; }
    public bool IsNonCompliant { get; private set; } = false;

    public PunchRecord(string userId, DateOnly date, TimeOnly timeIn)
    {
        // UserId validation
        if(string.IsNullOrWhiteSpace(userId)) 
            throw new DomainException("UserId Id cannot be empty");

        // Date validation
        if (date == default)
            throw new DomainException("Date must be provided");

        // Time validation
        if (timeIn == default)
            throw new DomainException("Punch-in time must be provided");

        Id = Guid.NewGuid().ToString();
        UserId = userId;
        Date = date;
        TimeIn = timeIn;
        TimeOut = null;
        TotalHours = null;
    }

    public void PunchOut(TimeOnly timeOut)
    {
        if (TimeOut.HasValue)
            throw new DomainException("Already punched out");
        if (timeOut == default)
            throw new DomainException("Punch-out time must be provided");
        if (timeOut < TimeIn)
            throw new DomainException("Punch-out time must be after punch-in time");

        TimeOut = timeOut;
        TotalHours = timeOut - TimeIn;
    }

    public TimeSpan? GetTotalHours()
    {
        if (!TimeOut.HasValue)
            return null;

        return TotalHours;
    }

    public void UpdateDate(DateOnly newDate)
    {
        if (newDate == default)
            throw new DomainException("Date must be provided");
        Date = newDate;
    }

    public void UpdateTimeIn(TimeOnly newTimeIn)
    {
        if (newTimeIn == default)
            throw new DomainException("Punch-in time must be provided");
        TimeIn = newTimeIn;
    }

    public void UpdateTimeOut(TimeOnly newTimeOut)
    {
        if (newTimeOut == default)
            throw new DomainException("Punch-out time must be provided");
        TimeIn = newTimeOut;
    }

    public void MarkAsNonCompliant()
    {
        IsNonCompliant = true;
    }

    public bool IsComplete() => TimeOut.HasValue;
}

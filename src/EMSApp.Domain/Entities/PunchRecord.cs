using EMSApp.Domain.Exceptions;

namespace EMSApp.Domain;

public class PunchRecord
{
    public string Id {  get; private set; }
    public string UserId { get; private set; }
    public DateOnly Date { get; private set; }
    public TimeOnly TimeIn { get; private set; }
    public TimeOnly? TimeOut { get; private set; }
    public TimeSpan? TotalHours { get; private set; }
    public List<BreakSession> BreakSessions { get; private set; }

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
        BreakSessions = new List<BreakSession>();
    }

    public void PunchOut(TimeOnly timeOut)
    {
        if (BreakSessions.Any(bs => !bs.EndTime.HasValue))
            throw new DomainException("Cannot punch out while a break is still active");
        if (TimeOut.HasValue)
            throw new DomainException("Already punched out");
        if (timeOut == default)
            throw new DomainException("Punch-out time must be provided");
        if (timeOut < TimeIn)
            throw new DomainException("Punch-out time must be after punch-in time");

        TimeOut = timeOut;
        TotalHours = timeOut - TimeIn;
    }

    public void AddBreakSession(BreakSession session)
    {
        if (session == null)
            throw new DomainException("Break session cannot be null");
        if (!session.EndTime.HasValue)
            throw new DomainException("Break session must be finished");
        if (TimeOut.HasValue)
            throw new DomainException("Cannot add break session after punch-out");
        if (session.StartTime < TimeIn)
            throw new DomainException("Break session must after punch-in time");


        BreakSessions.Add(session);
    }

    public BreakSession StartBreakSession(TimeOnly startTime)
    {
        if (BreakSessions.Any(bs => !bs.EndTime.HasValue))
            throw new DomainException("There is already an open break.");

        var bs = new BreakSession(this.Id, startTime);
        BreakSessions.Add(bs);
        return bs;
    }

    public void EndBreakSession(string breakSessionId, TimeOnly endTime)
    {
        var bs = BreakSessions.SingleOrDefault(x => x.Id == breakSessionId)
                 ?? throw new DomainException("Break session not found.");

        bs.End(endTime);
    }

    public TimeSpan GetTotalBreakDuration()
    {
        return BreakSessions
            .Aggregate(TimeSpan.Zero, (acc, b) => acc + (b.Duration ?? TimeSpan.Zero));
    }


    public TimeSpan? GetWorkedTime()
    {
        if (!TimeOut.HasValue)
            return null;

        return TotalHours - GetTotalBreakDuration();
    }

    public bool IsComplete() => TimeOut.HasValue;
}

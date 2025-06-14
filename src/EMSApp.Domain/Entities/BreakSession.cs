using EMSApp.Domain.Exceptions;

namespace EMSApp.Domain;

public class BreakSession
{
    public string Id { get; private set; }
    public string PunchRecordId { get; private set; }
    public TimeOnly StartTime { get; private set; }
    public TimeOnly? EndTime { get; private set; }
    public TimeSpan? Duration { get; private set; }
    public bool IsNonCompliant { get; private set; } = false;

    public BreakSession(string punchRecordId, TimeOnly start)
    {
        // Punch Record validation
        if (string.IsNullOrWhiteSpace(punchRecordId))
            throw new DomainException("Punch Record Id cannot be empty");

        // StartTime time validation
        if (start == default)
            throw new DomainException("StartTime time must be provided");

        Id = Guid.NewGuid().ToString();
        PunchRecordId = punchRecordId;
        StartTime = start;
        EndTime = null;
        Duration = null;
    }

    public void End(TimeOnly end)
    {
        if (EndTime.HasValue)
            throw new DomainException("Break has already ended");
        if (end == default)
            throw new DomainException("EndTime time must be provided");
        if (end < StartTime)
            throw new DomainException("EndTime time must be after start time");

        EndTime = end;
        Duration = end - StartTime;
    }

    public bool IsComplete() => EndTime.HasValue;

    public void UpdateStartTime(TimeOnly newStartTime)
    {
        if (newStartTime == default)
            throw new DomainException("StartTime time must be provided");

        StartTime = newStartTime;
    }

    public void UpdateEndTime(TimeOnly newEndTime)
    {
        if (newEndTime == default)
            throw new DomainException("EndTime time must be provided");

        EndTime = newEndTime;
    }

    public void MarkAsNonCompliant()
    {
        IsNonCompliant = true;
    }
}

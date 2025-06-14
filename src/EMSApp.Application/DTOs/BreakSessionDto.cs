namespace EMSApp.Application;

public sealed record BreakSessionDto(
    string Id,
    string PunchRecordId,
    TimeOnly StartTime,
    TimeOnly? EndTime,
    TimeSpan? Duration,
    bool IsNonCompliant
);

namespace EMSApp.Application;

public sealed record PunchRecordDto(
    string Id,
    string UserId,
    DateOnly Date,
    TimeOnly TimeIn,
    TimeOnly? TimeOut,
    TimeSpan? TotalHours,
    bool IsNonCompliant
);

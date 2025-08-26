namespace EMSApp.Api;

public sealed record CreatePunchRecordRequest(
    string UserId,
    DateOnly Date,
    TimeOnly TimeIn
);
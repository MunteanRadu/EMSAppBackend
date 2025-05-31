namespace EMSApp.Api;

public record CreatePunchRecordRequest(
    string UserId,
    DateOnly Date,
    TimeOnly TimeIn
);
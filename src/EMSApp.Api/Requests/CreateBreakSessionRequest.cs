namespace EMSApp.Api;

public sealed record CreateBreakSessionRequest(
    TimeOnly StartTime
);

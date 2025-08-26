namespace EMSApp.Api;

public sealed record UpdateBreakSessionRequest(
    TimeOnly EndTime
);

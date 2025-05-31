namespace EMSApp.Api;

public record class UpdateBreakSessionRequest
{
    public TimeOnly EndTime { get; init; }
}

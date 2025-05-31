namespace EMSApp.Api;

public record class UpdateScheduleRequest
{
    public TimeOnly? StartTime { get; init; }
    public TimeOnly? EndTime { get; init; }
    public bool? IsWorkingDay { get; init; }
}

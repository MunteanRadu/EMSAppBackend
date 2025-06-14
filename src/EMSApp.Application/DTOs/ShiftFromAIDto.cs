namespace EMSApp.Application;

public sealed record ShiftFromAiDto
{
    public string UserId { get; set; } = string.Empty;
    public DateOnly Date { get; set; }
    public string Shift { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
}

using EMSApp.Domain;

namespace EMSApp.Application;

public record AssignmentFeedbackDto
{
    public string Id { get; init; } = null!;
    public string AssignmentId { get; init; } = null!;
    public string UserId { get; init; } = null!;
    public string Text { get; init; } = null!;
    public DateTime TimeStamp { get; init; }
    public FeedbackType Type { get; init; }
}

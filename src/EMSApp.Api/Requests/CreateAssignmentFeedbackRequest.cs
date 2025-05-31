using EMSApp.Domain;

namespace EMSApp.Api;

public record class CreateAssignmentFeedbackRequest
{
    public string AssignmentId { get; init; } = null!;
    public string UserId { get; init; } = null!;
    public string Text { get; init; } = null!;
    public FeedbackType Type { get; init; }
}

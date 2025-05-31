using EMSApp.Domain;

namespace EMSApp.Api;

public record class UpdateAssignmentFeedbackRequest
{
    public string? Text { get; init; }
}

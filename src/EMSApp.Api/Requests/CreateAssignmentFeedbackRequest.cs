using EMSApp.Domain;

namespace EMSApp.Api;

public sealed record CreateAssignmentFeedbackRequest(
    string AssignmentId,
    string UserId,
    string Text,
    FeedbackType Type
);
    

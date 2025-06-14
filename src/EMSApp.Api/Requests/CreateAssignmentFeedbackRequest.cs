using EMSApp.Domain;

namespace EMSApp.Api;

public record class CreateAssignmentFeedbackRequest(
    string AssignmentId,
    string UserId,
    string Text,
    FeedbackType Type
);
    

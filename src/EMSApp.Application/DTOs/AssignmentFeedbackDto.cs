using EMSApp.Domain;

namespace EMSApp.Application;

public record AssignmentFeedbackDto(
    string Id,
    string AssignmentId,
    string UserId,
    string Text,
    DateTime TimeStamp,
    FeedbackType Type
);
    

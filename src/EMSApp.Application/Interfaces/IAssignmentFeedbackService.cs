using EMSApp.Domain;

namespace EMSApp.Application;

public interface IAssignmentFeedbackService
{
    Task<AssignmentFeedback> CreateAsync(string assignmentId, string userId, string text, FeedbackType type, CancellationToken ct);
    Task<AssignmentFeedback?> GetByIdAsync(string id, CancellationToken ct);
    Task<IReadOnlyList<AssignmentFeedback>> ListByAssignmentAsync(string assignmentId, CancellationToken ct);
    Task UpdateAsync(AssignmentFeedback assignmentFeedback, CancellationToken ct);
    Task DeleteAsync(string id,  CancellationToken ct);
    Task<IReadOnlyList<AssignmentFeedback>> GetAllAsync(CancellationToken ct);
}

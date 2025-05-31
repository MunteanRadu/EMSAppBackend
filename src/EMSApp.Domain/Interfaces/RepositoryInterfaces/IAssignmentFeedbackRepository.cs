namespace EMSApp.Domain;

public interface IAssignmentFeedbackRepository
{
    Task<AssignmentFeedback?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AssignmentFeedback>> ListByAssignmentAsync(string taskId, CancellationToken cancellationToken = default);
    Task CreateAsync(AssignmentFeedback taskFeedback, CancellationToken cancellationToken = default);
    Task UpdateAsync(AssignmentFeedback taskFeedback, bool isUpsert, CancellationToken cancellationToken = default);
    Task DeleteAsync(string id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AssignmentFeedback>> GetAllAsync(CancellationToken cancellationToken = default);
}

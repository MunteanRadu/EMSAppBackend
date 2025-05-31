using EMSApp.Application;
using EMSApp.Domain;

namespace EMSApp.Infrastructure;

public class AssignmentFeedbackService : IAssignmentFeedbackService
{
    private readonly IAssignmentFeedbackRepository _repo;
    public AssignmentFeedbackService(IAssignmentFeedbackRepository repo) => _repo = repo;

    public async Task<AssignmentFeedback> CreateAsync(string assignmentId, string userId, string text, FeedbackType type, CancellationToken ct)
    {
        var assignmentFeedback = new AssignmentFeedback(assignmentId, userId, text, type);
        await _repo.CreateAsync(assignmentFeedback, ct);
        return assignmentFeedback;
    }

    public Task<AssignmentFeedback?> GetByIdAsync(string id, CancellationToken ct)
    {
        return _repo.GetByIdAsync(id, ct);
    }

    public Task<IReadOnlyList<AssignmentFeedback>> ListByAssignmentAsync(string assignmentId, CancellationToken ct)
    {
        return _repo.ListByAssignmentAsync(assignmentId, ct);
    }

    public Task UpdateAsync(AssignmentFeedback assignmentFeedback, CancellationToken ct)
    {
        return _repo.UpdateAsync(assignmentFeedback, false, ct);
    }

    public Task DeleteAsync(string id, CancellationToken ct)
    {
        return _repo.DeleteAsync(id, ct);
    }

    public Task<IReadOnlyList<AssignmentFeedback>> GetAllAsync(CancellationToken ct)
    {
        return _repo.GetAllAsync(ct);
    }
}

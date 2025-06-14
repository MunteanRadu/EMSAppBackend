using System.Linq.Expressions;

namespace EMSApp.Domain;

public interface ILeaveRequestRepository
{
    Task<LeaveRequest?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LeaveRequest>> GetAllAsync(CancellationToken ct);
    Task<IReadOnlyList<LeaveRequest>> GetByUserAsync(string userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LeaveRequest>> GetByManagerAsync(string managerId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LeaveRequest>> GetByStatusAsync(LeaveStatus status, CancellationToken cancellationToken = default);
    Task CreateAsync(LeaveRequest request, CancellationToken cancellationToken = default);
    Task UpdateAsync(LeaveRequest request, bool isUpsert, CancellationToken cancellationToken = default);
    Task DeleteAsync(string id, CancellationToken cancellationToken = default);
    Task<IEnumerable<LeaveRequest>> GetApprovedLeavesForWeekAsync(IEnumerable<string> userIds, DateOnly weekStart, CancellationToken ct);
    Task<IReadOnlyList<LeaveRequest>> FilterByAsync(Expression<Func<LeaveRequest, bool>> predicate, CancellationToken cancellationToken = default);
}

namespace EMSApp.Domain;

public interface ILeaveRequestRepository
{
    Task<LeaveRequest?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LeaveRequest>> GetAllAsync(CancellationToken ct);
    Task<IReadOnlyList<LeaveRequest>> ListByUserAsync(string userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LeaveRequest>> ListByManagerAsync(string managerId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LeaveRequest>> ListByStatusAsync(LeaveStatus status, CancellationToken cancellationToken = default);
    Task CreateAsync(LeaveRequest request, CancellationToken cancellationToken = default);
    Task UpdateAsync(LeaveRequest request, bool isUpsert, CancellationToken cancellationToken = default);
    Task DeleteAsync(string id, CancellationToken cancellationToken = default);
}

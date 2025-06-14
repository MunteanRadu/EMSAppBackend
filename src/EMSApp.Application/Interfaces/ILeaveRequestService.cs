using EMSApp.Domain;

namespace EMSApp.Application;

public interface ILeaveRequestService
{
    Task<LeaveRequest> CreateAsync(string userId, LeaveType type, DateOnly startDate, DateOnly endDate, string reason, CancellationToken ct);
    Task<LeaveRequest?> GetByIdAsync(string id, CancellationToken ct);
    Task<IReadOnlyList<LeaveRequest>> ListByUserAsync(string userId, CancellationToken ct);
    Task<IReadOnlyList<LeaveRequest>> ListByManagerAsync(string managerId, CancellationToken ct);
    Task<IReadOnlyList<LeaveRequest>> ListByStatusAsync(LeaveStatus status, CancellationToken ct);
    Task UpdateAsync(LeaveRequest request, CancellationToken ct);
    Task DeleteAsync(string id, CancellationToken ct);
    Task<IReadOnlyList<LeaveRequest>> GetAllAsync(CancellationToken ct);
    Task<bool> HasOverlappingLeaveAsync(string userId, DateOnly startDate, DateOnly endDate, CancellationToken ct);
    Task<int> CompleteDueRequestsAsync(CancellationToken ct);
    Task<int> GetRemainingLeaveDaysAsync(string userId, LeaveType type, int year, CancellationToken ct);
}

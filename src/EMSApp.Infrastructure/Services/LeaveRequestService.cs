using EMSApp.Application;
using EMSApp.Domain;

namespace EMSApp.Infrastructure;

public class LeaveRequestService : ILeaveRequestService
{
    private readonly ILeaveRequestRepository _repo;
    public LeaveRequestService(ILeaveRequestRepository repo)
    {
        _repo = repo;
    }

    public async Task<LeaveRequest> CreateAsync(string userId, LeaveType type, DateOnly startDate, DateOnly endDate, string reason, CancellationToken ct)
    {
        var leaveRequest = new LeaveRequest(userId, type, startDate, endDate, reason);
        await _repo.CreateAsync(leaveRequest, ct);
        return leaveRequest;
    }

    public Task<LeaveRequest?> GetByIdAsync(string id, CancellationToken ct)
    {
        return _repo.GetByIdAsync(id, ct);
    }

    public Task<IReadOnlyList<LeaveRequest>> ListByManagerAsync(string managerId, CancellationToken ct)
    {
        return _repo.ListByManagerAsync(managerId, ct);
    }

    public Task<IReadOnlyList<LeaveRequest>> ListByStatusAsync(LeaveStatus status, CancellationToken ct)
    {
        return _repo.ListByStatusAsync(status, ct);
    }

    public Task<IReadOnlyList<LeaveRequest>> ListByUserAsync(string userId, CancellationToken ct)
    {
        return _repo.ListByUserAsync(userId, ct);
    }

    public Task UpdateAsync(LeaveRequest request, CancellationToken ct)
    {
        return _repo.UpdateAsync(request, false, ct);
    }

    public Task DeleteAsync(string id, CancellationToken ct)
    {
        return _repo.DeleteAsync(id, ct);
    }

    public Task<IReadOnlyList<LeaveRequest>> GetAllAsync(CancellationToken ct)
    {
        return _repo.GetAllAsync(ct);
    }

    public async Task<int> CompleteDueRequestsAsync(CancellationToken ct)
    {
        var approved = await _repo.ListByStatusAsync(LeaveStatus.Approved, ct);

        var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);
        var due = approved.Where(r => r.EndDate <= today).ToList();

        foreach (var r in due)
        {
            r.Complete();
            await _repo.UpdateAsync(r, isUpsert: false, cancellationToken: ct);
        }

        return due.Count;
    }
}

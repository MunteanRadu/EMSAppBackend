using EMSApp.Application;
using EMSApp.Domain;
using EMSApp.Domain.Exceptions;

namespace EMSApp.Infrastructure;

public class LeaveRequestService : ILeaveRequestService
{
    private readonly ILeaveRequestRepository _repo;
    private readonly IPolicyService _policyService;
    private readonly IUserRepository _userRepository;
    public LeaveRequestService(ILeaveRequestRepository repo, IPolicyService policyService, IUserRepository userRepository)
    {
        _repo = repo;
        _policyService = policyService;
        _userRepository = userRepository;
    }

    public async Task<LeaveRequest> CreateAsync(string userId, LeaveType type, DateOnly startDate, DateOnly endDate, string reason, CancellationToken ct)
    {
        var user = await _userRepository.GetByIdAsync(userId, ct)
               ?? throw new DomainException("User not found.");

        if (string.IsNullOrWhiteSpace(user.DepartmentId))
            throw new DomainException("You must be assigned to a department before requesting leave.");

        var overlap = await HasOverlappingLeaveAsync(userId, startDate, endDate, ct);
        if (overlap)
            throw new DomainException("You already have a leave request overlapping with this period.");

        var year = startDate.Year;
        var remainingDays = await GetRemainingLeaveDaysAsync(userId, type, year, ct);
        var requestedDays = CountBusinessDays(startDate, endDate);
        if (requestedDays > remainingDays)
            throw new DomainException($"You don't have enough leave days of type {type}");

        var leaveRequest = new LeaveRequest(userId, type, startDate, endDate, reason);
        await _repo.CreateAsync(leaveRequest, ct);
        return leaveRequest;
    }

    public async Task<int> GetRemainingLeaveDaysAsync(string userId, LeaveType type, int year, CancellationToken ct)
    {
        var policy = await _policyService.GetByYearAsync(year, ct);
        if (policy == null)
            throw new DomainException($"No company policy defined for year {year}.");

        var quota = policy.GetLeaveQuota(type);

        var startOfYear = new DateOnly(year, 1, 1);
        var startOfNextYear = new DateOnly(year + 1, 1, 1);

        var approvedRequests = await _repo.FilterByAsync(lr =>
            lr.UserId == userId &&
            lr.Type == type &&
            lr.Status == LeaveStatus.Approved &&
            lr.StartDate >= startOfYear &&
            lr.StartDate < startOfNextYear, ct);

        var usedDays = approvedRequests.Sum(lr => CountBusinessDays(lr.StartDate, lr.EndDate));

        var remaining = quota - usedDays;
        return remaining < 0 ? 0 : remaining;
    }

    public Task<LeaveRequest?> GetByIdAsync(string id, CancellationToken ct)
    {
        return _repo.GetByIdAsync(id, ct);
    }

    public Task<IReadOnlyList<LeaveRequest>> ListByManagerAsync(string managerId, CancellationToken ct)
    {
        return _repo.GetByManagerAsync(managerId, ct);
    }

    public Task<IReadOnlyList<LeaveRequest>> ListByStatusAsync(LeaveStatus status, CancellationToken ct)
    {
        return _repo.GetByStatusAsync(status, ct);
    }

    public Task<IReadOnlyList<LeaveRequest>> ListByUserAsync(string userId, CancellationToken ct)
    {
        return _repo.GetByUserAsync(userId, ct);
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
        var approved = await _repo.GetByStatusAsync(LeaveStatus.Approved, ct);

        var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);
        var due = approved.Where(r => r.EndDate <= today).ToList();

        foreach (var r in due)
        {
            r.Complete();
            await _repo.UpdateAsync(r, isUpsert: false, cancellationToken: ct);
        }

        return due.Count;
    }

    public async Task<bool> HasOverlappingLeaveAsync(string userId, DateOnly startDate, DateOnly endDate, CancellationToken ct)
    {
        var overlapping = await _repo.FilterByAsync(lr =>
            lr.UserId == userId &&
            lr.Status != LeaveStatus.Rejected &&
            lr.StartDate <= endDate &&
            lr.EndDate >= startDate, ct);

        return overlapping.Any();
    }

    public static int CountBusinessDays(DateOnly start, DateOnly end)
    {
        int days = 0;
        for (var date = start; date <= end; date = date.AddDays(1))
        {
            if (date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday)
                days++;
        }
        return days;
    }

}

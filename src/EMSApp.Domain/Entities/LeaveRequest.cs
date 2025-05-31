using EMSApp.Domain.Exceptions;

namespace EMSApp.Domain;

public enum LeaveStatus { Pending, Approved, Rejected, Completed }

public class LeaveRequest
{
    public string Id { get; private set; }
    public string UserId { get; private set; }
    public LeaveType Type { get; private set; }
    public DateOnly StartDate { get; private set; }
    public DateOnly EndDate { get; private set; }
    public string Reason { get; private set; }
    public LeaveStatus Status { get; private set;}
    public string? ManagerId { get; private set; }
    public DateTimeOffset? RequestedAt{ get; private set; }
    public DateTimeOffset? DecisionAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }

    public LeaveRequest() { }

    public LeaveRequest(string userId, LeaveType type, DateOnly startDate, DateOnly endDate, string reason)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        // Past‐date guard
        if (startDate < today)
            throw new DomainException("Cannot request leave starting in the past");

        // Optional future‐horizon guard (1 year)
        if (startDate > today.AddYears(1))
            throw new DomainException("Leave cannot start more than 1 year from today");

        // End‐date checks (existing)
        if (endDate < startDate)
            throw new DomainException("End date must be on or after the start date");

        // User validation
        if (string.IsNullOrWhiteSpace(userId)) 
            throw new DomainException("User Id cannot be empty");

        // LeaveType validation
        if (!Enum.IsDefined(typeof(LeaveType), type))
            throw new DomainException("Invalid leave type");

        // StartDate validation
        if (startDate == default)
            throw new DomainException("Start date must be provided");

        // EndDate validation
        if (endDate == default)
            throw new DomainException("End date must be provided");
        if (endDate < startDate)
            throw new DomainException("End date must be after start date");

        // Reason validation
        if (string.IsNullOrWhiteSpace(reason))
            throw new DomainException("Reason cannot be empty");

        Id = Guid.NewGuid().ToString();
        UserId = userId;
        Type = type;
        StartDate = startDate;
        EndDate = endDate;
        Reason = reason;
        Status = LeaveStatus.Pending;
        RequestedAt = DateTimeOffset.UtcNow;
    }

    public void Approve(string managerId)
    {
        if (Status != LeaveStatus.Pending)
            throw new DomainException("Can only approve a pending leave request");

        ValidateManager(managerId);

        Status = LeaveStatus.Approved;
        ManagerId = managerId;
        DecisionAt = DateTimeOffset.UtcNow;
    }

    public void Reject(string managerId)
    {
        if (Status != LeaveStatus.Pending)
            throw new DomainException("Can only reject a pending leave request");

        ValidateManager(managerId);

        Status = LeaveStatus.Rejected;
        ManagerId = managerId;
        DecisionAt = DateTimeOffset.UtcNow;
        CompletedAt = DecisionAt;
    }

    public void Complete()
    {
        if (Status != LeaveStatus.Approved)
            throw new DomainException("Can only finalize an approved leave request");

        Status = LeaveStatus.Completed;
        CompletedAt = DateTimeOffset.UtcNow;
    }

    public bool IsCompleted() => Status.Equals(LeaveStatus.Completed);

    private void ValidateManager(string managerId)
    {
        // Manager Id validation
        if (string.IsNullOrWhiteSpace(managerId))
            throw new DomainException("Manager Id cannot be empty");
    }

    public void UpdateType(LeaveType newType)
    {
        if (!Enum.IsDefined(typeof(LeaveType), newType))
            throw new DomainException("Invalid leave type");
        Type = newType;
    }

    public void UpdateStartDate(DateOnly newDate)
    {
        if (newDate == default)
            throw new DomainException("Start date must be provided");
        StartDate = newDate;
    }

    public void UpdateEndDate(DateOnly newDate)
    {
        if (newDate == default)
            throw new DomainException("End date must be provided");
        if (newDate < StartDate)
            throw new DomainException("End date must be after start date");
        EndDate = newDate;
    }

    public void UpdateReason(string newReason)
    {
        if (string.IsNullOrWhiteSpace(newReason))
            throw new DomainException("Reason cannot be empty");
        Reason = newReason;
    }
}

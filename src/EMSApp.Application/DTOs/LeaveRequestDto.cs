using EMSApp.Domain;

namespace EMSApp.Application;

public sealed record LeaveRequestDto(
     string Id,
     string UserId,
     LeaveType Type,
     DateOnly StartDate,
     DateOnly EndDate,
     string Reason,
     LeaveStatus Status,
     string? ManagerId,
     DateTimeOffset? RequestedAt,
     DateTimeOffset? DecisionAt,
     DateTimeOffset? CompletedAt
);
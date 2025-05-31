using EMSApp.Domain;

namespace EMSApp.Api;

public sealed record UpdateLeaveRequestRequest(
     LeaveType? Type,
     DateOnly? StartDate,
     DateOnly? EndDate,
     string? Reason
);

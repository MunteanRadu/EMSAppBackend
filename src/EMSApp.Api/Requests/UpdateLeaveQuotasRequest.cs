using EMSApp.Domain;

namespace EMSApp.Api;

public sealed record UpdateLeaveQuotasRequest(
    IDictionary<LeaveType, int>? LeaveQuotas
);

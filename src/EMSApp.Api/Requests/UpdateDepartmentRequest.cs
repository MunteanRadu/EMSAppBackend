namespace EMSApp.Api;

public sealed record UpdateDepartmentRequest(
    string? Name,
    string? ManagerId
);

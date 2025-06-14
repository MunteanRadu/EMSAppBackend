namespace EMSApp.Api;

public record class UpdateDepartmentRequest(
    string? Name,
    string? ManagerId
);

namespace EMSApp.Api;

public record class UpdateDepartmentRequest
{
    public string? Name { get; init; }
    public string? ManagerId { get; init; }
}

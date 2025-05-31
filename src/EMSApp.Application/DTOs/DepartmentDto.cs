namespace EMSApp.Application;

public class DepartmentDto
{
    public string Id { get; init; } = null!;
    public string Name { get; init; } = null!;
    public string ManagerId { get; init; } = null!;
    public List<string> Employees { get; init; } = null!;
}

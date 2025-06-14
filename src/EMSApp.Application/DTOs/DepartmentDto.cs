namespace EMSApp.Application;

public sealed record DepartmentDto(
    string Id,
    string Name,
    string ManagerId,
    List<string> Employees
);

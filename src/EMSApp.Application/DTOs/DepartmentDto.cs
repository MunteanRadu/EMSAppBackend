namespace EMSApp.Application;

public class DepartmentDto(
    string Id,
    string Name,
    string ManagerId,
    List<string> Employees
);

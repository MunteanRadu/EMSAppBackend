namespace EMSApp.Application;

<<<<<<< HEAD
public class DepartmentDto(
=======
public sealed record DepartmentDto(
>>>>>>> 73f68f78fad61200a38ac8e657024323c185547d
    string Id,
    string Name,
    string ManagerId,
    List<string> Employees
);

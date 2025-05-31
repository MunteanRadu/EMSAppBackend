using EMSApp.Domain.Exceptions;
using System.Xml.Linq;

namespace EMSApp.Domain.Entities;

public class Department
{
    public string Id { get; private set; }
    public string Name { get; private set; }
    public string ManagerId { get; private set; }

    public List<string> Employees { get; private set; } = new();

    private Department()
    {
        Employees = new List<string>();
    }

    public Department(string name) 
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("DepartmentId name cannot be empty");

        Id = Guid.NewGuid().ToString();
        Name = name;
        Employees = new List<string>();
    }

    public void AddEmployee(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new DomainException("UserId ID cannot be empty");
        if (Employees.Contains(userId))
            throw new DomainException("UserId is already in the department");

        Employees.Add(userId);
    }

    public void RemoveEmployee(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new DomainException("UserId ID cannot be empty");
        if (!Employees.Contains(userId))
            throw new DomainException("UserId was not in this department");

        Employees.Remove(userId);
    }

    public void UpdateName(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
            throw new DomainException("DepartmentId name cannot be empty");

        Name = newName;
    }

    public void AssignManager(string managerId)
    {
        if (string.IsNullOrWhiteSpace(managerId))
            throw new DomainException("DepartmentId manager cannot be empty");

        ManagerId = managerId;
    }
}

using EMSApp.Domain.Entities;
using EMSApp.Domain.Exceptions;

namespace EMSApp.Tests;

[Trait("Category", "Domain")]
public class DepartmentTests
{
    [Fact]
    public void Constructor_ValidParameters_CreatesDepartment()
    {
        // Arrange
        var name = "New DepartmentId";
        var manager = "manager-123";

        // Act
        var department = new Department(name, manager);

        // Assert
        Assert.Equal(name, department.Name);
        Assert.Equal(manager, department.ManagerId);
        Assert.Empty(department.Employees);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_InvalidManager_ThrowDomainException(string badManagerId)
    {
        // Arrange
        var name = "New DepartmentId";

        // Act & Assert
        var ex = Assert.Throws<DomainException>(() =>
            new Department(name, badManagerId)
        );
        Assert.Contains("DepartmentId manager cannot be empty", ex.Message);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_InvalidName_ThrowDomainException(string badName)
    {
        // Arrange
        var manager = "manager-123";

        // Act & Assert
        var ex = Assert.Throws<DomainException>(() =>
            new Department(badName, manager)
        );
        Assert.Contains("DepartmentId name cannot be empty", ex.Message);
    }

    [Fact]
    public void AddEmployee_ValidParameters_AddsEmployeeToList()
    {
        // Arrange
        var name = "New DepartmentId";
        var manager = "manager-123";
        var department = new Department(name, manager);

        // Act & Assert
        var employeeId = "employee-123";
        Assert.Empty(department.Employees);
        department.AddEmployee(employeeId);
        Assert.Single(department.Employees);
        Assert.Equal(employeeId, department.Employees[0]);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void AddEmployee_InvalidParameters_ThrowsDomainException(string badEmployeeId)
    {
        // Arrange
        var name = "New DepartmentId";
        var manager = "manager-123";
        var department = new Department(name, manager);

        // Act & Assert
        var ex = Assert.Throws<DomainException>(() =>
            department.AddEmployee(badEmployeeId)
        );
        Assert.Contains("UserId ID cannot be empty", ex.Message);
    }

    [Fact]
    public void AddEmployee_ExistingEmployee_ThrowsDomainException()
    {
        // Arrange
        var name = "New DepartmentId";
        var manager = "manager-123";
        var department = new Department(name, manager);
        var employee = "employee-123";

        // Act & Assert
        department.AddEmployee(employee);
        var ex = Assert.Throws<DomainException>(() =>
            department.AddEmployee(employee)
        );
        Assert.Contains("UserId is already in the department", ex.Message);
    }

    [Fact]
    public void RemoveEmployee_ValidParameters_AddsEmployeeToList()
    {
        // Arrange
        var name = "New DepartmentId";
        var manager = "manager-123";
        var employee = "employee-123";
        var department = new Department(name, manager);
        department.AddEmployee(employee);

        // Act & Assert
        Assert.Single(department.Employees);
        department.RemoveEmployee(employee);
        Assert.Empty(department.Employees);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void RemoveEmployee_InvalidParameters_ThrowsDomainException(string badEmployeeId)
    {
        // Arrange
        var name = "New DepartmentId";
        var manager = "manager-123";
        var employee = "employee-123";
        var department = new Department(name, manager);
        department.AddEmployee(employee);

        // Act & Assert
        var ex = Assert.Throws<DomainException>(() =>
            department.RemoveEmployee(badEmployeeId)
        );
        Assert.Contains("UserId ID cannot be empty", ex.Message);
    }

    [Fact]
    public void RemoveEmployee_NonExistingEmployee_ThrowsDomainException()
    {
        // Arrange
        var name = "New DepartmentId";
        var manager = "manager-123";
        var department = new Department(name, manager);

        // Act & Assert
        var employee = "employee-123";
        var ex = Assert.Throws<DomainException>(() =>
            department.RemoveEmployee(employee)
        );
        Assert.Contains("UserId was not in this department", ex.Message);
    }
}

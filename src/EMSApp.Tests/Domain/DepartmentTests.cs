using System;
using EMSApp.Domain.Entities;
using EMSApp.Domain.Exceptions;
using Xunit;

namespace EMSApp.Tests
{
    [Trait("Category", "Domain")]
    public class DepartmentTests
    {
        [Fact]
        public void Constructor_ValidName_CreatesDepartment()
        {
            // Arrange
            var name = "New Department";

            // Act
            var department = new Department(name);

            // Assert
            Assert.False(string.IsNullOrWhiteSpace(department.Id));
            Assert.Equal(name, department.Name);
            Assert.Empty(department.Employees);
            Assert.Null(department.ManagerId);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Constructor_InvalidName_ThrowsDomainException(string badName)
        {
            // Act & Assert
            var ex = Assert.Throws<DomainException>(() => new Department(badName));
            Assert.Contains("DepartmentId name cannot be empty", ex.Message);
        }

        [Fact]
        public void AssignManager_ValidId_SetsManagerId()
        {
            // Arrange
            var department = new Department("Dept A");
            var managerId = "mgr-123";

            // Act
            department.AssignManager(managerId);

            // Assert
            Assert.Equal(managerId, department.ManagerId);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void AssignManager_InvalidId_ThrowsDomainException(string badManagerId)
        {
            // Arrange
            var department = new Department("Dept A");

            // Act & Assert
            var ex = Assert.Throws<DomainException>(() => department.AssignManager(badManagerId));
            Assert.Contains("DepartmentId manager cannot be empty", ex.Message);
        }

        [Fact]
        public void UpdateName_ValidName_ChangesName()
        {
            // Arrange
            var department = new Department("Old Name");
            var newName = "New Name";

            // Act
            department.UpdateName(newName);

            // Assert
            Assert.Equal(newName, department.Name);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void UpdateName_InvalidName_ThrowsDomainException(string badName)
        {
            // Arrange
            var department = new Department("Dept X");

            // Act & Assert
            var ex = Assert.Throws<DomainException>(() => department.UpdateName(badName));
            Assert.Contains("DepartmentId name cannot be empty", ex.Message);
        }

        [Fact]
        public void AddEmployee_ValidUserId_AddsEmployee()
        {
            // Arrange
            var department = new Department("Dept B");
            var userId = "emp-123";

            // Act
            department.AddEmployee(userId);

            // Assert
            Assert.Single(department.Employees);
            Assert.Equal(userId, department.Employees[0]);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void AddEmployee_InvalidUserId_ThrowsDomainException(string badUserId)
        {
            // Arrange
            var department = new Department("Dept B");

            // Act & Assert
            var ex = Assert.Throws<DomainException>(() => department.AddEmployee(badUserId));
            Assert.Contains("UserId ID cannot be empty", ex.Message);
        }

        [Fact]
        public void AddEmployee_DuplicateUserId_ThrowsDomainException()
        {
            // Arrange
            var department = new Department("Dept B");
            var userId = "emp-123";
            department.AddEmployee(userId);

            // Act & Assert
            var ex = Assert.Throws<DomainException>(() => department.AddEmployee(userId));
            Assert.Contains("UserId is already in the department", ex.Message);
        }

        [Fact]
        public void RemoveEmployee_ExistingUserId_RemovesEmployee()
        {
            // Arrange
            var department = new Department("Dept C");
            var userId = "emp-321";
            department.AddEmployee(userId);

            // Act
            department.RemoveEmployee(userId);

            // Assert
            Assert.Empty(department.Employees);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void RemoveEmployee_InvalidUserId_ThrowsDomainException(string badUserId)
        {
            // Arrange
            var department = new Department("Dept D");
            department.AddEmployee("emp-001");

            // Act & Assert
            var ex = Assert.Throws<DomainException>(() => department.RemoveEmployee(badUserId));
            Assert.Contains("UserId ID cannot be empty", ex.Message);
        }

        [Fact]
        public void RemoveEmployee_NonExistingUserId_ThrowsDomainException()
        {
            // Arrange
            var department = new Department("Dept E");

            // Act & Assert
            var ex = Assert.Throws<DomainException>(() => department.RemoveEmployee("emp-999"));
            Assert.Contains("UserId was not in this department", ex.Message);
        }
    }
}

using System;
using DomainTask = EMSApp.Domain.Entities;
using EMSApp.Domain.Exceptions;

namespace EMSApp.Tests
{
    [Trait("Category", "Domain")]
    public class TaskTests
    {
        [Fact]
        public void Constructor_ValidParameters_CreatesTask()
        {
            // Arrange
            var title = "Do something";
            var desc = "Description";
            var due = DateTime.UtcNow.Date.AddDays(5);
            var departmentId = "dept-123";
            var managerId = "mgr-456";

            // Act
            var task = new DomainTask.Assignment(title, desc, due, departmentId, managerId);

            // Assert
            Assert.Equal(title, task.Title);
            Assert.Equal(desc, task.Description);
            Assert.Equal(due, task.DueDate);
            Assert.Equal(departmentId, task.DepartmentId);
            Assert.Equal(managerId, task.ManagerId);
            Assert.Equal(DomainTask.AssignmentStatus.Pending, task.Status);
            Assert.Null(task.AssignedToId);
            Assert.True(task.CreatedAt <= DateTime.UtcNow);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Constructor_InvalidTitle_ThrowsDomainException(string badTitle)
        {
            // Arrange
            var desc = "Description";
            var due = DateTime.UtcNow.Date.AddDays(1);
            var departmentId = "dept-123";
            var managerId = "mgr-456";

            // Act & Assert
            var ex = Assert.Throws<DomainException>(() =>
                new DomainTask.Assignment(badTitle, desc, due, departmentId, managerId)
            );
            Assert.Contains("Title cannot be empty", ex.Message);
        }

        [Fact]
        public void Constructor_InvalidDueDate_ThrowsDomainException()
        {
            // Arrange
            var title = "Do something";
            var desc = "Description";
            var past = DateTime.UtcNow.Date.AddDays(-1);
            var departmentId = "dept-123";
            var managerId = "mgr-456";

            // Act & Assert
            var ex = Assert.Throws<DomainException>(() =>
                new DomainTask.Assignment(title, desc, past, departmentId, managerId)
            );
            Assert.Contains("Due date must be in the future", ex.Message);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Constructor_InvalidDepartment_ThrowsDomainException(string badDept)
        {
            // Arrange
            var title = "Do something";
            var desc = "Description";
            var due = DateTime.UtcNow.Date.AddDays(1);
            var managerId = "mgr-456";

            // Act & Assert
            var ex = Assert.Throws<DomainException>(() =>
                new DomainTask.Assignment(title, desc, due, badDept, managerId)
            );
            Assert.Contains("DepartmentId cannot be empty", ex.Message);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Constructor_InvalidManager_ThrowsDomainException(string badManager)
        {
            // Arrange
            var title = "Do something";
            var desc = "Description";
            var due = DateTime.UtcNow.Date.AddDays(1);
            var departmentId = "dept-123";

            // Act & Assert
            var ex = Assert.Throws<DomainException>(() =>
                new DomainTask.Assignment(title, desc, due, departmentId, badManager)
            );
            Assert.Contains("ManagerId cannot be empty", ex.Message);
        }

        [Fact]
        public void Start_WhenPending_SetsStatusInProgressAndAssignee()
        {
            // Arrange
            var task = new DomainTask.Assignment("Do", "Desc", DateTime.UtcNow.Date.AddDays(1), "D1", "M1");

            // Act
            task.Start("user-123");

            // Assert
            Assert.Equal(DomainTask.AssignmentStatus.InProgress, task.Status);
            Assert.Equal("user-123", task.AssignedToId);
        }

        [Fact]
        public void Start_WhenNotPending_ThrowsDomainException()
        {
            // Arrange
            var task = new DomainTask.Assignment("Do", "Desc", DateTime.UtcNow.Date.AddDays(1), "D1", "M1");
            task.Start("user-123");

            // Act & Assert
            var ex = Assert.Throws<DomainException>(() => task.Start("user-456"));
            Assert.Contains("Can only start a pending task", ex.Message);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Start_WithEmptyAssignee_ThrowsDomainException(string assignee)
        {
            // Arrange
            var task = new DomainTask.Assignment("Do", "Desc", DateTime.UtcNow.Date.AddDays(1), "D1", "M1");

            // Act & Assert
            var ex = Assert.Throws<DomainException>(() => task.Start(assignee));
            Assert.Contains("Assignment must be assigned to an employee", ex.Message);
        }

        [Fact]
        public void Complete_InProgress_SetsStatusDone()
        {
            // Arrange
            var task = new DomainTask.Assignment("Do", "Desc", DateTime.UtcNow.Date.AddDays(1), "D1", "M1");
            task.Start("user-123");

            // Act
            task.Complete();

            // Assert
            Assert.Equal(DomainTask.AssignmentStatus.Done, task.Status);
        }

        [Fact]
        public void Complete_NotInProgress_ThrowsDomainException()
        {
            // Arrange
            var task = new DomainTask.Assignment("Do", "Desc", DateTime.UtcNow.Date.AddDays(1), "D1", "M1");

            // Act & Assert
            var ex = Assert.Throws<DomainException>(() => task.Complete());
            Assert.Contains("Can only complete an in-progress task", ex.Message);
        }

        [Fact]
        public void Approve_AfterDone_SetsStatusApproved()
        {
            // Arrange
            var task = new DomainTask.Assignment("Do", "Desc", DateTime.UtcNow.Date.AddDays(1), "D1", "M1");
            task.Start("user-123");
            task.Complete();

            // Act
            task.Approve();

            // Assert
            Assert.Equal(DomainTask.AssignmentStatus.Approved, task.Status);
        }

        [Fact]
        public void Approve_NotDone_ThrowsDomainException()
        {
            // Arrange
            var task = new DomainTask.Assignment("Do", "Desc", DateTime.UtcNow.Date.AddDays(1), "D1", "M1");
            task.Start("user-123");

            // Act & Assert
            var ex = Assert.Throws<DomainException>(() => task.Approve());
            Assert.Contains("Can only approve a completed task", ex.Message);
        }

        [Fact]
        public void Reject_AfterDone_SetsStatusRejected()
        {
            // Arrange
            var task = new DomainTask.Assignment("Do", "Desc", DateTime.UtcNow.Date.AddDays(1), "D1", "M1");
            task.Start("user-123");
            task.Complete();

            // Act
            task.Reject();

            // Assert
            Assert.Equal(DomainTask.AssignmentStatus.Rejected, task.Status);
        }

        [Fact]
        public void Reject_NotDone_ThrowsDomainException()
        {
            // Arrange
            var task = new DomainTask.Assignment("Do", "Desc", DateTime.UtcNow.Date.AddDays(1), "D1", "M1");
            task.Start("user-123");

            // Act & Assert
            var ex = Assert.Throws<DomainException>(() => task.Reject());
            Assert.Contains("Can only reject a completed task", ex.Message);
        }
    }
}

using DomainTask = EMSApp.Domain.Entities;
using EMSApp.Domain.Exceptions;

namespace EMSApp.Tests;

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
        var assignee = "user-123";

        //Act
        var task = new DomainTask.Assignment(title, desc, due, assignee);

        //Assert
        Assert.Equal(title, task.Title);
        Assert.Equal(desc, task.Description);
        Assert.Equal(DomainTask.AssignmentStatus.Pending, task.Status);
        Assert.Equal(assignee, task.AssignedToId);
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
        var assignee = "user-123";

        // Act & Assert
        var ex = Assert.Throws<DomainException>(() =>
            new DomainTask.Assignment(badTitle, desc, due, assignee)
        );
        Assert.Contains("AssignmentId title cannot be empty", ex.Message);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_InvalidUser_ThrowsDomainException(string badUserId)
    {
        // Arrange
        var title = "Do something";
        var desc = "Description";
        var due = DateTime.UtcNow.Date.AddDays(1);

        // Act & Assert
        var ex = Assert.Throws<DomainException>(() =>
            new DomainTask.Assignment(title, desc, due, badUserId)
        );
        Assert.Contains("A task must be assigned to a user", ex.Message);
    }

    [Fact]
    public void Constructor_PastDueDate_ThrowsDomainException()
    {
        // Arrange
        var title = "Do something";
        var desc = "Description";
        var past = DateTime.UtcNow.Date.AddDays(-1);
        var assignee = "user-123";

        // Act & Assert
        var ex = Assert.Throws<DomainException>(() =>
            new DomainTask.Assignment(title, desc, past, assignee)
        );
        Assert.Contains("Due date must be in the future", ex.Message);
    }

    [Fact]
    public void Start_WhenPending_SetsStatusInProgress()
    {
        // Arrange
        var title = "Do something";
        var desc = "Description";
        var dueDate = DateTime.UtcNow.Date.AddDays(1);
        var assignee = "user-123";

        // Act & Assert
        var task = new DomainTask.Assignment(title, desc, dueDate, assignee);
        task.Start();
        Assert.Equal(DomainTask.AssignmentStatus.InProgress, task.Status);
    }

    [Fact]
    public void Start_WhenNotPending_ThrowsDomainException()
    {
        // Arrange
        var title = "Do something";
        var desc = "Description";
        var dueDate = DateTime.UtcNow.Date.AddDays(1);
        var assignee = "user-123";

        // Act & Assert
        var task = new DomainTask.Assignment(title, desc, dueDate, assignee);
        task.Start();

        var ex = Assert.Throws<DomainException>(() => task.Start());
        Assert.Contains("Can only start a pending task", ex.Message);
    }

    [Fact]
    public void Complete_InProgress_SetsStatusDone()
    {
        // Arrange
        var title = "Do something";
        var desc = "Description";
        var dueDate = DateTime.UtcNow.Date.AddDays(1);
        var assignee = "user-123";
        var task = new DomainTask.Assignment(title, desc, dueDate, assignee);
        task.Start();

        // Act & Assert
        task.Complete();
        Assert.Equal(DomainTask.AssignmentStatus.Done, task.Status);
    }

    [Fact]
    public void Complete_NotInProgress_ThrowsDomainException()
    {
        // Arrange
        var title = "Do something";
        var desc = "Description";
        var dueDate = DateTime.UtcNow.Date.AddDays(1);
        var assignee = "user-123";
        var task = new DomainTask.Assignment(title, desc, dueDate, assignee);

        // Act & Assert
        var ex = Assert.Throws<DomainException>(() => task.Complete());
        Assert.Contains("Can only complete an in-progress task", ex.Message);
    }

    [Fact]
    public void Review_AfterDome_SetsStatusReviewed()
    {
        // Arrange
        var title = "Do something";
        var desc = "Description";
        var dueDate = DateTime.UtcNow.Date.AddDays(1);
        var assignee = "user-123";
        var task = new DomainTask.Assignment(title, desc, dueDate, assignee);
        task.Start();
        task.Complete();

        // Act & Assert
        task.Review();
        Assert.Equal(DomainTask.AssignmentStatus.Approved, task.Status);
    }

    [Fact]
    public void Review_NotDone_ThrowsDomainException()
    {
        // Arrange
        var title = "Do something";
        var desc = "Description";
        var dueDate = DateTime.UtcNow.Date.AddDays(1);
        var assignee = "user-123";
        var task = new DomainTask.Assignment(title, desc, dueDate, assignee);
        task.Start();

        // Act & Assert
        var ex = Assert.Throws<DomainException>(() => task.Review());
        Assert.Contains("Can only review a completed task", ex.Message);
    }
}

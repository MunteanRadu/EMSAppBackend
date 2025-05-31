using EMSApp.Domain;
using EMSApp.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.Runtime.ConstrainedExecution;

namespace EMSApp.Tests;

[Trait("Category", "Domain")]
public class LeaveRequestTests
{
    /// <summary>
    /// Shared test helper
    /// </summary>
    /// <param name="act"></param>
    /// <param name="expectedMessage"></param>
    private static void AssertThrowsLeaveRequestException(Action act, string expectedMessage)
    {
        var ex = Assert.Throws<DomainException>(act);
        Assert.Contains(expectedMessage, ex.Message);
    }

    /// <summary>
    /// Creates a valid LeaveRequest
    /// </summary>
    /// <returns></returns>
    private static LeaveRequest CreateValidLeaveRequest()
    {
        return new LeaveRequest("user-123", LeaveType.Parental, new DateOnly(2025, 4, 26), new DateOnly(2025, 5, 5), "newborn child");
    }

    /// <summary>
    /// Generates valid parameters for LeaveRequest
    /// </summary>
    /// <returns></returns>
    private static (string userId, LeaveType type, DateOnly startDate, DateOnly endDate, string reason, LeaveStatus status, string managerId, DateTimeOffset requestedAt, DateTimeOffset decisionAt, DateTimeOffset completedAt) GetValidParameters()
        => ("user-321", LeaveType.Sick, new DateOnly(2025, 4, 27), new DateOnly(2025, 5, 6), "pneumonia", LeaveStatus.Pending, "manager-321", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddHours(1), DateTimeOffset.UtcNow.AddDays(10));

    public static IEnumerable<object[]> InvalidEndDates() =>
    [
        [default(DateOnly), "End date must be provided"],
        [new DateOnly(2025, 4, 26), "End date must be after start date"]
    ];

    /// CONSTRUCTOR TESTS


    [Fact]
    public void Constructor_ValidParameters_CreatesLeaveRequest()
    {
        // Arrange
        var valid = GetValidParameters();

        // Act
        var request = new LeaveRequest(valid.userId, valid.type, valid.startDate, valid.endDate, valid.reason);

        // Assert
        Assert.Equal(valid.userId, request.UserId);
        Assert.Equal(valid.type, request.Type);
        Assert.Equal(valid.startDate, request.StartDate);
        Assert.Equal(valid.endDate, request.EndDate);
        Assert.Equal(valid.reason, request.Reason);
        Assert.True(valid.requestedAt < request.RequestedAt);
        Assert.Equal(LeaveStatus.Pending, request.Status);
        Assert.False(request.IsCompleted());
    }

    // UserId validation
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void Constructor_InvalidUserId_ThrowsDomainException(string badUserId)
    {
        // Arrange
        var valid = GetValidParameters();
        var expectedMessage = "User Id cannot be empty";

        // Act & Assert
        AssertThrowsLeaveRequestException(() => 
            new LeaveRequest(badUserId, valid.type, valid.startDate, valid.endDate, valid.reason),
            expectedMessage
        );
    }

    // LeaveType validation
    [Theory]
    [InlineData((LeaveType)999)]
    public void Constructor_InvalidLeaveType_ThrowsDomainException(LeaveType badLeaveType)
    {
        // Arrange
        var valid = GetValidParameters();
        var expectedMessage = "Invalid leave type";

        // Act & Assert
        AssertThrowsLeaveRequestException(() => 
            new LeaveRequest(valid.userId, badLeaveType, valid.startDate, valid.endDate, valid.reason),
            expectedMessage
        );
    }

    // StartDate validation
    [Theory]
    [InlineData(default)]
    public void Constructor_InvalidStartDate_ThrowsDomainException(DateOnly badStartDate)
    {
        // Arrange
        var valid = GetValidParameters();
        var expectedMessage = "Start date must be provided";

        // Act & Assert
        AssertThrowsLeaveRequestException(() => 
            new LeaveRequest(valid.userId, valid.type, badStartDate, valid.endDate, valid.reason),
            expectedMessage
        );
    }

    // EndDate validation
    [Theory]
    [MemberData(nameof(InvalidEndDates))]
    public void Constructor_InvalidEndDate_ThrowsDomainException(DateOnly badEndDate, string expectedMessage)
    {
        // Arrange
        var valid = GetValidParameters();

        // Act & Assert
        AssertThrowsLeaveRequestException(() => 
            new LeaveRequest(valid.userId, valid.type, valid.startDate, badEndDate, valid.reason),
            expectedMessage
        );
    }

    // Reason validation
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void Constructor_InvalidReason_ThrowsDomainException(string badReason)
    {
        // Arrange
        var valid = GetValidParameters();
        var expectedMessage = "Reason cannot be empty";

        // Act & Assert
        AssertThrowsLeaveRequestException(() => 
            new LeaveRequest(valid.userId, valid.type, valid.startDate, valid.endDate, badReason),
            expectedMessage
        );
    }

    /// METHODS TESTS

    // Approve - status pending
    [Fact]
    public void Approve_WhenPending_SetsStatusApproved()
    {
        // Arrange
        var request = CreateValidLeaveRequest();
        var valid = GetValidParameters();

        // Act
        request.Approve(valid.managerId);

        // Assert
        Assert.Equal(LeaveStatus.Approved, request.Status);
        Assert.Equal(valid.managerId, request.ManagerId);
        Assert.True(request.RequestedAt < request.DecisionAt);
    }

    // Approve - status not pending
    [Fact]
    public void Approve_WhenNotPending_ThrowsDomainException()
    {
        // Arrange
        var request = CreateValidLeaveRequest();
        var valid = GetValidParameters();
        var expectedMessage = "Can only approve a pending leave request";
        request.Reject(valid.managerId);

        // Act & Assert
        AssertThrowsLeaveRequestException(() => 
            request.Approve(valid.managerId),
            expectedMessage
        );
    }

    // Approve - invalid ManagerId
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void Approve_InvalidManagerId_ThrowsDomainException(string badManagerId)
    {
        // Arrange
        var request = CreateValidLeaveRequest();
        var valid = GetValidParameters();
        var expectedMessage = "Manager Id cannot be empty";

        // Act & Assert
        AssertThrowsLeaveRequestException(() =>
            request.Approve(badManagerId),
            expectedMessage
        );
    }

    // Reject - status pending
    [Fact]
    public void Reject_WhenPending_SetsStatusRejected()
    {
        // Arrange
        var request = CreateValidLeaveRequest();
        var valid = GetValidParameters();

        // Act
        request.Reject(valid.managerId);

        // Assert
        Assert.Equal(LeaveStatus.Rejected, request.Status);
        Assert.Equal(valid.managerId, request.ManagerId);
        Assert.True(request.RequestedAt < request.DecisionAt);
        Assert.Equal(request.DecisionAt, request.CompletedAt);
    }

    // Reject - status not pending
    [Fact]
    public void Reject_WhenNotPending_ThrowsDomainException()
    {
        // Arrange
        var request = CreateValidLeaveRequest();
        var valid = GetValidParameters();
        var expectedMessage = "Can only reject a pending leave request";

        request.Approve(valid.managerId);

        // Act & Assert
        AssertThrowsLeaveRequestException(() => 
            request.Reject(valid.managerId),
            expectedMessage
        );
    }

    // Reject - invalid ManagerId
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void Reject_InvalidManagerId_ThrowsDomainException(string badManagerId)
    {
        // Arrange
        var request = CreateValidLeaveRequest();
        var valid = GetValidParameters();
        var expectedMessage = "Manager Id cannot be empty";

        // Act & Assert
        AssertThrowsLeaveRequestException(() =>
            request.Reject(badManagerId),
            expectedMessage
        );
    }

    // Complete - status approved
    [Fact]
    public void Complete_WhenApproved_SetsStatusCompleted()
    {
        // Arrange
        var request = CreateValidLeaveRequest();
        var valid = GetValidParameters();

        request.Approve(valid.managerId);
        Assert.Equal(LeaveStatus.Approved, request.Status);

        // Act
        request.Complete();

        // Assert
        Assert.Equal(LeaveStatus.Completed, request.Status);
        Assert.True(request.RequestedAt < request.CompletedAt);
        Assert.True(request.DecisionAt < request.CompletedAt);
    }

    // Complete - status not approved
    [Fact]
    public void Complete_WhenNotApproved_ThrowsDomainException()
    {
        // Arrange
        var request = CreateValidLeaveRequest();
        var valid = GetValidParameters();
        var expectedMessage = "Can only finalize an approved leave request";

        request.Reject(valid.managerId);
        Assert.Equal(LeaveStatus.Rejected, request.Status);

        // Act & Assert
        AssertThrowsLeaveRequestException(() => 
            request.Complete(), 
            expectedMessage
        );
    }

    // IsComplete - true
    [Fact]
    public void IsComplete_WhenCompleted_ReturnsTrue()
    {
        // Arrange
        var request = CreateValidLeaveRequest();
        var valid = GetValidParameters();

        // Act
        request.Approve(valid.managerId);
        request.Complete();

        // Assert
        Assert.True(request.IsCompleted());
    }

    // IsComplete - false
    [Fact]
    public void IsComplete_WhenNotCompleted_ReturnsFalse()
    {
        // Arrange
        var request = CreateValidLeaveRequest();
        var valid = GetValidParameters();

        // Act
        request.Approve(valid.managerId);

        // Assert
        Assert.False(request.IsCompleted());
    }
}

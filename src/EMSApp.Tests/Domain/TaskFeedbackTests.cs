using EMSApp.Domain;
using EMSApp.Domain.Exceptions;
using Microsoft.Extensions.Primitives;

namespace EMSApp.Tests;

[Trait("Category", "Domain")]
public class TaskFeedbackTests
{
    /// <summary>
    /// Shared test helper
    /// </summary>
    /// <param name="act"></param>
    /// <param name="expectedMessage"></param>
    private static void AssertThrowsTaskFeedbackException(Action act, string expectedMessage)
    {
        var ex = Assert.Throws<DomainException>(act);
        Assert.Contains(expectedMessage, ex.Message);
    }

    /// <summary>
    /// Creates a valid AssignmentFeedback
    /// </summary>
    /// <returns></returns>
    private static AssignmentFeedback CreateValidTaskFeedback()
    {
        return new AssignmentFeedback("task-321", "user-321", "Another Valid Text", FeedbackType.Employee);
    }

    /// <summary>
    /// Valid parameters for AssignmentFeedback
    /// </summary>
    /// <returns></returns>
    private static (string taskId, string userId, string text, DateTime timeStamp, FeedbackType type) GetValidParameters()
        => ("task-123", "user-123", "Valid Text", new DateTime(2025, 4, 25, 12, 0, 0), FeedbackType.Manager);

    public static IEnumerable<object[]> InvalidText =>
    [
        [null, "Feedback text cannot be empty"],
        ["", "Feedback text cannot be empty"],
        ["   ", "Feedback text cannot be empty"],
        [new string('x', 1001), "Feedback text too long"],

    ];

    /// CONSTRUCTOR TESTS

    [Fact]
    public void Constructor_ValidParameters_CreatesTaskFeedback()
    {
        // Arrange
        var valid = GetValidParameters();
        var before = DateTime.UtcNow.AddSeconds(-1);

        // Act
        var feedback = new AssignmentFeedback(valid.taskId, valid.userId, valid.text, valid.type);

        // Assert
        Assert.Equal(valid.taskId, feedback.AssignmentId);
        Assert.Equal(valid.userId, feedback.UserId);
        Assert.Equal(valid.text, feedback.Text);
        Assert.Equal(valid.type, feedback.Type);
        Assert.InRange(feedback.TimeStamp, before, DateTime.UtcNow.AddSeconds(1));
    }

    // AssignmentId validation
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_InvalidTaskId_ThrowsDomainException(string badTaskId)
    {
        // Arrange
        var valid = GetValidParameters();

        // Act & Assert
        var ex = Assert.Throws<DomainException>(() => 
            new AssignmentFeedback(badTaskId, valid.userId, valid.text, valid.type)
        );
        Assert.Contains("AssignmentId Id cannot be empty", ex.Message);
    }

    // UserId validation
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_InvalidUserId_ThrowsDomainException(string badUserId)
    {
        // Arrange
        var valid = GetValidParameters();

        // Act & Assert
        var ex = Assert.Throws<DomainException>(() =>
            new AssignmentFeedback(valid.taskId, badUserId, valid.text, valid.type)
        );
        Assert.Contains("UserId Id cannot be empty", ex.Message);
    }

    // Text validation
    [Theory]
    [MemberData(nameof(InvalidText))]
    public void Constructor_InvalidText_ThrowsDomainException(string badText, string expectedMessage)
    {
        // Arrange
        var valid = GetValidParameters();

        // Act & Assert
        AssertThrowsTaskFeedbackException(() => 
            new AssignmentFeedback(valid.taskId, valid.userId, badText, valid.type),
            expectedMessage
        );
    }

    // FeedbackType validation
    [Theory]
    [InlineData((FeedbackType)999)]
    public void Constructor_InvalidFeedbackType_ThrowsDomainException(FeedbackType badType)
    {
        // Arrange
        var valid = GetValidParameters();

        // Act & Assert
        AssertThrowsTaskFeedbackException(() =>
            new AssignmentFeedback(valid.taskId, valid.userId, valid.text, badType),
            "Feedback type is invalid"
        );
    }

    /// METHODS TESTS

    // Edit - valid
    [Fact]
    public void Edit_ValidText_EditsFeedbackText()
    {
        // Arrange
        var valid = GetValidParameters();
        var feedback = CreateValidTaskFeedback();

        // Act
        Assert.NotEqual(valid.text, feedback.Text);
        feedback.Edit(valid.text);

        // Assert
        Assert.Equal(valid.text, feedback.Text);
    }

    // Edit - invalid
    [Theory]
    [MemberData(nameof(InvalidText))]
    public void Edit_InvalidText_ThrowsDomainException(string badText, string expectedMessage)
    {
        // Arrange
        var feedback = CreateValidTaskFeedback();

        // Act & Assert
        AssertThrowsTaskFeedbackException(() => 
            feedback.Edit(badText),
            expectedMessage
        );
    }
}

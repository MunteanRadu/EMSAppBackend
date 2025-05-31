using EMSApp.Domain;
using EMSApp.Domain.Exceptions;

namespace EMSApp.Tests;

[Trait("Category", "Domain")]
public class BreakSessionTests
{
    /// <summary>
    /// Shared test helper
    /// </summary>
    /// <param name="act"></param>
    /// <param name="expectedMessage"></param>
    private static void AssertThrowsBreakSessionException(Action act, string expectedMessage)
    {
        var ex = Assert.Throws<DomainException>(act);
        Assert.Contains(expectedMessage, ex.Message);
    }

    /// <summary>
    /// Creates a valid BreakSession
    /// </summary>
    /// <returns></returns>
    private static BreakSession CreateValidBreakSession()
    {
        var session = new BreakSession("punch-123", new TimeOnly(12, 0));
        session.End(new TimeOnly(12, 30));
        return session;
    }

    /// <summary>
    /// Generates valid parameters for BreakSession
    /// </summary>
    /// <returns></returns>
    private static (string punchRecordId, TimeOnly start, TimeOnly end, TimeSpan duration) GetValidParameters()
        => ("punch-123", new TimeOnly(12, 0), new TimeOnly(12, 30), new TimeSpan(0, 30, 0));

    public static IEnumerable<object[]> InvalidEndTimes =>
    [
        [default(TimeOnly), "EndTime time must be provided"],
        [new TimeOnly(11, 0), "EndTime time must be after start time"],
    ];

    /// CONSTRUCTOR TESTS

    [Fact]
    public void Constructor_ValidParameters_CreatesBreakSession()
    {
        // Arrange
        var valid = GetValidParameters();

        // Act & PreAssert
        var session = new BreakSession(valid.punchRecordId, valid.start);
        Assert.False(session.IsComplete());
        Assert.Null(session.EndTime);
        Assert.Null(session.Duration);
        session.End(valid.end);

        // Assert
        Assert.Equal(valid.punchRecordId, session.PunchRecordId);
        Assert.Equal(valid.start, session.StartTime);
        Assert.Equal(valid.end, session.EndTime);
        Assert.Equal(valid.duration, session.Duration);
        Assert.True(session.IsComplete());
    }

    // PunchRecordId validation
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_InvalidPunchRecordId_ThrowsDomainException(string badPunchId)
    {
        // Arrange
        var valid = GetValidParameters();

        // Act & Assert
        var ex = Assert.Throws<DomainException>(() => 
            new BreakSession(badPunchId, valid.start)
        );
        Assert.Contains("Punch Record Id cannot be empty", ex.Message);
    }

    // StartTime time validation
    [Theory]
    [InlineData(default, "StartTime time must be provided")]
    public void Constructor_InvalidStartTime_ThrowsDomainException(TimeOnly badTime, string expectedMessage)
    {
        // Arrange
        var valid = GetValidParameters();

        // Act & Assert
        AssertThrowsBreakSessionException(() => 
            new BreakSession(valid.punchRecordId, badTime),
            expectedMessage
        );
    }

    /// METHODS TESTS

    // End - valid
    [Fact]
    public void EndBreak_ValidData_EndsBreak()
    {
        // Arrange
        var valid = GetValidParameters();
        var session = new BreakSession(valid.punchRecordId, valid.start);

        // Act
        session.End(valid.end);

        // Assert
        Assert.Equal(valid.end, session.EndTime);
        Assert.Equal(valid.duration, session.Duration);
        Assert.True(session.IsComplete());
    }

    // End - break already ended
    [Fact]
    public void EndBreak_BreakAlreadyEnded_ThrowsDomainException()
    {
        // Arrange
        var valid = GetValidParameters();
        var session = new BreakSession(valid.punchRecordId, valid.start);
        session.End(valid.end);

        // Act & Assert
        var ex = Assert.Throws<DomainException>(() =>
            session.End(valid.end)
        );
        Assert.Contains("Break has already ended", ex.Message);
    }

    // End - invalid
    [Theory]
    [MemberData(nameof(InvalidEndTimes))]
    public void EndBreak_EndBeforeStart_ThrowsException(TimeOnly badTime, string expectedMessage)
    {
        // Arrange
        var valid = GetValidParameters();
        var session = new BreakSession(valid.punchRecordId, valid.start);

        // Act & Assert
        AssertThrowsBreakSessionException(() => 
            session.End(badTime),
            expectedMessage
        );
    }
}

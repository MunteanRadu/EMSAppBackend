using EMSApp.Domain;
using EMSApp.Domain.Exceptions;

namespace EMSApp.Tests;

[Trait("Category", "Domain")]
public class PunchRecordTests
{
    /// <summary>
    /// Shared test helper
    /// </summary>
    /// <param name="act"></param>
    /// <param name="expectedMessage"></param>
    private static void AssertThrowsPunchRecordException(Action act, string expectedMessage)
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
    /// Generates valid parameters for PunchRecord
    /// </summary>
    /// <returns></returns>
    private static (string UserId, DateOnly Date, TimeOnly TimeIn, TimeOnly TimeOut, TimeSpan TotalHours) GetValidParams()
        => ("user-123", new DateOnly(2025, 4, 25), new TimeOnly(8, 0), new TimeOnly(16, 0), new TimeSpan(8, 0, 0));

    /// <summary>
    /// Values for DateOnly testing
    /// </summary>
    public static IEnumerable<object[]> InvalidDates =>
    [
        [default(DateOnly), "Date must be provided"],
    ];
    
    /// <summary>
    /// Values for TimeOnlySettings
    /// </summary>
    public static IEnumerable<object[]> InvalidPunchOutTimes =>
    [
        [default(TimeOnly), "Punch-out time must be provided"],
        [new TimeOnly(7, 0), "Punch-out time must be after punch-in time"],
    ];

    /// CONSTRUCTOR TESTS

    [Fact]
    public void Constructor_ValidParameters_CreatesPunchRecord()
    {
        // Arrange
        var valid = GetValidParams();
        var sessions = new List<BreakSession>();
        var session = CreateValidBreakSession();
        sessions.Add(session);

        // Act
        var punchRecord = new PunchRecord(valid.UserId, valid.Date, valid.TimeIn);
        punchRecord.AddBreakSession(session);
        punchRecord.PunchOut(valid.TimeOut);

        // Assert
        Assert.Equal(valid.UserId, punchRecord.UserId);
        Assert.Equal(valid.Date, punchRecord.Date);
        Assert.Equal(valid.TimeIn, punchRecord.TimeIn);
        Assert.Equal(valid.TimeOut, punchRecord.TimeOut);
        Assert.Equal(valid.TotalHours, punchRecord.TotalHours);
        Assert.Equal(sessions, punchRecord.BreakSessions);
        Assert.Equal(session.Duration, punchRecord.GetTotalBreakDuration());
        Assert.True(punchRecord.IsComplete());
    }

    [Fact]
    public void Constructor_NoBreaks_BreakSessionsIsEmpty()
    {
        // Arrange
        var valid = GetValidParams();
        
        // Act
        var pr = new PunchRecord(valid.UserId, valid.Date, valid.TimeIn);

        // Assert
        Assert.Empty(pr.BreakSessions);
        Assert.Equal(TimeSpan.Zero, pr.GetTotalBreakDuration());
    }


    // UserId validation
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_InvalidUser_ThrowsDomainException(string badUserId)
    {
        // Arrange
        var valid = GetValidParams();

        // Act & Assert
        var ex = Assert.Throws<DomainException>(() =>
            new PunchRecord(badUserId, valid.Date, valid.TimeIn)
        );
        Assert.Contains("UserId Id cannot be empty", ex.Message);
    }

    // Date validation
    [Theory]
    [MemberData(nameof(InvalidDates))]
    public void Constructor_InvalidDate_ThrowsDomainException(DateOnly badDate, string expectedMessage)
    {
        // Arrange
        var valid = GetValidParams();

        // Act & Assert
        AssertThrowsPunchRecordException(() =>
            new PunchRecord(valid.UserId, badDate, valid.TimeIn),
            expectedMessage
        );
    }

    // TimeIn validation
    [Theory]
    [InlineData(default, "Punch-in time must be provided")]
    public void Constructor_InvalidTimeIn_ThrowsDomainException(TimeOnly badTime, string expectedMessage)
    {
        // Arrange
        var valid = GetValidParams();

        // Act & Assert
        AssertThrowsPunchRecordException(() => 
            new PunchRecord(valid.UserId, valid.Date, badTime),
            expectedMessage
        );
    }

    /// METHODS TESTS

    // PunchOut - valid
    [Fact]
    public void PunchOut_ValidTimeOut_PunchesOut()
    {
        // Arrange
        var valid = GetValidParams();
        var punchRecord = new PunchRecord(valid.UserId, valid.Date, valid.TimeIn);

        // Act
        punchRecord.PunchOut(valid.TimeOut);

        // Assert
        Assert.Equal(valid.TimeOut, punchRecord.TimeOut);
        Assert.Equal(valid.TotalHours, punchRecord.TotalHours);
        Assert.True(punchRecord.IsComplete());
        var ex = Assert.Throws<DomainException>(() =>
            punchRecord.PunchOut(valid.TimeOut)
        );
        Assert.Contains("Already punched out", ex.Message);
    }

    // PunchOut - invalid
    [Theory]
    [MemberData(nameof(InvalidPunchOutTimes))]
    public void PunchOut_InvalidTimeOut_ThrowsDomainException(TimeOnly badTime, string expectedMessage)
    {
        // Arrange
        var valid = GetValidParams();
        var punchRecord = new PunchRecord(valid.UserId, valid.Date, valid.TimeIn);

        // Act & Assert
        AssertThrowsPunchRecordException(() =>
            punchRecord.PunchOut(badTime),
            expectedMessage
        );
    }

    // AddBreakSession - valid
    [Fact]
    public void AddBreakSession_ValidData_AddsBreakSession()
    {
        // Arrange
        var session = CreateValidBreakSession();
        var sessions = new List<BreakSession>();
        sessions.Add(session);
        var valid = GetValidParams();
        var punchRecord = new PunchRecord(valid.UserId, valid.Date, valid.TimeIn);

        // Act
        punchRecord.AddBreakSession(session);

        // Assert
        Assert.Equal(sessions, punchRecord.BreakSessions);
    }

    // AddBreakSession - null
    [Fact]
    public void AddBreakSession_NullSession_ThrowsDomainException()
    {
        // Arrange
        var valid = GetValidParams();
        var punchRecord = new PunchRecord(valid.UserId, valid.Date, valid.TimeIn);

        // Act & Assert
        var ex = Assert.Throws<DomainException>(() => 
            punchRecord.AddBreakSession(null)
        );
        Assert.Contains("Break session cannot be null", ex.Message);
    }

    // AddBreakSession - session ended already
    [Fact]
    public void AddBreakSession_SessionEnded_ThrowsDomainException()
    {
        // Arrange
        var valid = GetValidParams();
        var punchRecord = new PunchRecord(valid.UserId, valid.Date, valid.TimeIn);
        var invalidSession = new BreakSession(punchRecord.Id, valid.TimeIn.AddHours(1));

        // Act & Assert
        var ex = Assert.Throws<DomainException>(() =>
            punchRecord.AddBreakSession(invalidSession)
        );
        Assert.Contains("Break session must be finished", ex.Message);
    }

    // AddBreakSession - session not after punch-in
    [Fact]
    public void AddBreakSession_InvalidSession_ThrowsDomainException()
    {
        // Arrange
        var valid = GetValidParams();
        var punchRecord = new PunchRecord(valid.UserId, valid.Date, valid.TimeIn);
        var invalidSession = new BreakSession("punch-123", new TimeOnly(7, 0));
        invalidSession.End(new TimeOnly(8, 0));

        // Act & Assert
        var ex = Assert.Throws<DomainException>(() => 
            punchRecord.AddBreakSession(invalidSession)
        );
        Assert.Contains("Break session must after punch-in time", ex.Message);
    }

    // AddBreakSession - null
    [Fact]
    public void AddBreakSession_AfterPunchOut_ThrowsDomainException()
    {
        // Arrange
        var valid = GetValidParams();
        var punchRecord = new PunchRecord(valid.UserId, valid.Date, valid.TimeIn);
        punchRecord.PunchOut(valid.TimeOut);
        var session = CreateValidBreakSession();

        // Act & Assert
        var ex = Assert.Throws<DomainException>(() =>
            punchRecord.AddBreakSession(session)
        );
        Assert.Contains("Cannot add break session after punch-out", ex.Message);
    }

    // GetWorkedTime
    [Fact]
    public void GetWorkedTime_ValidData_ReturnsWorkedTime()
    {
        // Arrange
        var session = CreateValidBreakSession();
        var sessions = new List<BreakSession>();
        sessions.Add(session);
        var valid = GetValidParams();
        var punchRecord = new PunchRecord(valid.UserId, valid.Date, valid.TimeIn);
        punchRecord.AddBreakSession(session);
        
        var validWorkedTime = new TimeSpan(7, 30, 0);

        // Act & Assert
        Assert.Equal(null, punchRecord.GetWorkedTime());
        punchRecord.PunchOut(valid.TimeOut);
        Assert.Equal(validWorkedTime, punchRecord.GetWorkedTime());
    }

    // GetTotalBreakDuration - no breaks
    [Fact]
    public void GetTotalBreakDuration_NoBreaks_ReturnsZero()
    {
        var valid = GetValidParams();
        var pr = new PunchRecord(valid.UserId, valid.Date, valid.TimeIn);
        Assert.Equal(TimeSpan.Zero, pr.GetTotalBreakDuration());
    }

    // IsComplete - before punch-out
    [Fact]
    public void IsComplete_BeforePunchOut_ReturnsFalse()
    {
        var valid = GetValidParams();
        var pr = new PunchRecord(valid.UserId, valid.Date, valid.TimeIn);
        Assert.False(pr.IsComplete());
    }

}

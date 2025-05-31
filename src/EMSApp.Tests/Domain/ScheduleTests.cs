using EMSApp.Domain;
using EMSApp.Domain.Exceptions;
using System.Diagnostics.Metrics;

namespace EMSApp.Tests;

[Trait("Category", "Domain")]
public class ScheduleTests
{
    /// <summary>
    /// Shared test helper
    /// </summary>
    /// <param name="act"></param>
    /// <param name="expectedMessage"></param>
    private static void AssertThrowsScheduleException(Action act, string expectedMessage)
    {
        var ex = Assert.Throws<DomainException>(act);
        Assert.Contains(expectedMessage, ex.Message);
    }

    /// <summary>
    /// Creates a valid Schedule
    /// </summary>
    /// <returns></returns>
    private static Schedule CreateValidSchedule()
    {
        return new Schedule(
            "department-321",
            "manager-321",
            DayOfWeek.Thursday,
            TimeOnly.Parse("08:00"),
            TimeOnly.Parse("16:00"),
            true
            );
    }

    /// <summary>
    /// Generates a valid HashSet<DateOnly>
    /// </summary>
    /// <returns></returns>
    private static HashSet<DateOnly> GetValidExceptionList()
    {
        return new HashSet<DateOnly>(
            [
                new DateOnly(2025, 1, 1),
                new DateOnly(2025, 1, 2),
                new DateOnly(2025, 1, 24),
                new DateOnly(2025, 5, 1),
                new DateOnly(2025, 12, 1),
            ]);
    }
    
    /// <summary>
    /// Generates valid parameters for Schedule
    /// </summary>
    /// <returns></returns>
    public static (
        string departmentId,
        string managerId,
        DayOfWeek day,
        TimeOnly startTime,
        TimeOnly endTime,
        bool isWorkingDay,
        HashSet<DateOnly> _exceptions) GetValidParameters()
        => (
        "department-123",
        "manager-123",
        DayOfWeek.Wednesday,
        TimeOnly.Parse("07:00"),
        TimeOnly.Parse("15:00"),
        true,
        GetValidExceptionList());

    public static IEnumerable<object[]> InvalidEndTimes() =>
    [
        [default(TimeOnly), "End time must be provided"],
        [TimeOnly.Parse("05:00"), "End time must be after start time"]
    ];

    /// CONSTRUCTOR TESTS

    [Fact]
    public void Constructor_ValidParameters_CreatesSchedule()
    {
        // Arrange
        var valid = GetValidParameters();

        // Act
        var Schedule = new Schedule(valid.departmentId, valid.managerId, valid.day, valid.startTime, valid.endTime, valid.isWorkingDay);

        // Assert
        Assert.Equal(valid.departmentId, Schedule.DepartmentId);
        Assert.Equal(valid.managerId, Schedule.ManagerId);
        Assert.Equal(valid.day, Schedule.Day);
        Assert.Equal(valid.startTime, Schedule.StartTime);
        Assert.Equal(valid.endTime, Schedule.EndTime);
        Assert.Equal(valid.isWorkingDay, Schedule.IsWorkingDay);
    }

    // DepartmentId validation
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void Constructor_InvalidDepartmentId_ThrowsDomainException(string badDepartmentId)
    {
        // Arrange
        var valid = GetValidParameters();

        // Act & Assert
        AssertThrowsScheduleException(() =>
            new Schedule(badDepartmentId, valid.managerId, valid.day, valid.startTime, valid.endTime, valid.isWorkingDay),
            "Department Id cannot be empty"
        );
    }

    // ManagerId validation
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void Constructor_InvalidManagerId_ThrowsDomainException(string badManagerId)
    {
        // Arrange
        var valid = GetValidParameters();

        // Act & Assert
        AssertThrowsScheduleException(() => 
            new Schedule(valid.departmentId, badManagerId, valid.day, valid.startTime, valid.endTime, valid.isWorkingDay),
            "Manager Id cannot be empty"
        );
    }

    // Day validation
    [Fact]
    public void Constructor_InvalidDay_ThrowsDomainException()
    {
        // Arrange
        var valid = GetValidParameters();

        // Act & Assert
        AssertThrowsScheduleException(() =>
            new Schedule(valid.departmentId, valid.managerId, (DayOfWeek)999, valid.startTime, valid.endTime, valid.isWorkingDay),
            "Invalid day of week"
        );
    }

    // StartTime validation
    [Fact]
    public void Constructor_InvalidStartTime_ThrowsDomainException()
    {
        // Arrange
        var valid = GetValidParameters();

        // Act & Assert
        AssertThrowsScheduleException(() =>
            new Schedule(valid.departmentId, valid.managerId, valid.day, default, valid.endTime, valid.isWorkingDay),
            "Start time must be provided"
        );
    }

    // EndTime validation
    [Theory]
    [MemberData(nameof(InvalidEndTimes))]
    public void Constructor_InvalidEndTime_ThrowsDomainException(TimeOnly badEndTime, string expectedMessage)
    {
        // Arrange
        var valid = GetValidParameters();

        // Act & Assert
        AssertThrowsScheduleException(() =>
            new Schedule(valid.departmentId, valid.managerId, valid.day, valid.startTime, badEndTime, valid.isWorkingDay),
            expectedMessage
        );
    }

    /// METHODS TESTS

    // IsWithinShift - valid
    [Fact]
    public void IsWithinShift_Valid_ReturnsTrue()
    {
        // Arrange
        var schedule = CreateValidSchedule();
        var valid = GetValidParameters();
        var date = new DateOnly(2025, 4, 24);
        var time = valid.startTime.AddHours(1);

        // Act & Assert
        Assert.True(schedule.IsWithinShift(date, time));
    }

    // IsWithinShift - wrong day
    [Fact]
    public void IsWithinShift_WrongDay_ReturnsFalse()
    {
        // Arrange
        var schedule = CreateValidSchedule();
        var valid = GetValidParameters();
        var date = new DateOnly(2025, 4, 25);
        var time = valid.startTime.AddHours(1);

        // Act & Assert
        Assert.False(schedule.IsWithinShift(date, time));
    }

    // IsWithinShift - not working
    [Fact]
    public void IsWithinShift_NotWorkingDay_ReturnsFalse()
    {
        // Arrange
        var valid = GetValidParameters();
        var schedule = new Schedule(valid.departmentId, valid.managerId, valid.day, valid.startTime, valid.endTime, false);
        var date = new DateOnly(2025, 4, 26);
        var time = valid.startTime.AddHours(1);

        // Act & Assert
        Assert.False(schedule.IsWithinShift(date, time));
    }

    // IsWithinShift - with exception
    [Fact]
    public void IsWithinShift_WithException_ReturnsFalse()
    {
        // Arrange
        var schedule = CreateValidSchedule();
        var valid = GetValidParameters();
        var date = new DateOnly(2025, 4, 26);
        schedule.AddException(date);
        var time = valid.startTime.AddHours(1);

        // Act & Assert
        Assert.False(schedule.IsWithinShift(date, time));
    }

    // NextOccurences - valid
    [Fact]
    public void NextOccurences_ReturnsCorrectDates()
    {
        // Arrange
        var valid = GetValidParameters();
        var schedule = new Schedule(valid.departmentId, valid.managerId, DayOfWeek.Monday, valid.startTime, valid.endTime, valid.isWorkingDay);
        var date = new DateOnly(2025, 4, 1);
        
        // Act
        var next = schedule.NextOccurrences(date, 3).ToList();

        // Assert
        Assert.Equal(new DateOnly(2025, 4, 7), next[0]);
        Assert.Equal(new DateOnly(2025, 4, 14), next[1]);
        Assert.Equal(new DateOnly(2025, 4, 21), next[2]);
    }

    // NextOccurences - zero or negative count
    [Fact]
    public void NextOccurences_ZeroOrNegativeCount_Empty()
    {
        // Arrange
        var valid = GetValidParameters();
        var schedule = new Schedule(valid.departmentId, valid.managerId, DayOfWeek.Monday, valid.startTime, valid.endTime, valid.isWorkingDay);
        var date = new DateOnly(2025, 4, 1);

        // Act & Assert
        Assert.Empty(schedule.NextOccurrences(date, 0));
        Assert.Empty(schedule.NextOccurrences(date, -1));
    }

    // NextOccurences - with exceptions
    [Fact]
    public void NextOccurences_WithExceptions_Skips()
    {
        // Arrange
        var valid = GetValidParameters();
        var schedule = new Schedule(valid.departmentId, valid.managerId, DayOfWeek.Wednesday, valid.startTime, valid.endTime, valid.isWorkingDay);
        var date = new DateOnly(2025, 4, 21);
        var exceptionDate = new DateOnly(2025, 4, 23);
        schedule.AddException(exceptionDate);

        // Act
        var next = schedule.NextOccurrences(date, 2).ToList();

        // Assert
        Assert.Equal(new DateOnly(2025, 4, 30), next[0]);
        Assert.Equal(new DateOnly(2025, 5, 7), next[1]);
    }

    // UpdateShift - valid
    [Fact]
    public void UpdateShift_Valid_UpdatesShift()
    {
        // Arrange
        var schedule = CreateValidSchedule();
        var newStart = new TimeOnly(10, 0);
        var newEnd = new TimeOnly(18, 0);

        // Act
        schedule.UpdateShift(newStart, newEnd, false);

        // Assert
        Assert.Equal(newStart, schedule.StartTime);
        Assert.Equal(newEnd, schedule.EndTime);
        Assert.False(schedule.IsWorkingDay);
    }

    // UpdateShift - invalid
    [Fact]
    public void UpdateShift_InvalidEnd_ThrowsDomainException()
    {
        // Arrange
        var schedule = CreateValidSchedule();

        // Act & Assert
        AssertThrowsScheduleException(() =>
            schedule.UpdateShift(schedule.EndTime, schedule.StartTime, true),
            "End time must be after start time"
        );
    }

    // AddException - already exists
    [Fact]
    public void AddException_AlreadyExists_ThrowsDomainException()
    {
        // Arrange
        var valid = GetValidParameters();
        var schedule = CreateValidSchedule();
        var date = new DateOnly(2025, 4, 26);
        schedule.AddException(date);

        // Act & Assert
        AssertThrowsScheduleException(() =>
            schedule.AddException(date),
            $"Already excepted {date}"
        );
    }

    // RemoveException - valid
    [Fact]
    public void RemoveException_Vaid_RemovesException()
    {
        // Arrange
        var schedule = CreateValidSchedule();
        var date = new DateOnly(2025, 4, 26);
        schedule.AddException(date);

        // Act
        schedule.RemoveException(date);

        // Assert
        Assert.Empty(schedule.Exceptions);
    }

    // RemoveException - doesn't exist
    [Fact]
    public void RemoveException_NotExists_ThrowsDomainException()
    {
        // Arrange
        var schedule = CreateValidSchedule();
        var date = new DateOnly(2025, 4, 26);

        // Act & Assert
        AssertThrowsScheduleException(() =>
            schedule.RemoveException(date),
            $"No exception found for {date}"
        );
    }
}

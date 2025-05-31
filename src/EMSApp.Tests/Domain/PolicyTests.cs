using EMSApp.Domain;
using EMSApp.Domain.Exceptions;

namespace EMSApp.Tests;

[Trait("Category", "Domain")]
public class PolicyTests
{
    /// <summary>
    /// Shared test helper
    /// </summary>
    /// <param name="act"></param>
    /// <param name="expectedMessage"></param>
    private static void AssertThrowsPolicyException(Action act, string expectedMessage)
    {
        var ex = Assert.Throws<DomainException>(act);
        Assert.Contains(expectedMessage, ex.Message);
    }

    /// <summary>
    /// Generates valid parameters for Policy
    /// </summary>
    /// <returns></returns>
    private static (
        int year,
        TimeOnly workDayStart,
        TimeOnly workDayEnd,
        TimeSpan punchInTolerance,
        TimeSpan punchOutTolerance,
        TimeSpan maxSingleBreak,
        TimeSpan MaxTotalBreakPerDay,
        decimal overtimeMultiplier,
        IDictionary<LeaveType, int> leaveQuotas
        )
        GetValidParameters()
        => (2025, new TimeOnly(9, 0), new TimeOnly(17, 0), new TimeSpan(0, 15, 0), new TimeSpan(0, 10, 0), new TimeSpan(0, 30, 0), new TimeSpan(2, 0, 0), 1.5m, GetValidQuotas());

    private static Dictionary<LeaveType, int> GetValidQuotas()
        => Enum.GetValues<LeaveType>()
        .Cast<LeaveType>()
        .ToDictionary(lt => lt, _ => 10);

    public static IEnumerable<object[]> InvalidWorkEndDays() => 
    [
        [default(TimeOnly), "Work day end must be provided"],
        [new TimeOnly(8, 0), "Work day end must be after work day start"],
    ];

    public static IEnumerable<object[]> InvalidPunchTolerances() =>
    [
        [default(TimeSpan), default(TimeSpan), "Punch tolerances must be provided"],
        [new TimeSpan(-1), new TimeSpan(-1), "Punch tolerances must be non-negative"],
    ];

    public static IEnumerable<object[]> InvalidMaxSingleBreaks() =>
    [   
        [default(TimeSpan), "Max single break must be provided"],
        [new TimeSpan(-1), "Max single break must be non-negative"],
    ];
    
    public static IEnumerable<object[]> InvalidMaxTotalBreakPerDays() =>
    [   
        [default(TimeSpan), "Total breaks per day limit must be provided"],
        [new TimeSpan(0, 29, 0), "Total breaks per day must be >= single break limit"],
    ];

    public static IEnumerable<object[]> InvalidBreakRules() =>
    [
        [default(TimeSpan), default(TimeSpan), "Break rules must be provided"],
        [new TimeSpan(-1), new TimeSpan(-2), "Invalid break rules"],
    ];

    ///CONSTRUCTOR TESTS

    [Fact]
    public void Constructor_ValidParameters_CreatesPolicy()
    {
        // Arrange
        var valid = GetValidParameters();

        // Act
        var policy = new Policy(
            valid.year,
            valid.workDayStart,
            valid.workDayEnd,
            valid.punchInTolerance,
            valid.punchOutTolerance,
            valid.maxSingleBreak,
            valid.MaxTotalBreakPerDay,
            valid.overtimeMultiplier,
            valid.leaveQuotas);

        // Assert
        Assert.Equal(valid.year, policy.Year);
        Assert.Equal(valid.workDayStart, policy.WorkDayStart);
        Assert.Equal(valid.workDayEnd, policy.WorkDayEnd);
        Assert.Equal(valid.punchInTolerance, policy.PunchInTolerance);
        Assert.Equal(valid.punchOutTolerance, policy.PunchOutTolerance);
        Assert.Equal(valid.maxSingleBreak, policy.MaxSingleBreak);
        Assert.Equal(valid.MaxTotalBreakPerDay, policy.MaxTotalBreakPerDay);
        Assert.Equal(valid.overtimeMultiplier, policy.OvertimeMultiplier);
        foreach (var lt in valid.leaveQuotas.Keys)
            Assert.Equal(10, policy.GetLeaveQuota(lt));
    }

    // Year validation
    [Theory]
    [InlineData(1999)]
    [InlineData(3001)]
    public void Constructor_InvalidYear_ThrowsDomainException(int badYear)
    {
        // Arrange
        var valid = GetValidParameters();
        var expectedMessage = "Year must be between 2000 and 3000";

        // Act & Assert
        AssertThrowsPolicyException(() =>
            new Policy(
                badYear,
                valid.workDayStart,
                valid.workDayEnd,
                valid.punchInTolerance,
                valid.punchOutTolerance,
                valid.maxSingleBreak,
                valid.MaxTotalBreakPerDay,
                valid.overtimeMultiplier,
                valid.leaveQuotas),
            expectedMessage
        );
    }

    // WorkStartDay validation
    [Theory]
    [InlineData(default)]
    public void Constructor_InvalidWorkStartDay_ThrowsDomainException(TimeOnly badWorkStartDay)
    {
        // Arrange
        var valid = GetValidParameters();
        var expectedMessage = "Work day start must be provided";

        // Act & Assert
        AssertThrowsPolicyException(() =>
            new Policy(
                valid.year,
                badWorkStartDay,
                valid.workDayEnd,
                valid.punchInTolerance,
                valid.punchOutTolerance,
                valid.maxSingleBreak,
                valid.MaxTotalBreakPerDay,
                valid.overtimeMultiplier,
                valid.leaveQuotas),
            expectedMessage
        );
    }

    // WorkEndDay validation
    [Theory]
    [MemberData(nameof(InvalidWorkEndDays))]
    public void Constructor_InvalidWorkEndDay_ThrowsDomainException(TimeOnly badWorkEndDay, string expectedMessage)
    {
        // Arrange
        var valid = GetValidParameters();

        // Act & Assert
        AssertThrowsPolicyException(() =>
            new Policy(
                valid.year,
                valid.workDayStart,
                badWorkEndDay,
                valid.punchInTolerance,
                valid.punchOutTolerance,
                valid.maxSingleBreak,
                valid.MaxTotalBreakPerDay,
                valid.overtimeMultiplier,
                valid.leaveQuotas),
            expectedMessage
        );
    }

    // Punch Tolerances validation
    [Theory]
    [MemberData(nameof(InvalidPunchTolerances))]
    public void Constructor_InvalidPunchTolerances_ThrowsDomainException(TimeSpan badPunchInTolerance, TimeSpan badPunchOutTolerance, string expectedMessage)
    {
        // Arrange
        var valid = GetValidParameters();

        // Act & Assert
        AssertThrowsPolicyException(() =>
            new Policy(
                valid.year,
                valid.workDayStart,
                valid.workDayEnd,
                badPunchInTolerance,
                valid.punchOutTolerance,
                valid.maxSingleBreak,
                valid.MaxTotalBreakPerDay,
                valid.overtimeMultiplier,
                valid.leaveQuotas),
            expectedMessage
        );
        AssertThrowsPolicyException(() =>
            new Policy(
                valid.year,
                valid.workDayStart,
                valid.workDayEnd,
                valid.punchInTolerance,
                badPunchOutTolerance,
                valid.maxSingleBreak,
                valid.MaxTotalBreakPerDay,
                valid.overtimeMultiplier,
                valid.leaveQuotas),
            expectedMessage
        );
    }

    // MaxSingleBreak validation
    [Theory]
    [MemberData(nameof(InvalidMaxSingleBreaks))]
    public void Constructor_InvalidMaxSingleBreak_ThrowsDomainException(TimeSpan badMaxSingleBreak, string expectedMessage)
    {
        // Arrange
        var valid = GetValidParameters();

        // Act & Assert
        AssertThrowsPolicyException(() =>
            new Policy(
                valid.year,
                valid.workDayStart,
                valid.workDayEnd,
                valid.punchInTolerance,
                valid.punchOutTolerance,
                badMaxSingleBreak,
                valid.MaxTotalBreakPerDay,
                valid.overtimeMultiplier,
                valid.leaveQuotas),
            expectedMessage
        );
    }

    // MaxTotalBreakPerDay validation
    [Theory]
    [MemberData(nameof(InvalidMaxTotalBreakPerDays))]
    public void Constructor_InvalidMaxTotalBreakPerDay_ThrowsDomainException(TimeSpan badMaxTotalBreakPerDay, string expectedMessage)
    {
        // Arrange
        var valid = GetValidParameters();

        // Act & Assert
        AssertThrowsPolicyException(() =>
            new Policy(
                valid.year,
                valid.workDayStart,
                valid.workDayEnd,
                valid.punchInTolerance,
                valid.punchOutTolerance,
                valid.maxSingleBreak,
                badMaxTotalBreakPerDay,
                valid.overtimeMultiplier,
                valid.leaveQuotas),
            expectedMessage
        );
    }

    // OvertimeMultiplier validation
    [Theory]
    [InlineData(null)]
    [InlineData(0.5)]
    public void Constructor_InvalidOvertimeMultiplier(decimal badOvertimeMultiplier)
    {
        // Arrange
        var valid = GetValidParameters();
        var expectedMessage = "Overtime multiplier must be >= 1.0";

        // Act & Assert
        AssertThrowsPolicyException(() =>
            new Policy(
                valid.year,
                valid.workDayStart,
                valid.workDayEnd,
                valid.punchInTolerance,
                valid.punchOutTolerance,
                valid.maxSingleBreak,
                valid.MaxTotalBreakPerDay,
                badOvertimeMultiplier,
                valid.leaveQuotas),
            expectedMessage
        );
    }

    [Fact]
    public void Constructor_MissingLeaveQuota_ThrowsDomainException()
    {
        // Arrange
        var valid = GetValidParameters();
        valid.leaveQuotas.Remove(LeaveType.Sick);
        AssertThrowsPolicyException(() =>
            new Policy(
                valid.year,
                valid.workDayStart,
                valid.workDayEnd,
                valid.punchInTolerance,
                valid.punchOutTolerance,
                valid.maxSingleBreak,
                valid.MaxTotalBreakPerDay,
                valid.overtimeMultiplier,
                valid.leaveQuotas),
            $"Missing leave quota for {LeaveType.Sick}"
        );
    }

    /// METHODS TESTS

    // IsValidPunchIn
    [Theory]
    [InlineData("08:45", true)]
    [InlineData("08:40", false)]
    [InlineData("9:15", true)]
    [InlineData("9:20", false)]
    public void IsValidPunchIn_BehavesCorrectly(string time, bool expected)
    {
        // Arrange
        var valid = GetValidParameters();
        var policy = new Policy(
                valid.year,
                valid.workDayStart,
                valid.workDayEnd,
                valid.punchInTolerance,
                valid.punchOutTolerance,
                valid.maxSingleBreak,
                valid.MaxTotalBreakPerDay,
                valid.overtimeMultiplier,
                valid.leaveQuotas);
        var t = TimeOnly.Parse(time);

        // Act & Assert
        Assert.Equal(expected, policy.IsValidPunchIn(t));
    }

    // IsValidPunchOut
    [Theory]
    [InlineData("16:50", true)]
    [InlineData("16:45", false)]
    [InlineData("17:10", true)]
    [InlineData("17:15", false)]
    public void IsValidPunchOut_BehavesCorrectly(string time, bool expected)
    {
        // Arrange
        var valid = GetValidParameters();
        var policy = new Policy(
                valid.year,
                valid.workDayStart,
                valid.workDayEnd,
                valid.punchInTolerance,
                valid.punchOutTolerance,
                valid.maxSingleBreak,
                valid.MaxTotalBreakPerDay,
                valid.overtimeMultiplier,
                valid.leaveQuotas);
        var t = TimeOnly.Parse(time);

        // Act & Assert
        Assert.Equal(expected, policy.IsValidPunchOut(t));
    }

    // SetLeaveQuota - valid
    [Fact]
    public void SetLeaveQuota_Valid_UpdatesQuota()
    {
        // Arrange
        var valid = GetValidParameters();
        var policy = new Policy(
                valid.year,
                valid.workDayStart,
                valid.workDayEnd,
                valid.punchInTolerance,
                valid.punchOutTolerance,
                valid.maxSingleBreak,
                valid.MaxTotalBreakPerDay,
                valid.overtimeMultiplier,
                valid.leaveQuotas);

        // Act
        policy.SetLeaveQuota(LeaveType.Annual, 20);

        // Assert
        Assert.Equal(20, policy.GetLeaveQuota(LeaveType.Annual));
    }

    // SetLeaveQuota - negative
    [Fact]
    public void SetLeaveQuota_Negative_ThrowsDomainException()
    {
        // Arrange
        var valid = GetValidParameters();
        var policy = new Policy(
                valid.year,
                valid.workDayStart,
                valid.workDayEnd,
                valid.punchInTolerance,
                valid.punchOutTolerance,
                valid.maxSingleBreak,
                valid.MaxTotalBreakPerDay,
                valid.overtimeMultiplier,
                valid.leaveQuotas);

        // Act & Assert
        AssertThrowsPolicyException(() => 
            policy.SetLeaveQuota(LeaveType.Annual, -1),
            "Leave quota cannot be negative"
        );
    }

    // SetWorkingHours - valid
    [Fact]
    public void SetWorkingHours_ValidHours_UpdatesWorkingHours()
    {
        // Arrange
        var valid = GetValidParameters();
        var policy = new Policy(
                valid.year,
                valid.workDayStart,
                valid.workDayEnd,
                valid.punchInTolerance,
                valid.punchOutTolerance,
                valid.maxSingleBreak,
                valid.MaxTotalBreakPerDay,
                valid.overtimeMultiplier,
                valid.leaveQuotas);
        var t1 = TimeOnly.Parse("07:00");
        var t2 = TimeOnly.Parse("16:00");

        // Act
        policy.SetWorkingHours(t1, t2);

        // Assert
        Assert.Equal(t1, policy.WorkDayStart);
        Assert.Equal(t2, policy.WorkDayEnd);
    }

    // SetWorkingHours - invalid
    [Fact]
    public void SetWorkingHours_InvalidHours_ThrowsDomainException()
    {
        // Arrange
        var valid = GetValidParameters();
        var policy = new Policy(
                valid.year,
                valid.workDayStart,
                valid.workDayEnd,
                valid.punchInTolerance,
                valid.punchOutTolerance,
                valid.maxSingleBreak,
                valid.MaxTotalBreakPerDay,
                valid.overtimeMultiplier,
                valid.leaveQuotas);
        var t1 = TimeOnly.Parse("07:00");
        var t2 = TimeOnly.Parse("16:00");

        // Act & Assert
        AssertThrowsPolicyException(() =>
            policy.SetWorkingHours(t2, t1),
            "End time must be after start time"
        );
        

    }
    // SetPunchTolerances - valid
    [Fact]
    public void SetPunchTolerances_ValidData_UpdatesPunchTolerances()
    {
        // Arrange
        var valid = GetValidParameters();
        var policy = new Policy(
                valid.year,
                valid.workDayStart,
                valid.workDayEnd,
                valid.punchInTolerance,
                valid.punchOutTolerance,
                valid.maxSingleBreak,
                valid.MaxTotalBreakPerDay,
                valid.overtimeMultiplier,
                valid.leaveQuotas);

        var p1 = TimeSpan.Parse("20:00");
        var p2 = TimeSpan.Parse("15:00");

        // Act
        policy.SetPunchTolerances(p1, p2);

        // Assert
        Assert.Equal(p1, policy.PunchInTolerance);
        Assert.Equal(p2, policy.PunchOutTolerance);
    }

    // SetPunchTolerances - invalid
    [Theory]
    [MemberData(nameof(InvalidPunchTolerances))]
    public void SetPunchTolerances_InvalidData_ThrowsDomainException(TimeSpan punchInTolerance, TimeSpan punchOutTolerance, string expectedMessage)
    {
        // Arrange
        var valid = GetValidParameters();
        var policy = new Policy(
                valid.year,
                valid.workDayStart,
                valid.workDayEnd,
                valid.punchInTolerance,
                valid.punchOutTolerance,
                valid.maxSingleBreak,
                valid.MaxTotalBreakPerDay,
                valid.overtimeMultiplier,
                valid.leaveQuotas);

        // Act & Assert
        AssertThrowsPolicyException(() =>
            policy.SetPunchTolerances(punchInTolerance, punchOutTolerance),
            expectedMessage
        );
    }

    // SetBreakRules - valid
    [Fact]
    public void SetBreakRule_ValidBreakRules_UpdatesBreakRules()
    {
        // Arrange
        var valid = GetValidParameters();
        var policy = new Policy(
                valid.year,
                valid.workDayStart,
                valid.workDayEnd,
                valid.punchInTolerance,
                valid.punchOutTolerance,
                valid.maxSingleBreak,
                valid.MaxTotalBreakPerDay,
                valid.overtimeMultiplier,
                valid.leaveQuotas);

        var b1 = TimeSpan.Parse("1:00:00");
        var b2 = TimeSpan.Parse("3:00:00");

        // Act
        policy.SetBreakRules(b1, b2);

        // Assert
        Assert.Equal(b1, policy.MaxSingleBreak);
        Assert.Equal(b2, policy.MaxTotalBreakPerDay);

    }

    // SetBreakRules - invalid
    [Theory]
    [MemberData(nameof(InvalidBreakRules))]
    public void SetBreakRules_InvalidBreakRules_ThrowsDomainException(TimeSpan maxSingleBreak, TimeSpan maxTotalPerDay, string expectedMessage)
    {
        // Arrange
        var valid = GetValidParameters();
        var policy = new Policy(
                valid.year,
                valid.workDayStart,
                valid.workDayEnd,
                valid.punchInTolerance,
                valid.punchOutTolerance,
                valid.maxSingleBreak,
                valid.MaxTotalBreakPerDay,
                valid.overtimeMultiplier,
                valid.leaveQuotas);

        // Act & Assert
        AssertThrowsPolicyException(() => 
            policy.SetBreakRules(maxSingleBreak, maxTotalPerDay),
            expectedMessage
        );
    }

    // SetOvertimeMultiplier - valid
    [Fact]
    public void SetOvertimeMultiplier_ValidMultiplier_UpdatesOvertimeMultiplier()
    {
        // Arrange
        var valid = GetValidParameters();
        var policy = new Policy(
                valid.year,
                valid.workDayStart,
                valid.workDayEnd,
                valid.punchInTolerance,
                valid.punchOutTolerance,
                valid.maxSingleBreak,
                valid.MaxTotalBreakPerDay,
                valid.overtimeMultiplier,
                valid.leaveQuotas);

        var newOvertimeMultiplier = 2.5m;

        // Act
        policy.SetOvertimeMultiplier(newOvertimeMultiplier);

        // Assert
        Assert.Equal(newOvertimeMultiplier, policy.OvertimeMultiplier);
    }

    // SetOvertimeMultiplier - invalid
    [Theory]
    [InlineData(null)]
    [InlineData(0.5)]
    public void SetOvertimeMultiplier_InvalidMultiplier_ThrowsDomainException(decimal badOvertimeMultiplier)
    {
        // Arrange
        var valid = GetValidParameters();
        var policy = new Policy(
                valid.year,
                valid.workDayStart,
                valid.workDayEnd,
                valid.punchInTolerance,
                valid.punchOutTolerance,
                valid.maxSingleBreak,
                valid.MaxTotalBreakPerDay,
                valid.overtimeMultiplier,
                valid.leaveQuotas);

        // Act & Assert
        AssertThrowsPolicyException(() =>
            policy.SetOvertimeMultiplier(badOvertimeMultiplier),
            "Overtime multiplier must be >= 1.0"
        );
    }
}

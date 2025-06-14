using EMSApp.Domain.Exceptions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Options;

namespace EMSApp.Domain;

public enum LeaveType { Annual, Compassionate, Parental, Paid, Unpaid, Sick, TOIL, Academic, Misc }

/// <summary>
/// Encapsulates company-wide rules for working hours, punch tolerances, and leave quotas per type.
/// </summary>
public class Policy
{
    [BsonId]
    [BsonRepresentation(BsonType.Int32)]
    public int Year { get; private set; }

    public TimeOnly WorkDayStart { get; private set; }
    public TimeOnly WorkDayEnd { get; private set; }

    public TimeSpan PunchInTolerance { get; private set; }
    public TimeSpan PunchOutTolerance { get; private set; }

    public TimeSpan MaxSingleBreak { get; private set; }
    public TimeSpan MaxTotalBreakPerDay { get; private set; }
    public decimal OvertimeMultiplier { get; private set; }

    [BsonDictionaryOptions(DictionaryRepresentation.ArrayOfDocuments)]
    public Dictionary<LeaveType, int>? LeaveQuotas { get; private set; }

    public Policy() { }

    public Policy(
        int year,
        TimeOnly workDayStart,
        TimeOnly workDayEnd,
        TimeSpan punchInTolerance,
        TimeSpan punchOutTolerance,
        TimeSpan maxSingleBreak,
        TimeSpan maxTotalBreakPerDay,
        decimal overtimeMultiplier,
        IDictionary<LeaveType, int> leaveQuotas)
    {
        // Year validation
        if (year < 2000 || year > 3000)
            throw new DomainException("Year must be between 2000 and 3000");

        // WorkDayStart validation
        if (workDayStart == default)
            throw new DomainException("Work day start must be provided");

        // WorkDayEnd validation
        if (workDayEnd == default)
            throw new DomainException("Work day end must be provided");
        if (workDayEnd <= workDayStart)
            throw new DomainException("Work day end must be after work day start");

        // LeaveTypes validation
        foreach (LeaveType lt in Enum.GetValues(typeof(LeaveType)))
            if (!leaveQuotas.ContainsKey(lt))
                throw new DomainException($"Missing leave quota for {lt}");

        // MaxSingleBreak validation
        if (maxSingleBreak == default)
            throw new DomainException("Max single break must be provided");
        if (maxSingleBreak < TimeSpan.Zero)
            throw new DomainException("Max single break must be non-negative");

        // MaxTotalBreakPerDay validation
        if (maxTotalBreakPerDay == default)
            throw new DomainException("Total breaks per day limit must be provided");
        if (maxTotalBreakPerDay < maxSingleBreak)
            throw new DomainException("Total breaks per day must be >= single break limit");

        // OvertimeMultiplier validation
        if (overtimeMultiplier < 1m)
            throw new DomainException("Overtime multiplier must be >= 1.0");



        WorkDayStart = workDayStart;
        WorkDayEnd = workDayEnd;
        PunchInTolerance = punchInTolerance;
        PunchOutTolerance = punchOutTolerance;
        Year = year;
        MaxSingleBreak = maxSingleBreak;
        MaxTotalBreakPerDay = maxTotalBreakPerDay;
        OvertimeMultiplier = overtimeMultiplier;
        LeaveQuotas = new Dictionary<LeaveType, int>(leaveQuotas);
    }

    /// <summary>
    /// Returns true if punch-in time is within allowed hours
    /// </summary>
    /// <param name="timeIn"></param>
    /// <returns></returns>
    public bool IsValidPunchIn(TimeOnly timeIn)
        => timeIn >= WorkDayStart.Add(-PunchInTolerance);

    /// <summary>
    /// Returns true if punch-out time is within allowed hours
    /// </summary>
    /// <param name="timeOut"></param>
    /// <returns></returns>
    public bool IsValidPunchOut(TimeOnly timeOut)
        => timeOut <= WorkDayEnd.Add(PunchOutTolerance);

    /// <summary>
    /// How many days of this leave type are allowed per year
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public int GetLeaveQuota(LeaveType type)
        => LeaveQuotas!.TryGetValue(type, out var q) ? q : 0;

    /// <summary>
    /// Update the quota for a given leave type
    /// </summary>
    /// <param name="type"></param>
    /// <param name="days"></param>
    /// <exception cref="DomainException"></exception>
    public void SetLeaveQuota(LeaveType type, int days)
    {
        if (days < 0)
            throw new DomainException("Leave quota cannot be negative");
        var mutable = new Dictionary<LeaveType, int>(LeaveQuotas!)
        {
            [type] = days
        };
        LeaveQuotas = mutable;
    }

    /// <summary>
    /// Update working hours window
    /// </summary>
    /// <param name="newStart"></param>
    /// <param name="newEnd"></param>
    /// <exception cref="DomainException"></exception>
    public void SetWorkingHours(TimeOnly newStart, TimeOnly newEnd)
    {
        if (newEnd <= newStart)
            throw new DomainException("End time must be after start time");

        WorkDayStart = newStart;
        WorkDayEnd = newEnd;
    }

    /// <summary>
    /// Update punch tolerances
    /// </summary>
    /// <param name="punchInTolerance"></param>
    /// <param name="punchOutTolereance"></param>
    /// <exception cref="DomainException"></exception>
    public void SetPunchTolerances(TimeSpan punchInTolerance, TimeSpan punchOutTolerance)
    {
        PunchInTolerance = punchInTolerance;
        PunchOutTolerance = punchOutTolerance;
    }

    /// <summary>
    /// Update break rules
    /// </summary>
    /// <param name="singleBreak"></param>
    /// <param name="totalPerDay"></param>
    public void SetBreakRules(TimeSpan singleBreak, TimeSpan totalPerDay)
    {
        if (singleBreak == default || totalPerDay == default)
            throw new DomainException("Break rules must be provided");
        if (singleBreak < TimeSpan.Zero || totalPerDay < singleBreak)
            throw new DomainException("Invalid break rules");

        MaxSingleBreak = singleBreak;
        MaxTotalBreakPerDay = totalPerDay;
    }

    /// <summary>
    /// Update overtime multiplier
    /// </summary>
    /// <param name="m"></param>
    /// <exception cref="DomainException"></exception>
    public void SetOvertimeMultiplier(decimal m)
    {
        if (m < 1m)
            throw new DomainException("Overtime multiplier must be >= 1.0");

        OvertimeMultiplier = m;
    }

    public void SetLeaveQuotas(IDictionary<LeaveType, int> leaveQuotas)
    {
        LeaveQuotas = new Dictionary<LeaveType, int>(leaveQuotas);
    }
}

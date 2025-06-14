using System;
using System.Collections.Generic;
using System.Linq;
using EMSApp.Domain;
using EMSApp.Domain.Exceptions;
using Xunit;

namespace EMSApp.Tests
{
    [Trait("Category", "Domain")]
    public class PolicyTests
    {
        private static void AssertThrowsPolicyException(Action act, string expectedMessage)
        {
            var ex = Assert.Throws<DomainException>(act);
            Assert.Contains(expectedMessage, ex.Message);
        }

        private static (int year, TimeOnly workDayStart, TimeOnly workDayEnd,
                       TimeSpan punchInTolerance, TimeSpan punchOutTolerance,
                       TimeSpan maxSingleBreak, TimeSpan maxTotalBreakPerDay,
                       decimal overtimeMultiplier, IDictionary<LeaveType, int> leaveQuotas)
            GetValidParameters()
        {
            var quotas = Enum.GetValues<LeaveType>().ToDictionary(lt => lt, lt => 10);
            return (2025,
                    new TimeOnly(9, 0), new TimeOnly(17, 0),
                    TimeSpan.FromMinutes(15), TimeSpan.FromMinutes(10),
                    TimeSpan.FromMinutes(30), TimeSpan.FromHours(2),
                    1.5m, quotas);
        }

        [Fact]
        public void Constructor_ValidParameters_CreatesPolicy()
        {
            var v = GetValidParameters();
            var policy = new Policy(v.year, v.workDayStart, v.workDayEnd,
                                     v.punchInTolerance, v.punchOutTolerance,
                                     v.maxSingleBreak, v.maxTotalBreakPerDay,
                                     v.overtimeMultiplier, v.leaveQuotas);

            Assert.Equal(v.year, policy.Year);
            Assert.Equal(v.workDayStart, policy.WorkDayStart);
            Assert.Equal(v.workDayEnd, policy.WorkDayEnd);
            Assert.Equal(v.punchInTolerance, policy.PunchInTolerance);
            Assert.Equal(v.punchOutTolerance, policy.PunchOutTolerance);
            Assert.Equal(v.maxSingleBreak, policy.MaxSingleBreak);
            Assert.Equal(v.maxTotalBreakPerDay, policy.MaxTotalBreakPerDay);
            Assert.Equal(v.overtimeMultiplier, policy.OvertimeMultiplier);
            foreach (var lt in v.leaveQuotas.Keys)
                Assert.Equal(10, policy.GetLeaveQuota(lt));
        }

        [Theory]
        [InlineData(1999)]
        [InlineData(3001)]
        public void Constructor_InvalidYear_Throws(int badYear)
        {
            var v = GetValidParameters();
            AssertThrowsPolicyException(() =>
                new Policy(badYear, v.workDayStart, v.workDayEnd,
                           v.punchInTolerance, v.punchOutTolerance,
                           v.maxSingleBreak, v.maxTotalBreakPerDay,
                           v.overtimeMultiplier, v.leaveQuotas),
                "Year must be between 2000 and 3000");
        }

        [Fact]
        public void Constructor_InvalidWorkDayStart_Throws()
        {
            var v = GetValidParameters();
            AssertThrowsPolicyException(() =>
                new Policy(v.year, default, v.workDayEnd,
                           v.punchInTolerance, v.punchOutTolerance,
                           v.maxSingleBreak, v.maxTotalBreakPerDay,
                           v.overtimeMultiplier, v.leaveQuotas),
                "Work day start must be provided");
        }

        [Theory]
        [InlineData(default, "Work day end must be provided")]
        [InlineData("08:00", "Work day end must be after work day start")]
        public void Constructor_InvalidWorkDayEnd_Throws(string endStr, string expected)
        {
            var v = GetValidParameters();
            var end = endStr == default ? default : TimeOnly.Parse(endStr);
            AssertThrowsPolicyException(() =>
                new Policy(v.year, v.workDayStart, end,
                           v.punchInTolerance, v.punchOutTolerance,
                           v.maxSingleBreak, v.maxTotalBreakPerDay,
                           v.overtimeMultiplier, v.leaveQuotas),
                expected);
        }

        [Theory]
        [InlineData(default, "Max single break must be provided")]
        [InlineData("-00:10", "Max single break must be non-negative")]
        public void Constructor_InvalidMaxSingleBreak_Throws(string msbStr, string expected)
        {
            var v = GetValidParameters();
            var msb = msbStr == default ? default : TimeSpan.Parse(msbStr);
            AssertThrowsPolicyException(() =>
                new Policy(v.year, v.workDayStart, v.workDayEnd,
                           v.punchInTolerance, v.punchOutTolerance,
                           msb, v.maxTotalBreakPerDay,
                           v.overtimeMultiplier, v.leaveQuotas), expected);
        }

        [Theory]
        [InlineData(default, "Total breaks per day limit must be provided")]
        [InlineData("00:29:00", "Total breaks per day must be >= single break limit")]
        public void Constructor_InvalidMaxTotalBreak_Throws(string mtbStr, string expected)
        {
            var v = GetValidParameters();
            var mtb = mtbStr == default ? default : TimeSpan.Parse(mtbStr);
            AssertThrowsPolicyException(() =>
                new Policy(v.year, v.workDayStart, v.workDayEnd,
                           v.punchInTolerance, v.punchOutTolerance,
                           v.maxSingleBreak, mtb,
                           v.overtimeMultiplier, v.leaveQuotas), expected);
        }

        [Theory]
        [InlineData(0.5, "Overtime multiplier must be >= 1.0")]
        public void Constructor_InvalidOvertimeMultiplier_Throws(decimal mul, string expected)
        {
            var v = GetValidParameters();
            AssertThrowsPolicyException(() =>
                new Policy(v.year, v.workDayStart, v.workDayEnd,
                           v.punchInTolerance, v.punchOutTolerance,
                           v.maxSingleBreak, v.maxTotalBreakPerDay,
                           mul, v.leaveQuotas), expected);
        }

        [Fact]
        public void Constructor_MissingLeaveQuota_Throws()
        {
            var v = GetValidParameters();
            v.leaveQuotas.Remove(LeaveType.Sick);
            AssertThrowsPolicyException(() =>
                new Policy(v.year, v.workDayStart, v.workDayEnd,
                           v.punchInTolerance, v.punchOutTolerance,
                           v.maxSingleBreak, v.maxTotalBreakPerDay,
                           v.overtimeMultiplier, v.leaveQuotas),
                $"Missing leave quota for {LeaveType.Sick}");
        }

        [Theory]
        [InlineData("08:45", true)]
        [InlineData("08:40", false)]
        [InlineData("09:10", true)]
        [InlineData("10:00", true)]
        public void IsValidPunchIn_BehavesCorrectly(string tStr, bool expected)
        {
            var v = GetValidParameters();
            var policy = new Policy(v.year, v.workDayStart, v.workDayEnd,
                                     v.punchInTolerance, v.punchOutTolerance,
                                     v.maxSingleBreak, v.maxTotalBreakPerDay,
                                     v.overtimeMultiplier, v.leaveQuotas);
            var t = TimeOnly.Parse(tStr);
            Assert.Equal(expected, policy.IsValidPunchIn(t));
        }

        [Theory]
        [InlineData("17:00", true)]
        [InlineData("16:50", true)]
        [InlineData("17:10", true)]
        [InlineData("17:15", false)]
        public void IsValidPunchOut_BehavesCorrectly(string tStr, bool expected)
        {
            var v = GetValidParameters();
            var policy = new Policy(v.year, v.workDayStart, v.workDayEnd,
                                     v.punchInTolerance, v.punchOutTolerance,
                                     v.maxSingleBreak, v.maxTotalBreakPerDay,
                                     v.overtimeMultiplier, v.leaveQuotas);
            var t = TimeOnly.Parse(tStr);
            Assert.Equal(expected, policy.IsValidPunchOut(t));
        }

        [Fact]
        public void SetLeaveQuota_Valid_UpdatesQuota()
        {
            var v = GetValidParameters();
            var policy = new Policy(v.year, v.workDayStart, v.workDayEnd,
                                     v.punchInTolerance, v.punchOutTolerance,
                                     v.maxSingleBreak, v.maxTotalBreakPerDay,
                                     v.overtimeMultiplier, v.leaveQuotas);
            policy.SetLeaveQuota(LeaveType.Annual, 20);
            Assert.Equal(20, policy.GetLeaveQuota(LeaveType.Annual));
        }

        [Fact]
        public void SetLeaveQuota_Negative_Throws()
        {
            var v = GetValidParameters();
            var policy = new Policy(v.year, v.workDayStart, v.workDayEnd,
                                     v.punchInTolerance, v.punchOutTolerance,
                                     v.maxSingleBreak, v.maxTotalBreakPerDay,
                                     v.overtimeMultiplier, v.leaveQuotas);
            AssertThrowsPolicyException(() => policy.SetLeaveQuota(LeaveType.Annual, -1),
                                        "Leave quota cannot be negative");
        }

        [Fact]
        public void SetWorkingHours_Valid_Updates()
        {
            var v = GetValidParameters();
            var policy = new Policy(v.year, v.workDayStart, v.workDayEnd,
                                     v.punchInTolerance, v.punchOutTolerance,
                                     v.maxSingleBreak, v.maxTotalBreakPerDay,
                                     v.overtimeMultiplier, v.leaveQuotas);
            var start = TimeOnly.Parse("07:00");
            var end = TimeOnly.Parse("16:00");
            policy.SetWorkingHours(start, end);
            Assert.Equal(start, policy.WorkDayStart);
            Assert.Equal(end, policy.WorkDayEnd);
        }

        [Fact]
        public void SetWorkingHours_Invalid_Throws()
        {
            var v = GetValidParameters();
            var policy = new Policy(v.year, v.workDayStart, v.workDayEnd,
                                     v.punchInTolerance, v.punchOutTolerance,
                                     v.maxSingleBreak, v.maxTotalBreakPerDay,
                                     v.overtimeMultiplier, v.leaveQuotas);
            var start = TimeOnly.Parse("16:00");
            var end = TimeOnly.Parse("07:00");
            AssertThrowsPolicyException(() => policy.SetWorkingHours(start, end),
                                        "End time must be after start time");
        }

        [Fact]
        public void SetPunchTolerances_Valid_Updates()
        {
            var v = GetValidParameters();
            var policy = new Policy(v.year, v.workDayStart, v.workDayEnd,
                                     v.punchInTolerance, v.punchOutTolerance,
                                     v.maxSingleBreak, v.maxTotalBreakPerDay,
                                     v.overtimeMultiplier, v.leaveQuotas);
            var pin = TimeSpan.FromMinutes(20);
            var pout = TimeSpan.FromMinutes(15);
            policy.SetPunchTolerances(pin, pout);
            Assert.Equal(pin, policy.PunchInTolerance);
            Assert.Equal(pout, policy.PunchOutTolerance);
        }

        [Fact]
        public void SetBreakRules_Valid_Updates()
        {
            var v = GetValidParameters();
            var policy = new Policy(v.year, v.workDayStart, v.workDayEnd,
                                     v.punchInTolerance, v.punchOutTolerance,
                                     v.maxSingleBreak, v.maxTotalBreakPerDay,
                                     v.overtimeMultiplier, v.leaveQuotas);
            var msb = TimeSpan.FromHours(1);
            var mtb = TimeSpan.FromHours(3);
            policy.SetBreakRules(msb, mtb);
            Assert.Equal(msb, policy.MaxSingleBreak);
            Assert.Equal(mtb, policy.MaxTotalBreakPerDay);
        }

        [Theory]
        [InlineData(0, 0, "Break rules must be provided")]
        [InlineData(-1, -2, "Invalid break rules")]
        public void SetBreakRules_Invalid_Throws(int msbMin, int mtbMin, string expected)
        {
            var v = GetValidParameters();
            var policy = new Policy(v.year, v.workDayStart, v.workDayEnd,
                                     v.punchInTolerance, v.punchOutTolerance,
                                     v.maxSingleBreak, v.maxTotalBreakPerDay,
                                     v.overtimeMultiplier, v.leaveQuotas);
            var msb = TimeSpan.FromMinutes(msbMin);
            var mtb = TimeSpan.FromMinutes(mtbMin);
            AssertThrowsPolicyException(() => policy.SetBreakRules(msb, mtb), expected);
        }

        [Fact]
        public void SetOvertimeMultiplier_Valid_Updates()
        {
            var v = GetValidParameters();
            var policy = new Policy(v.year, v.workDayStart, v.workDayEnd,
                                     v.punchInTolerance, v.punchOutTolerance,
                                     v.maxSingleBreak, v.maxTotalBreakPerDay,
                                     v.overtimeMultiplier, v.leaveQuotas);
            var mul = 2.5m;
            policy.SetOvertimeMultiplier(mul);
            Assert.Equal(mul, policy.OvertimeMultiplier);
        }

        [Theory]
        [InlineData(0, "Overtime multiplier must be >= 1.0")]
        public void SetOvertimeMultiplier_Invalid_Throws(decimal mul, string expected)
        {
            var v = GetValidParameters();
            var policy = new Policy(v.year, v.workDayStart, v.workDayEnd,
                                     v.punchInTolerance, v.punchOutTolerance,
                                     v.maxSingleBreak, v.maxTotalBreakPerDay,
                                     v.overtimeMultiplier, v.leaveQuotas);
            AssertThrowsPolicyException(() => policy.SetOvertimeMultiplier(mul), expected);
        }
    }
}

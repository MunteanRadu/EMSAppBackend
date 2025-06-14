using System;
using Xunit;
using EMSApp.Domain;
using EMSApp.Domain.Exceptions;

namespace EMSApp.Tests.Domain
{
    public class ShiftRuleTests
    {
        private static void AssertThrows(string expected, Action act)
        {
            var ex = Assert.Throws<DomainException>(act);
            Assert.Contains(expected, ex.Message);
        }

        [Fact]
        public void Ctor_ValidParameters_SetsProperties()
        {
            var rule = new ShiftRule("dept-1", 1, 2, 3, 2, 8.0);
            Assert.Equal("dept-1", rule.DepartmentId);
            Assert.Equal(1, rule.MinPerShift1);
            Assert.Equal(2, rule.MinPerShift2);
            Assert.Equal(3, rule.MinPerNightShift);
            Assert.Equal(2, rule.MaxConsecutiveNightShifts);
            Assert.Equal(8.0, rule.MinRestHoursBetweenShifts);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Ctor_InvalidDepartment_Throws(string badDept) =>
            AssertThrows("DepartmentId cannot be empty", () =>
                new ShiftRule(badDept, 1, 1, 1, 1, 1.0));

        [Theory]
        [InlineData(-1, 0, 0)]
        [InlineData(0, -1, 0)]
        [InlineData(0, 0, -1)]
        public void Ctor_NegativeMins_Throws(int s1, int s2, int ns) =>
            AssertThrows("Min values cannot be negative", () =>
                new ShiftRule("d", s1, s2, ns, 1, 1.0));

        [Theory]
        [InlineData(0)]
        [InlineData(-5)]
        public void Ctor_BadMaxConsec_Throws(int badMax) =>
            AssertThrows("MaxConsecutiveNightShifts must be at least 1", () =>
                new ShiftRule("d", 0, 0, 0, badMax, 1.0));

        [Fact]
        public void Update_ValidParameters_ChangesProperties()
        {
            var rule = new ShiftRule("d", 1, 1, 1, 1, 2.0);
            rule.Update(5, 6, 7, 3, 9.5);
            Assert.Equal(5, rule.MinPerShift1);
            Assert.Equal(6, rule.MinPerShift2);
            Assert.Equal(7, rule.MinPerNightShift);
            Assert.Equal(3, rule.MaxConsecutiveNightShifts);
            Assert.Equal(9.5, rule.MinRestHoursBetweenShifts);
        }

        [Theory]
        [InlineData(-1, 0, 0)]
        [InlineData(0, -1, 0)]
        [InlineData(0, 0, -1)]
        public void Update_NegativeMins_Throws(int s1, int s2, int ns) =>
            AssertThrows("Min values cannot be negative", () =>
                new ShiftRule("d", 1, 1, 1, 1, 1.0).Update(s1, s2, ns, 1, 1.0));

        [Theory]
        [InlineData(0)]
        [InlineData(-2)]
        public void Update_BadMaxConsec_Throws(int badMax) =>
            AssertThrows("MaxConsecutiveNightShifts must be at least 1", () =>
                new ShiftRule("d", 1, 1, 1, 1, 1.0).Update(1, 1, 1, badMax, 1.0));
    }

    public class ShiftAssignmentTests
    {
        private static void AssertThrows(string expected, Action act)
        {
            var ex = Assert.Throws<DomainException>(act);
            Assert.Contains(expected, ex.Message);
        }

        [Fact]
        public void Ctor_ValidParameters_SetsAll()
        {
            var date = new DateOnly(2025, 6, 1);
            var start = new TimeOnly(8, 0);
            var end = new TimeOnly(16, 0);
            var a = new ShiftAssignment("u1", date, ShiftType.Shift2, start, end, "dept", "mgr");
            Assert.Equal("u1", a.UserId);
            Assert.Equal(date, a.Date);
            Assert.Equal(ShiftType.Shift2, a.Shift);
            Assert.Equal(start, a.StartTime);
            Assert.Equal(end, a.EndTime);
            Assert.Equal("dept", a.DepartmentId);
            Assert.Equal("mgr", a.ManagerId);
        }

        [Theory]
        [InlineData(null, "Date must be provided")]
        [InlineData("", "UserId cannot be empty")]
        public void Ctor_BadIds_Throws(string bad, string expected)
        {
            var d = new DateOnly(2025, 6, 1);
            AssertThrows(expected, () =>
                new ShiftAssignment(bad, d, ShiftType.Shift1, new TimeOnly(8, 0), new TimeOnly(9, 0), "dept", "mgr"));
        }

        [Fact]
        public void Ctor_DefaultDate_Throws()
        {
            AssertThrows("Date must be provided", () =>
                new ShiftAssignment("u", default, ShiftType.Shift1, new TimeOnly(8, 0), new TimeOnly(9, 0), "dept", "mgr"));
        }

        [Theory]
        [InlineData("09:00", "08:00", "End time must be after start time")]
        [InlineData("08:00", "08:00", "End time must be after start time")]
        public void Ctor_EndNotAfterStart_Throws(string s, string e, string expected)
        {
            var st = TimeOnly.Parse(s);
            var et = TimeOnly.Parse(e);
            AssertThrows(expected, () =>
                new ShiftAssignment("u", new DateOnly(2025, 6, 1), ShiftType.Shift1, st, et, "dept", "mgr"));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("  ")]
        public void Ctor_BadDepartmentOrManager_Throws(string bad)
        {
            var d = new DateOnly(2025, 6, 1);
            AssertThrows("DepartmentId cannot be empty", () =>
                new ShiftAssignment("u", d, ShiftType.Shift1, new TimeOnly(8, 0), new TimeOnly(9, 0), bad, "mgr"));
            AssertThrows("ManagerId cannot be empty", () =>
                new ShiftAssignment("u", d, ShiftType.Shift1, new TimeOnly(8, 0), new TimeOnly(9, 0), "dept", bad));
        }

        [Fact]
        public void UpdateShift_Valid_ChangesProperties()
        {
            var d = new DateOnly(2025, 6, 1);
            var a = new ShiftAssignment("u", d, ShiftType.Shift1, new TimeOnly(8, 0), new TimeOnly(12, 0), "d", "m");
            a.UpdateShift(ShiftType.NightShift, new TimeOnly(0, 0), new TimeOnly(6, 0));
            Assert.Equal(ShiftType.NightShift, a.Shift);
            Assert.Equal(new TimeOnly(0, 0), a.StartTime);
            Assert.Equal(new TimeOnly(6, 0), a.EndTime);
        }

        [Fact]
        public void UpdateShift_BadTimes_Throws()
        {
            var d = new DateOnly(2025, 6, 1);
            var a = new ShiftAssignment("u", d, ShiftType.Shift1, new TimeOnly(8, 0), new TimeOnly(12, 0), "d", "m");
            AssertThrows("End time must be after start time", () =>
                a.UpdateShift(ShiftType.Shift2, new TimeOnly(10, 0), new TimeOnly(9, 0)));
        }
    }
}

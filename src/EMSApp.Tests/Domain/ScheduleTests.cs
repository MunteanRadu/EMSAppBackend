using System;
using EMSApp.Domain;
using EMSApp.Domain.Exceptions;
using Xunit;

namespace EMSApp.Tests
{
    [Trait("Category", "Domain")]
    public class ScheduleTests
    {
        [Fact]
        public void Constructor_ValidParameters_CreatesSchedule()
        {
            // Arrange
            var departmentId = "dept-123";
            var managerId = "mgr-456";
            var shift = ShiftType.Shift2;
            var day = DayOfWeek.Wednesday;
            var start = TimeOnly.Parse("09:00");
            var end = TimeOnly.Parse("17:00");
            var isWorkingDay = true;

            // Act
            var schedule = new Schedule(departmentId, managerId, shift, day, start, end, isWorkingDay);

            // Assert
            Assert.Equal(departmentId, schedule.DepartmentId);
            Assert.Equal(managerId, schedule.ManagerId);
            Assert.Equal(shift, schedule.ShiftType);
            Assert.Equal(day, schedule.Day);
            Assert.Equal(start, schedule.StartTime);
            Assert.Equal(end, schedule.EndTime);
            Assert.Equal(isWorkingDay, schedule.IsWorkingDay);
            Assert.False(string.IsNullOrWhiteSpace(schedule.Id));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Constructor_InvalidDepartmentId_ThrowsDomainException(string badDept)
        {
            // Act & Assert
            var ex = Assert.Throws<DomainException>(() =>
                new Schedule(badDept, "mgr", ShiftType.Shift1, DayOfWeek.Monday, TimeOnly.Parse("08:00"), TimeOnly.Parse("16:00"), true)
            );
            Assert.Contains("Department Id cannot be empty", ex.Message);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Constructor_InvalidManagerId_ThrowsDomainException(string badMgr)
        {
            var ex = Assert.Throws<DomainException>(() =>
                new Schedule("dept", badMgr, ShiftType.Shift1, DayOfWeek.Monday, TimeOnly.Parse("08:00"), TimeOnly.Parse("16:00"), true)
            );
            Assert.Contains("Manager Id cannot be empty", ex.Message);
        }

        [Fact]
        public void Constructor_InvalidDay_ThrowsDomainException()
        {
            var ex = Assert.Throws<DomainException>(() =>
                new Schedule("dept", "mgr", ShiftType.Shift1, (DayOfWeek)999, TimeOnly.Parse("08:00"), TimeOnly.Parse("16:00"), true)
            );
            Assert.Contains("Invalid day of week", ex.Message);
        }

        [Fact]
        public void Constructor_DefaultEndTime_ThrowsDomainException()
        {
            var ex = Assert.Throws<DomainException>(() =>
                new Schedule("dept", "mgr", ShiftType.Shift1, DayOfWeek.Friday, TimeOnly.Parse("08:00"), default, true)
            );
            Assert.Contains("End time must be provided", ex.Message);
        }

        [Fact]
        public void Constructor_EndTimeBeforeOrEqualStart_ThrowsDomainException()
        {
            var start = TimeOnly.Parse("09:00");
            var endSame = TimeOnly.Parse("09:00");
            var endBefore = TimeOnly.Parse("08:00");

            var exSame = Assert.Throws<DomainException>(() =>
                new Schedule("dept", "mgr", ShiftType.Shift1, DayOfWeek.Friday, start, endSame, true)
            );
            Assert.Contains("End time must be after start time", exSame.Message);

            var exBefore = Assert.Throws<DomainException>(() =>
                new Schedule("dept", "mgr", ShiftType.Shift1, DayOfWeek.Friday, start, endBefore, true)
            );
            Assert.Contains("End time must be after start time", exBefore.Message);
        }

        [Fact]
        public void UpdateShift_ValidParameters_UpdatesProperties()
        {
            // Arrange
            var schedule = new Schedule("dept", "mgr", ShiftType.Shift1, DayOfWeek.Tuesday, TimeOnly.Parse("08:00"), TimeOnly.Parse("16:00"), true);
            var newShift = ShiftType.NightShift;
            var newStart = TimeOnly.Parse("15:00");
            var newEnd = newStart.AddHours(8);
            var newWorkingDay = false;

            // Act
            schedule.UpdateShift(newShift, newStart, newEnd, newWorkingDay);

            // Assert
            Assert.Equal(newShift, schedule.ShiftType);
            Assert.Equal(newStart, schedule.StartTime);
            Assert.Equal(newEnd, schedule.EndTime);
            Assert.Equal(newWorkingDay, schedule.IsWorkingDay);
        }

        [Fact]
        public void UpdateShift_InvalidEndBeforeOrEqualStart_ThrowsDomainException()
        {
            var schedule = new Schedule("dept", "mgr", ShiftType.Shift1, DayOfWeek.Tuesday, TimeOnly.Parse("08:00"), TimeOnly.Parse("16:00"), true);
            var start = TimeOnly.Parse("08:00");
            var endEqual = TimeOnly.Parse("08:00");
            var endBefore = TimeOnly.Parse("07:00");

            var exEqual = Assert.Throws<DomainException>(() =>
                schedule.UpdateShift(schedule.ShiftType, start, endEqual, true)
            );
            Assert.Contains("End time must be after start time", exEqual.Message);

            var exBefore = Assert.Throws<DomainException>(() =>
                schedule.UpdateShift(schedule.ShiftType, start, endBefore, true)
            );
            Assert.Contains("End time must be after start time", exBefore.Message);
        }
    }
}

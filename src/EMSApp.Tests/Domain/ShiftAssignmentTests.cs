using System;
using EMSApp.Domain;
using EMSApp.Domain.Exceptions;
using Xunit;

namespace EMSApp.Tests
{
    [Trait("Category", "Domain")]
    public class ShiftAssignmentTests
    {
        [Fact]
        public void Constructor_ValidParameters_CreatesShiftAssignment()
        {
            // Arrange
            var userId = "user-123";
            var date = new DateOnly(2025, 6, 16);
            var shift = ShiftType.Shift1;
            var start = new TimeOnly(8, 0);
            var end = new TimeOnly(16, 0);
            var departmentId = "dept-456";
            var managerId = "mgr-789";

            // Act
            var assignment = new ShiftAssignment(userId, date, shift, start, end, departmentId, managerId);

            // Assert
            Assert.False(string.IsNullOrWhiteSpace(assignment.Id));
            Assert.Equal(userId, assignment.UserId);
            Assert.Equal(date, assignment.Date);
            Assert.Equal(shift, assignment.Shift);
            Assert.Equal(start, assignment.StartTime);
            Assert.Equal(end, assignment.EndTime);
            Assert.Equal(departmentId, assignment.DepartmentId);
            Assert.Equal(managerId, assignment.ManagerId);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Constructor_InvalidUserId_ThrowsDomainException(string badUserId)
        {
            var date = new DateOnly(2025, 6, 16);
            var start = new TimeOnly(8, 0);
            var end = new TimeOnly(16, 0);
            var ex = Assert.Throws<DomainException>(() =>
                new ShiftAssignment(badUserId, date, ShiftType.Shift2, start, end, "dept", "mgr"));
            Assert.Contains("UserId cannot be empty", ex.Message);
        }

        [Fact]
        public void Constructor_InvalidDate_ThrowsDomainException()
        {
            var start = new TimeOnly(8, 0);
            var end = new TimeOnly(16, 0);
            var ex = Assert.Throws<DomainException>(() =>
                new ShiftAssignment("user", default, ShiftType.Shift2, start, end, "dept", "mgr"));
            Assert.Contains("Date must be provided", ex.Message);
        }

        [Theory]
        [InlineData(9, 0, 9, 0)]
        [InlineData(10, 0, 9, 0)]
        public void Constructor_InvalidEndTime_ThrowsDomainException(int sh, int sm, int eh, int em)
        {
            var date = new DateOnly(2025, 6, 16);
            var start = new TimeOnly(sh, sm);
            var end = new TimeOnly(eh, em);
            var ex = Assert.Throws<DomainException>(() =>
                new ShiftAssignment("user", date, ShiftType.NightShift, start, end, "dept", "mgr"));
            Assert.Contains("End time must be after start time", ex.Message);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Constructor_InvalidDepartmentId_ThrowsDomainException(string badDept)
        {
            var date = new DateOnly(2025, 6, 16);
            var start = new TimeOnly(8, 0);
            var end = new TimeOnly(16, 0);
            var ex = Assert.Throws<DomainException>(() =>
                new ShiftAssignment("user", date, ShiftType.Shift1, start, end, badDept, "mgr"));
            Assert.Contains("DepartmentId cannot be empty", ex.Message);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Constructor_InvalidManagerId_ThrowsDomainException(string badMgr)
        {
            var date = new DateOnly(2025, 6, 16);
            var start = new TimeOnly(8, 0);
            var end = new TimeOnly(16, 0);
            var ex = Assert.Throws<DomainException>(() =>
                new ShiftAssignment("user", date, ShiftType.Shift1, start, end, "dept", badMgr));
            Assert.Contains("ManagerId cannot be empty", ex.Message);
        }

        [Fact]
        public void UpdateShift_ValidParameters_UpdatesProperties()
        {
            // Arrange
            var assignment = new ShiftAssignment(
                "user-123",
                new DateOnly(2025, 6, 16),
                ShiftType.Shift1,
                new TimeOnly(8, 0),
                new TimeOnly(16, 0),
                "dept-456",
                "mgr-789"
            );

            // Act
            var newShift = ShiftType.NightShift;
            var newStart = new TimeOnly(9, 0);
            var newEnd = new TimeOnly(17, 0);
            assignment.UpdateShift(newShift, newStart, newEnd);

            // Assert
            Assert.Equal(newShift, assignment.Shift);
            Assert.Equal(newStart, assignment.StartTime);
            Assert.Equal(newEnd, assignment.EndTime);
        }

        [Theory]
        [InlineData(8, 0, 8, 0)]
        [InlineData(9, 0, 8, 0)]
        public void UpdateShift_InvalidTimes_ThrowsDomainException(int nsh, int nsm, int neh, int nem)
        {
            var assignment = new ShiftAssignment(
                "user-123",
                new DateOnly(2025, 6, 16),
                ShiftType.Shift1,
                new TimeOnly(8, 0),
                new TimeOnly(16, 0),
                "dept-456",
                "mgr-789"
            );
            var newStart = new TimeOnly(nsh, nsm);
            var newEnd = new TimeOnly(neh, nem);
            var ex = Assert.Throws<DomainException>(() =>
                assignment.UpdateShift(ShiftType.Shift2, newStart, newEnd));
            Assert.Contains("End time must be after start time", ex.Message);
        }
    }
}

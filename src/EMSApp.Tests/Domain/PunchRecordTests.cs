using System;
using EMSApp.Domain;
using EMSApp.Domain.Exceptions;
using Xunit;

namespace EMSApp.Tests
{
    [Trait("Category", "Domain")]
    public class PunchRecordTests
    {
        [Fact]
        public void Constructor_ValidParameters_CreatesPunchRecord()
        {
            // Arrange
            var userId = "user-123";
            var date = new DateOnly(2025, 4, 25);
            var timeIn = new TimeOnly(8, 0);

            // Act
            var record = new PunchRecord(userId, date, timeIn);

            // Assert
            Assert.False(string.IsNullOrWhiteSpace(record.Id));
            Assert.Equal(userId, record.UserId);
            Assert.Equal(date, record.Date);
            Assert.Equal(timeIn, record.TimeIn);
            Assert.Null(record.TimeOut);
            Assert.Null(record.GetTotalHours());
            Assert.False(record.IsComplete());
            Assert.False(record.IsNonCompliant);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Constructor_InvalidUserId_ThrowsDomainException(string badUser)
        {
            var date = new DateOnly(2025, 4, 25);
            var timeIn = new TimeOnly(8, 0);
            var ex = Assert.Throws<DomainException>(() => new PunchRecord(badUser, date, timeIn));
            Assert.Contains("UserId Id cannot be empty", ex.Message);
        }

        [Fact]
        public void Constructor_InvalidDate_ThrowsDomainException()
        {
            var userId = "user-123";
            var timeIn = new TimeOnly(8, 0);
            var ex = Assert.Throws<DomainException>(() => new PunchRecord(userId, default, timeIn));
            Assert.Contains("Date must be provided", ex.Message);
        }

        [Fact]
        public void Constructor_InvalidTimeIn_ThrowsDomainException()
        {
            var userId = "user-123";
            var date = new DateOnly(2025, 4, 25);
            var ex = Assert.Throws<DomainException>(() => new PunchRecord(userId, date, default));
            Assert.Contains("Punch-in time must be provided", ex.Message);
        }

        [Fact]
        public void PunchOut_ValidTimeOut_SetsTimeOutAndTotalHours()
        {
            // Arrange
            var record = new PunchRecord("user-123", new DateOnly(2025, 4, 25), new TimeOnly(8, 0));
            var timeOut = new TimeOnly(16, 0);

            // Act
            record.PunchOut(timeOut);

            // Assert
            Assert.Equal(timeOut, record.TimeOut);
            Assert.Equal(TimeSpan.FromHours(8), record.GetTotalHours());
            Assert.True(record.IsComplete());
        }

        [Fact]
        public void PunchOut_AlreadyPunchedOut_ThrowsDomainException()
        {
            var record = new PunchRecord("user-123", new DateOnly(2025, 4, 25), new TimeOnly(8, 0));
            record.PunchOut(new TimeOnly(16, 0));
            var ex = Assert.Throws<DomainException>(() => record.PunchOut(new TimeOnly(17, 0)));
            Assert.Contains("Already punched out", ex.Message);
        }

        [Theory]
        [InlineData(default)]
        [InlineData("07:00")]
        public void PunchOut_InvalidTimeOut_ThrowsDomainException(string timeOutStr)
        {
            var record = new PunchRecord("user-123", new DateOnly(2025, 4, 25), new TimeOnly(8, 0));
            TimeOnly timeOut = timeOutStr == default ? default : TimeOnly.Parse(timeOutStr);
            var ex = Assert.Throws<DomainException>(() => record.PunchOut(timeOut));
            Assert.Contains(timeOutStr == default ? "Punch-out time must be provided" : "Punch-out time must be after punch-in time", ex.Message);
        }

        [Fact]
        public void GetTotalHours_NotCompleted_ReturnsNull()
        {
            var record = new PunchRecord("user-123", new DateOnly(2025, 4, 25), new TimeOnly(8, 0));
            Assert.Null(record.GetTotalHours());
        }

        [Fact]
        public void UpdateDate_ValidDate_UpdatesDate()
        {
            var record = new PunchRecord("user-123", new DateOnly(2025, 4, 25), new TimeOnly(8, 0));
            var newDate = new DateOnly(2025, 4, 26);
            record.UpdateDate(newDate);
            Assert.Equal(newDate, record.Date);
        }

        [Fact]
        public void UpdateDate_Invalid_ThrowsDomainException()
        {
            var record = new PunchRecord("user-123", new DateOnly(2025, 4, 25), new TimeOnly(8, 0));
            var ex = Assert.Throws<DomainException>(() => record.UpdateDate(default));
            Assert.Contains("Date must be provided", ex.Message);
        }

        [Fact]
        public void UpdateTimeIn_Valid_UpdatesTimeIn()
        {
            var record = new PunchRecord("user-123", new DateOnly(2025, 4, 25), new TimeOnly(8, 0));
            var newTimeIn = new TimeOnly(9, 0);
            record.UpdateTimeIn(newTimeIn);
            Assert.Equal(newTimeIn, record.TimeIn);
        }

        [Fact]
        public void UpdateTimeIn_Invalid_ThrowsDomainException()
        {
            var record = new PunchRecord("user-123", new DateOnly(2025, 4, 25), new TimeOnly(8, 0));
            var ex = Assert.Throws<DomainException>(() => record.UpdateTimeIn(default));
            Assert.Contains("Punch-in time must be provided", ex.Message);
        }

        [Fact]
        public void UpdateTimeOut_Invalid_ThrowsDomainException()
        {
            var record = new PunchRecord("user-123", new DateOnly(2025, 4, 25), new TimeOnly(8, 0));
            var ex = Assert.Throws<DomainException>(() => record.UpdateTimeOut(default));
            Assert.Contains("Punch-out time must be provided", ex.Message);
        }

        [Fact]
        public void MarkAsNonCompliant_SetsFlag()
        {
            var record = new PunchRecord("user-123", new DateOnly(2025, 4, 25), new TimeOnly(8, 0));
            Assert.False(record.IsNonCompliant);
            record.MarkAsNonCompliant();
            Assert.True(record.IsNonCompliant);
        }
    }
}

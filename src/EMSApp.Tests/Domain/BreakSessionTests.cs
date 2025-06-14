using System;
using EMSApp.Domain;
using EMSApp.Domain.Exceptions;
using Xunit;

namespace EMSApp.Tests
{
    [Trait("Category", "Domain")]
    public class BreakSessionTests
    {
        private static void AssertThrowsBreakSessionException(Action act, string expectedMessage)
        {
            var ex = Assert.Throws<DomainException>(act);
            Assert.Contains(expectedMessage, ex.Message);
        }

        private static BreakSession CreateValidBreakSession()
        {
            var session = new BreakSession("punch-123", new TimeOnly(12, 0));
            session.End(new TimeOnly(12, 30));
            return session;
        }

        [Fact]
        public void Constructor_ValidParameters_CreatesBreakSession()
        {
            // Arrange
            var sessionId = "punch-123";
            var start = new TimeOnly(12, 0);
            var end = new TimeOnly(12, 30);

            // Act
            var session = new BreakSession(sessionId, start);

            // Pre-assert
            Assert.False(session.IsComplete());
            Assert.Null(session.EndTime);
            Assert.Null(session.Duration);

            // Finish session
            session.End(end);

            // Assert
            Assert.Equal(sessionId, session.PunchRecordId);
            Assert.Equal(start, session.StartTime);
            Assert.Equal(end, session.EndTime);
            Assert.Equal(TimeSpan.FromMinutes(30), session.Duration);
            Assert.True(session.IsComplete());
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Constructor_InvalidPunchRecordId_ThrowsDomainException(string badId)
        {
            var time = new TimeOnly(12, 0);
            AssertThrowsBreakSessionException(
                () => new BreakSession(badId, time),
                "Punch Record Id cannot be empty"
            );
        }

        [Theory]
        [InlineData(default, "StartTime time must be provided")]
        public void Constructor_InvalidStartTime_ThrowsDomainException(TimeOnly badTime, string expectedMessage)
        {
            AssertThrowsBreakSessionException(
                () => new BreakSession("punch-123", badTime),
                expectedMessage
            );
        }

        [Fact]
        public void EndBreak_ValidData_EndsBreak()
        {
            // Arrange
            var session = new BreakSession("punch-123", new TimeOnly(12, 0));
            var end = new TimeOnly(12, 30);

            // Act
            session.End(end);

            // Assert
            Assert.Equal(end, session.EndTime);
            Assert.Equal(TimeSpan.FromMinutes(30), session.Duration);
            Assert.True(session.IsComplete());
        }

        [Fact]
        public void EndBreak_BreakAlreadyEnded_ThrowsDomainException()
        {
            // Arrange
            var session = CreateValidBreakSession();
            var end = session.EndTime.Value;

            // Act & Assert
            var ex = Assert.Throws<DomainException>(() => session.End(end));
            Assert.Contains("Break has already ended", ex.Message);
        }

        [Theory]
        [InlineData(default, "EndTime time must be provided")]
        [InlineData("11:00", "EndTime time must be after start time")]
        public void EndBreak_InvalidEndTime_ThrowsDomainException(string timeStr, string expectedMessage)
        {
            var session = new BreakSession("punch-123", new TimeOnly(12, 0));
            var badTime = timeStr == default ? default : TimeOnly.Parse(timeStr);

            AssertThrowsBreakSessionException(
                () => session.End(badTime),
                expectedMessage
            );
        }
    }
}

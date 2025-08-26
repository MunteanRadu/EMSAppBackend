using System;
using EMSApp.Domain;
using EMSApp.Domain.Exceptions;
using Xunit;

namespace EMSApp.Tests
{
    [Trait("Category", "Domain")]
    public class LeaveRequestTests
    {
        private static void AssertThrowsLeaveRequestException(Action act, string expectedMessage)
        {
            var ex = Assert.Throws<DomainException>(act);
            Assert.Contains(expectedMessage, ex.Message);
        }

        private static LeaveRequest CreateValidLeaveRequest()
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var start = today.AddDays(1);
            var end = start.AddDays(5);
            return new LeaveRequest("user-123", LeaveType.Parental, start, end, "newborn child");
        }

        [Fact]
        public void Constructor_ValidParameters_CreatesLeaveRequest()
        {
            // Arrange
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var start = today.AddDays(1);
            var end = start.AddDays(5);

            // Act
            var request = new LeaveRequest("user-321", LeaveType.Sick, start, end, "pneumonia");

            // Assert
            Assert.Equal("user-321", request.UserId);
            Assert.Equal(LeaveType.Sick, request.Type);
            Assert.Equal(start, request.StartDate);
            Assert.Equal(end, request.EndDate);
            Assert.Equal("pneumonia", request.Reason);
            Assert.NotNull(request.RequestedAt);
            Assert.Equal(LeaveStatus.Pending, request.Status);
            Assert.False(request.IsCompleted());
        }

        [Fact]
        public void Constructor_StartDateInPast_ThrowsDomainException()
        {
            var yesterday = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1);
            AssertThrowsLeaveRequestException(
                () => new LeaveRequest("user", LeaveType.Annual, yesterday, yesterday.AddDays(1), "reason"),
                "Cannot request leave starting in the past"
            );
        }

        [Fact]
        public void Constructor_StartDateTooFarInFuture_ThrowsDomainException()
        {
            var future = DateOnly.FromDateTime(DateTime.UtcNow).AddYears(2);
            AssertThrowsLeaveRequestException(
                () => new LeaveRequest("user", LeaveType.Annual, future, future.AddDays(1), "reason"),
                "Leave cannot start more than 1 year from today"
            );
        }

        [Fact]
        public void Constructor_EndDateBeforeStart_ThrowsDomainException()
        {
            var tomorrow = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1);
            AssertThrowsLeaveRequestException(
                () => new LeaveRequest("user", LeaveType.Annual, tomorrow, tomorrow.AddDays(-1), "reason"),
                "End date must be on or after the start date"
            );
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Constructor_InvalidUserId_ThrowsDomainException(string badUser)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1);
            var end = today.AddDays(1);
            AssertThrowsLeaveRequestException(
                () => new LeaveRequest(badUser, LeaveType.Sick, today, end, "reason"),
                "User Id cannot be empty"
            );
        }

        [Theory]
        [InlineData((LeaveType)999)]
        public void Constructor_InvalidLeaveType_ThrowsDomainException(LeaveType badType)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1);
            AssertThrowsLeaveRequestException(
                () => new LeaveRequest("user", badType, today, today.AddDays(1), "reason"),
                "Invalid leave type"
            );
        }

        [Fact]
        public void Constructor_InvalidStartDate_Default_ThrowsDomainException()
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1);
            AssertThrowsLeaveRequestException(
                () => new LeaveRequest("user", LeaveType.Sick, default, today, "reason"),
                "Cannot request leave starting in the past"
            );
        }

        [Fact]
        public void Constructor_InvalidEndDate_Default_ThrowsDomainException()
        {
            var tomorrow = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1);
            AssertThrowsLeaveRequestException(
                () => new LeaveRequest("user", LeaveType.Sick, tomorrow, default, "reason"),
                "End date must be on or after the start date"
            );
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Constructor_InvalidReason_ThrowsDomainException(string badReason)
        {
            var tomorrow = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1);
            AssertThrowsLeaveRequestException(
                () => new LeaveRequest("user", LeaveType.Sick, tomorrow, tomorrow.AddDays(1), badReason),
                "Reason cannot be empty"
            );
        }

        [Fact]
        public void Approve_WhenPending_SetsStatusApproved()
        {
            var request = CreateValidLeaveRequest();
            // Act
            request.Approve("mgr-1");
            // Assert
            Assert.Equal(LeaveStatus.Approved, request.Status);
            Assert.Equal("mgr-1", request.ManagerId);
            Assert.NotNull(request.DecisionAt);
        }

        [Fact]
        public void Approve_WhenNotPending_ThrowsDomainException()
        {
            var request = CreateValidLeaveRequest();
            request.Reject("mgr");
            AssertThrowsLeaveRequestException(
                () => request.Approve("mgr"),
                "Can only approve a pending leave request"
            );
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Approve_InvalidManagerId_ThrowsDomainException(string badMgr)
        {
            var request = CreateValidLeaveRequest();
            AssertThrowsLeaveRequestException(
                () => request.Approve(badMgr),
                "Manager Id cannot be empty"
            );
        }

        [Fact]
        public void Reject_WhenPending_SetsStatusRejected()
        {
            var request = CreateValidLeaveRequest();
            request.Reject("mgr-2");
            Assert.Equal(LeaveStatus.Rejected, request.Status);
            Assert.Equal("mgr-2", request.ManagerId);
            Assert.NotNull(request.DecisionAt);
            Assert.Equal(request.DecisionAt, request.CompletedAt);
        }

        [Fact]
        public void Reject_WhenNotPending_ThrowsDomainException()
        {
            var request = CreateValidLeaveRequest();
            request.Approve("mgr");
            AssertThrowsLeaveRequestException(
                () => request.Reject("mgr"),
                "Can only reject a pending leave request"
            );
        }

        [Fact]
        public void Complete_WhenApproved_SetsStatusCompleted()
        {
            var request = CreateValidLeaveRequest();
            request.Approve("mgr");
            // Act
            request.Complete();
            // Assert
            Assert.Equal(LeaveStatus.Completed, request.Status);
            Assert.NotNull(request.CompletedAt);
        }

        [Fact]
        public void Complete_WhenNotApproved_ThrowsDomainException()
        {
            var request = CreateValidLeaveRequest();
            request.Reject("mgr");
            AssertThrowsLeaveRequestException(
                () => request.Complete(),
                "Can only finalize an approved leave request"
            );
        }

        [Fact]
        public void IsCompleted_ReturnsExpected()
        {
            var request = CreateValidLeaveRequest();
            Assert.False(request.IsCompleted());
            request.Approve("mgr");
            request.Complete();
            Assert.True(request.IsCompleted());
        }
    }
}

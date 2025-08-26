using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using EMSApp.Application;
using EMSApp.Domain;
using EMSApp.Domain.Entities;
using EMSApp.Domain.Exceptions;
using EMSApp.Infrastructure;
using Moq;
using Xunit;

namespace EMSApp.Tests
{
    [Trait("Category", "Service")]
    public class LeaveRequestServiceTests
    {
        private readonly Mock<ILeaveRequestRepository> _repo;
        private readonly Mock<IPolicyService> _policySvc;
        private readonly Mock<IUserRepository> _userRepo;
        private readonly ILeaveRequestService _service;
        private readonly CancellationToken _ct = CancellationToken.None;

        public LeaveRequestServiceTests()
        {
            _repo = new Mock<ILeaveRequestRepository>();
            _policySvc = new Mock<IPolicyService>();
            _userRepo = new Mock<IUserRepository>();
            _service = new LeaveRequestService(_repo.Object, _policySvc.Object, _userRepo.Object);
        }

        [Fact]
        public async Task CreateAsync_UserNotFound_Throws()
        {
            _userRepo.Setup(r => r.GetByIdAsync("u", _ct))
                     .ReturnsAsync((User?)null);

            await Assert.ThrowsAsync<DomainException>(() =>
                _service.CreateAsync("u", LeaveType.Paid,
                    DateOnly.FromDateTime(DateTime.UtcNow),
                    DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1),
                    "r", _ct));
        }

        [Fact]
        public async Task CreateAsync_NoDepartment_Throws()
        {
            var u = new User("e@e", "user", "password", "");
            _userRepo.Setup(r => r.GetByIdAsync("u", _ct)).ReturnsAsync(u);

            await Assert.ThrowsAsync<DomainException>(() =>
                _service.CreateAsync("u", LeaveType.Paid,
                    DateOnly.FromDateTime(DateTime.UtcNow),
                    DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1),
                    "r", _ct));
        }

        [Fact]
        public async Task CreateAsync_Overlapping_Throws()
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var existing = new LeaveRequest("u",
                LeaveType.Paid,
                today,
                today.AddDays(2),
                "r");
            existing.Approve("m");
            _repo.Setup(r => r.FilterByAsync(
                    It.IsAny<Expression<Func<LeaveRequest, bool>>>(), _ct))
                 .ReturnsAsync(new[] { existing });

            var u = new User("e@e", "user", "password", "d");
            _userRepo.Setup(r => r.GetByIdAsync("u", _ct)).ReturnsAsync(u);

            _policySvc.Setup(s => s.GetByYearAsync(today.Year, _ct))
                      .ReturnsAsync(new Policy(
                        today.Year,
                        new TimeOnly(9, 0), new TimeOnly(17, 0),
                        TimeSpan.FromMinutes(15), TimeSpan.FromMinutes(10),
                        TimeSpan.FromMinutes(30), TimeSpan.FromHours(2),
                        1m,
                        GetQuotas()));

            await Assert.ThrowsAsync<DomainException>(() =>
                _service.CreateAsync("u", LeaveType.Paid,
                    today.AddDays(1), today.AddDays(3),
                    "r2", _ct));
        }

        [Fact]
        public async Task CreateAsync_NotEnoughDays_Throws()
        {
            var u = new User("e@e", "user", "password", "d");
            _userRepo.Setup(r => r.GetByIdAsync("u", _ct)).ReturnsAsync(u);

            var policy = new Policy(2025,
                new TimeOnly(9, 0), new TimeOnly(17, 0),
                TimeSpan.FromMinutes(15), TimeSpan.FromMinutes(10),
                TimeSpan.FromMinutes(30), TimeSpan.FromHours(2),
                1m, GetQuotas());
            _policySvc.Setup(s => s.GetByYearAsync(2025, _ct)).ReturnsAsync(policy);

            _repo.Setup(r => r.FilterByAsync(It.IsAny<Expression<Func<LeaveRequest, bool>>>(), _ct))
                 .ReturnsAsync(Array.Empty<LeaveRequest>());

            await Assert.ThrowsAsync<DomainException>(() =>
                _service.CreateAsync("u", LeaveType.Annual,
                    new DateOnly(2025, 1, 1), new DateOnly(2025, 1, 7),
                    "r", _ct));
        }

        [Fact]
        public async Task GetRemainingLeaveDaysAsync_ComputesCorrectly()
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1);
            var approved = new LeaveRequest("u",
                LeaveType.Paid,
                today.AddDays(-1),
                today.AddDays(1),
                "r");
            approved.Approve("m");

            _repo.Setup(r => r.FilterByAsync(
                    It.IsAny<Expression<Func<LeaveRequest, bool>>>(), _ct))
                 .ReturnsAsync(new[] { approved });

            var policy = new Policy(
                today.Year,
                new TimeOnly(9, 0), new TimeOnly(17, 0),
                TimeSpan.FromMinutes(15), TimeSpan.FromMinutes(10),
                TimeSpan.FromMinutes(30), TimeSpan.FromHours(2),
                1m,
                GetQuotas());
            _policySvc.Setup(s => s.GetByYearAsync(today.Year, _ct))
                      .ReturnsAsync(policy);

            var remaining = await _service.GetRemainingLeaveDaysAsync("u", LeaveType.Paid, today.Year, _ct);

            var used = CountBusinessDays(approved.StartDate, approved.EndDate);
            var expected = policy.GetLeaveQuota(LeaveType.Paid) - used;
            Assert.Equal(expected, remaining);
        }

        [Fact]
        public void CountBusinessDays_IgnoresWeekends()
        {
            var count = LeaveRequestService.CountBusinessDays(
                new DateOnly(2025, 1, 1), new DateOnly(2025, 1, 7));
            Assert.Equal(5, count);
        }

        [Fact]
        public async Task CompleteDueRequestsAsync_CompletesAndReturnsCount()
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1);
            var due = new LeaveRequest("u", LeaveType.Paid,
                today.AddDays(-1), today.AddDays(-1), "r");
            due.Approve("m");
            var notDue = new LeaveRequest("u", LeaveType.Paid,
                today, today.AddDays(2), "r2");
            notDue.Approve("m");

            _repo.Setup(r => r.GetByStatusAsync(LeaveStatus.Approved, _ct))
                 .ReturnsAsync(new[] { due, notDue });

            var count = await _service.CompleteDueRequestsAsync(_ct);

            Assert.Equal(1, count);
            Assert.Equal(LeaveStatus.Completed, due.Status);
            _repo.Verify(r => r.UpdateAsync(due, false, _ct), Times.Once);
        }

        [Fact]
        public async Task HasOverlappingLeaveAsync_DetectsOverlap()
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var lr = new LeaveRequest("u", LeaveType.Paid,
                today, today.AddDays(2), "r");
            _repo.Setup(r => r.FilterByAsync(
                    It.IsAny<Expression<Func<LeaveRequest, bool>>>(), _ct))
                 .ReturnsAsync(new[] { lr });

            var result = await _service.HasOverlappingLeaveAsync("u",
                today.AddDays(1), today.AddDays(3), _ct);

            Assert.True(result);
        }

        private static IDictionary<LeaveType, int> GetQuotas() =>
        Enum.GetValues<LeaveType>()
            .Cast<LeaveType>()
            .ToDictionary(lt => lt, _ => 10);

        private static int CountBusinessDays(DateOnly start, DateOnly end)
        {
            var days = 0;
            for (var d = start; d <= end; d = d.AddDays(1))
                if (d.DayOfWeek != DayOfWeek.Saturday && d.DayOfWeek != DayOfWeek.Sunday)
                    days++;
            return days;
        }

    }
}

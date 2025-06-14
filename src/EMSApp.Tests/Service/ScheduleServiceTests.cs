using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EMSApp.Application;
using EMSApp.Domain;
using EMSApp.Domain.Entities;
using EMSApp.Infrastructure;
using Moq;
using Xunit;

namespace EMSApp.Tests
{
    [Trait("Category", "Service")]
    public class ScheduleServiceTests
    {
        private readonly Mock<IScheduleRepository> _repoMock;
        private readonly IScheduleService _service;
        private readonly CancellationToken _ct = CancellationToken.None;

        public ScheduleServiceTests()
        {
            _repoMock = new Mock<IScheduleRepository>();
            _service = new ScheduleService(_repoMock.Object);
        }

        [Fact]
        public async Task CreateAsync_ValidData_CreatesScheduleAndCallsRepo()
        {
            // Arrange
            var deptId = "dept1";
            var mgrId = "mgr1";
            var shift = ShiftType.Shift1;
            var day = DayOfWeek.Tuesday;
            var start = new TimeOnly(7, 0);
            var end = new TimeOnly(15, 0);
            var isWork = true;

            // Act
            var schedule = await _service.CreateAsync(deptId, mgrId, shift, day, start, end, isWork, _ct);

            // Assert
            Assert.NotNull(schedule);
            Assert.Equal(deptId, schedule.DepartmentId);
            Assert.Equal(mgrId, schedule.ManagerId);
            Assert.Equal(shift, schedule.ShiftType);
            Assert.Equal(day, schedule.Day);
            Assert.Equal(start, schedule.StartTime);
            Assert.Equal(end, schedule.EndTime);
            Assert.Equal(isWork, schedule.IsWorkingDay);

            _repoMock.Verify(r => r.CreateAsync(
                It.Is<Schedule>(s =>
                    s.DepartmentId == deptId &&
                    s.ManagerId == mgrId &&
                    s.ShiftType == shift &&
                    s.Day == day &&
                    s.StartTime == start &&
                    s.EndTime == end &&
                    s.IsWorkingDay == isWork
                ), _ct),
                Times.Once
            );
        }

        [Fact]
        public async Task GetByIdAsync_Existing_ReturnsSameSchedule()
        {
            // Arrange
            var schedule = new Schedule("d", "m", ShiftType.Shift2, DayOfWeek.Friday, new TimeOnly(9, 0), new TimeOnly(17, 0), true);
            _repoMock.Setup(r => r.GetByIdAsync(schedule.Id, _ct)).ReturnsAsync(schedule);

            // Act
            var result = await _service.GetByIdAsync(schedule.Id, _ct);

            // Assert
            Assert.Same(schedule, result);
            _repoMock.Verify(r => r.GetByIdAsync(schedule.Id, _ct), Times.Once);
        }

        [Fact]
        public async Task GetByIdAsync_NonExistent_ReturnsNull()
        {
            _repoMock.Setup(r => r.GetByIdAsync("no-id", _ct)).ReturnsAsync((Schedule?)null);

            var result = await _service.GetByIdAsync("no-id", _ct);

            Assert.Null(result);
        }

        [Fact]
        public async Task ListByDepartmentAsync_ForwardsCall()
        {
            var list = new List<Schedule>();
            _repoMock.Setup(r => r.GetByDepartmentAsync("deptX", _ct)).ReturnsAsync(list);

            var result = await _service.ListByDepartmentAsync("deptX", _ct);

            Assert.Same(list, result);
            _repoMock.Verify(r => r.GetByDepartmentAsync("deptX", _ct), Times.Once);
        }

        [Fact]
        public async Task ListByManagerAsync_ForwardsCall()
        {
            var list = new List<Schedule>();
            _repoMock.Setup(r => r.GetByManagerIdAsync("mgrX", _ct)).ReturnsAsync(list);

            var result = await _service.ListByManagerAsync("mgrX", _ct);

            Assert.Same(list, result);
            _repoMock.Verify(r => r.GetByManagerIdAsync("mgrX", _ct), Times.Once);
        }

        [Fact]
        public async Task GetAllAsync_ForwardsCall()
        {
            var list = new List<Schedule>();
            _repoMock.Setup(r => r.GetAllAsync(_ct)).ReturnsAsync(list);

            var result = await _service.GetAllAsync(_ct);

            Assert.Same(list, result);
            _repoMock.Verify(r => r.GetAllAsync(_ct), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_CallsRepositoryWithUpsertFalse()
        {
            var sched = new Schedule("d", "m", ShiftType.Shift2, DayOfWeek.Sunday, new TimeOnly(16, 0), new TimeOnly(23, 0), false);

            await _service.UpdateAsync(sched, _ct);

            _repoMock.Verify(r => r.UpdateAsync(sched, false, _ct), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_CallsRepository()
        {
            await _service.DeleteAsync("sched-1", _ct);

            _repoMock.Verify(r => r.DeleteAsync("sched-1", _ct), Times.Once);
        }
    }
}

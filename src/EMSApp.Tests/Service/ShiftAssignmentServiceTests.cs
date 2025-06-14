using EMSApp.Application;
using EMSApp.Application.Interfaces;
using EMSApp.Domain;
using EMSApp.Domain.Entities;
using EMSApp.Infrastructure;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Xunit;

namespace EMSApp.Tests.Service
{
    [Trait("Category", "Service")]
    public class ShiftAssignmentServiceTests
    {
        private readonly Mock<IScheduleRepository> _scheduleRepo = new();
        private readonly Mock<IDepartmentRepository> _deptRepo = new();
        private readonly Mock<ILeaveRequestRepository> _leaveRepo = new();
        private readonly Mock<IUserRepository> _userRepo = new();
        private readonly Mock<IShiftRuleRepository> _ruleRepo = new();
        private readonly Mock<IScheduleGenerationService> _genSvc = new();
        private readonly Mock<IShiftAssignmentRepository> _repo = new();
        private readonly ShiftAssignmentService _svc;
        private readonly CancellationToken _ct = CancellationToken.None;

        public ShiftAssignmentServiceTests()
        {
            _svc = new ShiftAssignmentService(
                _scheduleRepo.Object,
                _deptRepo.Object,
                _leaveRepo.Object,
                _userRepo.Object,
                _ruleRepo.Object,
                _genSvc.Object,
                _repo.Object);
        }

        [Fact]
        public async Task GenerateWeeklyScheduleAsync_NoSchedules_DoesNotAdd()
        {
            var deptId = "d1";
            var weekStart = new DateOnly(2025, 6, 16);

            _scheduleRepo.Setup(r => r.GetByDepartmentAsync(deptId, _ct))
                         .ReturnsAsync(Array.Empty<Schedule>());
            _deptRepo.Setup(r => r.GetByIdAsync(deptId, _ct))
                     .ReturnsAsync(new Department("dep"));
            // no leaves, no rule
            _leaveRepo.Setup(r => r.GetApprovedLeavesForWeekAsync(It.IsAny<IEnumerable<string>>(), weekStart, _ct))
                      .ReturnsAsync(Array.Empty<LeaveRequest>());
            _ruleRepo.Setup(r => r.GetByDepartmentAsync(deptId, _ct))
                     .ReturnsAsync(new ShiftRule(deptId, 1, 1, 1, 1, 0));

            await _svc.GenerateWeeklyScheduleAsync(deptId, weekStart, _ct);

            _repo.Verify(r => r.DeleteByDepartmentAndWeekAsync(deptId, weekStart, _ct), Times.Once);
            _repo.Verify(r => r.AddManyAsync(It.IsAny<IEnumerable<ShiftAssignment>>(), _ct), Times.Never);
        }

        [Fact]
        public async Task GenerateWeeklyScheduleAsync_WithSchedules_AddsAssignments()
        {
            var deptId = "d1";
            var weekStart = new DateOnly(2025, 6, 16);
            var dept = new Department("dep");
            dept.AssignManager("mgr");
            dept.AddEmployee("u1");
            dept.AddEmployee("u2");

            // one working shift on Monday
            var sched = new Schedule("d1", "mgr", ShiftType.Shift1, DayOfWeek.Monday, TimeOnly.Parse("08:00"), TimeOnly.Parse("16:00"), true);
            _scheduleRepo.Setup(r => r.GetByDepartmentAsync(deptId, _ct))
                         .ReturnsAsync(new[] { sched });
            _deptRepo.Setup(r => r.GetByIdAsync(deptId, _ct))
                     .ReturnsAsync(dept);
            _leaveRepo.Setup(r => r.GetApprovedLeavesForWeekAsync(dept.Employees, weekStart, _ct))
                      .ReturnsAsync(Array.Empty<LeaveRequest>());
            _ruleRepo.Setup(r => r.GetByDepartmentAsync(deptId, _ct))
                     .ReturnsAsync(new ShiftRule(deptId, 1, 1, 1, 1, 0));

            // Act
            await _svc.GenerateWeeklyScheduleAsync(deptId, weekStart, _ct);

            // Assert
            _repo.Verify(r => r.DeleteByDepartmentAndWeekAsync(deptId, weekStart, _ct), Times.Once);
            _repo.Verify(r => r.AddManyAsync(
                It.Is<IEnumerable<ShiftAssignment>>(list => list.All(a => a.DepartmentId == deptId)),
                _ct),
                Times.Once);
        }

        [Fact]
        public async Task GetAll_ForwardsToRepository()
        {
            var dummy = new List<ShiftAssignment> { new ShiftAssignment("u", DateOnly.FromDateTime(DateTime.UtcNow), ShiftType.Shift1, TimeOnly.Parse("8:00"), TimeOnly.Parse("16:00"), "d", "mgr") };
            _repo.Setup(r => r.GetAllAsync(_ct)).ReturnsAsync(dummy);

            var result = await _svc.GetAll(_ct);

            Assert.Same(dummy, result);
            _repo.Verify(r => r.GetAllAsync(_ct), Times.Once);
        }

        [Fact]
        public async Task GetUserScheduleAsync_ForwardsToRepository()
        {
            var weekStart = new DateOnly(2025, 6, 16);
            var dummy = new[] { new ShiftAssignment("u", weekStart, ShiftType.Shift2, TimeOnly.Parse("7:00"), TimeOnly.Parse("15:00"), "d", "mgr") };
            _repo.Setup(r => r.GetByUserAndWeekAsync("u", weekStart, _ct)).ReturnsAsync(dummy);

            var result = await _svc.GetUserScheduleAsync("u", weekStart, _ct);

            Assert.Equal(dummy, result);
            _repo.Verify(r => r.GetByUserAndWeekAsync("u", weekStart, _ct), Times.Once);
        }

        [Fact]
        public async Task SaveGeneratedShiftsAsync_SkipsInvalidShiftStrings()
        {
            var deptId = "d1";
            var weekStart = new DateOnly(2025, 6, 16);
            var dept = new Department("dep");
            dept.AssignManager("mgr");
            dept.AddEmployee("u1");
            _deptRepo.Setup(r => r.GetByIdAsync(deptId, _ct)).ReturnsAsync(dept);

            var dtos = new List<ShiftFromAiDto> {
                new ShiftFromAiDto {
                    UserId = "u1",
                    Date = weekStart,
                    Shift = "Shift1",
                    StartTime = TimeOnly.Parse("08:00"),
                    EndTime   = TimeOnly.Parse("16:00")
                },
                new ShiftFromAiDto {
                    UserId = "u1",
                    Date = weekStart,
                    Shift = "BadShift",
                    StartTime = TimeOnly.Parse("09:00"),
                    EndTime   = TimeOnly.Parse("17:00")
                }
            };

            await _svc.SaveGeneratedShiftsAsync(deptId, weekStart, dtos, _ct);

            _repo.Verify(r => r.DeleteByDepartmentAndWeekAsync(deptId, weekStart, _ct), Times.Once);
            _repo.Verify(r => r.AddManyAsync(
                It.Is<IEnumerable<ShiftAssignment>>(list =>
                    list.Count() == 1 &&
                    list.First().Shift == ShiftType.Shift1),
                _ct),
                Times.Once);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
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
    public class PunchRecordServiceTests
    {
        private readonly Mock<IShiftAssignmentRepository> _shiftRepo;
        private readonly Mock<IPunchRecordRepository> _punchRepo;
        private readonly Mock<ILeaveRequestService> _leaveSvc;
        private readonly Mock<IPolicyService> _policySvc;
        private readonly Mock<IBreakSessionRepository> _breakRepo;
        private readonly Mock<IMapper> _mapper;
        private readonly IPunchRecordService _service;
        private readonly CancellationToken _ct = CancellationToken.None;

        public PunchRecordServiceTests()
        {
            _shiftRepo = new Mock<IShiftAssignmentRepository>();
            _punchRepo = new Mock<IPunchRecordRepository>();
            _leaveSvc = new Mock<ILeaveRequestService>();
            _policySvc = new Mock<IPolicyService>();
            _breakRepo = new Mock<IBreakSessionRepository>();
            _mapper = new Mock<IMapper>();
            _service = new PunchRecordService(
                _shiftRepo.Object,
                _punchRepo.Object,
                _leaveSvc.Object,
                _policySvc.Object,
                _breakRepo.Object,
                _mapper.Object);
        }

        [Fact]
        public async Task CreateAsync_NoLeaveAndPolicyExists_CreatesRecord()
        {
            var userId = "u1";
            var date = new DateOnly(2025, 6, 1);
            var timeIn = new TimeOnly(9, 0);
            _leaveSvc.Setup(s => s.ListByUserAsync(userId, _ct))
                     .ReturnsAsync(Array.Empty<LeaveRequest>());
            var policy = new Policy(
                year: 2025,
                workDayStart: new TimeOnly(8, 0),
                workDayEnd: new TimeOnly(17, 0),
                punchInTolerance: TimeSpan.FromMinutes(15),
                punchOutTolerance: TimeSpan.FromMinutes(10),
                maxSingleBreak: TimeSpan.FromMinutes(30),
                maxTotalBreakPerDay: TimeSpan.FromHours(2),
                overtimeMultiplier: 1.5m,
                leaveQuotas: Enum.GetValues<LeaveType>()
                                 .Cast<LeaveType>()
                                 .ToDictionary(lt => lt, _ => 10));
            _policySvc.Setup(s => s.GetByYearAsync(2025, _ct))
                      .ReturnsAsync(policy);
            _shiftRepo.Setup(s => s.GetForUserOnDateAsync(userId, date, _ct))
                      .ReturnsAsync((ShiftAssignment?)null);

            var result = await _service.CreateAsync(userId, date, timeIn, _ct);

            Assert.Equal(userId, result.UserId);
            Assert.Equal(date, result.Date);
            Assert.Equal(timeIn, result.TimeIn);
            _punchRepo.Verify(r => r.CreateAsync(
                It.Is<PunchRecord>(p =>
                    p.UserId == userId &&
                    p.Date == date &&
                    p.TimeIn == timeIn),
                _ct), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_WithActiveLeave_ThrowsDomainException()
        {
            var userId = "u1";
            var date = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1);
            var timeIn = new TimeOnly(9, 0);
            var leave = new LeaveRequest("u1", LeaveType.Sick, date.AddDays(-1), date.AddDays(1), "reason");
            leave.Approve("mgr");
            _leaveSvc.Setup(s => s.ListByUserAsync(userId, _ct))
                     .ReturnsAsync(new[] { leave });

            await Assert.ThrowsAsync<DomainException>(() =>
                _service.CreateAsync(userId, date, timeIn, _ct));
        }

        [Fact]
        public async Task CreateAsync_MissingPolicy_ThrowsDomainException()
        {
            _leaveSvc.Setup(s => s.ListByUserAsync(It.IsAny<string>(), _ct))
                     .ReturnsAsync(Array.Empty<LeaveRequest>());
            _policySvc.Setup(s => s.GetByYearAsync(2025, _ct))
                      .ReturnsAsync((Policy?)null);

            await Assert.ThrowsAsync<DomainException>(() =>
                _service.CreateAsync("u", new DateOnly(2025, 6, 1), new TimeOnly(9, 0), _ct));
        }

        [Fact]
        public async Task PunchOutAsync_ValidData_UpdatesAndReturnsDto()
        {
            var punchId = "pr1";
            var date = new DateOnly(2025, 6, 1);
            var timeIn = new TimeOnly(9, 0);
            var record = new PunchRecord("u1", date, timeIn);
            _punchRepo.Setup(r => r.GetByIdAsync(punchId, _ct))
                      .ReturnsAsync(record);
            var policy = new Policy(
                year: 2025,
                workDayStart: new TimeOnly(8, 0),
                workDayEnd: new TimeOnly(17, 0),
                punchInTolerance: TimeSpan.FromMinutes(15),
                punchOutTolerance: TimeSpan.FromMinutes(10),
                maxSingleBreak: TimeSpan.FromMinutes(30),
                maxTotalBreakPerDay: TimeSpan.FromHours(2),
                overtimeMultiplier: 1.5m,
                leaveQuotas: Enum.GetValues<LeaveType>()
                                 .Cast<LeaveType>()
                                 .ToDictionary(lt => lt, _ => 10));
            _policySvc.Setup(s => s.GetByYearAsync(2025, _ct))
                      .ReturnsAsync(policy);
            _shiftRepo.Setup(s => s.GetForUserOnDateAsync("u1", date, _ct))
                      .ReturnsAsync((ShiftAssignment?)null);
            _breakRepo.Setup(b => b.ListByPunchRecordAsync(punchId, _ct))
                      .ReturnsAsync(Array.Empty<BreakSession>());
            var dto = new PunchRecordDto(record.Id, record.UserId, record.Date, record.TimeIn, null, null, false);
            _mapper.Setup(m => m.Map<PunchRecordDto>(record)).Returns(dto);

            var result = await _service.PunchOutAsync(punchId, new TimeOnly(17, 0), _ct);

            Assert.Equal(dto, result);
            _punchRepo.Verify(r => r.UpdateAsync(record, true, _ct), Times.Once);
        }

        [Fact]
        public async Task PunchOutAsync_NotFound_ThrowsKeyNotFoundException()
            => await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _service.PunchOutAsync("no-id", new TimeOnly(17, 0), _ct));

        [Fact]
        public async Task PunchOutAsync_MissingPolicy_ThrowsDomainException()
        {
            var punchId = "pr2";
            var record = new PunchRecord("u1", new DateOnly(2025, 6, 1), new TimeOnly(9, 0));
            _punchRepo.Setup(r => r.GetByIdAsync(punchId, _ct)).ReturnsAsync(record);
            _policySvc.Setup(s => s.GetByYearAsync(2025, _ct)).ReturnsAsync((Policy?)null);

            await Assert.ThrowsAsync<DomainException>(() =>
                _service.PunchOutAsync(punchId, new TimeOnly(17, 0), _ct));
        }

        [Fact]
        public async Task PunchOutAsync_TooLate_ThrowsDomainException()
        {
            var punchId = "pr3";
            var date = new DateOnly(2025, 6, 1);
            var record = new PunchRecord("u1", date, new TimeOnly(9, 0));
            _punchRepo.Setup(r => r.GetByIdAsync(punchId, _ct)).ReturnsAsync(record);

            var policy = new Policy(
                year: 2025,
                workDayStart: new TimeOnly(8, 0),
                workDayEnd: new TimeOnly(17, 0),
                punchInTolerance: TimeSpan.FromMinutes(15),
                punchOutTolerance: TimeSpan.FromMinutes(10),
                maxSingleBreak: TimeSpan.FromMinutes(30),
                maxTotalBreakPerDay: TimeSpan.FromHours(2),
                overtimeMultiplier: 1.5m,
                leaveQuotas: Enum.GetValues<LeaveType>()
                                 .Cast<LeaveType>()
                                 .ToDictionary(lt => lt, _ => 10));
            _policySvc.Setup(s => s.GetByYearAsync(2025, _ct)).ReturnsAsync(policy);

            var assignment = new ShiftAssignment(
                userId: "u1",
                date: date,
                shift: ShiftType.Shift1,
                startTime: new TimeOnly(9, 0),
                endTime: new TimeOnly(17, 0),
                departmentId: "dept",
                managerId: "mgr");
            _shiftRepo.Setup(s => s.GetForUserOnDateAsync("u1", date, _ct))
                      .ReturnsAsync(assignment);

            var tooLate = assignment.EndTime.Add(policy.PunchOutTolerance).Add(TimeSpan.FromMinutes(30));

            await Assert.ThrowsAsync<DomainException>(() =>
                _service.PunchOutAsync(punchId, tooLate, _ct));
        }
    }
}

using AutoMapper;
using EMSApp.Api;
using EMSApp.Api.Controllers;
using EMSApp.Application;
using EMSApp.Domain;
using EMSApp.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace EMSApp.Tests
{
    [Trait("Category", "Controller")]
    public class LeaveRequestsControllerTests
    {
        private readonly Mock<ILeaveRequestService> _svc = new();
        private readonly Mock<IMapper> _mapper = new();
        private readonly LeaveRequestsController _ctrl;
        private readonly CancellationToken _ct = CancellationToken.None;

        public LeaveRequestsControllerTests()
        {
            _ctrl = new LeaveRequestsController(_svc.Object, _mapper.Object);
        }

        [Fact]
        public async Task Create_ReturnsCreatedDto()
        {
            // Arrange
            var req = new CreateLeaveRequestRequest(
                UserId: "u1",
                Type: LeaveType.Paid,
                StartDate: DateOnly.FromDateTime(DateTime.Today),
                EndDate: DateOnly.FromDateTime(DateTime.Today).AddDays(5),
                Reason: "Vacation"
            );
            var lr = new LeaveRequest(req.UserId, req.Type, req.StartDate, req.EndDate, req.Reason);
            var dto = new LeaveRequestDto(
                lr.Id, lr.UserId, lr.Type, lr.StartDate, lr.EndDate,
                lr.Reason, lr.Status, lr.ManagerId, lr.RequestedAt, lr.DecisionAt, lr.CompletedAt
            );

            _svc
                .Setup(s => s.CreateAsync(req.UserId, req.Type, req.StartDate, req.EndDate, req.Reason, _ct))
                .ReturnsAsync(lr);

            _mapper
                .Setup(m => m.Map<LeaveRequestDto>(lr))
                .Returns(dto);

            // Act
            var action = await _ctrl.Create(req, _ct);

            // Assert
            var created = Assert.IsType<CreatedAtActionResult>(action.Result);
            Assert.Equal(nameof(_ctrl.GetById), created.ActionName);
            Assert.Equal(dto, created.Value);
            _svc.Verify(s => s.CreateAsync(
                req.UserId, req.Type, req.StartDate, req.EndDate, req.Reason, _ct),
                Times.Once);
        }

        [Fact]
        public async Task GetById_Found_ReturnsDto()
        {
            // Arrange
            var lr = new LeaveRequest("u2", LeaveType.Sick, DateOnly.Parse("2025-07-01"), DateOnly.Parse("2025-07-02"), "Ill");
            var dto = new LeaveRequestDto(
                lr.Id, lr.UserId, lr.Type, lr.StartDate, lr.EndDate,
                lr.Reason, lr.Status, lr.ManagerId, lr.RequestedAt, lr.DecisionAt, lr.CompletedAt
            );

            _svc.Setup(s => s.GetByIdAsync(lr.Id, _ct)).ReturnsAsync(lr);
            _mapper.Setup(m => m.Map<LeaveRequestDto>(lr)).Returns(dto);

            // Act
            var action = await _ctrl.GetById(lr.Id, _ct);

            // Assert
            Assert.Equal(dto, action.Value);
            _svc.Verify(s => s.GetByIdAsync(lr.Id, _ct), Times.Once);
        }

        [Fact]
        public async Task GetById_NotFound_Returns404()
        {
            // Arrange
            _svc.Setup(s => s.GetByIdAsync("no", _ct)).ReturnsAsync((LeaveRequest?)null);

            // Act
            var action = await _ctrl.GetById("no", _ct);

            // Assert
            Assert.IsType<NotFoundResult>(action.Result);
        }

        [Fact]
        public async Task Approve_Found_ReturnsOkDto()
        {
            // Arrange
            var lr = new LeaveRequest("u3", LeaveType.Paid, DateOnly.Parse("2025-08-01"), DateOnly.Parse("2025-08-03"), "Trip");
            _svc.Setup(s => s.GetByIdAsync(lr.Id, _ct)).ReturnsAsync(lr);
            var dto = new LeaveRequestDto(
                lr.Id, lr.UserId, lr.Type, lr.StartDate, lr.EndDate,
                lr.Reason, lr.Status, lr.ManagerId, lr.RequestedAt, lr.DecisionAt, lr.CompletedAt
            );
            _mapper.Setup(m => m.Map<LeaveRequestDto>(lr)).Returns(dto);

            // Act
            var result = await _ctrl.Approve(lr.Id, managerId: "mgr", _ct);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(dto, ok.Value);
            _svc.Verify(s => s.UpdateAsync(lr, _ct), Times.Once);
            Assert.Equal(LeaveStatus.Approved, lr.Status);
            Assert.Equal("mgr", lr.ManagerId);
        }

        [Fact]
        public async Task Approve_NotFound_Returns404()
        {
            // Arrange
            _svc.Setup(s => s.GetByIdAsync("no", _ct)).ReturnsAsync((LeaveRequest?)null);

            // Act
            var result = await _ctrl.Approve("no", "mgr", _ct);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Reject_Found_ReturnsOkDto()
        {
            // Arrange
            var lr = new LeaveRequest("u4", LeaveType.Paid, DateOnly.Parse("2025-09-01"), DateOnly.Parse("2025-09-02"), "Errand");
            _svc.Setup(s => s.GetByIdAsync(lr.Id, _ct)).ReturnsAsync(lr);
            var dto = new LeaveRequestDto(
                lr.Id, lr.UserId, lr.Type, lr.StartDate, lr.EndDate,
                lr.Reason, lr.Status, lr.ManagerId, lr.RequestedAt, lr.DecisionAt, lr.CompletedAt
            );
            _mapper.Setup(m => m.Map<LeaveRequestDto>(lr)).Returns(dto);

            // Act
            var result = await _ctrl.Reject(lr.Id, managerId: "mgr2", _ct);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(dto, ok.Value);
            _svc.Verify(s => s.UpdateAsync(lr, _ct), Times.Once);
            Assert.Equal(LeaveStatus.Rejected, lr.Status);
            Assert.Equal("mgr2", lr.ManagerId);
        }

        [Fact]
        public async Task Reject_NotFound_Returns404()
        {
            // Arrange
            _svc.Setup(s => s.GetByIdAsync("no", _ct)).ReturnsAsync((LeaveRequest?)null);

            // Act
            var result = await _ctrl.Reject("no", "mgr2", _ct);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task List_ReturnsAllDtos()
        {
            // Arrange
            var list = new List<LeaveRequest>
            {
                new LeaveRequest("u5", LeaveType.Sick, DateOnly.Parse("2025-10-01"), DateOnly.Parse("2025-10-01"), "Flu")
            };
            var dtos = new List<LeaveRequestDto>
            {
                new LeaveRequestDto(
                    list[0].Id, list[0].UserId, list[0].Type,
                    list[0].StartDate, list[0].EndDate, list[0].Reason,
                    list[0].Status, list[0].ManagerId,
                    list[0].RequestedAt, list[0].DecisionAt, list[0].CompletedAt
                )
            };

            _svc.Setup(s => s.GetAllAsync(_ct)).ReturnsAsync(list);
            _mapper.Setup(m => m.Map<IEnumerable<LeaveRequestDto>>(list)).Returns(dtos);

            // Act
            var action = await _ctrl.List(_ct);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(action.Result);
            Assert.Equal(dtos, ok.Value);
        }

        [Fact]
        public async Task ListByUser_AndStatus_And_All()
        {
            // Arrange
            var lr1 = new LeaveRequest("u6", LeaveType.Paid, DateOnly.Parse("2025-11-01"), DateOnly.Parse("2025-11-02"), "A");
            var lr2 = new LeaveRequest("u7", LeaveType.Sick, DateOnly.Parse("2025-11-03"), DateOnly.Parse("2025-11-03"), "B");
            var all = new List<LeaveRequest> { lr1, lr2 };
            var byUser = new List<LeaveRequest> { lr1 };
            var byStatus = new List<LeaveRequest> { lr2 };

            _svc.Setup(s => s.ListByUserAsync("u6", _ct)).ReturnsAsync(byUser);
            _svc.Setup(s => s.ListByStatusAsync(LeaveStatus.Pending, _ct)).ReturnsAsync(byStatus);
            _svc.Setup(s => s.GetAllAsync(_ct)).ReturnsAsync(all);

            _mapper.Setup(m => m.Map<IEnumerable<LeaveRequestDto>>(It.IsAny<IEnumerable<LeaveRequest>>()))
                   .Returns<IEnumerable<LeaveRequest>>(lts => {
                       var listDto = new List<LeaveRequestDto>();
                       foreach (var l in lts)
                           listDto.Add(new LeaveRequestDto(
                               l.Id, l.UserId, l.Type, l.StartDate, l.EndDate,
                               l.Reason, l.Status, l.ManagerId,
                               l.RequestedAt, l.DecisionAt, l.CompletedAt));
                       return listDto;
                   });

            // Act & Assert
            var action1 = await _ctrl.ListByUser("u6", null, _ct);
            var ok1 = Assert.IsType<OkObjectResult>(action1.Result);
            var result1 = Assert.IsAssignableFrom<IEnumerable<LeaveRequestDto>>(ok1.Value);
            Assert.Single(result1);

            var action2 = await _ctrl.ListByUser(null, LeaveStatus.Pending, _ct);
            var ok2 = Assert.IsType<OkObjectResult>(action2.Result);
            var result2 = Assert.IsAssignableFrom<IEnumerable<LeaveRequestDto>>(ok2.Value);
            Assert.Single(result2);

            var action3 = await _ctrl.ListByUser(null, null, _ct);
            var ok3 = Assert.IsType<OkObjectResult>(action3.Result);
            var result3 = Assert.IsAssignableFrom<IEnumerable<LeaveRequestDto>>(ok3.Value);
            Assert.Equal(2, result3.Count());
        }

        [Fact]
        public async Task GetRemainingLeaveDays_ReturnsOk()
        {
            // Arrange
            _svc.Setup(s => s.GetRemainingLeaveDaysAsync("u8", LeaveType.Sick, 2025, _ct))
                .ReturnsAsync(5);

            // Act
            var ok = Assert.IsType<OkObjectResult>(await _ctrl.GetRemainingLeaveDays("u8", LeaveType.Sick, 2025, _ct));

            // Assert
            Assert.Equal(5, ok.Value);
        }

        [Fact]
        public async Task Update_Found_CallsService()
        {
            // Arrange
            var lr = new LeaveRequest("u9", LeaveType.Paid, DateOnly.Parse("2025-12-01"), DateOnly.Parse("2025-12-02"), "C");
            var req = new UpdateLeaveRequestRequest(
                Type: LeaveType.Sick,
                StartDate: DateOnly.Parse("2025-12-03"),
                EndDate: DateOnly.Parse("2025-12-04"),
                Reason: "Updated"
            );
            _svc.Setup(s => s.GetByIdAsync(lr.Id, _ct)).ReturnsAsync(lr);

            // Act
            var result = await _ctrl.Update(lr.Id, req, _ct);

            // Assert
            Assert.IsType<NoContentResult>(result);
            _svc.Verify(s => s.UpdateAsync(lr, _ct), Times.Once);
            Assert.Equal(LeaveType.Sick, lr.Type);
            Assert.Equal(DateOnly.Parse("2025-12-03"), lr.StartDate);
            Assert.Equal("Updated", lr.Reason);
        }

        [Fact]
        public async Task Update_NotFound_Returns404()
        {
            // Arrange
            _svc.Setup(s => s.GetByIdAsync("no", _ct)).ReturnsAsync((LeaveRequest?)null);

            // Act
            var result = await _ctrl.Update("no", new UpdateLeaveRequestRequest(null, null, null, null), _ct);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Delete_CallsService()
        {
            // Act
            var result = await _ctrl.Delete("x1", _ct);

            // Assert
            Assert.IsType<NoContentResult>(result);
            _svc.Verify(s => s.DeleteAsync("x1", _ct), Times.Once);
        }
    }

    // helper to cast IEnumerable to List for counting in test
    internal static class EnumerableExtensions
    {
        public static List<T> AsList<T>(this IEnumerable<T> src) => new(src);
    }
}

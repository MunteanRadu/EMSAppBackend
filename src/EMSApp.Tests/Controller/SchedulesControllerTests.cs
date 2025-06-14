using AutoMapper;
using EMSApp.Api.Controllers;
using EMSApp.Api;
using EMSApp.Application;
using EMSApp.Application;
using EMSApp.Domain;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using Xunit;

namespace EMSApp.Tests.Controller
{
    [Trait("Category", "Controller")]
    public class SchedulesControllerTests
    {
        private readonly Mock<IScheduleService> _svc = new();
        private readonly Mock<IMapper> _mapper = new();
        private readonly SchedulesController _ctrl;
        private readonly CancellationToken _ct = CancellationToken.None;

        public SchedulesControllerTests()
        {
            _ctrl = new SchedulesController(_svc.Object, _mapper.Object);
        }

        [Fact]
        public async Task Create_Valid_ReturnsCreatedAtAction()
        {
            // Arrange
            var req = new CreateScheduleRequest(
                DepartmentId: "dept1", 
                ManagerId: "mgr1", 
                ShiftType: ShiftType.Shift1, 
                Day: DayOfWeek.Monday, 
                StartTime: TimeOnly.Parse("08:00"), 
                EndTime: TimeOnly.Parse("16:00"), 
                IsWorkingDay: true
            );
            var entity = new Schedule(
                departmentId: req.DepartmentId,
                managerId: req.ManagerId,
                shift: req.ShiftType,
                day: req.Day,
                startTime: req.StartTime,
                endTime: req.EndTime,
                isWorkingDay: req.IsWorkingDay
            );
            var dto = new ScheduleDto(
                Id: entity.Id,
                DepartmentId: entity.DepartmentId,
                ManagerId: entity.ManagerId,
                ShiftType: entity.ShiftType,
                Day: entity.Day,
                StartTime: entity.StartTime,
                EndTime: entity.EndTime,
                IsWorkingDay: entity.IsWorkingDay
            );

            _svc
                .Setup(s => s.CreateAsync(
                    req.DepartmentId,
                    req.ManagerId,
                    req.ShiftType,
                    req.Day,
                    req.StartTime,
                    req.EndTime,
                    req.IsWorkingDay,
                    _ct
                ))
                .ReturnsAsync(entity);
            _mapper
                .Setup(m => m.Map<ScheduleDto>(entity))
                .Returns(dto);

            // Act
            var result = await _ctrl.Create(req, _ct);

            // Assert
            var created = Assert.IsType<CreatedAtActionResult>(result.Result);
            Assert.Equal(nameof(_ctrl.GetById), created.ActionName);
            Assert.Equal(dto, created.Value);
            _svc.VerifyAll();
            _mapper.VerifyAll();
        }

        [Fact]
        public async Task GetById_Exists_ReturnsDto()
        {
            // Arrange
            var entity = new Schedule("d", "m", ShiftType.Shift1, DayOfWeek.Tuesday, TimeOnly.Parse("09:00"), TimeOnly.Parse("17:00"), false);
            var dto = new ScheduleDto(entity.Id, entity.DepartmentId, entity.ManagerId, entity.ShiftType, entity.Day, entity.StartTime, entity.EndTime, entity.IsWorkingDay);

            _svc.Setup(s => s.GetByIdAsync(entity.Id, _ct)).ReturnsAsync(entity);
            _mapper.Setup(m => m.Map<ScheduleDto>(entity)).Returns(dto);

            // Act
            var action = await _ctrl.GetById(entity.Id, _ct);

            // Assert
            Assert.Equal(dto, action.Value);
            _svc.Verify(s => s.GetByIdAsync(entity.Id, _ct), Times.Once);
            _mapper.Verify(m => m.Map<ScheduleDto>(entity), Times.Once);
        }

        [Fact]
        public async Task GetById_NotFound_Returns404()
        {
            _svc.Setup(s => s.GetByIdAsync("nope", _ct)).ReturnsAsync((Schedule?)null);

            var action = await _ctrl.GetById("nope", _ct);

            Assert.IsType<NotFoundResult>(action.Result);
        }

        [Fact]
        public async Task ListByDepartment_ReturnsOkWithDtos()
        {
            // Arrange
            var list = new List<Schedule>
            {
                new Schedule("d","m", ShiftType.Shift1, DayOfWeek.Wednesday,TimeOnly.Parse("08:00"),TimeOnly.Parse("12:00"),true)
            };
            var dtos = new List<ScheduleDto>
            {
                new ScheduleDto(
                    Id: list[0].Id,
                    DepartmentId: list[0].DepartmentId,
                    ManagerId: list[0].ManagerId,
                    ShiftType: list[0].ShiftType,
                    Day: list[0].Day,
                    StartTime: list[0].StartTime,
                    EndTime: list[0].EndTime,
                    IsWorkingDay: list[0].IsWorkingDay
                )
            };

            _svc.Setup(s => s.ListByDepartmentAsync("d", _ct)).ReturnsAsync(list);
            _mapper.Setup(m => m.Map<IEnumerable<ScheduleDto>>(list)).Returns(dtos);

            // Act
            var action = await _ctrl.ListByDepartment("d", _ct);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(action.Result);
            Assert.Equal(dtos, ok.Value);
        }

        [Fact]
        public async Task List_All_ReturnsOkWithDtos()
        {
            // Arrange
            var list = new List<Schedule>
            {
                new Schedule("d","m",ShiftType.Shift1, DayOfWeek.Friday,TimeOnly.Parse("07:00"),TimeOnly.Parse("15:00"),false)
            };
            var dtos = new List<ScheduleDto>
            {
                new ScheduleDto(
                    list[0].Id,
                    list[0].DepartmentId,
                    list[0].ManagerId,
                    list[0].ShiftType,
                    list[0].Day,
                    list[0].StartTime,
                    list[0].EndTime,
                    list[0].IsWorkingDay
                )
            };

            _svc.Setup(s => s.GetAllAsync(_ct)).ReturnsAsync(list);
            _mapper.Setup(m => m.Map<IEnumerable<ScheduleDto>>(list)).Returns(dtos);

            // Act
            var action = await _ctrl.List(_ct);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(action.Result);
            Assert.Equal(dtos, ok.Value);
        }

        [Fact]
        public async Task Update_Existing_InvokesServiceAndReturnsNoContent()
        {
            // Arrange
            var entity = new Schedule("d", "m", ShiftType.Shift1, DayOfWeek.Saturday, TimeOnly.Parse("06:00"), TimeOnly.Parse("14:00"), true);
            var req = new UpdateScheduleRequest(
                StartTime: TimeOnly.Parse("07:00"),
                EndTime: TimeOnly.Parse("15:00"),
                ShiftType: entity.ShiftType,
                IsWorkingDay: false
            );
            _svc.Setup(s => s.GetByIdAsync(entity.Id, _ct)).ReturnsAsync(entity);

            // Act
            var result = await _ctrl.Update(entity.Id, req, _ct);

            // Assert
            Assert.IsType<NoContentResult>(result);
            _svc.Verify(s => s.UpdateAsync(entity, _ct), Times.Once);
        }

        [Fact]
        public async Task Update_NotFound_Returns404()
        {
            _svc.Setup(s => s.GetByIdAsync("nope", _ct)).ReturnsAsync((Schedule?)null);

            var result = await _ctrl.Update("nope", new UpdateScheduleRequest(null, null, default, null), _ct);

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Delete_Always_CallsServiceAndReturnsNoContent()
        {
            var result = await _ctrl.Delete("some-id", _ct);

            Assert.IsType<NoContentResult>(result);
            _svc.Verify(s => s.DeleteAsync("some-id", _ct), Times.Once);
        }
    }
}

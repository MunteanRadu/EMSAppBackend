using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using EMSApp.Api;
using EMSApp.Application;
using EMSApp.Application.Interfaces;
using EMSApp.Domain;
using EMSApp.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace EMSApp.Tests.Controller
{
    [Trait("Category", "Controller")]
    public class ShiftAssignmentControllerTests
    {
        private readonly Mock<IScheduleGenerationService> _genSvc = new();
        private readonly Mock<IShiftAssignmentService> _svc = new();
        private readonly Mock<IUserRepository> _userRepo = new();
        private readonly Mock<IDepartmentRepository> _deptRepo = new();
        private readonly Mock<AutoMapper.IMapper> _mapper = new();
        private readonly ShiftAssignmentController _ctrl;
        private readonly CancellationToken _ct = CancellationToken.None;

        public ShiftAssignmentControllerTests()
        {
            _ctrl = new ShiftAssignmentController(
                _genSvc.Object, _svc.Object, _userRepo.Object, _deptRepo.Object, _mapper.Object);
        }

        private void SetUser(string role, string userId)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.Role, role),
                new Claim(ClaimTypes.NameIdentifier, userId)
            };
            _ctrl.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"))
                }
            };
        }

        [Fact]
        public async Task GenerateWeeklySchedule_NonMonday_ReturnsBadRequest()
        {
            SetUser("Manager", "m1");
            var res = await _ctrl.GenerateWeeklySchedule("d1", new DateTime(2025, 6, 10), _ct);
            var bad = Assert.IsType<BadRequestObjectResult>(res);
            Assert.Equal("WeekStart must be Monday", bad.Value);
        }

        [Fact]
        public async Task GenerateWeeklySchedule_NoUser_ReturnsUnauthorized()
        {
            _ctrl.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
            var monday = new DateTime(2025, 6, 9);
            var res = await _ctrl.GenerateWeeklySchedule("d1", monday, _ct);
            Assert.IsType<UnauthorizedResult>(res);
        }

        [Fact]
        public async Task GenerateWeeklySchedule_DeptNotFound_ReturnsNotFound()
        {
            SetUser("Manager", "m1");
            _deptRepo.Setup(d => d.GetByIdAsync("d1", _ct)).ReturnsAsync((Department?)null);
            var monday = new DateTime(2025, 6, 9);
            var res = await _ctrl.GenerateWeeklySchedule("d1", monday, _ct);
            var nf = Assert.IsType<NotFoundObjectResult>(res);
            Assert.Equal("Department not found", nf.Value);
        }

        [Fact]
        public async Task GenerateWeeklySchedule_NotManagerOrAdmin_ReturnsForbid()
        {
            SetUser("Manager", "other");
            var dept = new Department("d1");
            dept.AssignManager("mgr1");
            _deptRepo.Setup(d => d.GetByIdAsync("d1", _ct)).ReturnsAsync(dept);
            var monday = new DateTime(2025, 6, 9);
            var res = await _ctrl.GenerateWeeklySchedule("d1", monday, _ct);
            var forbid = Assert.IsType<ForbidResult>(res);
        }

        [Fact]
        public async Task GenerateWeeklySchedule_Manager_Succeeds_ReturnsOk()
        {
            SetUser("Manager", "mgr1");
            var dept = new Department("d1");
            dept.AssignManager("mgr1");
            _deptRepo.Setup(d => d.GetByIdAsync("d1", _ct)).ReturnsAsync(dept);
            var monday = new DateTime(2025, 6, 9);

            var res = await _ctrl.GenerateWeeklySchedule("d1", monday, _ct);
            var ok = Assert.IsType<OkObjectResult>(res);

            _svc.Verify(s =>
                s.GenerateWeeklyScheduleAsync("d1", DateOnly.FromDateTime(monday), _ct),
                Times.Once);

            var json = JsonSerializer.Serialize(ok.Value);
            Assert.Contains("Generated schedule for week starting on", json);
        }

        [Fact]
        public async Task AI_GenerateWeeklySchedule_InvalidJson_ReturnsBadRequest()
        {
            SetUser("Admin", "any");
            var dept = new Department("d1");
            dept.AssignManager("mgr1");
            _deptRepo.Setup(d => d.GetByIdAsync("d1", _ct)).ReturnsAsync(dept);
            var monday = new DateTime(2025, 6, 9);
            _genSvc.Setup(g => g.GetScheduleSuggestionJsonAsync("d1", DateOnly.FromDateTime(monday), _ct))
                   .ReturnsAsync("not-a-json");

            var res = await _ctrl.AI_GenerateWeeklySchedule("d1", monday, _ct);
            var bad = Assert.IsType<BadRequestObjectResult>(res);
            Assert.StartsWith("AI did not return valid JSON.", bad.Value.ToString());
        }

        [Fact]
        public async Task AI_GenerateWeeklySchedule_EmptyList_ReturnsOkMessage()
        {
            SetUser("Manager", "mgr1");
            var dept = new Department("d1");
            dept.AssignManager("mgr1");
            _deptRepo.Setup(d => d.GetByIdAsync("d1", _ct)).ReturnsAsync(dept);
            var monday = new DateTime(2025, 6, 9);

            _genSvc.Setup(g =>
                g.GetScheduleSuggestionJsonAsync("d1", DateOnly.FromDateTime(monday), _ct))
                   .ReturnsAsync("[]");

            var res = await _ctrl.AI_GenerateWeeklySchedule("d1", monday, _ct);
            var ok = Assert.IsType<OkObjectResult>(res);

            var text = ok.Value as string;
            Assert.Equal("AI returned no shifts to schedule.", text);
        }

        [Fact]
        public async Task AI_GenerateWeeklySchedule_Valid_CallsSave_ReturnsOk()
        {
            SetUser("Admin", "u");
            var dept = new Department("d1");
            dept.AssignManager("mgr");
            _deptRepo.Setup(d => d.GetByIdAsync("d1", _ct)).ReturnsAsync(dept);
            var monday = new DateTime(2025, 6, 9);
            var dtoList = new List<ShiftFromAiDto>
            {
                new ShiftFromAiDto { UserId="u1", Date=DateOnly.Parse("2025-06-09"), Shift="Shift1", StartTime=TimeOnly.Parse("08:00"), EndTime=TimeOnly.Parse("16:00") }
            };
            var json = JsonSerializer.Serialize(dtoList);
            _genSvc.Setup(g => g.GetScheduleSuggestionJsonAsync("d1", DateOnly.FromDateTime(monday), _ct))
                   .ReturnsAsync(json);

            var res = await _ctrl.AI_GenerateWeeklySchedule("d1", monday, _ct);
            var ok = Assert.IsType<OkObjectResult>(res);
            Assert.Equal("Schedule generated successfully.", ok.Value);
            _svc.Verify(s => s.SaveGeneratedShiftsAsync("d1", DateOnly.FromDateTime(monday), dtoList, _ct), Times.Once);
        }

        [Fact]
        public async Task List_ReturnsMappedDtos()
        {
            var domain = new List<ShiftAssignment> { new ShiftAssignment("u1", DateOnly.Parse("2025-06-09"), ShiftType.Shift1, TimeOnly.Parse("08:00"), TimeOnly.Parse("16:00"), "d", "mgr") };
            var dtos = new List<ShiftFromAiDto> { new ShiftFromAiDto { UserId = "u1", Date = DateOnly.Parse("2025-06-09"), Shift = "Shift1", StartTime = TimeOnly.Parse("08:00"), EndTime = TimeOnly.Parse("16:00") } };
            _svc.Setup(s => s.GetAll(_ct)).ReturnsAsync(domain);
            _mapper.Setup(m => m.Map<IEnumerable<ShiftFromAiDto>>(domain)).Returns(dtos);

            var action = await _ctrl.List(_ct);
            var ok = Assert.IsType<OkObjectResult>(action.Result);
            Assert.Equal(dtos, ok.Value);
        }

        [Fact]
        public async Task GetUserSchedule_Unauthenticated_ReturnsUnauthorized()
        {
            _ctrl.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
            var res = await _ctrl.GetUserSchedule("u", new DateTime(2025, 6, 9), _ct);
            Assert.IsType<UnauthorizedResult>(res);
        }

        [Fact]
        public async Task GetUserSchedule_NonMonday_ReturnsBadRequest()
        {
            SetUser("Admin", "u");
            var res = await _ctrl.GetUserSchedule("u", new DateTime(2025, 6, 10), _ct);
            var bad = Assert.IsType<BadRequestObjectResult>(res);
            Assert.Equal("WeekStart must be Monday", bad.Value);
        }

        [Fact]
        public async Task GetUserSchedule_Admin_CallsService()
        {
            SetUser("Admin", "x");
            var monday = new DateTime(2025, 6, 9);
            var list = new List<ShiftAssignment>();
            _svc.Setup(s => s.GetUserScheduleAsync("u", DateOnly.FromDateTime(monday), _ct))
                .ReturnsAsync(list);

            var res = await _ctrl.GetUserSchedule("u", monday, _ct);
            var ok = Assert.IsType<OkObjectResult>(res);
            Assert.Same(list, ok.Value);
        }
    }
}
 
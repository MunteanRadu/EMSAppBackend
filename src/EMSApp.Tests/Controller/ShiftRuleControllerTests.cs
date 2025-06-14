using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using EMSApp.Api;
using EMSApp.Domain;
using EMSApp.Domain.Entities;
using EMSApp.Application;
using Moq;
using Xunit;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EMSApp.Tests.Controller
{
    [Trait("Category", "Controller")]
    public class ShiftRuleControllerTests
    {
        private readonly Mock<IShiftRuleService> _ruleSvc = new();
        private readonly Mock<IDepartmentRepository> _deptRepo = new();
        private readonly ShiftRuleController _ctrl;
        private readonly CancellationToken _ct = CancellationToken.None;

        public ShiftRuleControllerTests()
        {
            _ctrl = new ShiftRuleController(_ruleSvc.Object, _deptRepo.Object);
        }

        private void SetUser(string role, string userId = "mgr")
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.Role, role),
                new Claim(ClaimTypes.NameIdentifier, userId)
            };
            var identity = new ClaimsIdentity(claims, "test");
            _ctrl.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
            };
        }

        [Fact]
        public async Task GetRule_DepartmentNotFound_ReturnsNotFound()
        {
            _deptRepo.Setup(d => d.GetByIdAsync("d1", _ct))
                     .ReturnsAsync((Department?)null);

            var res = await _ctrl.GetRule("d1", _ct);
            var notFound = Assert.IsType<NotFoundObjectResult>(res);
            Assert.Equal("Department not found", notFound.Value);
        }

        [Fact]
        public async Task GetRule_NoRuleDefined_ReturnsNotFound()
        {
            var dept = new Department("Sales");
            dept.AssignManager("mgr");
            _deptRepo.Setup(d => d.GetByIdAsync("d1", _ct))
                     .ReturnsAsync(dept);
            _ruleSvc.Setup(r => r.GetRuleByDepartmentAsync("d1", _ct))
                    .ReturnsAsync((ShiftRule?)null);

            var res = await _ctrl.GetRule("d1", _ct);
            var notFound = Assert.IsType<NotFoundObjectResult>(res);
            Assert.Equal("No rules are defined for this department", notFound.Value);
        }

        [Fact]
        public async Task GetRule_Found_ReturnsOk()
        {
            var dept = new Department("Ops");
            dept.AssignManager("mgr");
            var rule = new ShiftRule("d1", 1, 2, 3, 2, 12.0);
            _deptRepo.Setup(d => d.GetByIdAsync("d1", _ct))
                     .ReturnsAsync(dept);
            _ruleSvc.Setup(r => r.GetRuleByDepartmentAsync("d1", _ct))
                    .ReturnsAsync(rule);

            SetUser("manager", "mgr");

            var res = await _ctrl.GetRule("d1", _ct);
            var ok = Assert.IsType<OkObjectResult>(res);
            Assert.Same(rule, ok.Value);
        }

        [Fact]
        public async Task UpsertRule_DepartmentNotFound_ReturnsNotFound()
        {
            _deptRepo.Setup(d => d.GetByIdAsync("d2", _ct)).ReturnsAsync((Department?)null);
            var dto = new ShiftRuleDto { MinShift1 = 1, MinShift2 = 1, MinNightShift = 1, MaxConsecutiveNightShifts = 1, MinRestHoursBetweenShifts = 8 };

            var res = await _ctrl.UpsertRule("d2", dto, _ct);
            var notFound = Assert.IsType<NotFoundObjectResult>(res);
            Assert.Equal("Department not found", notFound.Value);
        }

        [Fact]
        public async Task UpsertRule_UserNotManagerOrAdmin_ReturnsForbid()
        {
            var dept = new Department("HR");
            dept.AssignManager("mgrA");
            _deptRepo.Setup(d => d.GetByIdAsync("HR", _ct)).ReturnsAsync(dept);
            SetUser("manager", "someoneElse");
            var dto = new ShiftRuleDto { MinShift1 = 1, MinShift2 = 1, MinNightShift = 1, MaxConsecutiveNightShifts = 1, MinRestHoursBetweenShifts = 8 };

            var res = await _ctrl.UpsertRule("HR", dto, _ct);
            Assert.IsType<ForbidResult>(res);
        }

        [Fact]
        public async Task UpsertRule_ManagerOrAdmin_Succeeds_ReturnsOk()
        {
            var dept = new Department("IT");
            dept.AssignManager("mgrX");
            var dto = new ShiftRuleDto { MinShift1 = 2, MinShift2 = 3, MinNightShift = 4, MaxConsecutiveNightShifts = 2, MinRestHoursBetweenShifts = 10 };
            var returnedRule = new ShiftRule("IT", dto.MinShift1, dto.MinShift2, dto.MinNightShift, dto.MaxConsecutiveNightShifts, dto.MinRestHoursBetweenShifts);

            _deptRepo.Setup(d => d.GetByIdAsync("IT", _ct)).ReturnsAsync(dept);
            _ruleSvc.Setup(r => r.CreateOrUpdateRuleAsync("IT", dto.MinShift1, dto.MinShift2, dto.MinNightShift, dto.MaxConsecutiveNightShifts, dto.MinRestHoursBetweenShifts, _ct))
                    .ReturnsAsync(returnedRule);

            // as manager
            SetUser("manager", "mgrX");
            var mgrRes = await _ctrl.UpsertRule("IT", dto, _ct);
            var ok1 = Assert.IsType<OkObjectResult>(mgrRes);
            Assert.Same(returnedRule, ok1.Value);

            // as admin
            SetUser("Admin", "other");
            var admRes = await _ctrl.UpsertRule("IT", dto, _ct);
            var ok2 = Assert.IsType<OkObjectResult>(admRes);
            Assert.Same(returnedRule, ok2.Value);
        }

        [Fact]
        public async Task DeleteRule_DepartmentNotFound_ReturnsNotFound()
        {
            _deptRepo.Setup(d => d.GetByIdAsync("X", _ct)).ReturnsAsync((Department?)null);

            var res = await _ctrl.DeleteRule("X", _ct);
            var notFound = Assert.IsType<NotFoundObjectResult>(res);
            Assert.Equal("Department not found", notFound.Value);
        }

        [Fact]
        public async Task DeleteRule_UserNotManagerOrAdmin_ReturnsForbid()
        {
            var dept = new Department("D");
            dept.AssignManager("mgrD");
            _deptRepo.Setup(d => d.GetByIdAsync("D", _ct)).ReturnsAsync(dept);
            SetUser("manager", "otherUser");

            var res = await _ctrl.DeleteRule("D", _ct);
            Assert.IsType<ForbidResult>(res);
        }

        [Fact]
        public async Task DeleteRule_ManagerOrAdmin_Succeeds_NoContent()
        {
            var dept = new Department("D2");
            dept.AssignManager("mgr2");
            _deptRepo.Setup(d => d.GetByIdAsync("D2", _ct)).ReturnsAsync(dept);
            SetUser("manager", "mgr2");

            var res = await _ctrl.DeleteRule("D2", _ct);
            Assert.IsType<NoContentResult>(res);
            _ruleSvc.Verify(r => r.DeleteRuleByDepartmentAsync("D2", _ct), Times.Once);

            // as admin
            SetUser("Admin", "someone");
            var res2 = await _ctrl.DeleteRule("D2", _ct);
            Assert.IsType<NoContentResult>(res2);
            _ruleSvc.Verify(r => r.DeleteRuleByDepartmentAsync("D2", _ct), Times.Exactly(2));
        }
    }
}

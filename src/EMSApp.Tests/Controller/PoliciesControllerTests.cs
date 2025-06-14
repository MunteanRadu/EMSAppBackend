using AutoMapper;
using EMSApp.Api;
using EMSApp.Api.Controllers;
using EMSApp.Application;
using EMSApp.Domain;
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
    public class PoliciesControllerTests
    {
        private readonly Mock<IPolicyService> _svc = new();
        private readonly Mock<IMapper> _mapper = new();
        private readonly PoliciesController _ctrl;
        private readonly CancellationToken _ct = CancellationToken.None;

        public PoliciesControllerTests()
        {
            _ctrl = new PoliciesController(_svc.Object, _mapper.Object);
        }

        private static IDictionary<LeaveType, int> SampleQuotas() =>
            new Dictionary<LeaveType, int>
            {
                [LeaveType.Annual] = 20,
                [LeaveType.Sick] = 10,
                [LeaveType.Paid] = 15,
                [LeaveType.Unpaid] = 5,
                [LeaveType.Parental] = 8,
                [LeaveType.Compassionate] = 2,
                [LeaveType.TOIL] = 0,
                [LeaveType.Academic] = 3,
                [LeaveType.Misc] = 1,
            };

        [Fact]
        public async Task Create_ReturnsCreatedDto()
        {
            // Arrange
            var req = new CreatePolicyRequest(
                Year: 2025,
                WorkDayStart: TimeOnly.Parse("08:00"),
                WorkDayEnd: TimeOnly.Parse("17:00"),
                PunchInTolerance: TimeSpan.FromMinutes(15),
                PunchOutTolerance: TimeSpan.FromMinutes(10),
                MaxSingleBreak: TimeSpan.FromMinutes(30),
                MaxTotalBreakPerDay: TimeSpan.FromHours(2),
                OvertimeMultiplier: 1.5m,
                LeaveQuotas: SampleQuotas()
            );
            var policy = new Policy(
                req.Year, req.WorkDayStart, req.WorkDayEnd,
                req.PunchInTolerance, req.PunchOutTolerance,
                req.MaxSingleBreak, req.MaxTotalBreakPerDay,
                req.OvertimeMultiplier, req.LeaveQuotas
            );
            var dto = new PolicyDto(
                policy.Year,
                policy.WorkDayStart,
                policy.WorkDayEnd,
                policy.PunchInTolerance,
                policy.PunchOutTolerance,
                policy.MaxSingleBreak,
                policy.MaxTotalBreakPerDay,
                policy.OvertimeMultiplier,
                policy.LeaveQuotas
            );

            _svc
                .Setup(s => s.CreateAsync(
                    req.Year,
                    req.WorkDayStart, req.WorkDayEnd,
                    req.PunchInTolerance, req.PunchOutTolerance,
                    req.MaxSingleBreak, req.MaxTotalBreakPerDay,
                    req.OvertimeMultiplier, req.LeaveQuotas, _ct))
                .ReturnsAsync(policy);

            _mapper
                .Setup(m => m.Map<PolicyDto>(policy))
                .Returns(dto);

            // Act
            var actionResult = await _ctrl.Create(req, _ct);

            // Assert
            var created = Assert.IsType<CreatedAtActionResult>(actionResult.Result);
            Assert.Equal(nameof(_ctrl.GetByYear), created.ActionName);
            Assert.Equal(dto, created.Value);

            _svc.Verify(s => s.CreateAsync(
                req.Year,
                req.WorkDayStart, req.WorkDayEnd,
                req.PunchInTolerance, req.PunchOutTolerance,
                req.MaxSingleBreak, req.MaxTotalBreakPerDay,
                req.OvertimeMultiplier, req.LeaveQuotas, _ct),
                Times.Once);
        }

        [Fact]
        public async Task List_ReturnsAllDtos()
        {
            // Arrange
            var policies = new List<Policy>
            {
                new Policy(
                    2024,
                    TimeOnly.Parse("09:00"), TimeOnly.Parse("18:00"),
                    TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(10),
                    TimeSpan.FromMinutes(30), TimeSpan.FromHours(1),
                    1m, SampleQuotas())
            };
            var dtos = new List<PolicyDto>
            {
                new PolicyDto(
                    policies[0].Year,
                    policies[0].WorkDayStart,
                    policies[0].WorkDayEnd,
                    policies[0].PunchInTolerance,
                    policies[0].PunchOutTolerance,
                    policies[0].MaxSingleBreak,
                    policies[0].MaxTotalBreakPerDay,
                    policies[0].OvertimeMultiplier,
                    policies[0].LeaveQuotas
                )
            };

            _svc.Setup(s => s.GetAllAsync(_ct)).ReturnsAsync(policies);
            _mapper.Setup(m => m.Map<IEnumerable<PolicyDto>>(policies)).Returns(dtos);

            // Act
            var action = await _ctrl.List(_ct);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(action.Result);
            Assert.Equal(dtos, ok.Value);
            _svc.Verify(s => s.GetAllAsync(_ct), Times.Once);
        }

        [Fact]
        public async Task GetByYear_Found_ReturnsDto()
        {
            // Arrange
            var p = new Policy(
                2023,
                TimeOnly.Parse("08:00"), TimeOnly.Parse("17:00"),
                TimeSpan.FromMinutes(15), TimeSpan.FromMinutes(15),
                TimeSpan.FromMinutes(45), TimeSpan.FromHours(2),
                1.2m, SampleQuotas()
            );
            var dto = new PolicyDto(
                p.Year, p.WorkDayStart, p.WorkDayEnd,
                p.PunchInTolerance, p.PunchOutTolerance,
                p.MaxSingleBreak, p.MaxTotalBreakPerDay,
                p.OvertimeMultiplier, p.LeaveQuotas
            );

            _svc.Setup(s => s.GetByYearAsync(p.Year, _ct)).ReturnsAsync(p);
            _mapper.Setup(m => m.Map<PolicyDto>(p)).Returns(dto);

            // Act
            var action = await _ctrl.GetByYear(p.Year, _ct);

            // Assert
            Assert.Equal(dto, action.Value);
            _svc.Verify(s => s.GetByYearAsync(p.Year, _ct), Times.Once);
        }

        [Fact]
        public async Task GetByYear_NotFound_Returns404()
        {
            // Arrange
            _svc.Setup(s => s.GetByYearAsync(1999, _ct)).ReturnsAsync((Policy?)null);

            // Act
            var action = await _ctrl.GetByYear(1999, _ct);

            // Assert
            Assert.IsType<NotFoundResult>(action.Result);
            _svc.Verify(s => s.GetByYearAsync(1999, _ct), Times.Once);
        }

        [Fact]
        public async Task UpdateLeaveQuotas_Existing_CallsService()
        {
            // Arrange
            var year = 2025;
            var p = new Policy(
                year,
                TimeOnly.Parse("08:00"), TimeOnly.Parse("17:00"),
                TimeSpan.FromMinutes(15), TimeSpan.FromMinutes(15),
                TimeSpan.FromMinutes(30), TimeSpan.FromHours(2),
                1m, SampleQuotas()
            );
            var newQuotas = new Dictionary<LeaveType, int>(SampleQuotas()) { [LeaveType.Annual] = 25 };
            var req = new UpdateLeaveQuotasRequest(newQuotas);

            _svc.Setup(s => s.GetByYearAsync(year, _ct)).ReturnsAsync(p);

            // Act
            var result = await _ctrl.UpdateLeaveQuotas(year, req, _ct);

            // Assert
            Assert.IsType<NoContentResult>(result);
            Assert.Equal(25, p.GetLeaveQuota(LeaveType.Annual));
            _svc.Verify(s => s.UpdateAsync(p, _ct), Times.Once);
        }

        [Fact]
        public async Task UpdateLeaveQuotas_NotFound_Returns404()
        {
            // Arrange
            _svc.Setup(s => s.GetByYearAsync(2000, _ct)).ReturnsAsync((Policy?)null);

            // Act
            var result = await _ctrl.UpdateLeaveQuotas(2000, new UpdateLeaveQuotasRequest(null), _ct);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task UpdatePut_Existing_CallsService()
        {
            // Arrange
            var year = 2026;
            var initial = new Policy(
                year,
                TimeOnly.Parse("09:00"), TimeOnly.Parse("18:00"),
                TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(10),
                TimeSpan.FromMinutes(20), TimeSpan.FromHours(1),
                1m, SampleQuotas()
            );
            var req = new UpdatePolicyRequest
            {
                WorkDayStart = TimeOnly.Parse("08:30"),
                PunchInTolerance = TimeSpan.FromMinutes(5),
                MaxSingleBreak = TimeSpan.FromMinutes(25),
                OvertimeMultiplier = 2m
            };
            _svc.Setup(s => s.GetByYearAsync(year, _ct)).ReturnsAsync(initial);

            // Act
            var result = await _ctrl.Update(year, req, _ct);

            // Assert
            Assert.IsType<NoContentResult>(result);
            Assert.Equal(TimeOnly.Parse("08:30"), initial.WorkDayStart);
            Assert.Equal(TimeSpan.FromMinutes(5), initial.PunchInTolerance);
            Assert.Equal(TimeSpan.FromMinutes(25), initial.MaxSingleBreak);
            Assert.Equal(2m, initial.OvertimeMultiplier);
            _svc.Verify(s => s.UpdateAsync(initial, _ct), Times.Once);
        }

        [Fact]
        public async Task UpdatePut_NotFound_Returns404()
        {
            // Arrange
            _svc.Setup(s => s.GetByYearAsync(2100, _ct)).ReturnsAsync((Policy?)null);

            // Act
            var result = await _ctrl.Update(2100, new UpdatePolicyRequest(), _ct);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Delete_CallsService()
        {
            // Act
            var result = await _ctrl.Delete(2030, _ct);

            // Assert
            Assert.IsType<NoContentResult>(result);
            _svc.Verify(s => s.DeleteAsync(2030, _ct), Times.Once);
        }
    }
}
